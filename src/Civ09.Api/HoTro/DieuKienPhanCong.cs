using Civ09.Api.DuLieu;
using Civ09.Api.ThucThe;

namespace Civ09.Api.HoTro;

// Kiểm tra đủ kỹ năng, còn chỗ và không trùng lịch trước khi phân công (BR04, BR08, BR09).
public class DieuKienPhanCong
{
    private readonly KhoNghiepVu _kho;

    public DieuKienPhanCong(KhoNghiepVu kho) => _kho = kho;

    public static bool DuKyNang(IEnumerable<string> kyNangCuaCa, IEnumerable<string> kyNangCuaHoSo)
        => !kyNangCuaCa.Except(kyNangCuaHoSo).Any();

    public static bool ConCho(int soDaPhanCong, int soNguoiCan) => soDaPhanCong < soNguoiCan;

    // Kiểm tra lại cả ba điều kiện; việc so lịch do DangKyCaLam.KiemTraLich đảm nhận.
    public async Task KiemTra(DieuKienCa ca, HoSoTinhNguyenVien hoSo)
    {
        if (!DuKyNang(ca.KyNangYeuCau, hoSo.LayKyNang()))
            throw LoiNghiepVu.XungDot("Tình nguyện viên không có đủ kỹ năng mà ca yêu cầu.");
        if (!ConCho(ca.SoDaPhanCong, ca.SoNguoiCan))
            throw LoiNghiepVu.XungDot("Ca đã đủ người, không thể phân công thêm.");
        if (await DangKyCaLam.KiemTraLich(_kho, hoSo.MaHoSo, ca))
            throw LoiNghiepVu.XungDot("Tình nguyện viên đã được phân công một ca khác trùng thời gian.");
    }
}
