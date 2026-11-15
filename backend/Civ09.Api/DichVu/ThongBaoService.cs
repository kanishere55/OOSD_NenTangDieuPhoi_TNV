using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

// Xem và đánh dấu đã đọc thông báo của chính mình (UC19, BR25).
public class ThongBaoService
{
    private readonly KhoNghiepVu _kho;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public ThongBaoService(KhoNghiepVu kho, GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    public async Task<object> XemThongBao(NguoiDangNhap nd, bool chiChuaDoc)
    {
        var q = _kho.ThongBao.AsNoTracking().Where(t => t.MaNguoiNhan == nd.MaNguoiDung);
        if (chiChuaDoc) q = q.Where(t => !t.DaDoc);
        var ds = await q.OrderByDescending(t => t.NgayTao).ThenBy(t => t.MaThongBao).Take(100)
            .Select(t => new { t.MaThongBao, t.Loai, t.NoiDung, t.DuongDan, t.NgayTao, t.DaDoc }).ToListAsync();
        var soChuaDoc = await _kho.ThongBao.CountAsync(t => t.MaNguoiNhan == nd.MaNguoiDung && !t.DaDoc);
        return new { SoChuaDoc = soChuaDoc, DanhSach = ds };
    }

    public Task<int> SoChuaDoc(NguoiDangNhap nd)
        => _kho.ThongBao.CountAsync(t => t.MaNguoiNhan == nd.MaNguoiDung && !t.DaDoc);

    public async Task DanhDauDaDoc(NguoiDangNhap nd, string maThongBao)
        => await _giaoDich.ThucHienAsync(async () =>
        {
            var t = await _kho.ThongBao.FirstOrDefaultAsync(x => x.MaThongBao == maThongBao)
                    ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy thông báo.");
            t.DanhDauDaDoc(nd.MaNguoiDung, _thoiGian.GetUtcNow().UtcDateTime);
        });

    public async Task DanhDauTatCa(NguoiDangNhap nd)
        => await _giaoDich.ThucHienAsync(async () =>
        {
            var bayGio = _thoiGian.GetUtcNow().UtcDateTime;
            foreach (var t in await _kho.ThongBao.Where(x => x.MaNguoiNhan == nd.MaNguoiDung && !x.DaDoc).ToListAsync())
                t.DanhDauDaDoc(nd.MaNguoiDung, bayGio);
        });
}
