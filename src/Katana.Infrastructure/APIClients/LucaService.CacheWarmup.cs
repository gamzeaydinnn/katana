using System;
using System.Threading;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Business.Mappers;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Cache warmup and helper methods for Luca stock card operations.
/// </summary>
public partial class LucaService : ILucaService
{
    private DateTime? _lastCacheWarmupUtc;

    public async Task<bool> WarmupCacheWithRetryAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
        {
            try
            {
                await EnsureAuthenticatedAsync();
                await EnsureBranchSelectedAsync();
                _lastCacheWarmupUtc = DateTime.UtcNow;
                _logger.LogInformation("✅ Luca cache warmup noop completed (attempt {Attempt}/{Max})", attempt, maxAttempts);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Luca cache warmup attempt {Attempt}/{Max} failed", attempt, maxAttempts);
                if (attempt == maxAttempts) break;
                try { await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken); } catch { }
            }
        }

        return false;
    }

    public Task<LucaCacheStatusDto> GetCacheStatusAsync()
    {
        var entryCount = _stockCardCache?.Count ?? 0;
        var status = entryCount > 0 ? "Warm" : "Empty";

        var dto = new LucaCacheStatusDto
        {
            IsWarm = entryCount > 0,
            EntryCount = entryCount,
            LastWarmupUtc = _lastCacheWarmupUtc,
            Status = status
        };

        return Task.FromResult(dto);
    }

    public async Task<bool> UpdateStockCardAsync(long stockCardId, Product product)
    {
        _logger.LogInformation("ℹ️ Luca API does not support stock card updates; performing upsert for SKU {Sku} (existingId={Id})", product.SKU, stockCardId);
        var createdId = await CreateStockCardAsync(product);
        return createdId.HasValue;
    }

    public async Task<long?> CreateStockCardAsync(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        var request = KatanaToLucaMapper.MapProductToStockCard(product);
        var upsertResult = await UpsertStockCardAsync(request);

        if (!upsertResult.IsSuccess && upsertResult.DuplicateRecords == 0)
            return null;

        var skartId = await FindStockCardBySkuAsync(product.SKU);
        if (skartId.HasValue && _stockCardCache != null)
        {
            await _stockCardCacheLock.WaitAsync();
            try
            {
                _stockCardCache[product.SKU] = skartId.Value;
            }
            finally
            {
                _stockCardCacheLock.Release();
            }
        }

        return skartId;
    }
}
