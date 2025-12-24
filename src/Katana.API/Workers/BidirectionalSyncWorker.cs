using Katana.Business.Services;

namespace Katana.API.Workers;

/// <summary>
/// Otomatik iki yonlu senkronizasyon:
/// - Her 30 dakikada bir Luca -> Katana
/// - Her 30 dakikada bir Katana -> Luca
/// </summary>
public class BidirectionalSyncWorker : BackgroundService
{
    private readonly ILogger<BidirectionalSyncWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public BidirectionalSyncWorker(
        ILogger<BidirectionalSyncWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BidirectionalSyncWorker baslatildi. Iki yonlu senkronizasyon: {Interval} dakika", _interval.TotalMinutes);

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("========================================");
                _logger.LogInformation("Iki yonlu senkronizasyon dongusu basliyor...");
                _logger.LogInformation("========================================");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var syncService = scope.ServiceProvider.GetRequiredService<BidirectionalSyncService>();
                    var sinceDate = DateTime.UtcNow.Subtract(_interval);

                    _logger.LogInformation("[1/2] Luca -> Katana senkronizasyonu basliyor...");
                    try
                    {
                        var lucaToKatana = await syncService.SyncFromLucaToKatanaAsync(
                            sinceDate: sinceDate,
                            maxCount: 100);

                        if (lucaToKatana.SuccessCount > 0)
                        {
                            _logger.LogInformation(
                                "Luca -> Katana BASARILI: {SuccessCount} guncelleme, {SkippedCount} atla, {FailCount} hata",
                                lucaToKatana.SuccessCount,
                                lucaToKatana.SkippedCount,
                                lucaToKatana.FailCount);

                            foreach (var product in lucaToKatana.UpdatedProducts)
                            {
                                _logger.LogInformation(
                                    "  OK {SKU}: {Changes}",
                                    product.SKU,
                                    string.Join(", ", product.Changes));
                            }
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Luca -> Katana: Guncellenecek urun yok ({SkippedCount} kontrol edildi)",
                                lucaToKatana.SkippedCount);
                        }

                        if (lucaToKatana.FailCount > 0)
                        {
                            _logger.LogWarning("Luca -> Katana: {FailCount} hata", lucaToKatana.FailCount);
                            foreach (var error in lucaToKatana.Errors)
                            {
                                _logger.LogWarning("  ERR {SKU}: {Error}", error.ProductSku, error.ErrorMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Luca -> Katana senkronizasyon hatasi");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                    _logger.LogInformation("[2/2] Katana -> Luca senkronizasyonu basliyor...");
                    try
                    {
                        var katanaToLuca = await syncService.SyncFromKatanaToLucaAsync(
                            sinceDate: sinceDate,
                            maxCount: 100);

                        if (katanaToLuca.SuccessCount > 0)
                        {
                            _logger.LogInformation(
                                "Katana -> Luca BASARILI: {SuccessCount} guncelleme, {SkippedCount} atla, {FailCount} hata",
                                katanaToLuca.SuccessCount,
                                katanaToLuca.SkippedCount,
                                katanaToLuca.FailCount);

                            foreach (var product in katanaToLuca.UpdatedProducts)
                            {
                                _logger.LogInformation(
                                    "  OK {SKU}: {Changes}",
                                    product.SKU,
                                    string.Join(", ", product.Changes));
                            }
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Katana -> Luca: Guncellenecek urun yok ({SkippedCount} kontrol edildi)",
                                katanaToLuca.SkippedCount);
                        }

                        if (katanaToLuca.FailCount > 0)
                        {
                            _logger.LogWarning("Katana -> Luca: {FailCount} hata", katanaToLuca.FailCount);
                            foreach (var error in katanaToLuca.Errors)
                            {
                                _logger.LogWarning("  ERR {SKU}: {Error}", error.ProductSku, error.ErrorMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Katana -> Luca senkronizasyon hatasi");
                    }

                    _logger.LogInformation("========================================");
                    _logger.LogInformation("Iki yonlu senkronizasyon dongusu tamamlandi");
                    _logger.LogInformation("Sonraki dongu: {NextRun}", DateTime.UtcNow.Add(_interval).ToString("HH:mm:ss"));
                    _logger.LogInformation("========================================");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BidirectionalSyncWorker genel hatasi");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("BidirectionalSyncWorker durduruluyor...");
    }
}
