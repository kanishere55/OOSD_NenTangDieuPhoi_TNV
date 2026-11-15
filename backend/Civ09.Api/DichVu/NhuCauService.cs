using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record NhuCauNhap(string MaToChuc, string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng,
    string? DiaChiCuThe = null, decimal? ViDo = null, decimal? KinhDo = null);
public record NhuCauSua(string TieuDe, string TomTatCongKhai, string KhuVucChung, string? HuongDanRieng,
    string? DiaChiCuThe = null, decimal? ViDo = null, decimal? KinhDo = null);
public record PhienBanNhap(int PhienBan);

// Quản lý nhu cầu: tạo, sửa, ảnh, địa điểm, công bố sau khi rà soát, đóng và hủy
// (UC03, BR01, BR05, BR16, BR21, BR22).
public class NhuCauService
{
    private const int SoAnhToiDa = 5;
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public NhuCauService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

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
                    .SelectMany(c => c.DangKy).Count(d => d.TrangThai == DangKyCaLam.CHO_DUYET),
                SoAnh = n.Anh.Count,
                CoViTri = n.ViDoVung != null
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // Hướng dẫn riêng, địa chỉ cụ thể và vị trí chính xác chỉ được đọc sau khi ChinhSachTruyCap cho phép (BR15, BR21).
    public async Task<object> XemChiTiet(NguoiDangNhap nd, string maNhuCau)
    {
        var n = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
        if (!_chinhSach.DuocXemRiengCuaNhuCau(nd, n.MaToChuc)) throw LoiNghiepVu.KhongDuQuyen();
        var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau);
        var bayGio = BayGio;
        var ca = await _kho.CaLam.AsNoTracking().Where(c => c.MaNhuCau == maNhuCau)
            .Select(c => new { c.TrangThai, c.BatDauDuKien }).ToListAsync();
        var anh = await _kho.AnhNhuCau.AsNoTracking().Where(a => a.MaNhuCau == maNhuCau).OrderBy(a => a.ThuTu)
            .Select(a => a.MaTep).ToListAsync();
        return new
        {
            n.MaNhuCau, n.MaToChuc, TenToChuc = n.ToChuc!.TenToChuc, n.TieuDe, n.TomTatCongKhai, n.KhuVucChung,
            n.PhienBanCongKhai, n.DaRaSoatCongKhai, n.TrangThai,
            n.ViDoVung, n.KinhDoVung, n.BanKinhVung,
            Anh = anh,
            SoCa = ca.Count,
            SoCaChuaXong = ca.Count(c => c.TrangThai is CaLam.MO or CaLam.DANG_DIEN_RA),
            CoCaDaBatDau = ca.Any(c => c.TrangThai is CaLam.DANG_DIEN_RA or CaLam.DA_KET_THUC
                                       || (c.TrangThai == CaLam.MO && bayGio >= c.BatDauDuKien)),
            HuongDanRieng = rieng?.HuongDanRieng,
            DiaChiCuThe = rieng?.DiaChiCuThe,
            ViDo = rieng?.ViDo,
            KinhDo = rieng?.KinhDo,
            LaDaiDien = nd.LaThanhVien(n.MaToChuc, CacVaiTro.DaiDien),
            LaDieuPhoi = nd.LaThanhVien(n.MaToChuc, CacVaiTro.DieuPhoi)
        };
    }

    public async Task<string> TaoNhuCau(NguoiDangNhap nd, NhuCauNhap dl)
    {
        _chinhSach.YeuCauThanhVien(nd, dl.MaToChuc, CacVaiTro.DaiDien);
        var nc = NhuCauCongDong.Tao(dl.MaToChuc, dl.TieuDe, dl.TomTatCongKhai, dl.KhuVucChung);
        KiemTraViTri(dl.ViDo, dl.KinhDo);
        nc.GanVung(dl.ViDo, dl.KinhDo, Random.Shared);
        await _giaoDich.ThucHienAsync(() =>
        {
            _kho.NhuCauCongDong.Add(nc);
            GanThongTinRieng(nc.MaNhuCau, null, dl.HuongDanRieng, dl.DiaChiCuThe, dl.ViDo, dl.KinhDo);
            return Task.CompletedTask;
        });
        return nc.MaNhuCau;
    }

    // BR05: phần công khai sửa được khi là bản nháp, hoặc khi đã công bố mà chưa có ca (khi đó trở về bản nháp).
    // Đổi vị trí trên bản đồ làm đổi vùng công khai nên theo cùng quy tắc (BR21).
    // Hướng dẫn riêng và địa chỉ cụ thể sửa được lúc nào cũng được, không ảnh hưởng nội dung đã rà soát.
    public async Task CapNhatNhuCau(NguoiDangNhap nd, string maNhuCau, NhuCauSua dl)
    {
        KiemTraViTri(dl.ViDo, dl.KinhDo);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayDeSua(nd, maNhuCau);
            if (nc.TrangThai is NhuCauCongDong.DONG or NhuCauCongDong.DA_HUY)
                throw LoiNghiepVu.XungDot("Nhu cầu đã đóng hoặc đã hủy.");
            var daCoCa = await _kho.CaLam.AnyAsync(c => c.MaNhuCau == maNhuCau);
            var phienBanTruoc = nc.PhienBanCongKhai;
            nc.CapNhatCongKhai(dl.TieuDe, dl.TomTatCongKhai, dl.KhuVucChung, daCoCa);
            var rieng = await ThongTinDieuPhoiRieng.LayNoiDung(_kho, maNhuCau);
            var doiViTri = Lam6(rieng?.ViDo) != Lam6(dl.ViDo) || Lam6(rieng?.KinhDo) != Lam6(dl.KinhDo);
            if (doiViTri)
            {
                // Nội dung công khai có thể vừa được đánh dấu sửa ở trên; mỗi lần lưu chỉ tăng phiên bản một lần.
                if (nc.PhienBanCongKhai == phienBanTruoc) nc.DanhDauSuaCongKhai(daCoCa);
                nc.GanVung(dl.ViDo, dl.KinhDo, Random.Shared);
            }
            GanThongTinRieng(nc.MaNhuCau, rieng, dl.HuongDanRieng, dl.DiaChiCuThe, dl.ViDo, dl.KinhDo);
        });
    }

    public async Task CongBoNhuCau(NguoiDangNhap nd, string maNhuCau, int phienBanDaRaSoat)
        => await _giaoDich.ThucHienAsync(async () => (await LayDeSua(nd, maNhuCau)).CongBo(phienBanDaRaSoat));

    // BR16
    public async Task DongNhuCau(NguoiDangNhap nd, string maNhuCau)
        => await _giaoDich.ThucHienAsync(async () => (await LayKemCa(nd, maNhuCau)).Dong());

    // BR16: các ca còn mở và đăng ký còn hiệu lực bị hủy trong cùng giao dịch; người đăng ký được báo (BR25).
    public async Task HuyNhuCau(NguoiDangNhap nd, string maNhuCau)
        => await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayKemCa(nd, maNhuCau);
            var biAnhHuong = nc.CaLam.Where(c => c.TrangThai == CaLam.MO)
                .SelectMany(c => c.DangKy).Where(d => d.ConHieuLuc).Select(d => d.MaHoSo).Distinct().ToList();
            nc.Huy(BayGio);
            foreach (var maHoSo in biAnhHuong)
                _thongBao.Gui(await _thongBao.NguoiDungCuaHoSo(maHoSo), BoThongBao.CA_BI_HUY,
                    $"Nhu cầu \"{nc.TieuDe}\" đã bị hủy nên các ca và đăng ký của bạn trong nhu cầu này cũng bị hủy.", "/dang-ky-cua-toi");
        });

    // BR22: thêm ảnh minh họa. Ảnh là nội dung công khai nên phải rà soát lại.
    public async Task<string> ThemAnh(NguoiDangNhap nd, string maNhuCau, IFormFile tep)
    {
        var anh = await XuLyAnh.ChuanHoaAsync(tep, nd.MaNguoiDung, BayGio);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayDeSua(nd, maNhuCau);
            var hienCo = await _kho.AnhNhuCau.Where(a => a.MaNhuCau == maNhuCau).ToListAsync();
            if (hienCo.Count >= SoAnhToiDa) throw LoiNghiepVu.KhongHopLe($"Mỗi nhu cầu có tối đa {SoAnhToiDa} ảnh.");
            nc.DanhDauSuaCongKhai(await _kho.CaLam.AnyAsync(c => c.MaNhuCau == maNhuCau));
            _kho.TepDinhKem.Add(anh);
            _kho.AnhNhuCau.Add(new AnhNhuCau
            {
                MaNhuCau = maNhuCau, MaTep = anh.MaTep, ThuTu = (byte)(hienCo.Count == 0 ? 1 : hienCo.Max(a => a.ThuTu) + 1)
            });
            return anh.MaTep;
        });
    }

    public async Task BoAnh(NguoiDangNhap nd, string maNhuCau, string maTep)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            var nc = await LayDeSua(nd, maNhuCau);
            var a = await _kho.AnhNhuCau.FirstOrDefaultAsync(x => x.MaNhuCau == maNhuCau && x.MaTep == maTep)
                    ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ảnh.");
            nc.DanhDauSuaCongKhai(await _kho.CaLam.AnyAsync(c => c.MaNhuCau == maNhuCau));
            _kho.AnhNhuCau.Remove(a);
            await _kho.SaveChangesAsync();
            _kho.TepDinhKem.Remove(await _kho.TepDinhKem.FirstAsync(t => t.MaTep == maTep));
        });
    }

    // Ảnh nhu cầu: ai cũng xem được khi nhu cầu đã công bố; khi còn nháp chỉ thành viên tổ chức xem được.
    public async Task<TepDinhKem> XemAnh(NguoiDangNhap? nd, string maTep)
    {
        var a = await _kho.AnhNhuCau.AsNoTracking()
                    .Where(x => x.MaTep == maTep)
                    .Select(x => new { x.MaNhuCau, x.MaTep })
                    .FirstOrDefaultAsync()
                ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ảnh.");
        var nc = await _kho.NhuCauCongDong.AsNoTracking().Include(n => n.ToChuc).FirstAsync(n => n.MaNhuCau == a.MaNhuCau);
        var congKhai = nc.TrangThai is NhuCauCongDong.CONG_BO or NhuCauCongDong.DONG && nc.DaRaSoatCongKhai
                       && nc.ToChuc!.TrangThai == ToChucCongDong.HOAT_DONG;
        if (!congKhai && (nd == null || !nd.LaThanhVienBatKy(nc.MaToChuc)))
            throw LoiNghiepVu.KhongTimThay("Không tìm thấy ảnh.");
        return await _kho.TepDinhKem.AsNoTracking().FirstAsync(t => t.MaTep == maTep);
    }

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

    private static void KiemTraViTri(decimal? viDo, decimal? kinhDo)
    {
        if ((viDo == null) != (kinhDo == null)) throw LoiNghiepVu.KhongHopLe("Vị trí trên bản đồ chưa đầy đủ.");
        if (viDo != null) ViTri.KiemTra(viDo.Value, kinhDo!.Value);
    }

    private static decimal? Lam6(decimal? x) => x == null ? null : Math.Round(x.Value, 6);

    private void GanThongTinRieng(string maNhuCau, ThongTinDieuPhoiRieng? hienCo, string? huongDan, string? diaChi,
        decimal? viDo, decimal? kinhDo)
    {
        var hd = string.IsNullOrWhiteSpace(huongDan) ? null : huongDan.Trim();
        var dc = ToChucCongDong.CatChuoi(diaChi, 300);
        if (hienCo == null)
        {
            if (hd == null && dc == null && viDo == null) return;
            _kho.ThongTinDieuPhoiRieng.Add(new ThongTinDieuPhoiRieng
            {
                MaNhuCau = maNhuCau, HuongDanRieng = hd, DiaChiCuThe = dc, ViDo = Lam6(viDo), KinhDo = Lam6(kinhDo)
            });
            return;
        }
        if (hd == null && dc == null && viDo == null) { _kho.ThongTinDieuPhoiRieng.Remove(hienCo); return; }
        hienCo.HuongDanRieng = hd;
        hienCo.DiaChiCuThe = dc;
        hienCo.ViDo = Lam6(viDo);
        hienCo.KinhDo = Lam6(kinhDo);
    }
}
