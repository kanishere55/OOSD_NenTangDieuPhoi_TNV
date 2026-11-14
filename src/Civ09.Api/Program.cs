using System.Text.Encodings.Web;
using System.Text.Json;
using Civ09.Api.DichVu;
using Civ09.Api.DuLieu;
using Civ09.Api.HoTro;
using Microsoft.EntityFrameworkCore;

// Khi chạy trực tiếp từ mã nguồn, đọc tệp .env ở thư mục gốc của repo (cùng tệp với docker compose).
DocTepEnv.Nap();

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Chuỗi kết nối dựng từ .env nếu chưa được cấu hình sẵn. Ứng dụng dùng tài khoản civ09_app, không dùng sa.
if (string.IsNullOrEmpty(cfg.GetConnectionString("CsdlChinh")))
{
    var matKhau = Environment.GetEnvironmentVariable("SQL_APP_PASSWORD");
    var cong = Environment.GetEnvironmentVariable("SQL_PORT") ?? "14330";
    cfg["ConnectionStrings:CsdlChinh"] =
        $"Server=127.0.0.1,{cong};Database=DieuPhoiTinhNguyenVien;User Id=civ09_app;Password={matKhau};TrustServerCertificate=True";
}
if (string.IsNullOrEmpty(cfg["Keycloak:ClientSecret"]))
    cfg["Keycloak:ClientSecret"] = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET");

builder.Services.AddDbContext<KhoNghiepVu>(o => o.UseSqlServer(cfg.GetConnectionString("CsdlChinh")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<GiaoDich>();
builder.Services.AddScoped<ChinhSachTruyCap>();
builder.Services.AddScoped<DieuKienPhanCong>();
builder.Services.AddScoped<BoLocCongKhai>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<HoSoService>();
builder.Services.AddScoped<NhuCauService>();
builder.Services.AddScoped<CaService>();
builder.Services.AddScoped<DieuPhoiService>();
builder.Services.AddScoped<DongGopService>();
builder.Services.AddScoped<ChiTietService>();
if (cfg.GetValue("TacVuNen:Bat", true))
    builder.Services.AddHostedService<TacVuChuyenMoc>();

CongOIDC.CauHinh(builder.Services, cfg);
builder.Services.AddAuthorization();
// Giữ nguyên tiếng Việt trong JSON thay vì mã hóa \uXXXX.
var tuyChonJson = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

var app = builder.Build();

// Đổi LoiNghiepVu thành mã HTTP kèm thông báo cho giao diện.
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (LoiNghiepVu loi) when (!ctx.Response.HasStarted)
    {
        ctx.Response.StatusCode = loi.MaHttp;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { loi = loi.Message }, tuyChonJson));
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Giao diện React đã build (khi chạy trong Docker) nằm ở wwwroot.
app.MapFallbackToFile("index.html");

app.Run();

// Đọc các dòng KHOA=GIA_TRI trong .env vào biến môi trường nếu biến đó chưa có.
static class DocTepEnv
{
    public static void Nap()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);
        for (var cap = 0; thuMuc != null && cap < 8; cap++, thuMuc = thuMuc.Parent)
        {
            var tep = Path.Combine(thuMuc.FullName, ".env");
            if (!File.Exists(tep)) continue;
            foreach (var dong in File.ReadAllLines(tep))
            {
                var d = dong.Trim();
                if (d.Length == 0 || d.StartsWith('#') || !d.Contains('=')) continue;
                var i = d.IndexOf('=');
                var khoa = d[..i].Trim();
                if (Environment.GetEnvironmentVariable(khoa) == null)
                    Environment.SetEnvironmentVariable(khoa, d[(i + 1)..].Trim());
            }
            return;
        }
    }
}

public partial class Program { }
