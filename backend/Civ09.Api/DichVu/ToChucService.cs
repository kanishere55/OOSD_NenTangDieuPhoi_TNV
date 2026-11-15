using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record DangKyToChucNhap(string TenToChuc, string? MoTa, string? KhuVucHoatDong, string? SoDienThoai);
public record ThanhVienNhap(string TenDangNhap, string VaiTro);

// Đăng ký tổ chức, quản lý thành viên, xem nhật ký truy cập và kỹ năng đang thiếu của tổ chức (UC20, UC22, UC24, UC25, BR26, BR28).
public class ToChucService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public ToChucService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // BR28: người dùng đã đăng nhập gửi đăng ký tổ chức, chờ quản trị viên duyệt.
    public async Task<string> DangKyToChuc(NguoiDangNhap nd, DangKyToChucNhap dl)
    {
        var tc = ToChucCongDong.DangKy(nd.MaNguoiDung, dl.TenToChuc, dl.MoTa, dl.KhuVucHoatDong, dl.SoDienThoai, BayGio);
        if (await _kho.ToChucCongDong.AnyAsync(t => t.MaNguoiDangKy == nd.MaNguoiDung && t.TrangThai == ToChucCongDong.CHO_DUYET))
            throw LoiNghiepVu.XungDot("Bạn đang có một đăng ký tổ chức chờ duyệt. Hãy chờ kết quả trước khi gửi đăng ký khác.");
        if (await _kho.ToChucCongDong.AnyAsync(t => t.TenToChuc == tc.TenToChuc
                                                    && (t.TrangThai == ToChucCongDong.CHO_DUYET || t.TrangThai == ToChucCongDong.HOAT_DONG)))
            throw LoiNghiepVu.XungDot("Đã có tổ chức cùng tên trên nền tảng hoặc đang chờ duyệt.");
        var quanTri = await _thongBao.QuanTriVien();
        return await _giaoDich.ThucHienAsync(() =>
        {
            _kho.ToChucCongDong.Add(tc);
            _thongBao.Gui(quanTri, BoThongBao.DANG_KY_TO_CHUC, $"{nd.TenHienThi} đăng ký tổ chức \"{tc.TenToChuc}\".",
                "/quan-tri/to-chuc", tc.MaToChuc);
            return Task.FromResult(tc.MaToChuc);
        });
    }

    public async Task<List<object>> XemDangKyCuaToi(NguoiDangNhap nd)
        => (await _kho.ToChucCongDong.AsNoTracking().Where(t => t.MaNguoiDangKy == nd.MaNguoiDung)
                .OrderByDescending(t => t.NgayDangKy)
                .Select(t => new { t.MaToChuc, t.TenToChuc, t.TrangThai, t.NgayDangKy, t.LyDoTuChoi, t.KhuVucHoatDong })
                .ToListAsync()).Cast<object>().ToList();

    // Thành viên của tổ chức: đại diện của tổ chức và quản trị viên xem được.
    public async Task<object> XemThanhVien(NguoiDangNhap nd, string maToChuc)
    {
        var tc = await LayToChuc(maToChuc);
        YeuCauQuanLy(nd, maToChuc);
        var ds = await _kho.ThanhVienToChuc.AsNoTracking().Where(t => t.MaToChuc == maToChuc)
            .OrderBy(t => t.VaiTro).ThenBy(t => t.NguoiDung!.TenHienThi)
            .Select(t => new
            {
                t.MaNguoiDung, t.NguoiDung!.TenHienThi, t.NguoiDung.TenDangNhap, t.VaiTro, t.DangHoatDong,
                TaiKhoanHoatDong = t.NguoiDung.DangHoatDong
            }).ToListAsync();
        return new { tc.MaToChuc, tc.TenToChuc, tc.TrangThai, ThanhVien = ds, LaQuanTri = nd.LaQuanTri };
    }

    // BR28: thêm thành viên bằng tên đăng nhập hoặc email của người đã có tài khoản. Đại diện chỉ thêm
    // điều phối viên; quản trị viên thêm được cả đại diện.
    public async Task ThemThanhVien(NguoiDangNhap nd, string maToChuc, ThanhVienNhap dl)
    {
        var tc = await LayToChuc(maToChuc);
        YeuCauQuanLy(nd, maToChuc);
        if (tc.TrangThai != ToChucCongDong.HOAT_DONG) throw LoiNghiepVu.XungDot("Tổ chức không còn hoạt động.");
        if (dl.VaiTro is not (CacVaiTro.DieuPhoi or CacVaiTro.DaiDien)) throw LoiNghiepVu.KhongHopLe("Vai trò chưa đúng.");
        if (dl.VaiTro == CacVaiTro.DaiDien && !nd.LaQuanTri)
            throw LoiNghiepVu.KhongDuQuyen("Chỉ quản trị viên mới thêm được đại diện cho tổ chức.");
        if (string.IsNullOrWhiteSpace(dl.TenDangNhap)) throw LoiNghiepVu.KhongHopLe("Hãy nhập tên đăng nhập hoặc email.");
        await _giaoDich.ThucHienAsync(async () =>
        {
            var nguoi = await NguoiDung.TimTheoTenDangNhap(_kho, dl.TenDangNhap)
                        ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy tài khoản này. Người đó cần đăng nhập vào nền tảng ít nhất một lần trước.");
            if (!nguoi.DangHoatDong) throw LoiNghiepVu.XungDot("Tài khoản này đang bị khóa.");
            var tv = await _kho.ThanhVienToChuc.FirstOrDefaultAsync(t => t.MaNguoiDung == nguoi.MaNguoiDung
                                                                       && t.MaToChuc == maToChuc && t.VaiTro == dl.VaiTro);
            if (tv is { DangHoatDong: true }) throw LoiNghiepVu.XungDot($"{nguoi.TenHienThi} đã là thành viên với vai trò này.");
            if (!nguoi.CoVaiTro(dl.VaiTro))
            {
                nguoi.CapVaiTro(dl.VaiTro);
                await _kho.SaveChangesAsync();
            }
            if (tv == null) _kho.ThanhVienToChuc.Add(new ThanhVienToChuc { MaNguoiDung = nguoi.MaNguoiDung, MaToChuc = maToChuc, VaiTro = dl.VaiTro });
            else tv.DangHoatDong = true;
            _thongBao.Gui(nguoi.MaNguoiDung, BoThongBao.CAP_VAI_TRO,
                $"Bạn được thêm làm {(dl.VaiTro == CacVaiTro.DaiDien ? "đại diện" : "điều phối viên")} của {tc.TenToChuc}. Hãy đăng xuất rồi đăng nhập lại nếu chưa thấy mục mới.",
                "/", maToChuc);
        });
    }

    // Ngừng tư cách thành viên: người đó mất quyền ở tổ chức, lịch sử vẫn giữ.
    public async Task NgungThanhVien(NguoiDangNhap nd, string maToChuc, string maNguoiDung, string vaiTro)
    {
        await LayToChuc(maToChuc);
        YeuCauQuanLy(nd, maToChuc);
        if (vaiTro == CacVaiTro.DaiDien && !nd.LaQuanTri)
            throw LoiNghiepVu.KhongDuQuyen("Chỉ quản trị viên mới ngừng được tư cách đại diện.");
        await _giaoDich.ThucHienAsync(async () =>
        {
            var tv = await _kho.ThanhVienToChuc.FirstOrDefaultAsync(t => t.MaNguoiDung == maNguoiDung && t.MaToChuc == maToChuc && t.VaiTro == vaiTro)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy thành viên.");
            if (!tv.DangHoatDong) throw LoiNghiepVu.XungDot("Thành viên này đã ngừng.");
            if (vaiTro == CacVaiTro.DaiDien && maNguoiDung == nd.MaNguoiDung)
                throw LoiNghiepVu.XungDot("Bạn không tự ngừng tư cách đại diện của mình.");
            tv.DangHoatDong = false;
        });
    }

    // BR26: nhật ký xem dữ liệu nhạy cảm của tổ chức, cho đại diện của tổ chức và quản trị viên.
    public async Task<object> XemNhatKy(NguoiDangNhap nd, string maToChuc, int trang)
    {
        await LayToChuc(maToChuc);
        YeuCauQuanLy(nd, maToChuc);
        return await NhatKy(_kho.NhatKyTruyCap.AsNoTracking().Where(n => n.MaToChuc == maToChuc), trang);
    }

    internal async Task<object> NhatKy(IQueryable<NhatKyTruyCap> q, int trang)
    {
        trang = Math.Max(1, trang);
        const int kichThuoc = 30;
        var tong = await q.CountAsync();
        var ds = await q.OrderByDescending(n => n.ThoiDiem).ThenByDescending(n => n.MaNhatKy)
            .Skip((trang - 1) * kichThuoc).Take(kichThuoc)
            .Select(n => new { n.MaNhatKy, n.ThoiDiem, n.LoaiDuLieu, n.MaDoiTuong, n.DuocPhep, TenNguoiXem = n.NguoiXem!.TenHienThi, n.NguoiXem.TenDangNhap })
            .ToListAsync();
        // Tên dễ hiểu của đối tượng: tiêu đề nhu cầu (hướng dẫn riêng), người khai và ca (ảnh minh chứng),
        // kỹ năng và người có chứng chỉ (chứng chỉ).
        var ma = ds.Select(n => n.MaDoiTuong).Distinct().ToList();
        var ten = new Dictionary<string, string>();
        foreach (var x in await _kho.NhuCauCongDong.AsNoTracking().Where(x => ma.Contains(x.MaNhuCau))
                     .Select(x => new { x.MaNhuCau, x.TieuDe }).ToListAsync())
            ten[x.MaNhuCau] = x.TieuDe;
        foreach (var x in await _kho.DongGop.AsNoTracking().Where(x => ma.Contains(x.MaDongGop))
                     .Select(x => new { x.MaDongGop, Nguoi = x.DangKy!.HoSo!.NguoiDung!.TenHienThi, x.DangKy.CaLam!.NhuCau!.TieuDe }).ToListAsync())
            ten[x.MaDongGop] = $"Bản khai của {x.Nguoi} ({x.TieuDe})";
        foreach (var x in await _kho.ChungChiKyNang.AsNoTracking().Where(x => ma.Contains(x.MaChungChi))
                     .Select(x => new { x.MaChungChi, x.KyNang!.TenKyNang, Nguoi = x.HoSo!.NguoiDung!.TenHienThi }).ToListAsync())
            ten[x.MaChungChi] = $"Chứng chỉ {x.TenKyNang} của {x.Nguoi}";
        return new
        {
            DanhSach = ds.Select(n => new
            {
                n.MaNhatKy, n.ThoiDiem, n.LoaiDuLieu, n.DuocPhep, n.TenNguoiXem, n.TenDangNhap,
                TenDoiTuong = ten.GetValueOrDefault(n.MaDoiTuong, "")
            }).ToList(),
            Trang = trang, KichThuoc = kichThuoc, TongSo = tong
        };
    }

    public async Task<object> ThongKeKyNangThieu(NguoiDangNhap nd, string maToChuc)
    {
        if (!nd.LaThanhVienBatKy(maToChuc) && !nd.LaQuanTri) throw LoiNghiepVu.KhongDuQuyen();
        return await _kho.ThongKeKyNangThieuAsync(maToChuc, BayGio);
    }

    private async Task<ToChucCongDong> LayToChuc(string ma)
        => await _kho.ToChucCongDong.AsNoTracking().FirstOrDefaultAsync(t => t.MaToChuc == ma)
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy tổ chức.");

    private static void YeuCauQuanLy(NguoiDangNhap nd, string maToChuc)
    {
        if (!nd.LaQuanTri && !nd.LaThanhVien(maToChuc, CacVaiTro.DaiDien))
            throw LoiNghiepVu.KhongDuQuyen("Chỉ đại diện của tổ chức và quản trị viên được thực hiện.");
    }
}
