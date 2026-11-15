using Civ09.Api.HoTro;
using Civ09.Api.ThucThe;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Lớp cơ sở của các Boundary: lấy người dùng đang đăng nhập từ phiên.
[ApiController]
public abstract class BoundaryCoSo : ControllerBase
{
    protected Task<NguoiDangNhap> NguoiDungAsync()
        => HttpContext.RequestServices.GetRequiredService<ChinhSachTruyCap>().LayNguoiDungAsync(User);

    // Người dùng nếu đã đăng nhập, null nếu là khách.
    protected async Task<NguoiDangNhap?> NguoiDungNeuCoAsync()
        => User.Identity?.IsAuthenticated == true ? await NguoiDungAsync() : null;

    // Địa chỉ gốc của nền tảng theo yêu cầu hiện tại, dùng trong mã QR và giấy xác nhận.
    protected string DiaChiGoc => $"{Request.Scheme}://{Request.Host}";

    // Trả ảnh lưu trong CSDL. Ảnh riêng tư không được lưu đệm ở máy trung gian.
    protected IActionResult Anh(TepDinhKem tep, bool congKhai)
    {
        Response.Headers.CacheControl = congKhai ? "public, max-age=3600" : "private, no-store";
        return File(tep.NoiDung, tep.LoaiNoiDung);
    }
}
