using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Katana.Business.DTOs.Sync;
using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Katana.Infrastructure.Workers
{
    /// <summary>
    /// Background worker for processing sync operations
    /// Supports parallel batch processing with retry policies
    /// </summary>
    public class SyncWorker : ISyncWorker
    {
        private readonly ILucaService _lucaService;
        private readonly IKatanaService _katanaService;
        private readonly IStockCardCache _stockCardCache;
        private readonly IntegrationDbContext _dbContext;
        private readonly ILogger<SyncWorker> _logger;

        // Parallel processing configuration
        private const int BATCH_SIZE = 20;
        private const int MAX_DEGREE_OF_PARALLELISM = 5;
        private const int MAX_RETRY_ATTEMPTS = 5; // ✅ Updated from 3 to 5 retries

        // Retry policy for failed batches
        private readonly AsyncRetryPolicy _retryPolicy;

        public SyncWorker(
            ILucaService lucaService,
            IKatanaService katanaService,
            IStockCardCache stockCardCache,
            IntegrationDbContext dbContext,
            ILogger<SyncWorker> logger)
        {
            _lucaService = lucaService ?? throw new ArgumentNullException(nameof(lucaService));
            _katanaService = katanaService ?? throw new ArgumentNullException(nameof(katanaService));
            _stockCardCache = stockCardCache ?? throw new ArgumentNullException(nameof(stockCardCache));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    MAX_RETRY_ATTEMPTS,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "⚠️ Retry {RetryCount}/{MaxRetries} after {Delay}s: {Error}",
                            retryCount, MAX_RETRY_ATTEMPTS, timeSpan.TotalSeconds, exception.Message);
                    });
        }

        public async Task<SyncResultDto> ProcessStockCardsAsync(int? limit = null, bool dryRun = false)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("🚀 Starting stock card sync job (limit={Limit}, dryRun={DryRun})", 
                limit, dryRun);

            var result = new SyncResultDto
            {
                IsDryRun = dryRun
            };

            // ✅ Create SyncOperationLog entry
            var operationLog = new SyncOperationLog
            {
                SyncType = "PRODUCT",
                Status = "InProgress",
                StartTime = DateTime.UtcNow,
                ProcessedRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0,
                ErrorMessage = null,
                Details = null
            };
            
            try
            {
                _dbContext.SyncOperationLogs.Add(operationLog);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("📝 Created SyncOperationLog entry (Id={OperationId})", operationLog.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to create SyncOperationLog entry, continuing...");
            }

            try
            {
                // Step 1: Warmup cache with retry
                _logger.LogInformation("🔥 Step 1/4: Warming up Redis cache...");
                var cacheWarmed = await _lucaService.WarmupCacheWithRetryAsync(MAX_RETRY_ATTEMPTS);
                if (!cacheWarmed)
                {
                    throw new Exception("Failed to warm up stock card cache after retries");
                }

                var cacheStatus = await _stockCardCache.GetCacheStatusAsync();
                _logger.LogInformation("✅ Cache ready: {Status}", cacheStatus.Status);

                // Step 2: Fetch Katana products
                _logger.LogInformation("📦 Step 2/4: Fetching products from Katana...");
                var allProductDtos = await _katanaService.GetProductsAsync();
                
                // Convert DTOs to Products (map relevant fields)
                var allProducts = allProductDtos.Select((dto, index) => new Product
                {
                    Id = index + 1, // Generate sequential ID for DTO without integer ID
                    SKU = dto.SKU ?? "",
                    Name = dto.Name ?? "",
                    SalesPrice = dto.SalesPrice ?? dto.Price,
                    Stock = dto.Available ?? dto.OnHand ?? 0
                }).ToList();
                
                var productsToSync = limit.HasValue
                    ? allProducts.Take(limit.Value).ToList()
                    : allProducts;

                _logger.LogInformation("📊 Found {Total} products, processing {Count}",
                    allProducts.Count, productsToSync.Count);

                result.TotalProcessed = productsToSync.Count;

                if (!productsToSync.Any())
                {
                    result.Success = true;
                    result.Message = "No products to sync";
                    result.Duration = stopwatch.Elapsed;
                    
                    // Update operation log
                    operationLog.Status = "Success";
                    operationLog.EndTime = DateTime.UtcNow;
                    operationLog.ProcessedRecords = 0;
                    operationLog.SuccessfulRecords = 0;
                    operationLog.FailedRecords = 0;
                    operationLog.Details = "No products to sync";
                    try { await _dbContext.SaveChangesAsync(); } catch { }
                    
                    return result;
                }

                // Step 3: Process in parallel batches
                _logger.LogInformation("⚡ Step 3/4: Processing {Count} products in parallel batches (size={BatchSize}, parallelism={Parallelism})",
                    productsToSync.Count, BATCH_SIZE, MAX_DEGREE_OF_PARALLELISM);

                var batchResults = await ProcessProductsInParallelBatchesAsync(productsToSync, dryRun);

                result.SuccessCount = batchResults.SuccessCount;
                result.FailedCount = batchResults.FailedCount;
                result.SkippedCount = batchResults.SkippedCount;
                result.Errors = batchResults.Errors;

                // Step 4: Summary
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = result.FailedCount == 0;
                result.Message = $"Sync completed: {result.SuccessCount} success, {result.FailedCount} failed, {result.SkippedCount} skipped in {result.Duration.TotalSeconds:F1}s";

                _logger.LogInformation("✅ {Message}", result.Message);
                
                // ✅ Update operation log with final results
                operationLog.Status = result.Success ? "Success" : "PartialSuccess";
                operationLog.EndTime = DateTime.UtcNow;
                operationLog.ProcessedRecords = result.TotalProcessed;
                operationLog.SuccessfulRecords = result.SuccessCount;
                operationLog.FailedRecords = result.FailedCount;
                operationLog.Details = result.Message;
                operationLog.ErrorMessage = result.Errors.Any() ? string.Join("; ", result.Errors.Take(3)) : null;
                
                try
                {
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("📝 Updated SyncOperationLog (Id={OperationId})", operationLog.Id);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ Failed to update SyncOperationLog");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Message = $"Sync job failed: {ex.Message}";
                result.Errors.Add(ex.ToString());

                _logger.LogError(ex, "❌ Stock card sync job failed");
                
                // ✅ Update operation log with error
                operationLog.Status = "Failed";
                operationLog.EndTime = DateTime.UtcNow;
                operationLog.ErrorMessage = ex.Message;
                operationLog.Details = ex.ToString();
                
                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ Failed to update SyncOperationLog with error");
                }
                
                return result;
            }
        }

        private async Task<BatchProcessingResult> ProcessProductsInParallelBatchesAsync(
            List<Product> products,
            bool dryRun)
        {
            // Split products into batches
            var batches = products
                .Select((product, index) => new { product, index })
                .GroupBy(x => x.index / BATCH_SIZE)
                .Select(g => g.Select(x => x.product).ToList())
                .ToList();

            _logger.LogInformation("📦 Created {BatchCount} batches from {ProductCount} products",
                batches.Count, products.Count);

            // Thread-safe counters
            var successCount = 0;
            var failedCount = 0;
            var skippedCount = 0;
            var errors = new ConcurrentBag<string>();

            // Process batches in parallel with max degree of parallelism
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MAX_DEGREE_OF_PARALLELISM
            };

            await Parallel.ForEachAsync(
                batches.Select((batch, index) => (batch, index)),
                parallelOptions,
                async (item, cancellationToken) =>
                {
                    var (batch, batchIndex) = item;
                    var batchNumber = batchIndex + 1;

                    try
                    {
                        _logger.LogInformation("🔄 Batch {BatchNumber}/{TotalBatches}: Processing {Count} products...",
                            batchNumber, batches.Count, batch.Count);

                        var batchResult = await _retryPolicy.ExecuteAsync(async () =>
                            await ProcessSingleBatchAsync(batch, batchNumber, dryRun));

                        // Thread-safe increment
                        System.Threading.Interlocked.Add(ref successCount, batchResult.SuccessCount);
                        System.Threading.Interlocked.Add(ref failedCount, batchResult.FailedCount);
                        System.Threading.Interlocked.Add(ref skippedCount, batchResult.SkippedCount);

                        foreach (var error in batchResult.Errors)
                        {
                            errors.Add(error);
                        }

                        _logger.LogInformation("✅ Batch {BatchNumber}/{TotalBatches}: {Success} success, {Failed} failed",
                            batchNumber, batches.Count, batchResult.SuccessCount, batchResult.FailedCount);
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Interlocked.Add(ref failedCount, batch.Count);
                        var errorMsg = $"Batch {batchNumber} failed after retries: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(ex, "❌ Batch {BatchNumber} failed permanently", batchNumber);
                    }
                });

            return new BatchProcessingResult
            {
                SuccessCount = successCount,
                FailedCount = failedCount,
                SkippedCount = skippedCount,
                Errors = errors.ToList()
            };
        }

        private async Task<BatchProcessingResult> ProcessSingleBatchAsync(
            List<Product> batch,
            int batchNumber,
            bool dryRun)
        {
            var result = new BatchProcessingResult();

            foreach (var product in batch)
            {
                try
                {
                    if (dryRun)
                    {
                        _logger.LogDebug("🔍 DRY RUN: Would sync product {Sku} - {Name}",
                            product.SKU, product.Name);
                        result.SuccessCount++;
                        continue;
                    }

                    // Check cache first
                    var cachedStockCardId = await _stockCardCache.GetStockCardIdAsync(product.SKU);

                    if (cachedStockCardId.HasValue)
                    {
                        // Update existing stock card
                        var updateSuccess = await _lucaService.UpdateStockCardAsync(
                            cachedStockCardId.Value,
                            product);

                        if (updateSuccess)
                        {
                            result.SuccessCount++;
                            _logger.LogDebug("✅ Updated: {Sku} → StockCardId={Id}",
                                product.SKU, cachedStockCardId.Value);
                        }
                        else
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Failed to update {product.SKU}");
                        }
                    }
                    else
                    {
                        // Create new stock card
                        var newStockCardId = await _lucaService.CreateStockCardAsync(product);

                        if (newStockCardId.HasValue)
                        {
                            // Cache the new mapping
                            await _stockCardCache.SetStockCardIdAsync(product.SKU, newStockCardId.Value);

                            result.SuccessCount++;
                            _logger.LogDebug("✅ Created: {Sku} → StockCardId={Id}",
                                product.SKU, newStockCardId.Value);
                        }
                        else
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Failed to create {product.SKU}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    var errorMsg = $"Product {product.SKU}: {ex.Message}";
                    result.Errors.Add(errorMsg);
                    _logger.LogError(ex, "❌ Error processing product {Sku}", product.SKU);
                }
            }

            return result;
        }

        public Task<SyncResultDto> ProcessCustomersAsync(int? limit = null, bool dryRun = false)
        {
            _logger.LogInformation("🚀 Starting customer sync job (limit={Limit}, dryRun={DryRun})", limit, dryRun);

            // TODO: Implement customer sync logic
            return Task.FromResult(new SyncResultDto
            {
                Success = true,
                Message = "Customer sync not yet implemented",
                IsDryRun = dryRun
            });
        }

        public Task<SyncResultDto> ProcessInvoicesAsync(int? limit = null, bool dryRun = false)
        {
            _logger.LogInformation("🚀 Starting invoice sync job (limit={Limit}, dryRun={DryRun})", limit, dryRun);

            // TODO: Implement invoice sync logic
            return Task.FromResult(new SyncResultDto
            {
                Success = true,
                Message = "Invoice sync not yet implemented",
                IsDryRun = dryRun
            });
        }

        // ✅ FIX CS1998: Changed async Task to Task (no await operators)
        public Task<SyncProgressDto> GetJobProgressAsync(string jobId)
        {
            // TODO: Implement job progress tracking via Hangfire API
            return Task.FromResult(new SyncProgressDto
            {
                JobId = jobId,
                Status = "Unknown",
                ProcessedCount = 0,
                TotalCount = 0
            });
        }

        private class BatchProcessingResult
        {
            public int SuccessCount { get; set; }
            public int FailedCount { get; set; }
            public int SkippedCount { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
}
