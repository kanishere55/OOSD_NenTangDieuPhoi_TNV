using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Civ09.Api.DichVu;

public record QuetMaNhap(string Ma);
public record CoMatNhap(DateTime GioVao, DateTime? GioRa);

// Điểm danh tại ca bằng mã QR và ghi nhận có mặt, vắng mặt (UC18, BR23, BR27).
public class DiemDanhService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public DiemDanhService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thoiGian = thoiGian; _thongBao = thongBao;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Mã QR của ca để điều phối viên đưa cho người tham gia quét. Chỉ hiện trong khung giờ điểm danh.
    public async Task<object> LayMaQr(NguoiDangNhap nd, string maCaLam, string diaChiGoc)
    {
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCaLam);
            var ca = await _kho.CaLam.Include(c => c.NhuCau).FirstOrDefaultAsync(c => c.MaCaLam == maCaLam)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");
            _chinhSach.YeuCauThanhVien(nd, ca.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            if (ca.TrangThai == CaLam.DA_HUY) throw LoiNghiepVu.XungDot("Ca đã bị hủy.");
            var bayGio = BayGio;
            if (bayGio < ca.BatDauDuKien - DiemDanh.SomTruocCa)
                throw LoiNghiepVu.XungDot("Mã điểm danh hiện từ 30 phút trước giờ bắt đầu ca.");
            if (bayGio > ca.KetThucDuKien + DiemDanh.TreSauCa)
                throw LoiNghiepVu.XungDot("Đã qua thời gian điểm danh của ca.");
            ca.KhoaDiemDanh ??= MaDiemDanh.TaoKhoa();
            var (ma, hetHan) = MaDiemDanh.MaHienTai(ca.KhoaDiemDanh, bayGio);
            var noiDung = $"{diaChiGoc.TrimEnd('/')}/diem-danh/{ca.MaCaLam}?ma={ma}";
            using var tao = new QRCodeGenerator();
            using var duLieu = tao.CreateQrCode(noiDung, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(duLieu).GetGraphic(10);
            return (object)new { Ma = ma, NoiDung = noiDung, HetHan = hetHan, AnhQr = "data:image/png;base64," + Convert.ToBase64String(png) };
        });
    }

    // Danh sách người đã phân công của ca kèm tình trạng điểm danh, cho điều phối viên.
    public async Task<object> XemDiemDanhCuaCa(NguoiDangNhap nd, string maCaLam)
    {
        var ca = await CaLam.LayDieuKien(_kho, maCaLam);
        _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
        var bayGio = BayGio;
        var ds = await _kho.DangKyCaLam.AsNoTracking()
            .Where(d => d.MaCaLam == maCaLam && d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
            .OrderBy(d => d.HoSo!.NguoiDung!.TenHienThi)
            .Select(d => new
            {
                d.MaDangKy, d.HoSo!.NguoiDung!.TenHienThi,
                DiemDanh = d.DiemDanh == null ? null : new { d.DiemDanh.KetQua, d.DiemDanh.GioVao, d.DiemDanh.GioRa, d.DiemDanh.CachGhi },
                DaKhai = d.DongGop != null
            }).ToListAsync();
        return new
        {
            ca.MaCaLam, ca.TieuDe, ca.BatDauDuKien, ca.KetThucDuKien, TrangThai = ca.TrangThaiCa,
            TrongGioDiemDanh = ca.TrangThaiCa != CaLam.DA_HUY && bayGio >= ca.BatDauDuKien - DiemDanh.SomTruocCa
                               && bayGio <= ca.KetThucDuKien + DiemDanh.TreSauCa,
            DuocGhiVang = ca.TrangThaiCa != CaLam.DA_HUY && bayGio >= ca.BatDauDuKien,
            DuocGhiCoMat = ca.TrangThaiCa != CaLam.DA_HUY && bayGio >= ca.BatDauDuKien - DiemDanh.SomTruocCa,
            NguoiThamGia = ds
        };
    }

    // Tình nguyện viên quét mã: lần đầu ghi giờ vào, lần sau ghi giờ ra.
    public async Task<object> QuetMa(NguoiDangNhap nd, string maCaLam, string? ma)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            var dk = await _kho.DangKyCaLam.Include(d => d.CaLam!).ThenInclude(c => c.NhuCau)
                         .Include(d => d.DiemDanh)
                         .FirstOrDefaultAsync(d => d.MaCaLam == maCaLam && d.MaHoSo == nd.MaHoSo && d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
                     ?? throw LoiNghiepVu.XungDot("Bạn chưa được phân công vào ca này nên không điểm danh được.");
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", dk.MaDangKy);
            if (!MaDiemDanh.HopLe(dk.CaLam!.KhoaDiemDanh, ma, BayGio))
                throw LoiNghiepVu.KhongHopLe("Mã điểm danh không đúng hoặc đã hết hạn. Mã đổi mỗi 2 phút, hãy quét lại mã đang hiện trên màn hình của điều phối viên.");
            var dd = dk.DiemDanh;
            var laVao = DiemDanh.QuetMa(dk, dk.CaLam, ref dd, BayGio);
            if (laVao) _kho.DiemDanh.Add(dd!);
            return (object)new { Loai = laVao ? "VAO" : "RA", Gio = laVao ? dd!.GioVao : dd!.GioRa, dk.CaLam.NhuCau!.TieuDe };
        });
    }

    public async Task GhiNhanCoMat(NguoiDangNhap nd, string maDangKy, CoMatNhap dl)
        => await GhiNhan(nd, maDangKy, (dk, ca, dd) => DiemDanh.GhiNhanCoMat(dk, ca, dd, nd.MaNguoiDung,
            CaService.ChuanUtc(dl.GioVao), dl.GioRa == null ? null : CaService.ChuanUtc(dl.GioRa.Value), BayGio));

    public async Task GhiNhanVang(NguoiDangNhap nd, string maDangKy)
        => await GhiNhan(nd, maDangKy, (dk, ca, dd) =>
        {
            var kq = DiemDanh.GhiNhanVang(dk, ca, dd, nd.MaNguoiDung, BayGio);
            _thongBao.Gui(dk.HoSo!.MaNguoiDung, BoThongBao.VANG_MAT,
                $"Bạn được ghi nhận vắng mặt ở ca \"{ca.NhuCau!.TieuDe}\". Nếu có nhầm lẫn, hãy liên hệ điều phối viên.",
                "/dang-ky-cua-toi", dk.MaDangKy);
            return kq;
        });

    private async Task GhiNhan(NguoiDangNhap nd, string maDangKy, Func<DangKyCaLam, CaLam, DiemDanh?, DiemDanh> lam)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);
            var dk = await _kho.DangKyCaLam.Include(d => d.CaLam!).ThenInclude(c => c.NhuCau)
                         .Include(d => d.DiemDanh).Include(d => d.DongGop).Include(d => d.HoSo)
                         .FirstOrDefaultAsync(d => d.MaDangKy == maDangKy)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy đăng ký.");
            _chinhSach.YeuCauThanhVien(nd, dk.CaLam!.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            var coSan = dk.DiemDanh;
            var kq = lam(dk, dk.CaLam, coSan);
            if (coSan == null) _kho.DiemDanh.Add(kq);
        });
    }
}
