using Katana.Data.Configuration;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Options;
using Katana.API.Controllers;

namespace Katana.API.Workers;

public class AutoSyncWorker : BackgroundService
{
    private readonly ILogger<AutoSyncWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly IOptionsMonitor<SyncSettings> _syncSettings;

    public AutoSyncWorker(
        ILogger<AutoSyncWorker> logger,
        IServiceProvider services,
        IOptionsMonitor<SyncSettings> syncSettings)
    {
        _logger = logger;
        _services = services;
        _syncSettings = syncSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoSyncWorker başlatıldı");
        
        // İlk başlangıçta 10 saniye bekle (sistem ayaklanması için)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = _syncSettings.CurrentValue;
                var cachedSettings = SettingsController.GetCachedSettings();
                var autoSync = cachedSettings?.AutoSync ?? settings.EnableAutoSync;
                var interval = cachedSettings?.SyncInterval ?? settings.Stock.SyncIntervalMinutes;

                if (!autoSync)
                {
                    _logger.LogDebug("Otomatik senkronizasyon devre dışı");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                _logger.LogInformation("Otomatik senkronizasyon başlatılıyor (interval: {Interval} dakika)", interval);
                await RunSyncAsync(stoppingToken);
                
                _logger.LogInformation("Sonraki senkronizasyon {Interval} dakika sonra", interval);
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoSyncWorker hatası");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("AutoSyncWorker durduruluyor");
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            _logger.LogInformation("Otomatik senkronizasyon başlatılıyor...");
            
            await syncService.SyncStockAsync();
            
            _logger.LogInformation("Otomatik senkronizasyon tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Senkronizasyon çalıştırılırken hata oluştu");
        }
    }
}
