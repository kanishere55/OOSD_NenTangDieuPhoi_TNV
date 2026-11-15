namespace Civ09.Api.ThucThe;

// Một ảnh lưu trong CSDL. Nội dung đã được xóa thông tin đi kèm và thu nhỏ trước khi lưu (BR22).
public class TepDinhKem
{
    public string MaTep { get; set; } = Guid.NewGuid().ToString();
    public string TenTep { get; set; } = "";
    public string LoaiNoiDung { get; set; } = "image/jpeg";
    public int KichThuoc { get; set; }
    public int ChieuRong { get; set; }
    public int ChieuCao { get; set; }
    public byte[] NoiDung { get; set; } = Array.Empty<byte>();
    public DateTime NgayTai { get; set; }
    public string MaNguoiTai { get; set; } = "";
}

// Ảnh minh họa của nhu cầu, thuộc nội dung công khai.
public class AnhNhuCau
{
    public string MaNhuCau { get; set; } = "";
    public string MaTep { get; set; } = "";
    public byte ThuTu { get; set; }

    public TepDinhKem? Tep { get; set; }
}

// Ảnh minh chứng của bản khai đóng góp, chỉ người liên quan xem được (BR24).
public class AnhMinhChung
{
    public string MaDongGop { get; set; } = "";
    public string MaTep { get; set; } = "";

    public TepDinhKem? Tep { get; set; }
}
