using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record CaNhap(DateTime BatDauDuKien, DateTime KetThucDuKien, int SoNguoiCan, List<string> KyNang);

// Quản lý và tìm ca, tiếp nhận hoặc rút đăng ký (UC04 đến UC07).
public class CaService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly BoLocCongKhai _boLoc;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public CaService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, BoLocCongKhai boLoc, GiaoDich giaoDich,
        TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _boLoc = boLoc; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // BR01: tìm ca không cần đăng nhập, kết quả chỉ có trường công khai.
    public Task<TrangKetQua<CaCongKhaiDto>> TimCa(BoLocCa loc) => _boLoc.TimAsync(loc);

    public async Task<CaCongKhaiDto> XemCaCongKhai(string maCaLam)
        => await _boLoc.LayMotCaAsync(maCaLam)
           ?? throw LoiNghiepVu.KhongTimThay("Ca không còn nhận đăng ký hoặc không tồn tại.");

    // BR04, BR06: ca và kỹ năng yêu cầu lưu trong cùng giao dịch.
    public async Task<string> TaoCa(NguoiDangNhap nd, string maNhuCau, CaNhap dl)
    {
        var kyNang = (dl.KyNang ?? new()).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("NhuCauCongDong", "MaNhuCau", maNhuCau);
            var nc = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
            _chinhSach.YeuCauThanhVien(nd, nc.MaToChuc, CacVaiTro.DieuPhoi);
            if (await _kho.KyNang.CountAsync(k => kyNang.Contains(k.MaKyNang)) != kyNang.Count)
                throw LoiNghiepVu.KhongHopLe("Có kỹ năng không nằm trong danh mục.");
            var ca = CaLam.TaoCa(nc, ChuanUtc(dl.BatDauDuKien), ChuanUtc(dl.KetThucDuKien), dl.SoNguoiCan, kyNang, BayGio);
            _kho.CaLam.Add(ca);
            return ca.MaCaLam;
        });
    }

    // BR11: sửa ca đang mở trước giờ bắt đầu; ca, kỹ năng yêu cầu lưu trong cùng giao dịch.
    public async Task CapNhatCa(NguoiDangNhap nd, string maCaLam, CaNhap dl)
    {
        var kyNang = (dl.KyNang ?? new()).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCaLam);
            var ca = await _kho.CaLam.Include(c => c.NhuCau).Include(c => c.YeuCauKyNang).Include(c => c.DangKy)
                         .AsSplitQuery().FirstOrDefaultAsync(c => c.MaCaLam == maCaLam)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");
            _chinhSach.YeuCauThanhVien(nd, ca.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            if (await _kho.KyNang.CountAsync(k => kyNang.Contains(k.MaKyNang)) != kyNang.Count)
                throw LoiNghiepVu.KhongHopLe("Có kỹ năng không nằm trong danh mục.");
            ca.CapNhat(ChuanUtc(dl.BatDauDuKien), ChuanUtc(dl.KetThucDuKien), dl.SoNguoiCan, kyNang, BayGio);
        });
    }

    public async Task<List<object>> XemCaCuaNhuCau(NguoiDangNhap nd, string maNhuCau)
    {
        var nc = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
        if (!nd.LaThanhVienBatKy(nc.MaToChuc)) throw LoiNghiepVu.KhongDuQuyen();
        var ds = await _kho.CaLam.AsNoTracking().Where(c => c.MaNhuCau == maNhuCau)
            .OrderBy(c => c.BatDauDuKien)
            .Select(c => new
            {
                c.MaCaLam, c.BatDauDuKien, c.KetThucDuKien, c.SoNguoiCan, c.TrangThai,
                KyNangYeuCau = c.YeuCauKyNang.Select(y => y.KyNang!.TenKyNang).ToList(),
                MaKyNangYeuCau = c.YeuCauKyNang.Select(y => y.MaKyNang).ToList(),
                SoChoDuyet = c.DangKy.Count(d => d.TrangThai == DangKyCaLam.CHO_DUYET),
                SoDaPhanCong = c.DangKy.Count(d => d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // BR12: hủy ca trước giờ bắt đầu, các đăng ký còn hiệu lực bị hủy theo, lịch sử vẫn giữ.
    public async Task HuyCa(NguoiDangNhap nd, string maCaLam)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCaLam);
            var ca = await _kho.CaLam.Include(c => c.NhuCau).Include(c => c.DangKy)
                         .FirstOrDefaultAsync(c => c.MaCaLam == maCaLam)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");
            _chinhSach.YeuCauThanhVien(nd, ca.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            ca.Huy(BayGio);
            foreach (var dk in ca.DangKy) dk.Huy();
        });
    }

    // UC06 (BR04, BR07). Khóa dòng hồ sơ rồi dòng ca theo cùng thứ tự với phân công, để việc hủy ca
    // hoặc chuyển mốc giờ không chen vào giữa lúc kiểm tra và lúc lưu đăng ký.
    public async Task<string> DangKy(NguoiDangNhap nd, string maCa)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", nd.MaHoSo!);
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCa);
            var hs = await _kho.HoSoTinhNguyenVien.AsNoTracking().Include(h => h.KyNang)
                .FirstAsync(h => h.MaHoSo == nd.MaHoSo);
            var ca = await CaLam.LayDieuKien(_kho, maCa);
            if (ca.TrangThaiCa != CaLam.MO || BayGio >= ca.BatDauDuKien || ca.TrangThaiNhuCau != NhuCauCongDong.CONG_BO)
                throw LoiNghiepVu.XungDot("Chỉ đăng ký được ca đang mở, chưa bắt đầu và thuộc nhu cầu đã công bố.");
            if (!DieuKienPhanCong.DuKyNang(ca.KyNangYeuCau, hs.LayKyNang()))
                throw LoiNghiepVu.XungDot("Bạn chưa có đủ kỹ năng mà ca yêu cầu.");
            if (await DangKyCaLam.LayThongTin(_kho, hs.MaHoSo, maCa) != null)
                throw LoiNghiepVu.XungDot("Bạn đã có một đăng ký còn hiệu lực cho ca này. Xem ở mục Đăng ký của tôi.");

            var dk = DangKyCaLam.TaoDangKy(hs.MaHoSo, maCa, BayGio);
            _kho.DangKyCaLam.Add(dk);
            return dk.MaDangKy;
        });
    }

    // UC07, BR11: rút đăng ký của mình trước giờ bắt đầu.
    public async Task RutDangKy(NguoiDangNhap nd, string maDangKy)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);
            var dk = await DangKyCaLam.LayThongTin(_kho, maDangKy);
            if (dk.MaHoSo != nd.MaHoSo) throw LoiNghiepVu.KhongDuQuyen("Bạn chỉ được rút đăng ký của chính mình.");
            if (BayGio >= dk.CaLam!.BatDauDuKien)
                throw LoiNghiepVu.XungDot("Đã đến giờ bắt đầu ca nên không thể rút đăng ký.");
            dk.Rut();
        });
    }

    public async Task<List<object>> XemDangKyCuaToi(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var bayGio = BayGio;
        var ds = await _kho.DangKyCaLam.AsNoTracking().Where(d => d.MaHoSo == nd.MaHoSo)
            .OrderByDescending(d => d.CaLam!.BatDauDuKien)
            .Select(d => new
            {
                d.MaDangKy, d.MaCaLam, d.TrangThai, d.NgayDangKy,
                d.CaLam!.BatDauDuKien, d.CaLam.KetThucDuKien, TrangThaiCa = d.CaLam.TrangThai,
                d.CaLam.NhuCau!.TieuDe, d.CaLam.NhuCau.KhuVucChung, TenToChuc = d.CaLam.NhuCau.ToChuc!.TenToChuc,
                DongGop = d.DongGop == null ? null : new { d.DongGop.MaDongGop, d.DongGop.TrangThai, d.DongGop.LyDoTraLai }
            }).ToListAsync();
        return ds.Select(d => (object)new
        {
            d.MaDangKy, d.MaCaLam, d.TrangThai, d.NgayDangKy, d.BatDauDuKien, d.KetThucDuKien, d.TrangThaiCa,
            d.TieuDe, d.KhuVucChung, d.TenToChuc, d.DongGop,
            DuocRut = (d.TrangThai == DangKyCaLam.CHO_DUYET || d.TrangThai == DangKyCaLam.DA_PHAN_CONG) && bayGio < d.BatDauDuKien,
            DuocKhai = d.TrangThai == DangKyCaLam.DA_PHAN_CONG && d.TrangThaiCa != CaLam.DA_HUY && bayGio >= d.KetThucDuKien
                       && (d.DongGop == null || d.DongGop.TrangThai == DongGop.CAN_BO_SUNG),
            DuocXemRieng = d.TrangThai == DangKyCaLam.DA_PHAN_CONG && d.TrangThaiCa != CaLam.DA_HUY && bayGio < d.KetThucDuKien
        }).ToList();
    }

    // Tác vụ nền gọi định kỳ. Việc chuyển trạng thái ca theo giờ do thủ tục sp_ChuyenMocThoiGianCa
    // thực hiện trong một giao dịch (BR12).
    public Task<KetQuaChuyenMoc> ChuyenMocThoiGian() => _kho.ChuyenMocThoiGianCaAsync(BayGio);

    internal static DateTime ChuanUtc(DateTime t) => t.Kind switch
    {
        DateTimeKind.Utc => t,
        DateTimeKind.Local => t.ToUniversalTime(),
        _ => DateTime.SpecifyKind(t, DateTimeKind.Utc)
    };
}
