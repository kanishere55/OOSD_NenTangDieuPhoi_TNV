namespace Civ09.Api.HoTro;

// Lỗi do vi phạm quy tắc nghiệp vụ hoặc quyền. Mỗi lỗi mang mã HTTP và thông báo cho người dùng.
public class LoiNghiepVu : Exception
{
    public int MaHttp { get; }

    public LoiNghiepVu(int maHttp, string thongBao) : base(thongBao)
    {
        MaHttp = maHttp;
    }

    // 400: dữ liệu nhập không hợp lệ.
    public static LoiNghiepVu KhongHopLe(string thongBao) => new(StatusCodes.Status400BadRequest, thongBao);

    // 403: không có quyền thực hiện.
    public static LoiNghiepVu KhongDuQuyen(string thongBao = "Bạn không có quyền thực hiện thao tác này.")
        => new(StatusCodes.Status403Forbidden, thongBao);

    // 404: không tìm thấy dữ liệu.
    public static LoiNghiepVu KhongTimThay(string thongBao) => new(StatusCodes.Status404NotFound, thongBao);

    // 409: trạng thái hiện tại không cho phép thao tác.
    public static LoiNghiepVu XungDot(string thongBao) => new(StatusCodes.Status409Conflict, thongBao);
}
