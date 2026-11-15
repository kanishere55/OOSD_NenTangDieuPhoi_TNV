using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record DongGopNhap(DateTime BatDauThucTe, DateTime KetThucThucTe, string MoTaCongViec);
public record QuyetDinhDuyet(bool XacNhan, string? LyDoTraLai);

// Khai, bổ sung, duyệt đóng góp, ảnh minh chứng và xem tổng giờ (UC09, UC10, UC12, BR13, BR14, BR23, BR24).
public class DongGopService
{
    private const int SoAnhToiDa = 5;
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public DongGopService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    private DateTime BayGio => _thoiGian.GetUtcNow().UtcDateTime;

    // Gửi bản khai mới, hoặc sửa và gửi lại bản đang cần bổ sung. Chỉ người đã được phân công, chỉ sau khi
    // ca kết thúc, và thời gian thực tế phải nằm trong khung giờ của ca (BR13, BR14). Có đủ giờ vào ra
    // từ điểm danh thì thời gian thực tế lấy theo điểm danh (BR23).
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

            var diemDanh = await _kho.DiemDanh.AsNoTracking().FirstOrDefaultAsync(x => x.MaDangKy == maDangKy);
            var batDau = CaService.ChuanUtc(dl.BatDauThucTe);
            var ketThuc = CaService.ChuanUtc(dl.KetThucThucTe);
            var ban = await DongGop.LayBanKhai(_kho, maDangKy);
            if (ban == null)
            {
                ban = DongGop.TaoBanKhai(dk, ca, batDau, ketThuc, dl.MoTaCongViec, diemDanh);
                _kho.DongGop.Add(ban);
            }
            else ban.GuiLai(ca, batDau, ketThuc, dl.MoTaCongViec, diemDanh);
            _thongBao.Gui(await _thongBao.DieuPhoiVienCua(ca.MaToChuc, nd.MaNguoiDung), BoThongBao.BAN_KHAI_MOI,
                $"{nd.TenHienThi} gửi bản khai đóng góp cho ca \"{ca.TieuDe}\".", "/dong-gop/cho-duyet", ban.MaDongGop);
            return ban.MaDongGop;
        });
    }

    // BR22, BR24: thêm ảnh minh chứng vào bản khai còn sửa được.
    public async Task<string> ThemAnh(NguoiDangNhap nd, string maDongGop, IFormFile tep)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        var anh = await XuLyAnh.ChuanHoaAsync(tep, nd.MaNguoiDung, BayGio);
        return await _giaoDich.ThucHienAsync(async () =>
        {
            var ban = await LayBanCuaToi(nd, maDongGop);
            if (ban.Anh.Count >= SoAnhToiDa) throw LoiNghiepVu.KhongHopLe($"Mỗi bản khai có tối đa {SoAnhToiDa} ảnh minh chứng.");
            _kho.TepDinhKem.Add(anh);
            _kho.AnhMinhChung.Add(new AnhMinhChung { MaDongGop = maDongGop, MaTep = anh.MaTep });
            return anh.MaTep;
        });
    }

    public async Task BoAnh(NguoiDangNhap nd, string maDongGop, string maTep)
    {
        _chinhSach.YeuCauTinhNguyenVien(nd);
        await _giaoDich.ThucHienAsync(async () =>
        {
            var ban = await LayBanCuaToi(nd, maDongGop);
            var a = ban.Anh.FirstOrDefault(x => x.MaTep == maTep) ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ảnh.");
            _kho.AnhMinhChung.Remove(a);
            await _kho.SaveChangesAsync();
            _kho.TepDinhKem.Remove(await _kho.TepDinhKem.FirstAsync(t => t.MaTep == maTep));
        });
    }

    // BR24, BR26: xem ảnh minh chứng sau khi kiểm tra quyền; ghi nhật ký cả khi bị từ chối.
    public async Task<TepDinhKem> XemAnh(NguoiDangNhap nd, string maTep)
    {
        var a = await _kho.AnhMinhChung.AsNoTracking().Where(x => x.MaTep == maTep)
                    .Select(x => new { x.MaDongGop, x.MaTep })
                    .FirstOrDefaultAsync()
                ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy ảnh.");
        var thongTin = await _kho.DongGop.AsNoTracking().Where(g => g.MaDongGop == a.MaDongGop)
            .Select(g => new { g.DangKy!.MaHoSo, g.DangKy.CaLam!.NhuCau!.MaToChuc }).FirstAsync();
        var duoc = _chinhSach.DuocXemAnhMinhChung(nd, thongTin.MaToChuc, thongTin.MaHoSo);
        await _chinhSach.GhiNhatKyAsync(nd, NhatKyTruyCap.ANH_MINH_CHUNG, a.MaDongGop, thongTin.MaToChuc, duoc);
        if (!duoc) throw LoiNghiepVu.KhongDuQuyen("Bạn không có quyền xem ảnh minh chứng này.");
        return await _kho.TepDinhKem.AsNoTracking().FirstAsync(t => t.MaTep == maTep);
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
                g.MaDongGop, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai, g.GioTheoDiemDanh,
                TenTinhNguyenVien = g.DangKy!.HoSo!.NguoiDung!.TenHienThi,
                MaNguoiKhai = g.DangKy.HoSo.MaNguoiDung,
                g.DangKy.CaLam!.NhuCau!.TieuDe,
                g.DangKy.CaLam.BatDauDuKien, g.DangKy.CaLam.KetThucDuKien,
                Anh = g.Anh.Select(a => a.MaTep).ToList(),
                DiemDanh = g.DangKy.DiemDanh == null ? null : new { g.DangKy.DiemDanh.KetQua, g.DangKy.DiemDanh.GioVao, g.DangKy.DiemDanh.GioRa }
            }).ToListAsync();
        return ds.Select(g => (object)new
        {
            g.MaDongGop, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai, g.TenTinhNguyenVien,
            g.TieuDe, g.BatDauDuKien, g.KetThucDuKien, g.GioTheoDiemDanh, g.Anh, g.DiemDanh,
            SoGio = DongGop.TinhSoGio(g.BatDauThucTe, g.KetThucThucTe),
            LaCuaToi = g.MaNguoiKhai == nd.MaNguoiDung
        }).ToList();
    }

    // BR14: không tự duyệt bản của mình; trả lại phải có lý do. Người khai được báo kết quả (BR25).
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
            if (qd.XacNhan)
            {
                ban.XacNhan(nd.MaNguoiDung, BayGio);
                _thongBao.Gui(ban.DangKy.HoSo.MaNguoiDung, BoThongBao.BAN_KHAI_DA_XAC_NHAN,
                    $"Bản khai {DongGop.TinhSoGio(ban.BatDauThucTe, ban.KetThucThucTe):0.##} giờ ở ca \"{ca.TieuDe}\" đã được xác nhận.",
                    "/dong-gop/cua-toi", ban.MaDongGop);
            }
            else
            {
                ban.TraLai(qd.LyDoTraLai);
                _thongBao.Gui(ban.DangKy.HoSo.MaNguoiDung, BoThongBao.BAN_KHAI_TRA_LAI,
                    $"Bản khai ở ca \"{ca.TieuDe}\" cần bổ sung. Lý do: {ban.LyDoTraLai}", $"/dang-ky/{ban.MaDangKy}/khai", ban.MaDongGop);
            }
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
                g.MaDongGop, g.MaDangKy, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai, g.GioTheoDiemDanh,
                g.LyDoTraLai, g.NgayXacNhan, g.DangKy!.CaLam!.NhuCau!.TieuDe, TenToChuc = g.DangKy.CaLam.NhuCau.ToChuc!.TenToChuc,
                Anh = g.Anh.Select(a => a.MaTep).ToList()
            }).ToListAsync();
        var tong = await _kho.TongGioDongGopAsync(nd.MaHoSo!);
        return new
        {
            TongGioDaXacNhan = tong.TongSoGiay / 3600m,
            tong.SoBanDaXacNhan,
            DanhSach = ds.Select(g => new
            {
                g.MaDongGop, g.MaDangKy, g.BatDauThucTe, g.KetThucThucTe, g.MoTaCongViec, g.TrangThai, g.GioTheoDiemDanh,
                g.LyDoTraLai, g.NgayXacNhan, g.TieuDe, g.TenToChuc, g.Anh,
                SoGio = DongGop.TinhSoGio(g.BatDauThucTe, g.KetThucThucTe)
            }).ToList()
        };
    }

    private async Task<DongGop> LayBanCuaToi(NguoiDangNhap nd, string maDongGop)
    {
        await _giaoDich.KhoaDongAsync("DongGop", "MaDongGop", maDongGop);
        var ban = await _kho.DongGop.Include(g => g.DangKy).Include(g => g.Anh)
                      .FirstOrDefaultAsync(g => g.MaDongGop == maDongGop)
                  ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy bản khai.");
        if (ban.DangKy!.MaHoSo != nd.MaHoSo) throw LoiNghiepVu.KhongDuQuyen("Đây không phải bản khai của bạn.");
        if (!ban.ConSuaAnhDuoc) throw LoiNghiepVu.XungDot("Bản khai đã được xác nhận nên không thêm hoặc bỏ ảnh được nữa.");
        return ban;
    }
}
