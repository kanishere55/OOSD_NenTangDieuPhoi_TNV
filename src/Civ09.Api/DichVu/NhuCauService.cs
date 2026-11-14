using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record NhuCauNhap(string MaToChuc, string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng);
public record NhuCauSua(string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng);
public record PhienBanNhap(int PhienBan);

// Quản lý nhu cầu: tạo, sửa, công bố sau khi rà soát, đóng và hủy (UC03, BR01, BR05, BR16).
public class NhuCauService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public NhuCauService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    // Nhu cầu của các tổ chức mà người dùng là đại diện hoặc điều phối viên.
    public async Task<List<object>> XemNhuCau(NguoiDangNhap nd)
    {
        var toChuc = nd.ThanhVien.Select(t => t.MaToChuc).Distinct().ToList();
        if (toChuc.Count == 0) throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho đại diện và điều phối viên.");
        var ds = await _kho.NhuCauCongDong.AsNoTracking()
            .Where(n => toChuc.Contains(n.MaToChuc))
            .OrderBy(n => n.TrangThai == NhuCauCongDong.NHAP ? 0 : n.TrangThai == NhuCauCongDong.CONG_BO ? 1 : 2)
            .ThenBy(n => n.TieuDe)
            .Select(n => new
            {
                n.MaNhuCau, n.MaToChuc, TenToChuc = n.ToChuc!.TenToChuc, n.TieuDe, n.KhuVucChung,
                n.TrangThai, n.PhienBanCongKhai, n.DaRaSoatCongKhai, SoCa = n.CaLam.Count,
                SoCaChuaXong = n.CaLam.Count(c => c.TrangThai == CaLam.MO || c.TrangThai == CaLam.DANG_DIEN_RA),
                SoChoDuyet = n.CaLam.Where(c => c.TrangThai == CaLam.MO)
                    .SelectMany(c => c.DangKy).Count(d => d.TrangThai == DangKyCaLam.CHO_DUYET)
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // Hướng dẫn riêng chỉ được đọc sau khi ChinhSachTruyCap cho phép (BR15).
    public async Task<object> XemChiTiet(NguoiDangNhap nd, string maNhuCau)
    {
        var n = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
        if (!_chinhSach.DuocXemRiengCuaNhuCau(nd, n.MaToChuc)) throw LoiNghiepVu.KhongDuQuyen();
        var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau);
        var bayGio = _thoiGian.GetUtcNow().UtcDateTime;
        var ca = await _kho.CaLam.AsNoTracking().Where(c => c.MaNhuCau == maNhuCau)
            .Select(c => new { c.TrangThai, c.BatDauDuKien }).ToListAsync();
        return new
        {
            n.MaNhuCau, n.MaToChuc, TenToChuc = n.ToChuc!.TenToChuc, n.TieuDe, n.TomTatCongKhai, n.KhuVucChung,
            n.PhienBanCongKhai, n.DaRaSoatCongKhai, n.TrangThai,
            SoCa = ca.Count,
            SoCaChuaXong = ca.Count(c => c.TrangThai is CaLam.MO or CaLam.DANG_DIEN_RA),
            CoCaDaBatDau = ca.Any(c => c.TrangThai is CaLam.DANG_DIEN_RA or CaLam.DA_KET_THUC
                                       || (c.TrangThai == CaLam.MO && bayGio >= c.BatDauDuKien)),
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

    // BR05: phần công khai sửa được khi là bản nháp, hoặc khi đã công bố mà chưa có ca (khi đó trở về bản nháp).
    // Sửa hướng dẫn riêng không ảnh hưởng nội dung đã rà soát.
    public async Task CapNhatNhuCau(NguoiDangNhap nd, string maNhuCau, NhuCauSua dl)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayDeSua(nd, maNhuCau);
            if (nc.TrangThai is NhuCauCongDong.DONG or NhuCauCongDong.DA_HUY)
                throw LoiNghiepVu.XungDot("Nhu cầu đã đóng hoặc đã hủy.");
            var daCoCa = await _kho.CaLam.AnyAsync(c => c.MaNhuCau == maNhuCau);
            nc.CapNhatCongKhai(dl.TieuDe, dl.TomTatCongKhai, dl.KhuVucChung, daCoCa);
            GanHuongDanRieng(nc.MaNhuCau, await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau), dl.HuongDanRieng);
        });
    }

    public async Task CongBoNhuCau(NguoiDangNhap nd, string maNhuCau, int phienBanDaRaSoat)
        => await _giaoDich.ThucHienAsync(async () => (await LayDeSua(nd, maNhuCau)).CongBo(phienBanDaRaSoat));

    // BR16
    public async Task DongNhuCau(NguoiDangNhap nd, string maNhuCau)
        => await _giaoDich.ThucHienAsync(async () => (await LayKemCa(nd, maNhuCau)).Dong());

    // BR16: các ca còn mở và đăng ký còn hiệu lực bị hủy trong cùng giao dịch.
    public async Task HuyNhuCau(NguoiDangNhap nd, string maNhuCau)
        => await _giaoDich.ThucHienAsync(async () =>
            (await LayKemCa(nd, maNhuCau)).Huy(_thoiGian.GetUtcNow().UtcDateTime));

    private async Task<NhuCauCongDong> LayKemCa(NguoiDangNhap nd, string maNhuCau)
    {
        var nc = await LayDeSua(nd, maNhuCau);
        await _kho.Entry(nc).Collection(n => n.CaLam).Query().Include(c => c.DangKy).LoadAsync();
        return nc;
    }

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
