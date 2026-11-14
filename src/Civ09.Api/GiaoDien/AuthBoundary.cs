using Civ09.Api.HoTro;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Civ09.Api.GiaoDien;

// Đăng nhập, đăng xuất và thông tin phiên (UC01).
[Route("api/auth")]
public class AuthBoundary : BoundaryCoSo
{
    // Chuyển sang trang đăng nhập của Keycloak.
    [HttpGet("dang-nhap")]
    public IActionResult DangNhap([FromQuery] string? quayVe)
        => Challenge(CongOIDC.TaoYeuCauDangNhap(quayVe), OpenIdConnectDefaults.AuthenticationScheme);

    // Xóa phiên trên nền tảng và trên Keycloak.
    [HttpGet("dang-xuat")]
    public IActionResult DangXuat()
        => SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);

    // Thông tin người đang đăng nhập; 401 nếu chưa đăng nhập.
    [HttpGet("toi")]
    public async Task<IActionResult> Toi()
    {
        var nd = await NguoiDungAsync();
        return Ok(new
        {
            nd.MaNguoiDung, nd.TenHienThi,
            VaiTro = nd.VaiTro.OrderBy(v => v),
            ThanhVien = nd.ThanhVien.Select(t => new { t.MaToChuc, t.TenToChuc, t.VaiTro }),
            LaTinhNguyenVien = nd.MaHoSo != null
        });
    }
}
