using Civ09.Api.HoTro;

namespace Civ09.Api.ThucThe;

// Thông báo trên web gửi tới một người dùng (BR25).
public class ThongBao
{
    public string MaThongBao { get; set; } = Guid.NewGuid().ToString();
    public string MaNguoiNhan { get; set; } = "";
    public string Loai { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string? DuongDan { get; set; }
    public string? MaThamChieu { get; set; }
    public DateTime NgayTao { get; set; }
    public bool DaDoc { get; set; }
    public DateTime? NgayDoc { get; set; }

    public static ThongBao Tao(string maNguoiNhan, string loai, string noiDung, string? duongDan, string? maThamChieu,
        DateTime thoiDiem)
        => new()
        {
            MaNguoiNhan = maNguoiNhan,
            Loai = loai,
            NoiDung = noiDung.Length > 500 ? noiDung[..497] + "..." : noiDung,
            DuongDan = duongDan,
            MaThamChieu = maThamChieu,
            NgayTao = thoiDiem
        };

    public void DanhDauDaDoc(string maNguoiDoc, DateTime thoiDiem)
    {
        if (maNguoiDoc != MaNguoiNhan) throw LoiNghiepVu.KhongDuQuyen("Đây không phải thông báo của bạn.");
        if (DaDoc) return;
        DaDoc = true;
        NgayDoc = thoiDiem;
    }
}

// Một lần xem dữ liệu nhạy cảm (BR26). Chỉ thêm, không sửa, không xóa.
public class NhatKyTruyCap
{
    public const string HUONG_DAN_RIENG = "HUONG_DAN_RIENG";
    public const string ANH_MINH_CHUNG = "ANH_MINH_CHUNG";
    public const string CHUNG_CHI = "CHUNG_CHI";

    public long MaNhatKy { get; set; }
    public string MaNguoiXem { get; set; } = "";
    public string LoaiDuLieu { get; set; } = "";
    public string MaDoiTuong { get; set; } = "";
    public string? MaToChuc { get; set; }
    public DateTime ThoiDiem { get; set; }
    public bool DuocPhep { get; set; }

    public NguoiDung? NguoiXem { get; set; }
}

// Giấy xác nhận giờ tình nguyện (BR29). Số liệu được chốt lúc cấp giấy.
public class GiayXacNhan
{
    public string MaGiay { get; set; } = Guid.NewGuid().ToString();
    public string MaHoSo { get; set; } = "";
    public string MaKiemTra { get; set; } = "";
    public string TenNguoiNhan { get; set; } = "";
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public int SoCa { get; set; }
    public long TongSoGiay { get; set; }
    public DateTime NgayCap { get; set; }

    public List<GiayXacNhanDongGop> DongGop { get; set; } = new();

    public static GiayXacNhan Cap(string maHoSo, string tenNguoiNhan, DateOnly? tuNgay, DateOnly? denNgay,
        IReadOnlyCollection<(string MaDongGop, long SoGiay)> banDaXacNhan, string maKiemTra, DateTime thoiDiem)
    {
        if (tuNgay != null && denNgay != null && tuNgay > denNgay)
            throw LoiNghiepVu.KhongHopLe("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        if (banDaXacNhan.Count == 0)
            throw LoiNghiepVu.XungDot("Trong khoảng thời gian này bạn chưa có bản khai nào được xác nhận.");
        var g = new GiayXacNhan
        {
            MaHoSo = maHoSo, TenNguoiNhan = tenNguoiNhan, TuNgay = tuNgay, DenNgay = denNgay,
            SoCa = banDaXacNhan.Count, TongSoGiay = banDaXacNhan.Sum(b => b.SoGiay),
            MaKiemTra = maKiemTra, NgayCap = thoiDiem
        };
        foreach (var b in banDaXacNhan) g.DongGop.Add(new GiayXacNhanDongGop { MaGiay = g.MaGiay, MaDongGop = b.MaDongGop });
        return g;
    }
}

public class GiayXacNhanDongGop
{
    public string MaGiay { get; set; } = "";
    public string MaDongGop { get; set; } = "";

    public DongGop? DongGop { get; set; }
}
