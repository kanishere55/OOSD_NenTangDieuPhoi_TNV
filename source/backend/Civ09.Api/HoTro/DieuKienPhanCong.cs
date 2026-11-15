using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Kiểm tra đủ kỹ năng, còn chỗ và không trùng lịch trước khi phân công (BR04, BR08, BR09, BR18).
public class DieuKienPhanCong
{
    private readonly KhoNghiepVu _kho;

    public DieuKienPhanCong(KhoNghiepVu kho) => _kho = kho;

    // Chỉ xét có kỹ năng hay không (dùng cho kiểm thử đơn vị và các chỗ chưa cần cấp độ).
    public static bool DuKyNang(IEnumerable<string> kyNangCuaCa, IEnumerable<string> kyNangCuaHoSo)
        => !kyNangCuaCa.Except(kyNangCuaHoSo).Any();

    // BR18: có kỹ năng, cấp độ không thấp hơn yêu cầu, đã xác minh nếu ca yêu cầu.
    public static bool DuKyNang(IEnumerable<YeuCauDk> yeuCau, IReadOnlyDictionary<string, int> capDoCuaHoSo,
        IReadOnlySet<string> daXacMinh)
        => yeuCau.All(y => capDoCuaHoSo.TryGetValue(y.MaKyNang, out var c) && c >= y.CapDoToiThieu
                           && (!y.CanXacMinh || daXacMinh.Contains(y.MaKyNang)));

    // Những kỹ năng còn thiếu, kèm lý do, để báo cho người dùng biết cần bổ sung gì.
    public static List<string> MoTaThieu(IEnumerable<YeuCauDk> yeuCau, IReadOnlyDictionary<string, int> capDoCuaHoSo,
        IReadOnlySet<string> daXacMinh, IReadOnlyDictionary<string, string> tenKyNang)
    {
        var ds = new List<string>();
        foreach (var y in yeuCau)
        {
            var ten = tenKyNang.GetValueOrDefault(y.MaKyNang, y.MaKyNang);
            if (!capDoCuaHoSo.TryGetValue(y.MaKyNang, out var c)) ds.Add($"chưa có kỹ năng {ten}");
            else if (c < y.CapDoToiThieu) ds.Add($"{ten} cần mức {MucCapDo.Ten(y.CapDoToiThieu).ToLower()} trở lên");
            else if (y.CanXacMinh && !daXacMinh.Contains(y.MaKyNang)) ds.Add($"{ten} cần chứng chỉ đã được xác minh");
        }
        return ds;
    }

    public static bool ConCho(int soDaPhanCong, int soNguoiCan) => soDaPhanCong < soNguoiCan;

    // Cấp độ từng kỹ năng và các kỹ năng đã xác minh của một hồ sơ.
    public async Task<(Dictionary<string, int> CapDo, HashSet<string> DaXacMinh)> KyNangCuaHoSo(string maHoSo)
    {
        var capDo = await _kho.KyNangSoHuu.AsNoTracking().Where(k => k.MaHoSo == maHoSo)
            .ToDictionaryAsync(k => k.MaKyNang, k => k.CapDo);
        var daXacMinh = (await _kho.ChungChiKyNang.AsNoTracking()
            .Where(c => c.MaHoSo == maHoSo && c.TrangThai == ChungChiKyNang.DA_XAC_MINH)
            .Select(c => c.MaKyNang).ToListAsync()).ToHashSet();
        return (capDo, daXacMinh);
    }

    // Kiểm tra lại cả ba điều kiện; việc so lịch do DangKyCaLam.KiemTraLich đảm nhận.
    public async Task KiemTra(DieuKienCa ca, HoSoTinhNguyenVien hoSo)
    {
        var (capDo, daXacMinh) = await KyNangCuaHoSo(hoSo.MaHoSo);
        if (!DuKyNang(ca.YeuCau, capDo, daXacMinh))
            throw LoiNghiepVu.XungDot("Tình nguyện viên không có đủ kỹ năng mà ca yêu cầu.");
        if (!ConCho(ca.SoDaPhanCong, ca.SoNguoiCan))
            throw LoiNghiepVu.XungDot("Ca đã đủ người, không thể phân công thêm.");
        if (await DangKyCaLam.KiemTraLich(_kho, hoSo.MaHoSo, ca))
            throw LoiNghiepVu.XungDot("Tình nguyện viên đã được phân công một ca khác trùng thời gian.");
    }
}
