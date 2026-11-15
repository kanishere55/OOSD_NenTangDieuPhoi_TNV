using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Tạo thông báo trên web cho người liên quan (BR25). Thông báo được thêm vào cùng giao dịch với thao tác
// gây ra nó, nên thao tác bị hủy thì thông báo cũng không được lưu.
public class BoThongBao
{
    public const string PHAN_CONG = "PHAN_CONG";
    public const string TU_CHOI = "TU_CHOI";
    public const string HUY_PHAN_CONG = "HUY_PHAN_CONG";
    public const string CA_DOI_GIO = "CA_DOI_GIO";
    public const string CA_BI_HUY = "CA_BI_HUY";
    public const string DANG_KY_MOI = "DANG_KY_MOI";
    public const string RUT_DANG_KY = "RUT_DANG_KY";
    public const string BAN_KHAI_MOI = "BAN_KHAI_MOI";
    public const string BAN_KHAI_DA_XAC_NHAN = "BAN_KHAI_DA_XAC_NHAN";
    public const string BAN_KHAI_TRA_LAI = "BAN_KHAI_TRA_LAI";
    public const string DE_XUAT_KY_NANG = "DE_XUAT_KY_NANG";
    public const string KET_QUA_DE_XUAT = "KET_QUA_DE_XUAT";
    public const string KET_QUA_CHUNG_CHI = "KET_QUA_CHUNG_CHI";
    public const string DANG_KY_TO_CHUC = "DANG_KY_TO_CHUC";
    public const string KET_QUA_TO_CHUC = "KET_QUA_TO_CHUC";
    public const string CAP_VAI_TRO = "CAP_VAI_TRO";
    public const string VANG_MAT = "VANG_MAT";

    private readonly KhoNghiepVu _kho;
    private readonly TimeProvider _thoiGian;

    public BoThongBao(KhoNghiepVu kho, TimeProvider thoiGian)
    {
        _kho = kho; _thoiGian = thoiGian;
    }

    public void Gui(string maNguoiNhan, string loai, string noiDung, string? duongDan = null, string? maThamChieu = null)
        => _kho.ThongBao.Add(ThongBao.Tao(maNguoiNhan, loai, noiDung, duongDan, maThamChieu, _thoiGian.GetUtcNow().UtcDateTime));

    public void Gui(IEnumerable<string> nguoiNhan, string loai, string noiDung, string? duongDan = null, string? maThamChieu = null)
    {
        foreach (var ma in nguoiNhan.Distinct()) Gui(ma, loai, noiDung, duongDan, maThamChieu);
    }

    // Điều phối viên đang hoạt động của một tổ chức, trừ người đang thao tác.
    public Task<List<string>> DieuPhoiVienCua(string maToChuc, string? truNguoi = null)
        => ThanhVienCua(maToChuc, CacVaiTro.DieuPhoi, truNguoi);

    public Task<List<string>> ThanhVienCua(string maToChuc, string vaiTro, string? truNguoi = null)
        => _kho.ThanhVienToChuc.AsNoTracking()
            .Where(t => t.MaToChuc == maToChuc && t.VaiTro == vaiTro && t.DangHoatDong && t.NguoiDung!.DangHoatDong
                        && t.MaNguoiDung != truNguoi)
            .Select(t => t.MaNguoiDung).ToListAsync();

    public Task<List<string>> QuanTriVien()
        => _kho.NguoiDungVaiTro.AsNoTracking()
            .Where(v => v.VaiTro == CacVaiTro.QuanTri)
            .Join(_kho.NguoiDung.Where(n => n.DangHoatDong), v => v.MaNguoiDung, n => n.MaNguoiDung, (v, n) => n.MaNguoiDung)
            .ToListAsync();

    public Task<string> NguoiDungCuaHoSo(string maHoSo)
        => _kho.HoSoTinhNguyenVien.AsNoTracking().Where(h => h.MaHoSo == maHoSo).Select(h => h.MaNguoiDung).FirstAsync();
}
