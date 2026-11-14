using Civ09.Api.HoTro;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Lớp cơ sở của các Boundary: lấy người dùng đang đăng nhập từ phiên.
[ApiController]
public abstract class BoundaryCoSo : ControllerBase
{
    protected Task<NguoiDangNhap> NguoiDungAsync()
        => HttpContext.RequestServices.GetRequiredService<ChinhSachTruyCap>().LayNguoiDungAsync(User);
}
