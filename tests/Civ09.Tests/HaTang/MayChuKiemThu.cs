using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Civ09.Api.DuLieu;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Civ09.Tests.HaTang;

// Đồng hồ giả để kiểm thử các quy tắc theo giờ mà không phải chờ thật.
public class DongHoGia : TimeProvider
{
    public static readonly DateTimeOffset MocGoc = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset BayGio { get; set; } = MocGoc;
    public override DateTimeOffset GetUtcNow() => BayGio;
}

// Máy chủ ứng dụng chạy trong bộ kiểm thử, nối tới SQL Server thật trong Docker
// (CSDL riêng DieuPhoiTinhNguyenVien_KiemThu, dựng lại từ đúng các tệp lệnh trong thư mục database).
// Đăng nhập được giả lập bằng tiêu đề X-Nguoi-Dung để đổi vai trò nhanh.
public class MayChuKiemThu : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TenCsdl = "DieuPhoiTinhNguyenVien_KiemThu";
    public DongHoGia DongHo { get; } = new();
    private readonly string _ketNoiMaster;
    private readonly string _ketNoiQuanTri;
    private readonly string _matKhauUngDung;
    // Kết nối bằng tài khoản civ09_app, giống máy chủ ứng dụng khi chạy thật.
    public string KetNoiCsdl { get; }

    // Tên đăng nhập mẫu và mã danh tính tương ứng (trùng keycloak/civ09-realm.json).
    public static readonly Dictionary<string, string> DanhTinh = new()
    {
        ["daidien1"] = "aaaaaaaa-0000-4000-8000-000000000001",
        ["dieuphoi1"] = "aaaaaaaa-0000-4000-8000-000000000002",
        ["dieuphoi2"] = "aaaaaaaa-0000-4000-8000-000000000003",
        ["tnv1"] = "aaaaaaaa-0000-4000-8000-000000000004",
        ["tnv2"] = "aaaaaaaa-0000-4000-8000-000000000005",
        ["tnv3"] = "aaaaaaaa-0000-4000-8000-000000000006",
        ["dieuphoi_tnv"] = "aaaaaaaa-0000-4000-8000-000000000007",
    };

    public MayChuKiemThu()
    {
        var env = DocEnv();
        var matKhau = Environment.GetEnvironmentVariable("SQL_SA_PASSWORD") ?? env.GetValueOrDefault("SQL_SA_PASSWORD", "");
        var cong = Environment.GetEnvironmentVariable("SQL_PORT") ?? env.GetValueOrDefault("SQL_PORT", "14330");
        _matKhauUngDung = Environment.GetEnvironmentVariable("SQL_APP_PASSWORD") ?? env.GetValueOrDefault("SQL_APP_PASSWORD", "");
        _ketNoiMaster = $"Server=127.0.0.1,{cong};Database=master;User Id=sa;Password={matKhau};TrustServerCertificate=True";
        _ketNoiQuanTri = $"Server=127.0.0.1,{cong};Database={TenCsdl};User Id=sa;Password={matKhau};TrustServerCertificate=True";
        KetNoiCsdl = $"Server=127.0.0.1,{cong};Database={TenCsdl};User Id=civ09_app;Password={_matKhauUngDung};TrustServerCertificate=True";
    }

    public async Task InitializeAsync()
    {
        await using (var cn = new SqlConnection(_ketNoiMaster))
        {
            await cn.OpenAsync();
            await ChayAsync(cn, $"IF DB_ID('{TenCsdl}') IS NOT NULL BEGIN " +
                                $"ALTER DATABASE [{TenCsdl}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{TenCsdl}]; END");
            await ChayAsync(cn, $"CREATE DATABASE [{TenCsdl}] COLLATE Vietnamese_CI_AS");
        }
        await using (var cn = new SqlConnection(_ketNoiQuanTri))
        {
            await cn.OpenAsync();
            foreach (var tep in new[] { "01_tao_bang.sql", "02_du_lieu_mau.sql", "03_thu_tuc_va_trigger.sql", "04_tai_khoan_ung_dung.sql" })
            {
                var noiDung = (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", tep)))
                    .Replace("$(MatKhauUngDung)", _matKhauUngDung.Replace("'", "''"));
                foreach (var lo in System.Text.RegularExpressions.Regex.Split(noiDung, @"^\s*GO\s*$",
                             System.Text.RegularExpressions.RegexOptions.Multiline))
                    if (!string.IsNullOrWhiteSpace(lo)) await ChayAsync(cn, lo);
            }
        }
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    // Chạy lệnh SQL thẳng vào CSDL bằng tài khoản civ09_app (không qua ứng dụng).
    public async Task ChaySqlUngDungAsync(string sql, params (string Ten, object GiaTri)[] thamSo)
    {
        await using var cn = new SqlConnection(KetNoiCsdl);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        foreach (var (ten, giaTri) in thamSo) cmd.Parameters.AddWithValue(ten, giaTri);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ChayAsync(SqlConnection cn, string sql)
    {
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CsdlChinh", KetNoiCsdl);
        builder.UseSetting("TacVuNen:Bat", "false");
        builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning));
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<DbContextOptions<KhoNghiepVu>>();
            s.AddDbContext<KhoNghiepVu>(o => o.UseSqlServer(KetNoiCsdl));
            s.RemoveAll<TimeProvider>();
            s.AddSingleton<TimeProvider>(DongHo);
            foreach (var d in s.Where(d => d.ServiceType == typeof(IHostedService)).ToList()) s.Remove(d);
            s.AddAuthentication(o =>
            {
                o.DefaultScheme = XacThucKiemThu.TenCheDo;
                o.DefaultAuthenticateScheme = XacThucKiemThu.TenCheDo;
                o.DefaultChallengeScheme = XacThucKiemThu.TenCheDo;
                o.DefaultForbidScheme = XacThucKiemThu.TenCheDo;
            }).AddScheme<AuthenticationSchemeOptions, XacThucKiemThu>(XacThucKiemThu.TenCheDo, _ => { });
        });
    }

    // Tạo HttpClient đăng nhập dưới tên người dùng mẫu; null là khách chưa đăng nhập.
    public HttpClient MayKhach(string? tenDangNhap = null)
    {
        var c = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (tenDangNhap != null) c.DefaultRequestHeaders.Add(XacThucKiemThu.TieuDe, DanhTinh[tenDangNhap]);
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return c;
    }

    private static Dictionary<string, string> DocEnv()
    {
        var kq = new Dictionary<string, string>();
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; thuMuc != null && i < 8; i++, thuMuc = thuMuc.Parent)
        {
            var tep = Path.Combine(thuMuc.FullName, ".env");
            if (!File.Exists(tep)) continue;
            foreach (var dong in File.ReadAllLines(tep))
            {
                var d = dong.Trim();
                if (d.Length == 0 || d.StartsWith('#') || !d.Contains('=')) continue;
                kq[d[..d.IndexOf('=')].Trim()] = d[(d.IndexOf('=') + 1)..].Trim();
            }
            break;
        }
        return kq;
    }
}

// Giả lập đăng nhập: đọc mã danh tính (sub) từ tiêu đề X-Nguoi-Dung.
public class XacThucKiemThu : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TenCheDo = "KiemThu";
    public const string TieuDe = "X-Nguoi-Dung";

    public XacThucKiemThu(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TieuDe, out var sub) || string.IsNullOrEmpty(sub))
            return Task.FromResult(AuthenticateResult.NoResult());
        var id = new ClaimsIdentity(new[] { new Claim("sub", sub.ToString()) }, TenCheDo);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), TenCheDo)));
    }
}

[CollectionDefinition(Ten)]
public class NhomKiemThuTichHop : ICollectionFixture<MayChuKiemThu>
{
    public const string Ten = "Kiểm thử tích hợp với SQL Server";
}
