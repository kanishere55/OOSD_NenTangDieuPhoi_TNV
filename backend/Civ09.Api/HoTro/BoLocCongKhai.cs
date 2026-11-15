using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Điều kiện lọc khi tìm ca công khai. Có vị trí và bán kính thì chỉ lấy ca có vùng nằm trong bán kính đó.
public record BoLocCa(string? MaKyNang, DateTime? TuNgay, DateTime? DenNgay, string? KhuVuc, int Trang = 1, int KichThuoc = 10,
    decimal? ViDo = null, decimal? KinhDo = null, double? BanKinhKm = null, string? SapXep = null);

public record YeuCauKyNangDto(string MaKyNang, string TenKyNang, int CapDoToiThieu, bool CanXacMinh);

// Một ca trong kết quả tìm kiếm công khai. Chỉ chứa các trường được phép công khai (BR01, BR21):
// vùng gần đúng, không có vị trí thật hay địa chỉ cụ thể.
public record CaCongKhaiDto(
    string MaCaLam, string MaNhuCau, string TenToChuc, string TieuDe, string TomTatCongKhai, string KhuVucChung,
    DateTime BatDauDuKien, DateTime KetThucDuKien, int SoNguoiCan, int SoDaPhanCong, List<string> KyNangYeuCau,
    List<YeuCauKyNangDto> YeuCau, decimal? ViDoVung, decimal? KinhDoVung, int? BanKinhVung, double? KhoangCachKm,
    List<string> Anh);

public record TrangKetQua<T>(List<T> DanhSach, int Trang, int KichThuoc, int TongSo);

// Điểm lọc cho kết quả tìm ca công khai: chỉ đọc view CaCongKhai và chỉ đưa ra các trường được công khai (BR01).
public class BoLocCongKhai
{
    private readonly KhoNghiepVu _kho;

    public BoLocCongKhai(KhoNghiepVu kho) => _kho = kho;

    public async Task<TrangKetQua<CaCongKhaiDto>> TimAsync(BoLocCa loc)
    {
        var kichThuoc = Math.Clamp(loc.KichThuoc, 1, 50);
        var trang = Math.Max(1, loc.Trang);
        var q = CaLam.TimCaCongKhai(_kho, loc);
        var theoViTri = loc.ViDo != null && loc.KinhDo != null;

        if (!theoViTri)
        {
            var tong = await q.CountAsync();
            var ds = await q.OrderBy(c => c.BatDauDuKien).ThenBy(c => c.MaCaLam)
                .Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();
            return new TrangKetQua<CaCongKhaiDto>(await ChuyenAsync(ds), trang, kichThuoc, tong);
        }

        // Có vị trí: số ca đang mở không lớn nên đọc hết rồi tính khoảng cách, lọc và xếp ngay trong bộ nhớ.
        var tatCa = await q.ToListAsync();
        var coKc = tatCa.Select(c => (Ca: c, Km: ViTri.KhoangCachKm(loc.ViDo, loc.KinhDo, c.ViDoVung, c.KinhDoVung)))
            .Where(x => loc.BanKinhKm == null || (x.Km != null && x.Km <= loc.BanKinhKm))
            .ToList();
        var xep = loc.SapXep == "gan-nhat"
            ? coKc.OrderBy(x => x.Km ?? double.MaxValue).ThenBy(x => x.Ca.BatDauDuKien)
            : coKc.OrderBy(x => x.Ca.BatDauDuKien).ThenBy(x => x.Ca.MaCaLam);
        var trangNay = xep.Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToList();
        var dto = await ChuyenAsync(trangNay.Select(x => x.Ca).ToList());
        dto = dto.Select((d, i) => d with { KhoangCachKm = trangNay[i].Km }).ToList();
        return new TrangKetQua<CaCongKhaiDto>(dto, trang, kichThuoc, coKc.Count);
    }

    public async Task<CaCongKhaiDto?> LayMotCaAsync(string maCaLam)
    {
        var c = await _kho.CaCongKhai.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaLam == maCaLam);
        return c == null ? null : (await ChuyenAsync(new List<CaCongKhai> { c }))[0];
    }

    // Các ca công khai còn mở, dùng để gợi ý ca phù hợp cho tình nguyện viên.
    public async Task<List<CaCongKhaiDto>> LayTatCaAsync()
        => await ChuyenAsync(await _kho.CaCongKhai.AsNoTracking().OrderBy(c => c.BatDauDuKien).ToListAsync());

    private async Task<List<CaCongKhaiDto>> ChuyenAsync(List<CaCongKhai> ds)
    {
        var ma = ds.Select(c => c.MaCaLam).ToList();
        var maNc = ds.Select(c => c.MaNhuCau).Distinct().ToList();
        var kyNang = await _kho.YeuCauKyNang.AsNoTracking()
            .Where(y => ma.Contains(y.MaCaLam))
            .Select(y => new { y.MaCaLam, y.MaKyNang, y.KyNang!.TenKyNang, y.CapDoToiThieu, y.CanXacMinh })
            .ToListAsync();
        var anh = await _kho.AnhNhuCau.AsNoTracking()
            .Where(a => maNc.Contains(a.MaNhuCau))
            .OrderBy(a => a.ThuTu).Select(a => new { a.MaNhuCau, a.MaTep }).ToListAsync();
        return ds.Select(c =>
        {
            var yc = kyNang.Where(k => k.MaCaLam == c.MaCaLam).OrderBy(k => k.TenKyNang)
                .Select(k => new YeuCauKyNangDto(k.MaKyNang, k.TenKyNang, k.CapDoToiThieu, k.CanXacMinh)).ToList();
            return new CaCongKhaiDto(
                c.MaCaLam, c.MaNhuCau, c.TenToChuc, c.TieuDe, c.TomTatCongKhai, c.KhuVucChung, c.BatDauDuKien, c.KetThucDuKien,
                c.SoNguoiCan, c.SoDaPhanCong, yc.Select(k => k.TenKyNang).ToList(), yc,
                c.ViDoVung, c.KinhDoVung, c.BanKinhVung, null,
                anh.Where(a => a.MaNhuCau == c.MaNhuCau).Select(a => a.MaTep).ToList());
        }).ToList();
    }
}
