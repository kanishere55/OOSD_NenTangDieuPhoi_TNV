using Civ09.Api.DichVu;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Civ09.Api.HoTro;

// Trao đổi với Keycloak theo OIDC (luồng Authorization Code có PKCE). Máy chủ ứng dụng đổi mã lấy token
// và giữ phiên bằng cookie, nên token không nằm trong mã chạy trên trình duyệt.
public static class CongOIDC
{
    public static void CauHinh(IServiceCollection services, IConfiguration cfg)
    {
        services.AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = "civ09_phien";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                // API trả mã 401/403 thay vì chuyển hướng, giao diện React tự xử lý.
                o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
                o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
            })
            .AddOpenIdConnect(o =>
            {
                o.Authority = cfg["Keycloak:Authority"];
                var metadata = cfg["Keycloak:MetadataAddress"];
                if (!string.IsNullOrEmpty(metadata)) o.MetadataAddress = metadata;
                o.ClientId = cfg["Keycloak:ClientId"];
                o.ClientSecret = cfg["Keycloak:ClientSecret"];
                o.RequireHttpsMetadata = cfg.GetValue("Keycloak:RequireHttpsMetadata", false);
                o.ResponseType = OpenIdConnectResponseType.Code;
                o.ResponseMode = OpenIdConnectResponseMode.Query;
                o.UsePkce = true;
                o.SaveTokens = true;
                o.MapInboundClaims = false;
                o.GetClaimsFromUserInfoEndpoint = false;
                o.Scope.Clear();
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.TokenValidationParameters.NameClaimType = "preferred_username";
                // Chạy thử trên máy bằng http nên cookie tạm của bước đăng nhập theo giao thức của yêu cầu.
                o.CorrelationCookie.SameSite = SameSiteMode.Lax;
                o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.NonceCookie.SameSite = SameSiteMode.Lax;
                o.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.SignedOutRedirectUri = "/";
                o.Events.OnTokenValidated = DoiMaVaKiemTra;
                o.Events.OnRemoteFailure = ctx =>
                {
                    // Phân biệt tài khoản chưa được cấp với các lỗi khác (hủy đăng nhập, phiên đăng nhập quá hạn).
                    var loi = ctx.Failure?.Message == LoiTaiKhoan ? "tai-khoan" : "dang-nhap";
                    ctx.Response.Redirect("/?loi=" + loi);
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
            });
    }

    private const string LoiTaiKhoan = "Tài khoản chưa được cấp trên nền tảng hoặc đã ngừng hoạt động.";

    // Tạo yêu cầu đăng nhập: chuyển người dùng sang trang đăng nhập của Keycloak.
    public static AuthenticationProperties TaoYeuCauDangNhap(string? quayVe)
        => new() { RedirectUri = string.IsNullOrEmpty(quayVe) || !quayVe.StartsWith('/') ? "/" : quayVe };

    // Sau khi đổi mã xác nhận lấy token và kiểm tra chữ ký, đối chiếu danh tính với tài khoản đã cấp.
    // Tài khoản chưa được cấp hoặc đã ngừng hoạt động thì không tạo phiên.
    private static async Task DoiMaVaKiemTra(TokenValidatedContext ctx)
    {
        var sub = ctx.Principal?.FindFirst(ChinhSachTruyCap.LoaiClaimDinhDanh)?.Value;
        var auth = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();
        if (!await auth.XuLyCallback(sub))
            ctx.Fail(LoiTaiKhoan);
    }
}
