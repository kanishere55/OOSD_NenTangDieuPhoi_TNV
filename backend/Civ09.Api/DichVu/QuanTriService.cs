using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

// Chức năng của quản trị viên nền tảng: duyệt tổ chức, khóa tài khoản, xem nhật ký truy cập (UC21, UC23, UC24, BR26, BR28).
public class QuanTriService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly ToChucService _toChuc;
    private readonly TimeProvider _thoiGian;

    public QuanTriService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao,
        ToChucService toChuc, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _toChuc = toChuc; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Số việc đang chờ quản trị viên, hiện ở trang chủ.
    public async Task<object> TongQuan(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauQuanTri(nd);
        return new
        {
            ToChucChoDuyet = await _kho.ToChucCongDong.CountAsync(t => t.TrangThai == ToChucCongDong.CHO_DUYET),
            DeXuatKyNangChoDuyet = await _kho.DeXuatKyNang.CountAsync(d => d.TrangThai == DeXuatKyNang.CHO_DUYET),
            ChungChiChoXacMinh = await _kho.ChungChiKyNang.CountAsync(c => c.TrangThai == ChungChiKyNang.CHO_XAC_MINH && c.MaHoSo != nd.MaHoSo)
        };
    }

    public async Task<List<object>> XemToChuc(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var ds = await _kho.ToChucCongDong.AsNoTracking()
            .OrderBy(t => t.TrangThai == ToChucCongDong.CHO_DUYET ? 0 : t.TrangThai == ToChucCongDong.HOAT_DONG ? 1 : 2)
            .ThenBy(t => t.TenToChuc)
            .Select(t => new
            {
                t.MaToChuc, t.TenToChuc, t.MoTa, t.KhuVucHoatDong, t.SoDienThoai, t.TrangThai, t.NgayDangKy, t.LyDoTuChoi,
                TenNguoiDangKy = t.NguoiDangKy == null ? null : t.NguoiDangKy.TenHienThi,
                TenDangNhapNguoiDangKy = t.NguoiDangKy == null ? null : t.NguoiDangKy.TenDangNhap,
                SoThanhVien = _kho.ThanhVienToChuc.Count(m => m.MaToChuc == t.MaToChuc && m.DangHoatDong),
                SoNhuCau = _kho.NhuCauCongDong.Count(n => n.MaToChuc == t.MaToChuc)
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // BR28: duyệt thì người đăng ký trở thành đại diện của tổ chức.
    public async Task DuyetToChuc(NguoiDangNhap nd, string maToChuc)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var tc = await LayToChuc(maToChuc);
            tc.Duyet(nd.MaNguoiDung, BayGio);
            var nguoi = await _kho.NguoiDung.Include(x => x.VaiTro).FirstAsync(x => x.MaNguoiDung == tc.MaNguoiDangKy);
            if (!nguoi.CoVaiTro(CacVaiTro.DaiDien))
            {
                nguoi.CapVaiTro(CacVaiTro.DaiDien);
                await _kho.SaveChangesAsync();
            }
            _kho.ThanhVienToChuc.Add(new ThanhVienToChuc { MaNguoiDung = nguoi.MaNguoiDung, MaToChuc = tc.MaToChuc, VaiTro = CacVaiTro.DaiDien });
            _thongBao.Gui(nguoi.MaNguoiDung, BoThongBao.KET_QUA_TO_CHUC,
                $"Tổ chức \"{tc.TenToChuc}\" đã được duyệt. Bạn là đại diện của tổ chức và có thể tạo nhu cầu, thêm điều phối viên.",
                "/", tc.MaToChuc);
        });
    }

    public async Task TuChoiToChuc(NguoiDangNhap nd, string maToChuc, string? lyDo)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var tc = await LayToChuc(maToChuc);
            tc.TuChoi(nd.MaNguoiDung, lyDo, BayGio);
            _thongBao.Gui(tc.MaNguoiDangKy!, BoThongBao.KET_QUA_TO_CHUC,
                $"Đăng ký tổ chức \"{tc.TenToChuc}\" chưa được duyệt. Lý do: {tc.LyDoTuChoi}", "/to-chuc/dang-ky", tc.MaToChuc);
        });
    }

    public async Task NgungToChuc(NguoiDangNhap nd, string maToChuc)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () => (await LayToChuc(maToChuc)).NgungHoatDong());
    }

    public async Task MoLaiToChuc(NguoiDangNhap nd, string maToChuc)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () => (await LayToChuc(maToChuc)).MoLai());
    }

    public async Task<List<object>> XemNguoiDung(NguoiDangNhap nd, string? tuKhoa)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var q = _kho.NguoiDung.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var t = tuKhoa.Trim();
            q = q.Where(x => x.TenHienThi.Contains(t) || (x.TenDangNhap != null && x.TenDangNhap.Contains(t))
                             || (x.Email != null && x.Email.Contains(t)));
        }
        var ds = await q.OrderBy(x => x.TenHienThi).Take(50)
            .Select(x => new
            {
                x.MaNguoiDung, x.TenHienThi, x.TenDangNhap, x.Email, x.DangHoatDong, x.NgayTao,
                VaiTro = x.VaiTro.Select(v => v.VaiTro).ToList(),
                ToChuc = x.ThanhVien.Where(m => m.DangHoatDong).Select(m => m.ToChuc!.TenToChuc + " (" + (m.VaiTro == CacVaiTro.DaiDien ? "đại diện" : "điều phối viên") + ")").ToList()
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    public async Task KhoaTaiKhoan(NguoiDangNhap nd, string maNguoiDung)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () => (await LayNguoiDung(maNguoiDung)).Khoa(nd.MaNguoiDung));
    }

    public async Task MoKhoaTaiKhoan(NguoiDangNhap nd, string maNguoiDung)
    {
        _chinhSach.YeuCauQuanTri(nd);
        await _giaoDich.ThucHienAsync(async () => (await LayNguoiDung(maNguoiDung)).MoKhoa());
    }

    public async Task<object> XemNhatKy(NguoiDangNhap nd, string? loai, int trang)
    {
        _chinhSach.YeuCauQuanTri(nd);
        var q = _kho.NhatKyTruyCap.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(loai)) q = q.Where(n => n.LoaiDuLieu == loai);
        return await _toChuc.NhatKy(q, trang);
    }

    private async Task<ToChucCongDong> LayToChuc(string ma)
    {
        await _giaoDich.KhoaDongAsync("ToChucCongDong", "MaToChuc", ma);
        return await _kho.ToChucCongDong.FirstOrDefaultAsync(t => t.MaToChuc == ma)
               ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy tổ chức.");
    }

    private async Task<NguoiDung> LayNguoiDung(string ma)
        => await _kho.NguoiDung.FirstOrDefaultAsync(x => x.MaNguoiDung == ma)
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy tài khoản.");
}
