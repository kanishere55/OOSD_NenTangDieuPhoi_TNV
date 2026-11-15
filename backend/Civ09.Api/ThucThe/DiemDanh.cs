using Civ09.Api.HoTro;

namespace Civ09.Api.ThucThe;

// Điểm danh của một đăng ký đã phân công: giờ vào, giờ ra hoặc vắng mặt (BR23, BR27).
public class DiemDanh
{
    public const string CO_MAT = "CO_MAT";
    public const string VANG_MAT = "VANG_MAT";
    public const string QR = "QR";
    public const string DIEU_PHOI = "DIEU_PHOI";

    // Được điểm danh vào từ 30 phút trước giờ bắt đầu; điểm danh ra chậm nhất 2 giờ sau giờ kết thúc.
    public static readonly TimeSpan SomTruocCa = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan TreSauCa = TimeSpan.FromHours(2);
    // Điểm danh ra cách giờ vào ít nhất 5 phút, tránh quét hai lần liền nhau.
    public static readonly TimeSpan CachToiThieu = TimeSpan.FromMinutes(5);

    public string MaDangKy { get; set; } = "";
    public string KetQua { get; set; } = CO_MAT;
    public DateTime? GioVao { get; set; }
    public DateTime? GioRa { get; set; }
    public string CachGhi { get; set; } = QR;
    public string? MaNguoiGhiNhan { get; set; }
    public DateTime NgayCapNhat { get; set; }

    // Tình nguyện viên quét mã QR tại ca: lần đầu là vào, lần sau là ra. Trả về true nếu là lần vào.
    public static bool QuetMa(DangKyCaLam dk, CaLam ca, ref DiemDanh? dd, DateTime bayGio)
    {
        if (dk.TrangThai != DangKyCaLam.DA_PHAN_CONG)
            throw LoiNghiepVu.XungDot("Chỉ người đã được phân công vào ca mới điểm danh được.");
        if (ca.TrangThai == CaLam.DA_HUY) throw LoiNghiepVu.XungDot("Ca đã bị hủy.");
        if (dd == null)
        {
            if (bayGio < ca.BatDauDuKien - SomTruocCa)
                throw LoiNghiepVu.XungDot("Chưa đến giờ điểm danh. Bạn điểm danh được từ 30 phút trước giờ bắt đầu ca.");
            if (bayGio > ca.KetThucDuKien)
                throw LoiNghiepVu.XungDot("Ca đã kết thúc nên không điểm danh vào được nữa. Hãy báo điều phối viên ghi nhận giúp.");
            dd = new DiemDanh
            {
                MaDangKy = dk.MaDangKy, KetQua = CO_MAT, GioVao = bayGio, CachGhi = QR, NgayCapNhat = bayGio
            };
            return true;
        }
        if (dd.KetQua == VANG_MAT)
            throw LoiNghiepVu.XungDot("Bạn đang được ghi nhận vắng mặt ở ca này. Hãy báo điều phối viên nếu có nhầm lẫn.");
        if (dd.GioRa != null)
            throw LoiNghiepVu.XungDot("Bạn đã điểm danh vào và ra cho ca này rồi.");
        if (bayGio - dd.GioVao!.Value < CachToiThieu)
            throw LoiNghiepVu.XungDot("Bạn vừa điểm danh vào. Khi làm xong, hãy quét lại mã để điểm danh ra.");
        if (bayGio > ca.KetThucDuKien + TreSauCa)
            throw LoiNghiepVu.XungDot("Đã quá 2 giờ sau giờ kết thúc ca nên không điểm danh ra được. Bạn vẫn khai được giờ làm thực tế.");
        dd.GioRa = bayGio;
        dd.NgayCapNhat = bayGio;
        return false;
    }

    // Điều phối viên ghi nhận có mặt, dùng khi người tham gia không quét được mã.
    public static DiemDanh GhiNhanCoMat(DangKyCaLam dk, CaLam ca, DiemDanh? dd, string maNguoiGhi,
        DateTime gioVao, DateTime? gioRa, DateTime bayGio)
    {
        KiemTraGhiNhan(dk, ca, bayGio, ca.BatDauDuKien - SomTruocCa);
        if (gioVao < ca.BatDauDuKien - SomTruocCa || gioVao > ca.KetThucDuKien)
            throw LoiNghiepVu.KhongHopLe("Giờ vào phải từ 30 phút trước giờ bắt đầu đến giờ kết thúc của ca.");
        if (gioRa != null && (gioRa <= gioVao || gioRa > ca.KetThucDuKien + TreSauCa))
            throw LoiNghiepVu.KhongHopLe("Giờ ra phải sau giờ vào và không quá 2 giờ sau giờ kết thúc ca.");
        if (gioVao > bayGio || (gioRa != null && gioRa > bayGio))
            throw LoiNghiepVu.KhongHopLe("Không ghi nhận giờ ở tương lai.");
        dd ??= new DiemDanh { MaDangKy = dk.MaDangKy };
        dd.KetQua = CO_MAT;
        dd.GioVao = gioVao;
        dd.GioRa = gioRa;
        dd.CachGhi = DIEU_PHOI;
        dd.MaNguoiGhiNhan = maNguoiGhi;
        dd.NgayCapNhat = bayGio;
        return dd;
    }

    // Điều phối viên ghi nhận vắng mặt, chỉ sau khi ca đã bắt đầu.
    public static DiemDanh GhiNhanVang(DangKyCaLam dk, CaLam ca, DiemDanh? dd, string maNguoiGhi, DateTime bayGio)
    {
        KiemTraGhiNhan(dk, ca, bayGio, ca.BatDauDuKien);
        dd ??= new DiemDanh { MaDangKy = dk.MaDangKy };
        dd.KetQua = VANG_MAT;
        dd.GioVao = null;
        dd.GioRa = null;
        dd.CachGhi = DIEU_PHOI;
        dd.MaNguoiGhiNhan = maNguoiGhi;
        dd.NgayCapNhat = bayGio;
        return dd;
    }

    private static void KiemTraGhiNhan(DangKyCaLam dk, CaLam ca, DateTime bayGio, DateTime tuLuc)
    {
        if (dk.TrangThai != DangKyCaLam.DA_PHAN_CONG)
            throw LoiNghiepVu.XungDot("Chỉ ghi nhận điểm danh cho người đã được phân công.");
        if (ca.TrangThai == CaLam.DA_HUY) throw LoiNghiepVu.XungDot("Ca đã bị hủy.");
        if (bayGio < tuLuc)
            throw LoiNghiepVu.XungDot(tuLuc == ca.BatDauDuKien
                ? "Chỉ ghi nhận vắng mặt sau khi ca đã bắt đầu."
                : "Chưa đến giờ điểm danh của ca.");
        if (dk.DongGop != null)
            throw LoiNghiepVu.XungDot("Người này đã khai đóng góp cho ca nên không đổi điểm danh được nữa.");
    }

    // Khoảng thời gian có mặt nằm trong khung giờ của ca, dùng làm thời gian thực tế của bản khai.
    public (DateTime BatDau, DateTime KetThuc)? KhoangThoiGianTrongCa(DateTime batDauCa, DateTime ketThucCa)
    {
        if (KetQua != CO_MAT || GioVao == null || GioRa == null) return null;
        var a = GioVao.Value < batDauCa ? batDauCa : GioVao.Value;
        var b = GioRa.Value > ketThucCa ? ketThucCa : GioRa.Value;
        return b > a ? (a, b) : null;
    }
}
