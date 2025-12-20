using Katana.Core.Entities;
using Katana.Core.Utilities;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Mapping tabloları için sync tracking ve hash-based change detection
/// </summary>
public interface IMappingSyncService
{
    Task UpdateMappingSyncStatusAsync<T>(T mapping, string syncStatus, string? error = null) where T : class;
    Task<bool> HasMappingChangedAsync<T>(T mapping, string? lastHash) where T : class;
    Task MarkMappingAsSyncedAsync<T>(T mapping, string? hash = null) where T : class;
    Task MarkMappingAsFailedAsync<T>(T mapping, string error) where T : class;
    Task<List<T>> GetPendingMappingsAsync<T>() where T : class;
    Task<List<T>> GetFailedMappingsAsync<T>() where T : class;
}

public class MappingSyncService : IMappingSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<MappingSyncService> _logger;

    public MappingSyncService(IntegrationDbContext context, ILogger<MappingSyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Mapping'in sync durumunu güncelle
    /// </summary>
    public async Task UpdateMappingSyncStatusAsync<T>(T mapping, string syncStatus, string? error = null) where T : class
    {
        if (mapping == null) return;

        var properties = typeof(T).GetProperties();
        var syncStatusProp = properties.FirstOrDefault(p => p.Name == "SyncStatus");
        var lastSyncErrorProp = properties.FirstOrDefault(p => p.Name == "LastSyncError");
        var lastSyncAtProp = properties.FirstOrDefault(p => p.Name == "LastSyncAt");

        if (syncStatusProp?.CanWrite == true)
            syncStatusProp.SetValue(mapping, syncStatus);

        if (lastSyncErrorProp?.CanWrite == true)
            lastSyncErrorProp.SetValue(mapping, error);

        if (lastSyncAtProp?.CanWrite == true)
            lastSyncAtProp.SetValue(mapping, DateTime.UtcNow);

        _context.Update(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Mapping sync status updated: {Type} -> {Status}", typeof(T).Name, syncStatus);
    }

    /// <summary>
    /// Mapping'in değişip değişmediğini kontrol et
    /// </summary>
    public Task<bool> HasMappingChangedAsync<T>(T mapping, string? lastHash) where T : class
    {
        if (mapping == null) return Task.FromResult(false);

        var currentHash = MappingHashHelper.ComputeHash(mapping);
        var hasChanged = MappingHashHelper.HasChanged(lastHash, mapping);

        if (hasChanged)
        {
            _logger.LogInformation("Mapping change detected: {Type}, OldHash: {OldHash}, NewHash: {NewHash}",
                typeof(T).Name, lastHash ?? "null", currentHash);
        }

        return Task.FromResult(hasChanged);
    }

    /// <summary>
    /// Mapping'i SYNCED olarak işaretle
    /// </summary>
    public async Task MarkMappingAsSyncedAsync<T>(T mapping, string? hash = null) where T : class
    {
        if (mapping == null) return;

        var properties = typeof(T).GetProperties();
        var syncStatusProp = properties.FirstOrDefault(p => p.Name == "SyncStatus");
        var lastSyncHashProp = properties.FirstOrDefault(p => p.Name == "LastSyncHash");
        var lastSyncAtProp = properties.FirstOrDefault(p => p.Name == "LastSyncAt");
        var lastSyncErrorProp = properties.FirstOrDefault(p => p.Name == "LastSyncError");

        if (syncStatusProp?.CanWrite == true)
            syncStatusProp.SetValue(mapping, "SYNCED");

        if (lastSyncHashProp?.CanWrite == true)
            lastSyncHashProp.SetValue(mapping, hash ?? MappingHashHelper.ComputeHash(mapping));

        if (lastSyncAtProp?.CanWrite == true)
            lastSyncAtProp.SetValue(mapping, DateTime.UtcNow);

        if (lastSyncErrorProp?.CanWrite == true)
            lastSyncErrorProp.SetValue(mapping, null);

        _context.Update(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Mapping marked as synced: {Type}", typeof(T).Name);
    }

    /// <summary>
    /// Mapping'i FAILED olarak işaretle
    /// </summary>
    public async Task MarkMappingAsFailedAsync<T>(T mapping, string error) where T : class
    {
        if (mapping == null) return;

        var properties = typeof(T).GetProperties();
        var syncStatusProp = properties.FirstOrDefault(p => p.Name == "SyncStatus");
        var lastSyncErrorProp = properties.FirstOrDefault(p => p.Name == "LastSyncError");
        var lastSyncAtProp = properties.FirstOrDefault(p => p.Name == "LastSyncAt");

        if (syncStatusProp?.CanWrite == true)
            syncStatusProp.SetValue(mapping, "FAILED");

        if (lastSyncErrorProp?.CanWrite == true)
            lastSyncErrorProp.SetValue(mapping, error);

        if (lastSyncAtProp?.CanWrite == true)
            lastSyncAtProp.SetValue(mapping, DateTime.UtcNow);

        _context.Update(mapping);
        await _context.SaveChangesAsync();

        _logger.LogError("Mapping marked as failed: {Type}, Error: {Error}", typeof(T).Name, error);
    }

    /// <summary>
    /// PENDING durumundaki mapping'leri getir
    /// </summary>
    public async Task<List<T>> GetPendingMappingsAsync<T>() where T : class
    {
        return await _context.Set<T>()
            .Where(m => EF.Property<string>(m, "SyncStatus") == "PENDING")
            .ToListAsync();
    }

    /// <summary>
    /// FAILED durumundaki mapping'leri getir
    /// </summary>
    public async Task<List<T>> GetFailedMappingsAsync<T>() where T : class
    {
        return await _context.Set<T>()
            .Where(m => EF.Property<string>(m, "SyncStatus") == "FAILED")
            .ToListAsync();
    }
}
