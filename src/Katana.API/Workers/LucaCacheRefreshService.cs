using Katana.Business.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Katana.API.Workers;

/// <summary>
/// Luca stok kartı cache'ini periyodik olarak yeniler.
/// Basit, hafif bir BackgroundService (Quartz yok).
/// </summary>
public sealed class LucaCacheRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LucaCacheRefreshService> _logger;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    public LucaCacheRefreshService(IServiceScopeFactory scopeFactory, ILogger<LucaCacheRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk gecikme: uygulama tam ayağa kalksın
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch { }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();

                _logger.LogInformation("🔄 Luca cache refresh loop started");
                var warmed = await lucaService.WarmupCacheWithRetryAsync(2);
                var status = await lucaService.GetCacheStatusAsync();
                _logger.LogInformation("✅ Luca cache refresh completed (warmed={Warmed}, entries={Entries}, last={Last})",
                    warmed, status.EntryCount, status.LastWarmupUtc?.ToString("o") ?? "N/A");
            }
            catch (OperationCanceledException)
            {
                // stopping
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Luca cache refresh loop failed");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
