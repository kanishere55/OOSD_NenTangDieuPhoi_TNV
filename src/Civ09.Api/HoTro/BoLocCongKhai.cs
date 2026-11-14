using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Điều kiện lọc khi tìm ca công khai.
public record BoLocCa(string? MaKyNang, DateTime? TuNgay, DateTime? DenNgay, string? KhuVuc, int Trang = 1, int KichThuoc = 10);

// Một ca trong kết quả tìm kiếm công khai. Chỉ chứa các trường được phép công khai (BR01).
public record CaCongKhaiDto(
    string MaCaLam, string TenToChuc, string TieuDe, string TomTatCongKhai, string KhuVucChung,
    DateTime BatDauDuKien, DateTime KetThucDuKien, int SoNguoiCan, int SoDaPhanCong, List<string> KyNangYeuCau);

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

        var tong = await q.CountAsync();
        var ds = await q.OrderBy(c => c.BatDauDuKien).ThenBy(c => c.MaCaLam)
            .Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();
        return new TrangKetQua<CaCongKhaiDto>(await ChuyenAsync(ds), trang, kichThuoc, tong);
    }

    public async Task<CaCongKhaiDto?> LayMotCaAsync(string maCaLam)
    {
        var c = await _kho.CaCongKhai.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaLam == maCaLam);
        return c == null ? null : (await ChuyenAsync(new List<CaCongKhai> { c }))[0];
    }

    private async Task<List<CaCongKhaiDto>> ChuyenAsync(List<CaCongKhai> ds)
    {
        var ma = ds.Select(c => c.MaCaLam).ToList();
        var kyNang = await _kho.YeuCauKyNang.AsNoTracking()
            .Where(y => ma.Contains(y.MaCaLam))
            .Select(y => new { y.MaCaLam, y.KyNang!.TenKyNang })
            .ToListAsync();
        return ds.Select(c => new CaCongKhaiDto(
            c.MaCaLam, c.TenToChuc, c.TieuDe, c.TomTatCongKhai, c.KhuVucChung, c.BatDauDuKien, c.KetThucDuKien,
            c.SoNguoiCan, c.SoDaPhanCong,
            kyNang.Where(k => k.MaCaLam == c.MaCaLam).Select(k => k.TenKyNang).OrderBy(t => t).ToList())).ToList();
    }
}
