using Katana.Business.Interfaces;
using Katana.Business.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Katana.Data.Context;

namespace Katana.Business.UseCases.Sync
{
    public class SyncService : ISyncService
    {
        private readonly IExtractorService _extractor;
        private readonly ITransformerService _transformer;
        private readonly ILoaderService _loader;
        private readonly IntegrationDbContext _db;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            IExtractorService extractor,
            ITransformerService transformer,
            ILoaderService loader,
            IntegrationDbContext db,
            ILogger<SyncService> logger)
        {
            _extractor = extractor;
            _transformer = transformer;
            _loader = loader;
            _db = db;
            _logger = logger;
        }

        public async Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null)
        {
            _logger.LogInformation("ETL: STOCK senkronizasyonu başladı.");

            var dtos = await _extractor.ExtractProductsAsync();
            var entities = await _transformer.ToProductsAsync(dtos);
            var loaded = await _loader.LoadProductsAsync(entities);

            _logger.LogInformation("ETL: STOCK senkronizasyonu tamamlandı. Yüklenen kayıt: {Count}", loaded);

            return new SyncResultDto
            {
                SyncType = "STOCK",
                IsSuccess = true,
                ProcessedRecords = dtos.Count,
                SuccessfulRecords = loaded,
                FailedRecords = dtos.Count - loaded,
                Message = "Stock sync completed"
            };
        }

        public async Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null)
        {
            _logger.LogInformation("ETL: INVOICE senkronizasyonu başladı.");

            var dtos = await _extractor.ExtractInvoicesAsync();
            var entities = await _transformer.ToInvoicesAsync(dtos);
            var loaded = await _loader.LoadInvoicesAsync(entities);

            _logger.LogInformation("ETL: INVOICE senkronizasyonu tamamlandı. {Count} kayıt yüklendi.", loaded);

            return new SyncResultDto
            {
                SyncType = "INVOICE",
                IsSuccess = true,
                ProcessedRecords = dtos.Count,
                SuccessfulRecords = loaded,
                FailedRecords = dtos.Count - loaded,
                Message = "Invoice sync completed"
            };
        }

        public async Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null)
        {
            _logger.LogInformation("ETL: CUSTOMER senkronizasyonu başladı.");

            var dtos = await _extractor.ExtractCustomersAsync();
            var entities = await _transformer.ToCustomersAsync(dtos);
            var loaded = await _loader.LoadCustomersAsync(entities);

            _logger.LogInformation("ETL: CUSTOMER senkronizasyonu tamamlandı. {Count} kayıt yüklendi.", loaded);

            return new SyncResultDto
            {
                SyncType = "CUSTOMER",
                IsSuccess = true,
                ProcessedRecords = dtos.Count,
                SuccessfulRecords = loaded,
                FailedRecords = dtos.Count - loaded,
                Message = "Customer sync completed"
            };
        }

        public async Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null)
        {
            _logger.LogInformation("ETL: Toplu senkronizasyon başlatılıyor...");

            var results = new List<SyncResultDto>
            {
                await SyncCustomersAsync(fromDate),
                await SyncStockAsync(fromDate),
                await SyncInvoicesAsync(fromDate)
            };

            return new BatchSyncResultDto
            {
                BatchTime = DateTime.UtcNow,
                Results = results
            };
        }

        public async Task<List<SyncStatusDto>> GetSyncStatusAsync()
        {
            var statuses = new List<SyncStatusDto>
            {
                new() { SyncType = "STOCK", CurrentStatus = "OK", LastSyncTime = DateTime.UtcNow },
                new() { SyncType = "INVOICE", CurrentStatus = "OK", LastSyncTime = DateTime.UtcNow },
                new() { SyncType = "CUSTOMER", CurrentStatus = "OK", LastSyncTime = DateTime.UtcNow }
            };
            return await Task.FromResult(statuses);
        }
    }
}
