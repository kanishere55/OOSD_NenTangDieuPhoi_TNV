using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record DongGopNhap(DateTime BatDauThucTe, DateTime KetThucThucTe, string MoTaCongViec);
public record QuyetDinhDuyet(bool XacNhan, string? LyDoTraLai);

// Khai, bổ sung, duyệt đóng góp và xem tổng giờ (UC09, UC10, UC12).
public class DongGopService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly TimeProvider _thoiGian;

    public DongGopService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Gửi bản khai mới, hoặc sửa và gửi lại bản đang cần bổ sung. Chỉ người đã được phân công, chỉ sau khi
    // ca kết thúc, và thời gian thực tế phải nằm trong khung giờ của ca (BR13, BR14).
    public async Task<string> GuiDongGop(NguoiDangNhap nd, string maDangKy, DongGopNhap dl)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DangKyCaLam", "MaDangKy", maDangKy);
            var dk = await DangKyCaLam.LayThongTin(_kho, maDangKy);
            if (dk.MaHoSo != nd.MaHoSo) throw LoiNghiepVu.KhongDuQuyen("Bạn chỉ được khai đóng góp cho đăng ký của mình.");
            if (dk.TrangThai != DangKyCaLam.DA_PHAN_CONG)
                throw LoiNghiepVu.XungDot("Chỉ người đã được phân công mới được khai đóng góp.");
            var ca = await CaLam.LayDieuKien(_kho, dk.MaCaLam);
            if (ca.TrangThaiCa == CaLam.DA_HUY) throw LoiNghiepVu.XungDot("Ca đã bị hủy.");
            if (BayGio < ca.KetThucDuKien)
                throw LoiNghiepVu.XungDot("Chỉ được khai đóng góp sau khi ca kết thúc.");

            var batDau = CaService.ChuanUtc(dl.BatDauThucTe);
            var ketThuc = CaService.ChuanUtc(dl.KetThucThucTe);
            var ban = await DongGop.LayBanKhai(_kho, maDangKy);
            if (ban == null)
            {
                ban = DongGop.TaoBanKhai(dk, ca, batDau, ketThuc, dl.MoTaCongViec);
                _kho.DongGop.Add(ban);
            }
            else ban.GuiLai(ca, batDau, ketThuc, dl.MoTaCongViec);
            return ban.MaDongGop;
        });
    }

    // Các bản khai chờ duyệt của những tổ chức mà người dùng là điều phối viên.
    public async Task<List<object>> XemDongGopChoDuyet(NguoiDangNhap nd)
    {
        var toChuc = nd.ToChucTheoVaiTro(CacVaiTro.DieuPhoi).ToList();
        if (toChuc.Count == 0) throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho điều phối viên.");
        var ds = await _kho.DongGop.AsNoTracking()
            .Where(g => g.TrangThai == DongGop.CHO_DUYET && toChuc.Contains(g.DangKy!.CaLam!.NhuCau!.MaToChuc))
            .OrderBy(g => g.DangKy!.CaLam!.KetThucDuKien)
            .Select(g => new
            {
                g.MaDongGop, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai,
                TenTinhNguyenVien = g.DangKy!.HoSo!.NguoiDung!.TenHienThi,
                MaNguoiKhai = g.DangKy.HoSo.MaNguoiDung,
                g.DangKy.CaLam!.NhuCau!.TieuDe,
                g.DangKy.CaLam.BatDauDuKien, g.DangKy.CaLam.KetThucDuKien
            }).ToListAsync();
        return ds.Select(g => (object)new
        {
            g.MaDongGop, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai, g.TenTinhNguyenVien,
            g.TieuDe, g.BatDauDuKien, g.KetThucDuKien,
            SoGio = DongGop.TinhSoGio(g.BatDauThucTe, g.KetThucThucTe),
            LaCuaToi = g.MaNguoiKhai == nd.MaNguoiDung
        }).ToList();
    }

    // BR14: không tự duyệt bản của mình; trả lại phải có lý do.
    public async Task DuyetDongGop(NguoiDangNhap nd, string maDongGop, QuyetDinhDuyet qd)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("DongGop", "MaDongGop", maDongGop);
            var ban = await DongGop.LayBanKhaiTheoMa(_kho, maDongGop);
            var ca = await CaLam.LayDieuKien(_kho, ban.DangKy!.MaCaLam);
            _chinhSach.YeuCauThanhVien(nd, ca.MaToChuc, CacVaiTro.DieuPhoi);
            if (ban.DangKy.HoSo!.MaNguoiDung == nd.MaNguoiDung)
                throw LoiNghiepVu.KhongDuQuyen("Bạn không được duyệt bản khai của chính mình.");
            if (qd.XacNhan) ban.XacNhan(nd.MaNguoiDung, BayGio);
            else ban.TraLai(qd.LyDoTraLai);
        });
    }

    // Lịch sử bản khai và tổng giờ; tổng chỉ cộng bản đã xác nhận, lấy từ sp_TongGioDongGop (UC12, BR14).
    public async Task<object> XemDongGopCuaToi(NguoiDangNhap nd)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var ds = await _kho.DongGop.AsNoTracking().Where(g => g.DangKy!.MaHoSo == nd.MaHoSo)
            .OrderByDescending(g => g.BatDauThucTe)
            .Select(g => new
            {
                g.MaDongGop, g.MaDangKy, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai,
                g.LyDoTraLai, g.NgayXacNhan, g.DangKy!.CaLam!.NhuCau!.TieuDe
            }).ToListAsync();
        var tong = await _kho.TongGioDongGopAsync(nd.MaHoSo!);
        return new
        {
            TongGioDaXacNhan = tong.TongSoGiay / 3600m,
            tong.SoBanDaXacNhan,
            DanhSach = ds.Select(g => new
            {
                g.MaDongGop, g.MaDangKy, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai,
                g.LyDoTraLai, g.NgayXacNhan, g.TieuDe,
                SoGio = DongGop.TinhSoGio(g.BatDauThucTe, g.KetThucThucTe)
            }).ToList()
        };
    }
}
