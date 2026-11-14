using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record NhuCauNhap(string MaToChuc, string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng);
public record NhuCauSua(string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng);
public record PhienBanNhap(int PhienBan);

// Quản lý nhu cầu: tạo, sửa bản nháp và công bố sau khi rà soát (UC03, BR01, BR05).
public class NhuCauService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;

    public NhuCauService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich;
    }

    // Nhu cầu của các tổ chức mà người dùng là đại diện hoặc điều phối viên.
    public async Task<List<object>> XemNhuCau(NguoiDangNhap nd)
    {
        var toChuc = nd.ThanhVien.Select(t => t.MaToChuc).Distinct().ToList();
        if (toChuc.Count == 0) throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho đại diện và điều phối viên.");
        var ds = await _kho.NhuCauCongDong.AsNoTracking()
            .Where(n => toChuc.Contains(n.MaToChuc))
            .OrderBy(n => n.TrangThai == NhuCauCongDong.NHAP ? 0 : 1).ThenBy(n => n.TieuDe)
            .Select(n => new
            {
                n.MaNhuCau, n.MaToChuc, TenToChuc = n.ToChuc!.TenToChuc, n.TieuDe, n.KhuVucChung,
                n.TrangThai, n.PhienBanCongKhai, n.DaRaSoatCongKhai, SoCa = n.CaLam.Count
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // Hướng dẫn riêng chỉ được đọc sau khi ChinhSachTruyCap cho phép (BR15).
    public async Task<object> XemChiTiet(NguoiDangNhap nd, string maNhuCau)
    {
        var n = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
        if (!_chinhSach.DuocXemRiengCuaNhuCau(nd, n.MaToChuc)) throw LoiNghiepVu.KhongDuQuyen();
        var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau);
        return new
        {
            n.MaNhuCau, n.MaToChuc, TenToChuc = n.ToChuc!.TenToChuc, n.TieuDe, n.TomTatCongKhai, n.KhuVucChung,
            n.PhienBanCongKhai, n.DaRaSoatCongKhai, n.TrangThai,
            HuongDanRieng = rieng?.HuongDanRieng,
            LaDaiDien = nd.LaThanhVien(n.MaToChuc, CacVaiTro.DaiDien),
            LaDieuPhoi = nd.LaThanhVien(n.MaToChuc, CacVaiTro.DieuPhoi)
        };
    }

    public async Task<string> TaoNhuCau(NguoiDangNhap nd, NhuCauNhap dl)
    {
        _chinhSach.YeuCauThanhVien(nd, dl.MaToChuc, CacVaiTro.DaiDien);
        var nc = NhuCauCongDong.Tao(dl.MaToChuc, dl.TieuDe, dl.TomTatCongKhai, dl.KhuVucChung);
        await _giaoDich.ThucHienAsync(() =>
        {
            _kho.NhuCauCongDong.Add(nc);
            GanHuongDanRieng(nc.MaNhuCau, null, dl.HuongDanRieng);
            return Task.CompletedTask;
        });
        return nc.MaNhuCau;
    }

    // BR05: phần công khai chỉ sửa khi còn là bản nháp; sửa hướng dẫn riêng không ảnh hưởng nội dung đã rà soát.
    public async Task CapNhatNhuCau(NguoiDangNhap nd, string maNhuCau, NhuCauSua dl)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayDeSua(nd, maNhuCau);
            if (nc.TrangThai is NhuCauCongDong.DONG or NhuCauCongDong.DA_HUY)
                throw LoiNghiepVu.XungDot("Nhu cầu đã đóng hoặc đã hủy.");
            nc.CapNhatCongKhai(dl.TieuDe, dl.TomTatCongKhai, dl.KhuVucChung);
            GanHuongDanRieng(nc.MaNhuCau, await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau), dl.HuongDanRieng);
        });
    }

    public async Task CongBoNhuCau(NguoiDangNhap nd, string maNhuCau, int phienBanDaRaSoat)
        => await _giaoDich.ThucHienAsync(async () => (await LayDeSua(nd, maNhuCau)).CongBo(phienBanDaRaSoat));

    private async Task<NhuCauCongDong> LayDeSua(NguoiDangNhap nd, string maNhuCau)
    {
        await _giaoDich.KhoaDongAsync("NhuCauCongDong", "MaNhuCau", maNhuCau);
        var nc = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
        _chinhSach.YeuCauThanhVien(nd, nc.MaToChuc, CacVaiTro.DaiDien);
        return nc;
    }

    private void GanHuongDanRieng(string maNhuCau, ThongTinDieuPhoiRieng? hienCo, string? noiDung)
    {
        if (noiDung == null) return;
        noiDung = noiDung.Trim();
        if (hienCo == null)
        {
            if (noiDung.Length > 0)
                _kho.ThongTinDieuPhoiRieng.Add(new ThongTinDieuPhoiRieng { MaNhuCau = maNhuCau, HuongDanRieng = noiDung });
        }
        else if (noiDung.Length == 0) _kho.ThongTinDieuPhoiRieng.Remove(hienCo);
        else hienCo.HuongDanRieng = noiDung;
    }
}
