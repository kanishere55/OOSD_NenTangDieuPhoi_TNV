using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

// Xét duyệt đăng ký của một ca: gợi ý xếp hạng, phân công, từ chối, hủy phân công (UC08, BR20, BR25).
public class DieuPhoiService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly DieuKienPhanCong _dieuKien;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public DieuPhoiService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, DieuKienPhanCong dieuKien,
        GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _dieuKien = dieuKien; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Người đăng ký của ca kèm thông tin từ thủ tục sp_GoiYUngVien và điểm phù hợp. Đây chỉ là gợi ý,
    // điều phối viên quyết định ai được phân công (BR04, BR20).
    public async Task<object> GoiYUngVien(NguoiDangNhap nd, string maCaLam)
    {
        var ca = await CaLam.LayDieuKien(_kho, maCaLam);
        _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
        var kyNangCa = await _kho.YeuCauKyNang.AsNoTracking().Where(y => y.MaCaLam == maCaLam)
            .OrderBy(y => y.KyNang!.TenKyNang)
            .Select(y => new { y.MaKyNang, y.KyNang!.TenKyNang, y.CapDoToiThieu, y.CanXacMinh, y.KyNang.CanChungChi }).ToListAsync();
        var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, ca.MaNhuCau);
        var vung = await _kho.NhuCauCongDong.AsNoTracking().Where(n => n.MaNhuCau == ca.MaNhuCau)
            .Select(n => new { n.ViDoVung, n.KinhDoVung }).FirstAsync();
        var viDoCa = rieng?.ViDo ?? vung.ViDoVung;
        var kinhDoCa = rieng?.KinhDo ?? vung.KinhDoVung;
        var dangKy = await _kho.GoiYUngVienAsync(maCaLam);
        var maHoSo = dangKy.Select(d => d.MaHoSo).Distinct().ToList();
        var maKnChungChi = kyNangCa.Where(k => k.CanChungChi).Select(k => k.MaKyNang).ToList();
        var chungChi = await _kho.ChungChiKyNang.AsNoTracking()
            .Where(c => maHoSo.Contains(c.MaHoSo) && maKnChungChi.Contains(c.MaKyNang)
                        && (c.TrangThai == ChungChiKyNang.CHO_XAC_MINH || c.TrangThai == ChungChiKyNang.DA_XAC_MINH))
            .Select(c => new { c.MaChungChi, c.MaHoSo, c.KyNang!.TenKyNang, c.TrangThai }).ToListAsync();

        var ds = dangKy.Select(d =>
        {
            var km = ViTri.KhoangCachKm(d.ViDo, d.KinhDo, viDoCa, kinhDoCa);
            var cham = ChamDiemPhuHop.Cham(new ThongTinGhep(d.DuKyNang, d.SoKyNangCanChungChi, d.SoKyNangDaXacMinh,
                km, d.KhopLichRanh, d.SoLanCoMat, d.SoLanVang));
            return new
            {
                d.MaDangKy, d.TenHienThi, d.TrangThai,
                NgayDangKy = DateTime.SpecifyKind(d.NgayDangKy, DateTimeKind.Utc),
                KyNangDaKhai = d.KyNangDaKhai.Length == 0 ? new List<string>() : d.KyNangDaKhai.Split("; ").ToList(),
                d.DuKyNang, d.ThieuXacMinh,
                TrungLich = d.TrangThai == DangKyCaLam.CHO_DUYET && d.TrungLich,
                KhoangCachKm = km == null ? (double?)null : Math.Round(km.Value, 1),
                d.KhopLichRanh, d.SoLanCoMat, d.SoLanVang,
                cham.Diem, cham.LyDo, cham.LuuY,
                ChungChi = chungChi.Where(c => c.MaHoSo == d.MaHoSo).Select(c => new { c.MaChungChi, c.TenKyNang, c.TrangThai }).ToList()
            };
        }).OrderBy(d => d.TrangThai == DangKyCaLam.CHO_DUYET ? 0 : 1).ThenByDescending(d => d.Diem).ThenBy(d => d.NgayDangKy).ToList();

        return new
        {
            ca.MaCaLam, ca.MaNhuCau, ca.TieuDe, ca.BatDauDuKien, ca.KetThucDuKien, ca.SoNguoiCan, TrangThai = ca.TrangThaiCa,
            KyNangYeuCau = kyNangCa.Select(k => k.TenKyNang).ToList(),
            YeuCau = kyNangCa.Select(k => new { k.MaKyNang, k.TenKyNang, k.CapDoToiThieu, k.CanXacMinh }).ToList(),
            ca.SoDaPhanCong,
            DangKy = ds
        };
    }

    // UC08 (BR09, BR10). Khóa dòng hồ sơ, dòng ca rồi dòng đăng ký; đọc lại điều kiện và lưu trong cùng
    // giao dịch, để hai yêu cầu đến cùng lúc phải lần lượt xử lý. Người được phân công nhận thông báo.
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
            _thongBao.Gui(hs.MaNguoiDung, BoThongBao.PHAN_CONG,
                $"Bạn đã được phân công ca \"{ca.TieuDe}\". Hãy xem hướng dẫn riêng để biết địa điểm.",
                $"/ca/{ca.MaCaLam}/huong-dan-rieng", dk.MaDangKy);
        });
    }

    // BR08, BR11: hủy phân công trước giờ bắt đầu, chỗ được trả lại cho ca.
    public async Task HuyPhanCong(NguoiDangNhap nd, string maDangKy)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);
            var dk = await DangKyCaLam.LayThongTin(_kho, maDangKy);
            var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
            _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
            if (dk.TrangThai != DangKyCaLam.DA_PHAN_CONG)
                throw LoiNghiepVu.XungDot("Chỉ hủy phân công với đăng ký đã được phân công.");
            if (BayGio >= ca.BatDauDuKien)
                throw LoiNghiepVu.XungDot("Đã đến giờ bắt đầu ca nên không hủy phân công được.");
            dk.Huy();
            _thongBao.Gui(await _thongBao.NguoiDungCuaHoSo(dk.MaHoSo), BoThongBao.HUY_PHAN_CONG,
                $"Điều phối viên đã hủy phân công của bạn ở ca \"{ca.TieuDe}\".", "/dang-ky-cua-toi", dk.MaDangKy);
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
            _thongBao.Gui(await _thongBao.NguoiDungCuaHoSo(dk.MaHoSo), BoThongBao.TU_CHOI,
                $"Đăng ký ca \"{ca.TieuDe}\" của bạn chưa được nhận. Bạn có thể tìm ca khác phù hợp hơn.", "/tim-ca", dk.MaDangKy);
        });
    }
}
