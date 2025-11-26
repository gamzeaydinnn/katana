
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Monitoring;





public class MetricsExporter
{
    private readonly ILogger<MetricsExporter> _logger;
    private static readonly Meter _meter = new("Katana.Integration", "1.0.0");

    
    private static readonly Counter<int> SyncSuccessCounter = _meter.CreateCounter<int>("sync_success_total", "count", "Başarılı senkronizasyon operasyon sayısı");
    private static readonly Counter<int> SyncedRecordCounter = _meter.CreateCounter<int>("synced_records_total", "count", "Başarılı senkronizasyonlarda işlenen kayıt sayısı");
    private static readonly Counter<int> SyncFailedCounter = _meter.CreateCounter<int>("sync_failed_total", "count", "Başarısız senkronizasyon sayısı");
    private static readonly Counter<int> RetryCounter = _meter.CreateCounter<int>("sync_retry_total", "count", "Yeniden deneme sayısı");
    private static readonly Counter<int> ApiCallCounter = _meter.CreateCounter<int>("api_call_total", "count", "Yapılan dış API çağrısı sayısı");

    
    private static readonly ObservableGauge<double> SyncDurationGauge = _meter.CreateObservableGauge("sync_duration_seconds", GetLastSyncDurations, "seconds", "Son senkronizasyon süreleri");

    
    private static readonly ConcurrentDictionary<string, double> _lastSyncDurations = new();

    public MetricsExporter(ILogger<MetricsExporter> logger)
    {
        _logger = logger;
        _logger.LogInformation("MetricsExporter initialized and monitoring started.");
    }

    
    
    
    public void RecordSyncSuccess(string syncType, int recordCount, TimeSpan duration)
    {
        SyncSuccessCounter.Add(1, KeyValuePair.Create<string, object?>("syncType", syncType));
        SyncedRecordCounter.Add(recordCount, KeyValuePair.Create<string, object?>("syncType", syncType));
        _lastSyncDurations[syncType] = duration.TotalSeconds;

        _logger.LogInformation("✅ {SyncType} sync succeeded. {Count} records processed in {Seconds}s", syncType, recordCount, duration.TotalSeconds);
    }

    
    
    
    public void RecordSyncFailure(string syncType, string errorMessage)
    {
        SyncFailedCounter.Add(1, KeyValuePair.Create<string, object?>("syncType", syncType));
        _logger.LogWarning("❌ {SyncType} sync failed. Error: {Error}", syncType, errorMessage);
    }

    
    
    
    public void RecordRetry(string syncType)
    {
        RetryCounter.Add(1, KeyValuePair.Create<string, object?>("syncType", syncType));
        _logger.LogInformation("🔁 Retry scheduled for {SyncType}", syncType);
    }

    
    
    
    public void RecordApiCall(string apiName)
    {
        ApiCallCounter.Add(1, KeyValuePair.Create<string, object?>("api", apiName));
        _logger.LogDebug("🌐 API call recorded for {Api}", apiName);
    }

    
    
    
    private static IEnumerable<Measurement<double>> GetLastSyncDurations()
    {
        foreach (var kv in _lastSyncDurations)
        {
            yield return new Measurement<double>(kv.Value, KeyValuePair.Create<string, object?>("syncType", kv.Key));
        }
    }
}
