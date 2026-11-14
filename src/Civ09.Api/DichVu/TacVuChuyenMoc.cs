namespace Civ09.Api.DichVu;

// Tác vụ nền chạy mỗi 30 giây, gọi CaService.ChuyenMocThoiGian() để chuyển trạng thái ca theo giờ.
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
                var kq = await scope.ServiceProvider.GetRequiredService<CaService>().ChuyenMocThoiGian();
                if (kq.SoCaBatDau + kq.SoCaKetThuc > 0)
                    _log.LogInformation("Chuyển mốc: {BatDau} ca bắt đầu, {KetThuc} ca kết thúc, {Huy} đăng ký chờ bị hủy",
                        kq.SoCaBatDau, kq.SoCaKetThuc, kq.SoDangKyBiHuy);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Tác vụ chuyển mốc thời gian gặp lỗi, sẽ thử lại ở lần sau");
            }
        } while (await nhip.WaitForNextTickAsync(stop));
    }
}
