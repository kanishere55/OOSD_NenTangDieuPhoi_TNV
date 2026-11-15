using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record CaNhap(DateTime BatDauDuKien, DateTime KetThucDuKien, int SoNguoiCan, List<YeuCauNhap> KyNang);

// Quản lý và tìm ca, gợi ý ca phù hợp, tiếp nhận hoặc rút đăng ký (UC04 đến UC07, UC17, BR18, BR20, BR25).
public class CaService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly BoLocCongKhai _boLoc;
    private readonly DieuKienPhanCong _dieuKien;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public CaService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, BoLocCongKhai boLoc, DieuKienPhanCong dieuKien,
        GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _boLoc = boLoc; _dieuKien = dieuKien; _giaoDich = giaoDich;
        _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // BR01: tìm ca không cần đăng nhập, kết quả chỉ có trường công khai.
    public Task<TrangKetQua<CaCongKhaiDto>> TimCa(BoLocCa loc) => _boLoc.TimAsync(loc);

    public async Task<CaCongKhaiDto> XemCaCongKhai(string maCaLam)
        => await _boLoc.LayMotCaAsync(maCaLam)
           ?? throw LoiNghiepVu.KhongTimThay("Ca không còn nhận đăng ký hoặc không tồn tại.");

    // BR20: các ca đang mở mà tình nguyện viên đủ điều kiện đăng ký, xếp theo điểm phù hợp.
    public async Task<List<object>> XemCaPhuHop(NguoiDangNhap nd, int soLuong = 10)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var hs = await _kho.HoSoTinhNguyenVien.AsNoTracking().Include(h => h.LichRanh).FirstAsync(h => h.MaHoSo == nd.MaHoSo);
        var (capDo, daXacMinh) = await _dieuKien.KyNangCuaHoSo(hs.MaHoSo);
        var tinCay = await _kho.MucTinCay.AsNoTracking().FirstOrDefaultAsync(m => m.MaHoSo == hs.MaHoSo);
        var dangKy = await _kho.DangKyCaLam.AsNoTracking()
            .Where(d => d.MaHoSo == hs.MaHoSo && (d.TrangThai == DangKyCaLam.CHO_DUYET || d.TrangThai == DangKyCaLam.DA_PHAN_CONG))
            .Select(d => new { d.MaCaLam, d.TrangThai, d.CaLam!.BatDauDuKien, d.CaLam.KetThucDuKien }).ToListAsync();
        var daPhanCong = dangKy.Where(d => d.TrangThai == DangKyCaLam.DA_PHAN_CONG).ToList();
        var lichRanh = hs.LichRanh.Select(l => ((int)l.Thu, l.Buoi)).ToHashSet();
        var kyNangCanChungChi = (await _kho.KyNang.AsNoTracking().Where(k => k.CanChungChi).Select(k => k.MaKyNang).ToListAsync()).ToHashSet();

        var ketQua = new List<(CaCongKhaiDto Ca, KetQuaCham Cham)>();
        foreach (var c in await _boLoc.LayTatCaAsync())
        {
            if (dangKy.Any(d => d.MaCaLam == c.MaCaLam)) continue;
            var yeuCau = c.YeuCau.Select(y => new YeuCauDk(y.MaKyNang, y.CapDoToiThieu, y.CanXacMinh)).ToList();
            if (!DieuKienPhanCong.DuKyNang(yeuCau, capDo, daXacMinh)) continue;
            if (daPhanCong.Any(d => DangKyCaLam.GiaoNhau(d.BatDauDuKien, d.KetThucDuKien, c.BatDauDuKien, c.KetThucDuKien))) continue;
            var km = ViTri.KhoangCachKm(hs.ViDo, hs.KinhDo, c.ViDoVung, c.KinhDoVung);
            if (km != null && hs.KhoangCachToiDa != null && km > hs.KhoangCachToiDa) continue;
            var canChungChi = c.YeuCau.Where(y => kyNangCanChungChi.Contains(y.MaKyNang)).Select(y => y.MaKyNang).ToList();
            var cham = ChamDiemPhuHop.Cham(new ThongTinGhep(
                true, canChungChi.Count, canChungChi.Count(daXacMinh.Contains), km,
                lichRanh.Count == 0 ? null : lichRanh.Contains(Buoi.Cua(c.BatDauDuKien)),
                tinCay?.SoLanCoMat ?? 0, tinCay?.SoLanVang ?? 0));
            ketQua.Add((c with { KhoangCachKm = km }, cham));
        }
        return ketQua.OrderByDescending(x => x.Cham.Diem).ThenBy(x => x.Ca.BatDauDuKien).Take(soLuong)
            .Select(x => (object)new { Ca = x.Ca, x.Cham.Diem, x.Cham.LyDo, x.Cham.LuuY }).ToList();
    }

    // BR04, BR06, BR18: ca và kỹ năng yêu cầu lưu trong cùng giao dịch.
    public async Task<string> TaoCa(NguoiDangNhap nd, string maNhuCau, CaNhap dl)
    {
        var kyNang = ChuanHoaYeuCau(dl.KyNang);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("NhuCauCongDong", "MaNhuCau", maNhuCau);
            var nc = await NhuCauCongDong.LayThongTin(_kho, maNhuCau);
            _chinhSach.YeuCauThanhVien(nd, nc.MaToChuc, CacVaiTro.DieuPhoi);
            await KiemTraKyNangCuaCa(kyNang, null);
            var ca = CaLam.TaoCa(nc, ChuanUtc(dl.BatDauDuKien), ChuanUtc(dl.KetThucDuKien), dl.SoNguoiCan, kyNang, BayGio);
            _kho.CaLam.Add(ca);
            return ca.MaCaLam;
        });
    }

    // BR11: sửa ca đang mở trước giờ bắt đầu; ca, kỹ năng yêu cầu lưu trong cùng giao dịch.
    // Đổi giờ thì người đang chờ duyệt được báo (BR25).
    public async Task CapNhatCa(NguoiDangNhap nd, string maCaLam, CaNhap dl)
    {
        var kyNang = ChuanHoaYeuCau(dl.KyNang);
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCaLam);
            var ca = await _kho.CaLam.Include(c => c.NhuCau).Include(c => c.YeuCauKyNang).Include(c => c.DangKy)
                         .AsSplitQuery().FirstOrDefaultAsync(c => c.MaCaLam == maCaLam)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");
            _chinhSach.YeuCauThanhVien(nd, ca.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            await KiemTraKyNangCuaCa(kyNang, ca.LayKyNangYeuCau().ToHashSet());
            var batDauCu = ca.BatDauDuKien; var ketThucCu = ca.KetThucDuKien;
            ca.CapNhat(ChuanUtc(dl.BatDauDuKien), ChuanUtc(dl.KetThucDuKien), dl.SoNguoiCan, kyNang, BayGio);
            if (ca.BatDauDuKien != batDauCu || ca.KetThucDuKien != ketThucCu)
                foreach (var dk in ca.DangKy.Where(d => d.TrangThai == DangKyCaLam.CHO_DUYET))
                    _thongBao.Gui(await _thongBao.NguoiDungCuaHoSo(dk.MaHoSo), BoThongBao.CA_DOI_GIO,
                        $"Ca \"{ca.NhuCau.TieuDe}\" mà bạn đăng ký đã đổi giờ. Hãy xem lại giờ mới.", "/ca/" + ca.MaCaLam, ca.MaCaLam);
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
                KyNangYeuCau = c.YeuCauKyNang.OrderBy(y => y.KyNang!.TenKyNang).Select(y => y.KyNang!.TenKyNang).ToList(),
                MaKyNangYeuCau = c.YeuCauKyNang.Select(y => y.MaKyNang).ToList(),
                YeuCau = c.YeuCauKyNang.OrderBy(y => y.KyNang!.TenKyNang)
                    .Select(y => new { y.MaKyNang, y.KyNang!.TenKyNang, y.CapDoToiThieu, y.CanXacMinh }).ToList(),
                SoChoDuyet = c.DangKy.Count(d => d.TrangThai == DangKyCaLam.CHO_DUYET),
                SoDaPhanCong = c.DangKy.Count(d => d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // BR12: hủy ca trước giờ bắt đầu, các đăng ký còn hiệu lực bị hủy theo, lịch sử vẫn giữ; người đăng ký được báo.
    public async Task HuyCa(NguoiDangNhap nd, string maCaLam)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCaLam);
            var ca = await _kho.CaLam.Include(c => c.NhuCau).Include(c => c.DangKy)
                         .FirstOrDefaultAsync(c => c.MaCaLam == maCaLam)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ca.");
            _chinhSach.YeuCauThanhVien(nd, ca.NhuCau!.MaToChuc, CacVaiTro.DieuPhoi);
            var biAnhHuong = ca.DangKy.Where(d => d.ConHieuLuc).Select(d => d.MaHoSo).ToList();
            ca.Huy(BayGio);
            foreach (var dk in ca.DangKy) dk.Huy();
            foreach (var maHoSo in biAnhHuong)
                _thongBao.Gui(await _thongBao.NguoiDungCuaHoSo(maHoSo), BoThongBao.CA_BI_HUY,
                    $"Ca \"{ca.NhuCau.TieuDe}\" đã bị hủy. Đăng ký của bạn cho ca này không còn hiệu lực.", "/dang-ky-cua-toi", ca.MaCaLam);
        });
    }

    // UC06 (BR04, BR07, BR18). Khóa dòng hồ sơ rồi dòng ca theo cùng thứ tự với phân công, để việc hủy ca
    // hoặc chuyển mốc giờ không chen vào giữa lúc kiểm tra và lúc lưu đăng ký. Điều phối viên được báo.
    public async Task<string> DangKy(NguoiDangNhap nd, string maCa)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("HoSoTinhNguyenVien", "MaHoSo", nd.MaHoSo!);
            await _giaoDich.KhoaDongAsync("CaLam", "MaCaLam", maCa);
            var ca = await CaLam.LayDieuKien(_kho, maCa);
            if (ca.TrangThaiCa != CaLam.MO || BayGio >= ca.BatDauDuKien || ca.TrangThaiNhuCau != NhuCauCongDong.CONG_BO)
                throw LoiNghiepVu.XungDot("Chỉ đăng ký được ca đang mở, chưa bắt đầu và thuộc nhu cầu đã công bố.");
            var (capDo, daXacMinh) = await _dieuKien.KyNangCuaHoSo(nd.MaHoSo!);
            if (!DieuKienPhanCong.DuKyNang(ca.YeuCau, capDo, daXacMinh))
            {
                var ten = await _kho.KyNang.Where(k => ca.KyNangYeuCau.Contains(k.MaKyNang)).ToDictionaryAsync(k => k.MaKyNang, k => k.TenKyNang);
                var thieu = DieuKienPhanCong.MoTaThieu(ca.YeuCau, capDo, daXacMinh, ten);
                throw LoiNghiepVu.XungDot("Bạn chưa đủ điều kiện kỹ năng của ca: " + string.Join("; ", thieu) + ".");
            }
            if (await DangKyCaLam.LayThongTin(_kho, nd.MaHoSo!, maCa) != null)
                throw LoiNghiepVu.XungDot("Bạn đã có một đăng ký còn hiệu lực cho ca này. Xem ở mục Đăng ký của tôi.");

            var dk = DangKyCaLam.TaoDangKy(nd.MaHoSo!, maCa, BayGio);
            _kho.DangKyCaLam.Add(dk);
            _thongBao.Gui(await _thongBao.DieuPhoiVienCua(ca.MaToChuc, nd.MaNguoiDung), BoThongBao.DANG_KY_MOI,
                $"{nd.TenHienThi} vừa đăng ký ca \"{ca.TieuDe}\".", $"/ca/{maCa}/xet-duyet", dk.MaDangKy);
            return dk.MaDangKy;
        });
    }

    // UC07, BR11: rút đăng ký của mình trước giờ bắt đầu. Rút khi đã được phân công thì điều phối viên được báo.
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
            var daPhanCong = dk.TrangThai == DangKyCaLam.DA_PHAN_CONG;
            dk.Rut();
            if (daPhanCong)
            {
                var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
                _thongBao.Gui(await _thongBao.DieuPhoiVienCua(ca.MaToChuc), BoThongBao.RUT_DANG_KY,
                    $"{nd.TenHienThi} đã rút khỏi ca \"{ca.TieuDe}\". Ca có thêm một chỗ trống.", $"/ca/{dk.MaCaLam}/xet-duyet", dk.MaDangKy);
            }
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
                DongGop = d.DongGop == null ? null : new { d.DongGop.MaDongGop, d.DongGop.TrangThai, d.DongGop.LyDoTraLai },
                DiemDanh = d.DiemDanh == null ? null : new { d.DiemDanh.KetQua, d.DiemDanh.GioVao, d.DiemDanh.GioRa }
            }).ToListAsync();
        return ds.Select(d => (object)new
        {
            d.MaDangKy, d.MaCaLam, d.TrangThai, d.NgayDangKy, d.BatDauDuKien, d.KetThucDuKien, d.TrangThaiCa,
            d.TieuDe, d.KhuVucChung, d.TenToChuc, d.DongGop, d.DiemDanh,
            DuocRut = (d.TrangThai == DangKyCaLam.CHO_DUYET || d.TrangThai == DangKyCaLam.DA_PHAN_CONG) && bayGio < d.BatDauDuKien,
            DuocKhai = d.TrangThai == DangKyCaLam.DA_PHAN_CONG && d.TrangThaiCa != CaLam.DA_HUY && bayGio >= d.KetThucDuKien
                       && d.DiemDanh?.KetQua != DiemDanh.VANG_MAT
                       && (d.DongGop == null || d.DongGop.TrangThai == DongGop.CAN_BO_SUNG),
            DuocXemRieng = d.TrangThai == DangKyCaLam.DA_PHAN_CONG && d.TrangThaiCa != CaLam.DA_HUY && bayGio < d.KetThucDuKien,
            TrongGioDiemDanh = d.TrangThai == DangKyCaLam.DA_PHAN_CONG && d.TrangThaiCa != CaLam.DA_HUY
                               && bayGio >= d.BatDauDuKien - DiemDanh.SomTruocCa && bayGio <= d.KetThucDuKien + DiemDanh.TreSauCa
        }).ToList();
    }

    // Tác vụ nền gọi định kỳ: chuyển trạng thái ca theo giờ (BR12) và nhắc trước ca 24 giờ (BR25).
    public Task<KetQuaChuyenMoc> ChuyenMocThoiGian() => _kho.ChuyenMocThoiGianCaAsync(BayGio);
    public Task<int> TaoNhacTruocCa() => _kho.TaoNhacTruocCaAsync(BayGio);

    private static List<YeuCauNhap> ChuanHoaYeuCau(List<YeuCauNhap>? ds)
        => (ds ?? new()).Where(k => !string.IsNullOrWhiteSpace(k.MaKyNang))
            .GroupBy(k => k.MaKyNang).Select(g => g.Last()).ToList();

    // Kỹ năng phải có trong danh mục và đang dùng (kỹ năng cũ của ca đang sửa vẫn giữ được); chỉ yêu cầu
    // đã xác minh với kỹ năng cần chứng chỉ (BR17, BR18).
    private async Task KiemTraKyNangCuaCa(List<YeuCauNhap> ds, HashSet<string>? daCo)
    {
        var ma = ds.Select(k => k.MaKyNang).ToList();
        var kn = await _kho.KyNang.AsNoTracking().Where(k => ma.Contains(k.MaKyNang)).ToListAsync();
        if (kn.Count != ma.Count || kn.Any(k => !k.DangSuDung && (daCo == null || !daCo.Contains(k.MaKyNang))))
            throw LoiNghiepVu.KhongHopLe("Có kỹ năng không nằm trong danh mục.");
        var sai = ds.Where(y => y.CanXacMinh && !kn.First(k => k.MaKyNang == y.MaKyNang).CanChungChi).ToList();
        if (sai.Count > 0)
            throw LoiNghiepVu.KhongHopLe("Chỉ yêu cầu chứng chỉ đã xác minh với kỹ năng cần chứng chỉ.");
    }

    internal static DateTime ChuanUtc(DateTime t) => t.Kind switch
    {
        DateTimeKind.Utc => t,
        DateTimeKind.Local => t.ToUniversalTime(),
        _ => DateTime.SpecifyKind(t, DateTimeKind.Utc)
    };
}
