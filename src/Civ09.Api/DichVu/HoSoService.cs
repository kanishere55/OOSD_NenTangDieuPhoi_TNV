using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record KyNangDto(string MaKyNang, string TenKyNang, string? MoTaKyNang);

// Xem hồ sơ và cập nhật kỹ năng (UC02, BR03).
public class HoSoService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public HoSoService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    public async Task<object> XemHoSo(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var hs = await _kho.HoSoTinhNguyenVien.AsNoTracking()
            .Include(h => h.KyNang).ThenInclude(k => k.KyNang)
            .FirstAsync(h => h.MaHoSo == nd.MaHoSo);
        return new
        {
            hs.MaHoSo,
            nd.TenHienThi,
            hs.NgayCapNhatKyNang,
            KyNang = hs.KyNang.Select(k => new KyNangDto(k.MaKyNang, k.KyNang!.TenKyNang, k.KyNang.MoTaKyNang))
                .OrderBy(k => k.TenKyNang).ToList()
        };
    }

    public async Task<List<KyNangDto>> XemDanhMucKyNang()
        => await _kho.KyNang.AsNoTracking().OrderBy(k => k.TenKyNang)
            .Select(k => new KyNangDto(k.MaKyNang, k.TenKyNang, k.MoTaKyNang)).ToListAsync();

    // Lưu danh sách kỹ năng mới. Không cho bỏ kỹ năng đang cần cho ca đã được phân công và chưa kết thúc (BR03).
    public async Task CapNhatKyNang(NguoiDangNhap nd, List<string> ds)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        ds = ds.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        if (await _kho.KyNang.CountAsync(k => ds.Contains(k.MaKyNang)) != ds.Count)
            throw LoiNghiepVu.KhongHopLe("Có kỹ năng không nằm trong danh mục.");

        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", nd.MaHoSo!);
            var hs = await _kho.HoSoTinhNguyenVien.Include(h => h.KyNang).FirstAsync(h => h.MaHoSo == nd.MaHoSo);
            var biBo = hs.LayKyNang().Except(ds).ToList();
            if (biBo.Count > 0)
            {
                var dangCan = new HashSet<string>();
                foreach (var dk in await DangKyCaLam.LayDaPhanCong(_kho, hs.MaHoSo))
                {
                    var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
                    if (ca.TrangThaiCa is CaLam.MO or CaLam.DANG_DIEN_RA)
                        dangCan.UnionWith(ca.KyNangYeuCau.Intersect(biBo));
                }
                if (dangCan.Count > 0)
                {
                    var ten = await _kho.KyNang.Where(k => dangCan.Contains(k.MaKyNang)).Select(k => k.TenKyNang).ToListAsync();
                    throw LoiNghiepVu.XungDot(
                        $"Không thể bỏ kỹ năng {string.Join(", ", ten)} vì đang cần cho ca bạn đã được phân công và chưa kết thúc.");
                }
            }
            hs.CapNhatKyNang(ds, _thoiGian.GetUtcNow().UtcDateTime);
        });
    }
}
