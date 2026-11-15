namespace Civ09.Api.DichVu;

// Tác vụ nền chạy mỗi 30 giây: chuyển trạng thái ca theo giờ (BR12) và tạo thông báo nhắc trước ca 24 giờ (BR25).
public class TacVuChuyenMoc : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<TacVuChuyenMoc> _log;

    public TacVuChuyenMoc(IServiceScopeFactory scopes, ILogger<TacVuChuyenMoc> log)
    {
        _scopes = scopes; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var nhip = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var dv = scope.ServiceProvider.GetRequiredService<CaService>();
                var kq = await dv.ChuyenMocThoiGian();
                if (kq.SoCaBatDau + kq.SoCaKetThuc > 0)
                    _log.LogInformation("Chuyển mốc: {BatDau} ca bắt đầu, {KetThuc} ca kết thúc, {Huy} đăng ký chờ bị hủy",
                        kq.SoCaBatDau, kq.SoCaKetThuc, kq.SoDangKyBiHuy);
                var soNhac = await dv.TaoNhacTruocCa();
                if (soNhac > 0) _log.LogInformation("Đã tạo {SoNhac} thông báo nhắc trước ca", soNhac);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Tác vụ chuyển mốc thời gian gặp lỗi, sẽ thử lại ở lần sau");
            }
        } while (await nhip.WaitForNextTickAsync(stop));
    }
}
