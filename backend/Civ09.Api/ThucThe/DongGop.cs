using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.ThucThe;

// Bản khai đóng góp của một đăng ký đã phân công.
public class DongGop
{
    public const string CHO_DUYET = "CHO_DUYET";
    public const string CAN_BO_SUNG = "CAN_BO_SUNG";
    public const string DA_XAC_NHAN = "DA_XAC_NHAN";

    public string MaDongGop { get; set; } = Guid.NewGuid().ToString();
    public string MaDangKy { get; set; } = "";
    public DateTime BatDauThucTe { get; set; }
    public DateTime KetThucThucTe { get; set; }
    public bool GioTheoDiemDanh { get; set; }
    public string MoTaCongViec { get; set; } = "";
    public string TrangThai { get; set; } = CHO_DUYET;
    public string? LyDoTraLai { get; set; }
    public string? MaNguoiXacNhan { get; set; }
    public DateTime? NgayXacNhan { get; set; }

    public DangKyCaLam? DangKy { get; set; }
    public List<AnhMinhChung> Anh { get; set; } = new();

    // BR24: ảnh minh chứng chỉ thêm, bỏ khi bản khai chưa được xác nhận.
    public bool ConSuaAnhDuoc => TrangThai is CHO_DUYET or CAN_BO_SUNG;

    // Bản khai của một đăng ký (có theo dõi thay đổi), nếu đã có.
    public static Task<DongGop?> LayBanKhai(KhoNghiepVu kho, string maDangKy)
        => kho.DongGop.FirstOrDefaultAsync(g => g.MaDangKy == maDangKy);

    // Bản khai theo mã, kèm đăng ký và hồ sơ của người khai.
    public static async Task<DongGop> LayBanKhaiTheoMa(KhoNghiepVu kho, string maDongGop)
        => await kho.DongGop.Include(g => g.DangKy!).ThenInclude(d => d.HoSo)
               .FirstOrDefaultAsync(g => g.MaDongGop == maDongGop)
           ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy bản khai.");

    // BR13, BR23: khi có đủ giờ vào và ra từ điểm danh thì thời gian thực tế lấy theo điểm danh.
    public static DongGop TaoBanKhai(DangKyCaLam dangKy, DieuKienCa ca, DateTime batDau, DateTime ketThuc, string moTa,
        DiemDanh? diemDanh = null)
    {
        var dg = new DongGop { MaDangKy = dangKy.MaDangKy };
        dg.GanNoiDung(ca, batDau, ketThuc, moTa, diemDanh);
        return dg;
    }

    // BR14: chỉ sửa bản đang cần bổ sung rồi gửi lại.
    public void GuiLai(DieuKienCa ca, DateTime batDau, DateTime ketThuc, string moTa, DiemDanh? diemDanh = null)
    {
        if (TrangThai != CAN_BO_SUNG)
            throw LoiNghiepVu.XungDot("Chỉ sửa và gửi lại bản khai đang cần bổ sung.");
        GanNoiDung(ca, batDau, ketThuc, moTa, diemDanh);
        TrangThai = CHO_DUYET;
    }

    public void XacNhan(string maNguoiXacNhan, DateTime thoiDiem)
    {
        if (TrangThai != CHO_DUYET)
            throw LoiNghiepVu.XungDot("Chỉ duyệt bản khai đang chờ duyệt.");
        TrangThai = DA_XAC_NHAN;
        MaNguoiXacNhan = maNguoiXacNhan;
        NgayXacNhan = thoiDiem;
    }

    public void TraLai(string? lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
            throw LoiNghiepVu.KhongHopLe("Khi trả lại bản khai phải ghi lý do.");
        if (TrangThai != CHO_DUYET)
            throw LoiNghiepVu.XungDot("Chỉ trả lại bản khai đang chờ duyệt.");
        TrangThai = CAN_BO_SUNG;
        LyDoTraLai = lyDo.Trim();
    }

    // Số giờ tính từ thời gian thực tế, người khai không tự nhập (BR13).
    public decimal TinhSoGio() => TinhSoGio(BatDauThucTe, KetThucThucTe);

    public static decimal TinhSoGio(DateTime batDau, DateTime ketThuc)
        => (decimal)(ketThuc - batDau).TotalSeconds / 3600m;

    // BR13: thời gian thực tế nằm trong khung giờ của ca và kết thúc sau bắt đầu.
    public static void KiemTraThoiGian(DateTime batDauCa, DateTime ketThucCa, DateTime batDau, DateTime ketThuc)
    {
        if (ketThuc <= batDau)
            throw LoiNghiepVu.KhongHopLe("Giờ kết thúc thực tế phải sau giờ bắt đầu.");
        if (batDau < batDauCa || ketThuc > ketThucCa)
            throw LoiNghiepVu.KhongHopLe("Thời gian thực tế phải nằm trong khung giờ của ca.");
    }

    private void GanNoiDung(DieuKienCa ca, DateTime batDau, DateTime ketThuc, string moTa, DiemDanh? diemDanh)
    {
        if (diemDanh?.KetQua == DiemDanh.VANG_MAT)
            throw LoiNghiepVu.XungDot("Bạn đã được ghi nhận vắng mặt ở ca này nên không khai đóng góp được. Nếu có nhầm lẫn, hãy liên hệ điều phối viên.");
        var theoDiemDanh = diemDanh?.KhoangThoiGianTrongCa(ca.BatDauDuKien, ca.KetThucDuKien);
        if (theoDiemDanh != null) (batDau, ketThuc) = theoDiemDanh.Value;
        GioTheoDiemDanh = theoDiemDanh != null;
        KiemTraThoiGian(ca.BatDauDuKien, ca.KetThucDuKien, batDau, ketThuc);
        if (string.IsNullOrWhiteSpace(moTa))
            throw LoiNghiepVu.KhongHopLe("Hãy mô tả công việc đã làm.");
        BatDauThucTe = batDau;
        KetThucThucTe = ketThuc;
        MoTaCongViec = moTa.Trim();
    }
}

// Dòng của view CaCongKhai: chỉ gồm các trường được phép công khai (BR01).
public class CaCongKhai
{
    public string MaCaLam { get; set; } = "";
    public string MaNhuCau { get; set; } = "";
    public string TenToChuc { get; set; } = "";
    public decimal? ViDoVung { get; set; }
    public decimal? KinhDoVung { get; set; }
    public int? BanKinhVung { get; set; }
    public string TieuDe { get; set; } = "";
    public string TomTatCongKhai { get; set; } = "";
    public string KhuVucChung { get; set; } = "";
    public DateTime BatDauDuKien { get; set; }
    public DateTime KetThucDuKien { get; set; }
    public int SoNguoiCan { get; set; }
    public int SoDaPhanCong { get; set; }
}
