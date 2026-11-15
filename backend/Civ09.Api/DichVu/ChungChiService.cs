using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.DichVu;

public record XacMinhNhap(bool HopLe, string? LyDo);

// Xem và xác minh chứng chỉ kỹ năng (UC15, BR19). Mọi lần xem ảnh chứng chỉ được ghi nhật ký (BR26).
public class ChungChiService
{
    private readonly KhoNghiepVu _kho;
    private readonly ChinhSachTruyCap _chinhSach;
    private readonly GiaoDich _giaoDich;
    private readonly BoThongBao _thongBao;
    private readonly TimeProvider _thoiGian;

    public ChungChiService(KhoNghiepVu kho, ChinhSachTruyCap chinhSach, GiaoDich giaoDich, BoThongBao thongBao, TimeProvider thoiGian)
    {
        _kho = kho; _chinhSach = chinhSach; _giaoDich = giaoDich; _thongBao = thongBao; _thoiGian = thoiGian;
    }

    // Chứng chỉ đang chờ xác minh: quản trị viên thấy tất cả; điều phối viên thấy của những người đang có
    // đăng ký còn hiệu lực vào ca của tổ chức mình. Không hiện chứng chỉ của chính người xem.
    public async Task<List<object>> XemChoXacMinh(NguoiDangNhap nd)
    {
        var toChuc = nd.ToChucTheoVaiTro(CacVaiTro.DieuPhoi).ToList();
        if (!nd.LaQuanTri && toChuc.Count == 0)
            throw LoiNghiepVu.KhongDuQuyen("Chức năng này dành cho điều phối viên và quản trị viên.");
        var q = _kho.ChungChiKyNang.AsNoTracking()
            .Where(c => c.TrangThai == ChungChiKyNang.CHO_XAC_MINH && c.MaHoSo != nd.MaHoSo);
        if (!nd.LaQuanTri)
            q = q.Where(c => _kho.DangKyCaLam.Any(d => d.MaHoSo == c.MaHoSo
                && (d.TrangThai == DangKyCaLam.CHO_DUYET || d.TrangThai == DangKyCaLam.DA_PHAN_CONG)
                && toChuc.Contains(d.CaLam!.NhuCau!.MaToChuc)));
        var ds = await q.OrderBy(c => c.NgayGui)
            .Select(c => new
            {
                c.MaChungChi, c.NgayGui, c.KyNang!.TenKyNang, TenTinhNguyenVien = c.HoSo!.NguoiDung!.TenHienThi,
                CapDo = _kho.KyNangSoHuu.Where(s => s.MaHoSo == c.MaHoSo && s.MaKyNang == c.MaKyNang).Select(s => (int?)s.CapDo).FirstOrDefault()
            }).ToListAsync();
        return ds.Cast<object>().ToList();
    }

    // Đọc ảnh chứng chỉ sau khi kiểm tra quyền; ghi nhật ký cả khi bị từ chối.
    public async Task<TepDinhKem> XemAnh(NguoiDangNhap nd, string maChungChi)
    {
        var cc = await _kho.ChungChiKyNang.AsNoTracking().FirstOrDefaultAsync(c => c.MaChungChi == maChungChi)
                 ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy chứng chỉ.");
        var duoc = await _chinhSach.DuocXemChungChi(nd, cc.MaHoSo);
        await _chinhSach.GhiNhatKyAsync(nd, NhatKyTruyCap.CHUNG_CHI, maChungChi, null, duoc);
        if (!duoc) throw LoiNghiepVu.KhongDuQuyen("Bạn không có quyền xem chứng chỉ này.");
        return await _kho.TepDinhKem.AsNoTracking().FirstAsync(t => t.MaTep == cc.MaTep);
    }

    public async Task XacMinh(NguoiDangNhap nd, string maChungChi, XacMinhNhap dl)
    {
        await _giaoDich.ThucHienAsync(async () =>
        {
            await _giaoDich.KhoaDongAsync("ChungChiKyNang", "MaChungChi", maChungChi);
            var cc = await _kho.ChungChiKyNang.Include(c => c.KyNang).Include(c => c.HoSo)
                         .FirstOrDefaultAsync(c => c.MaChungChi == maChungChi)
                     ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy chứng chỉ.");
            if (cc.MaHoSo == nd.MaHoSo) throw LoiNghiepVu.KhongDuQuyen("Bạn không được tự xác minh chứng chỉ của mình.");
            if (!await _chinhSach.DuocXacMinhChungChi(nd, cc.MaHoSo))
                throw LoiNghiepVu.KhongDuQuyen("Bạn không có quyền xác minh chứng chỉ này.");
            var bayGio = _thoiGian.GetUtcNow().UtcDateTime;
            if (dl.HopLe)
            {
                var banCu = await _kho.ChungChiKyNang.FirstOrDefaultAsync(c => c.MaHoSo == cc.MaHoSo && c.MaKyNang == cc.MaKyNang
                                                                                && c.TrangThai == ChungChiKyNang.DA_XAC_MINH);
                if (banCu != null)
                {
                    banCu.TrangThai = ChungChiKyNang.HET_HIEU_LUC;
                    await _kho.SaveChangesAsync();
                }
                cc.XacNhanHopLe(nd.MaNguoiDung, bayGio, null);
                _thongBao.Gui(cc.HoSo!.MaNguoiDung, BoThongBao.KET_QUA_CHUNG_CHI,
                    $"Chứng chỉ kỹ năng {cc.KyNang!.TenKyNang} của bạn đã được xác minh.", "/ho-so", cc.MaChungChi);
            }
            else
            {
                cc.TuChoi(nd.MaNguoiDung, dl.LyDo, bayGio);
                _thongBao.Gui(cc.HoSo!.MaNguoiDung, BoThongBao.KET_QUA_CHUNG_CHI,
                    $"Chứng chỉ kỹ năng {cc.KyNang!.TenKyNang} chưa được chấp nhận. Lý do: {cc.LyDoTuChoi}", "/ho-so", cc.MaChungChi);
            }
        });
    }
}
