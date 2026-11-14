using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

// Xét duyệt đăng ký của một ca: gợi ý, phân công, từ chối (UC08).
public class DieuPhoiService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly DieuKienPhanCong _dieuKien;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public DieuPhoiService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, DieuKienPhanCong dieuKien,
        GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _dieuKien = dieuKien; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Người đăng ký của ca kèm nhãn đủ kỹ năng, trùng lịch lấy từ thủ tục sp_GoiYUngVien.
    // Đây chỉ là gợi ý, điều phối viên quyết định ai được phân công (BR04).
    public async Task<object> GoiYUngVien(NguoiDangNhap nd, string maCaLam)
    {
        var ca = await CaLam.LayDieuKien(_kho, maCaLam);
        _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
        var tenKyNangCa = await _kho.KyNang.Where(k => ca.KyNangYeuCau.Contains(k.MaKyNang))
            .OrderBy(k => k.TenKyNang).Select(k => k.TenKyNang).ToListAsync();
        var dangKy = await _kho.GoiYUngVienAsync(maCaLam);

        return new
        {
            ca.MaCaLam, ca.TieuDe, ca.BatDauDuKien, ca.KetThucDuKien, ca.SoNguoiCan, TrangThai = ca.TrangThaiCa,
            KyNangYeuCau = tenKyNangCa,
            ca.SoDaPhanCong,
            DangKy = dangKy.Select(d => new
            {
                d.MaDangKy, d.TenHienThi, d.TrangThai,
                NgayDangKy = DateTime.SpecifyKind(d.NgayDangKy, DateTimeKind.Utc),
                KyNangDaKhai = d.KyNangDaKhai.Length == 0 ? new List<string>() : d.KyNangDaKhai.Split("; ").ToList(),
                d.DuKyNang,
                TrungLich = d.TrangThai == DangKyCaLam.CHO_DUYET && d.TrungLich
            })
        };
    }

    // UC08 (BR09, BR10). Khóa dòng hồ sơ, dòng ca rồi dòng đăng ký; đọc lại điều kiện và lưu trong cùng
    // giao dịch, để hai yêu cầu đến cùng lúc phải lần lượt xử lý.
    public async Task PhanCong(NguoiDangNhap nd, string maDangKy)
    {
        var khoa = await _kho.DangKyCaLam.AsNoTracking().Where(d => d.MaDangKy == maDangKy)
                       .Select(d => new { d.MaHoSo, d.MaCaLam }).FirstOrDefaultAsync()
                   ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy đăng ký.");

        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", khoa.MaHoSo);
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", khoa.MaCaLam);
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);

            var dk = await DangKyCaLam.LayThongTin(_kho, maDangKy);
            var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
            _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);

            if (dk.TrangThai != DangKyCaLam.CHO_DUYET)
                throw LoiNghiepVu.XungDot("Đăng ký này đã được xử lý, trạng thái hiện tại không còn là Chờ duyệt.");
            if (ca.TrangThaiCa != CaLam.MO || BayGio >= ca.BatDauDuKien || ca.TrangThaiNhuCau != NhuCauCongDong.CONG_BO)
                throw LoiNghiepVu.XungDot("Chỉ phân công khi ca còn mở, chưa đến giờ bắt đầu và nhu cầu còn công bố.");

            var hs = await _kho.HoSoTinhNguyenVien.AsNoTracking().Include(h => h.KyNang).Include(h => h.NguoiDung)
                .FirstAsync(h => h.MaHoSo == dk.MaHoSo);
            if (hs.NguoiDung is not { DangHoatDong: true })
                throw LoiNghiepVu.XungDot("Tài khoản của tình nguyện viên đã ngừng hoạt động.");

            await _dieuKien.KiemTra(ca, hs);
            dk.PhanCong(nd.MaNguoiDung, BayGio);
        });
    }

    // BR08: từ chối đăng ký đang chờ duyệt.
    public async Task TuChoi(NguoiDangNhap nd, string maDangKy)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);
            var dk = await DangKyCaLam.LayThongTin(_kho, maDangKy);
            var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
            _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
            dk.TuChoi();
        });
    }
}
