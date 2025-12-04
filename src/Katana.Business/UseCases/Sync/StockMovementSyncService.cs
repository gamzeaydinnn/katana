using Katana.Business.Interfaces;
using Katana.Core.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.UseCases.Sync;

/// <summary>
/// Stok Transfer ve Adjustment Luca Senkronizasyon Servisi
/// Katana → Luca Depo Transferi (33) ve DSH Fişleri (150-153)
/// </summary>
public class StockMovementSyncService : IStockMovementSyncService
{
    private readonly IntegrationDbContext _dbContext;
    private readonly ILucaService _lucaService;
    private readonly IStockMovementMappingRepository _mappingRepo;
    private readonly ILogger<StockMovementSyncService> _logger;

    public StockMovementSyncService(
        IntegrationDbContext dbContext,
        ILucaService lucaService,
        IStockMovementMappingRepository mappingRepo,
        ILogger<StockMovementSyncService> logger)
    {
        _dbContext = dbContext;
        _lucaService = lucaService;
        _mappingRepo = mappingRepo;
        _logger = logger;
    }

    #region Query Methods

    /// <summary>
    /// Tüm stok hareketlerini listeler (Transfer + Adjustment)
    /// </summary>
    public async Task<List<StockMovementSyncDto>> GetAllMovementsAsync(StockMovementFilterDto? filter = null)
    {
        var result = new List<StockMovementSyncDto>();

        // Filtrelere göre transferleri al
        if (filter?.MovementType == null || filter.MovementType == "TRANSFER")
        {
            var transfers = await GetTransfersQueryable(filter).ToListAsync();
            result.AddRange(transfers);
        }

        // Filtrelere göre adjustment'ları al
        if (filter?.MovementType == null || filter.MovementType == "ADJUSTMENT")
        {
            var adjustments = await GetAdjustmentsQueryable(filter).ToListAsync();
            result.AddRange(adjustments);
        }

        return result.OrderByDescending(m => m.MovementDate).ToList();
    }

    /// <summary>
    /// Bekleyen transferleri listeler
    /// </summary>
    public async Task<List<StockMovementSyncDto>> GetPendingTransfersAsync()
    {
        return await GetTransfersQueryable(new StockMovementFilterDto { SyncStatus = "PENDING" })
            .ToListAsync();
    }

    /// <summary>
    /// Bekleyen adjustment'ları listeler
    /// </summary>
    public async Task<List<StockMovementSyncDto>> GetPendingAdjustmentsAsync()
    {
        return await GetAdjustmentsQueryable(new StockMovementFilterDto { SyncStatus = "PENDING" })
            .ToListAsync();
    }

    private IQueryable<StockMovementSyncDto> GetTransfersQueryable(StockMovementFilterDto? filter)
    {
        var query = _dbContext.StockTransfers
            .Include(t => t.Product)
            .AsQueryable();

        // Status filtresi
        if (!string.IsNullOrEmpty(filter?.SyncStatus))
        {
            var status = filter.SyncStatus switch
            {
                "PENDING" => "Pending",
                "SYNCED" => "Synced",
                "ERROR" => "Error",
                _ => filter.SyncStatus
            };
            query = query.Where(t => t.Status == status);
        }

        // Tarih filtresi
        if (filter?.StartDate.HasValue == true)
            query = query.Where(t => t.TransferDate >= filter.StartDate.Value);
        if (filter?.EndDate.HasValue == true)
            query = query.Where(t => t.TransferDate <= filter.EndDate.Value);

        return query.Select(t => new StockMovementSyncDto
        {
            Id = t.Id,
            DocumentNo = $"TRF-{t.Id:D6}",
            MovementType = "TRANSFER",
            LocationInfo = $"{t.FromWarehouse} → {t.ToWarehouse}",
            MovementDate = t.TransferDate,
            TotalQuantity = t.Quantity,
            SyncStatus = t.Status == "Synced" ? "SYNCED" : (t.Status == "Error" ? "ERROR" : "PENDING"),
            LucaDocumentId = null, // TODO: LucaTransferId alanı eklenebilir
            ErrorMessage = null,
            SyncedAt = null,
            Rows = new List<StockMovementRowDto>
            {
                new StockMovementRowDto
                {
                    Id = t.Id,
                    ProductCode = t.Product != null ? t.Product.SKU : "",
                    ProductName = t.Product != null ? t.Product.Name : "",
                    Quantity = t.Quantity,
                    UnitCost = null
                }
            }
        });
    }

    private IQueryable<StockMovementSyncDto> GetAdjustmentsQueryable(StockMovementFilterDto? filter)
    {
        var query = _dbContext.PendingStockAdjustments.AsQueryable();

        // Status filtresi
        if (!string.IsNullOrEmpty(filter?.SyncStatus))
        {
            var status = filter.SyncStatus switch
            {
                "PENDING" => "Pending",
                "SYNCED" => "Synced",
                "ERROR" => "Error",
                _ => filter.SyncStatus
            };
            query = query.Where(a => a.Status == status);
        }

        // Tarih filtresi
        if (filter?.StartDate.HasValue == true)
            query = query.Where(a => a.RequestedAt >= filter.StartDate.Value);
        if (filter?.EndDate.HasValue == true)
            query = query.Where(a => a.RequestedAt <= filter.EndDate.Value);

        return query.Select(a => new StockMovementSyncDto
        {
            Id = (int)a.Id,
            DocumentNo = $"ADJ-{a.Id:D6}",
            MovementType = "ADJUSTMENT",
            LocationInfo = "Ana Depo", // TODO: Location bilgisi eklenmeli
            MovementDate = a.RequestedAt.UtcDateTime,
            TotalQuantity = Math.Abs(a.Quantity),
            SyncStatus = a.Status == "Synced" ? "SYNCED" : (a.Status == "Error" ? "ERROR" : "PENDING"),
            LucaDocumentId = null,
            ErrorMessage = a.RejectionReason,
            SyncedAt = a.ApprovedAt.HasValue ? a.ApprovedAt.Value.UtcDateTime : null,
            AdjustmentReason = a.Quantity < 0 ? "Fire/Sarf" : "Sayım Fazlası",
            Rows = new List<StockMovementRowDto>
            {
                new StockMovementRowDto
                {
                    Id = (int)a.Id,
                    ProductCode = a.Sku ?? "",
                    ProductName = "",
                    Quantity = Math.Abs(a.Quantity),
                    UnitCost = null
                }
            }
        });
    }

    #endregion

    #region Sync Methods

    /// <summary>
    /// Tek bir stok transferini Luca'ya senkronize eder
    /// </summary>
    public async Task<MovementSyncResultDto> SyncTransferToLucaAsync(int transferId)
    {
        try
        {
            _logger.LogInformation("🔄 Transfer {TransferId} Luca'ya senkronize ediliyor...", transferId);

            var transfer = await _dbContext.StockTransfers
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
            {
                return new MovementSyncResultDto
                {
                    Success = false,
                    MovementId = transferId,
                    MovementType = "TRANSFER",
                    ErrorMessage = "Transfer bulunamadı"
                };
            }

            // Luca Depo Transfer Request oluştur
            var lucaRequest = new LucaStockTransferRequest
            {
                StkDepoTransferBaslik = new LucaTransferHeader
                {
                    BelgeSeri = "TRF",
                    BelgeTarihi = transfer.TransferDate,
                    BelgeAciklama = $"Katana Transfer #{transferId}",
                    BelgeTurDetayId = LucaStockMovementTypes.DepoTransferi,
                    
                    // Depo kodlarını normalize et (Luca formatı: "001")
                    CikisDepoKodu = NormalizeWarehouseCode(transfer.FromWarehouse),
                    GirisDepoKodu = NormalizeWarehouseCode(transfer.ToWarehouse),
                    
                    DetayList = new List<LucaStockMovementRow>
                    {
                        new LucaStockMovementRow
                        {
                            KartTuru = 1,
                            KartKodu = await _mappingRepo.GetLucaStokKoduByProductIdAsync(transfer.ProductId) ?? $"STK-{transfer.ProductId}",
                            KartAdi = transfer.Product?.Name,
                            Miktar = (double)transfer.Quantity
                        }
                    }
                }
            };

            // Luca API'ye gönder
            var lucaId = await _lucaService.CreateWarehouseTransferAsync(lucaRequest);

            if (lucaId > 0)
            {
                // Başarılı - Status güncelle
                await _mappingRepo.SaveLucaTransferIdAsync(transferId, lucaId);
                
                _logger.LogInformation("✅ Transfer {TransferId} Luca'ya aktarıldı: {LucaId}", transferId, lucaId);
                
                return new MovementSyncResultDto
                {
                    Success = true,
                    MovementId = transferId,
                    MovementType = "TRANSFER",
                    LucaDocumentId = lucaId
                };
            }
            else
            {
                throw new Exception("Luca API'den geçerli ID dönmedi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Transfer {TransferId} senkronizasyonu başarısız", transferId);
            
            // Hata durumunu kaydet
            var transfer = await _dbContext.StockTransfers.FindAsync(transferId);
            if (transfer != null)
            {
                transfer.Status = "Error";
                await _dbContext.SaveChangesAsync();
            }
            
            return new MovementSyncResultDto
            {
                Success = false,
                MovementId = transferId,
                MovementType = "TRANSFER",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Tek bir stok düzeltmesini Luca'ya senkronize eder
    /// </summary>
    public async Task<MovementSyncResultDto> SyncAdjustmentToLucaAsync(int adjustmentId)
    {
        try
        {
            _logger.LogInformation("🔄 Adjustment {AdjustmentId} Luca'ya senkronize ediliyor...", adjustmentId);

            var adjustment = await _dbContext.PendingStockAdjustments
                .FirstOrDefaultAsync(a => a.Id == adjustmentId);

            if (adjustment == null)
            {
                return new MovementSyncResultDto
                {
                    Success = false,
                    MovementId = adjustmentId,
                    MovementType = "ADJUSTMENT",
                    ErrorMessage = "Düzeltme kaydı bulunamadı"
                };
            }

            // Luca Belge Türünü Belirle (negatif = fire, pozitif = sayım fazlası)
            var lucaTurId = LucaStockMovementTypes.GetAdjustmentType(adjustment.Quantity, adjustment.Notes);

            // Luca DSH Request oluştur
            var lucaRequest = new LucaStockVoucherRequest
            {
                StkDshBaslik = new LucaVoucherHeader
                {
                    BelgeSeri = "DSH",
                    BelgeTarihi = adjustment.RequestedAt.UtcDateTime,
                    DepoKodu = "ANA", // TODO: Location mapping gerekli
                    BelgeAciklama = $"Katana Adj #{adjustmentId} - {adjustment.Notes}",
                    BelgeTurDetayId = lucaTurId,
                    
                    DetayList = new List<LucaStockMovementRow>
                    {
                        new LucaStockMovementRow
                        {
                            KartTuru = 1,
                            KartKodu = adjustment.Sku ?? await _mappingRepo.GetLucaStokKoduByProductIdAsync(adjustment.ProductId) ?? $"STK-{adjustment.ProductId}",
                            Miktar = Math.Abs(adjustment.Quantity), // Luca'ya her zaman pozitif gönder
                            BirimFiyat = 0 // TODO: Maliyet fiyatı eklenebilir
                        }
                    }
                }
            };

            // Luca API'ye gönder
            var lucaId = await _lucaService.CreateStockVoucherAsync(lucaRequest);

            if (lucaId > 0)
            {
                // Başarılı - Status güncelle
                await _mappingRepo.SaveLucaAdjustmentIdAsync(adjustmentId, lucaId);
                
                _logger.LogInformation("✅ Adjustment {AdjustmentId} Luca'ya aktarıldı: {LucaId}", adjustmentId, lucaId);
                
                return new MovementSyncResultDto
                {
                    Success = true,
                    MovementId = adjustmentId,
                    MovementType = "ADJUSTMENT",
                    LucaDocumentId = lucaId
                };
            }
            else
            {
                throw new Exception("Luca API'den geçerli ID dönmedi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Adjustment {AdjustmentId} senkronizasyonu başarısız", adjustmentId);
            
            // Hata durumunu kaydet
            var adjustment = await _dbContext.PendingStockAdjustments.FindAsync((long)adjustmentId);
            if (adjustment != null)
            {
                adjustment.Status = "Error";
                await _dbContext.SaveChangesAsync();
            }
            
            return new MovementSyncResultDto
            {
                Success = false,
                MovementId = adjustmentId,
                MovementType = "ADJUSTMENT",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Toplu senkronizasyon
    /// </summary>
    public async Task<MovementBatchSyncResultDto> SyncBatchAsync(List<int> transferIds, List<int> adjustmentIds)
    {
        var results = new List<MovementSyncResultDto>();

        // Transferleri senkronize et
        foreach (var id in transferIds)
        {
            var result = await SyncTransferToLucaAsync(id);
            results.Add(result);
        }

        // Adjustment'ları senkronize et
        foreach (var id in adjustmentIds)
        {
            var result = await SyncAdjustmentToLucaAsync(id);
            results.Add(result);
        }

        return new MovementBatchSyncResultDto
        {
            TotalCount = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            Results = results
        };
    }

    /// <summary>
    /// Bekleyen tüm hareketleri senkronize eder
    /// </summary>
    public async Task<MovementBatchSyncResultDto> SyncAllPendingAsync()
    {
        var pendingTransfers = await _dbContext.StockTransfers
            .Where(t => t.Status == "Pending")
            .Select(t => t.Id)
            .ToListAsync();

        var pendingAdjustments = await _dbContext.PendingStockAdjustments
            .Where(a => a.Status == "Pending" || a.Status == "Approved")
            .Select(a => (int)a.Id)
            .ToListAsync();

        return await SyncBatchAsync(pendingTransfers, pendingAdjustments);
    }

    #endregion

    #region Dashboard

    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    public async Task<MovementDashboardStatsDto> GetDashboardStatsAsync()
    {
        var transferStats = await _dbContext.StockTransfers
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var adjustmentStats = await _dbContext.PendingStockAdjustments
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var lastSync = await _dbContext.StockTransfers
            .Where(t => t.Status == "Synced")
            .OrderByDescending(t => t.TransferDate)
            .Select(t => (DateTime?)t.TransferDate)
            .FirstOrDefaultAsync();

        return new MovementDashboardStatsDto
        {
            TotalTransfers = transferStats.Sum(s => s.Count),
            PendingTransfers = transferStats.FirstOrDefault(s => s.Status == "Pending")?.Count ?? 0,
            SyncedTransfers = transferStats.FirstOrDefault(s => s.Status == "Synced")?.Count ?? 0,
            FailedTransfers = transferStats.FirstOrDefault(s => s.Status == "Error")?.Count ?? 0,
            
            TotalAdjustments = adjustmentStats.Sum(s => s.Count),
            PendingAdjustments = adjustmentStats.Where(s => s.Status == "Pending" || s.Status == "Approved").Sum(s => s.Count),
            SyncedAdjustments = adjustmentStats.FirstOrDefault(s => s.Status == "Synced")?.Count ?? 0,
            FailedAdjustments = adjustmentStats.FirstOrDefault(s => s.Status == "Error")?.Count ?? 0,
            
            LastSyncDate = lastSync
        };
    }

    #endregion

    /// <summary>Luca depo kodu formatına normalize et (002)</summary>
    private static string NormalizeWarehouseCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "002";
        var trimmed = code.Trim();
        if (trimmed.Contains('-'))
        {
            var parts = trimmed.Split('-');
            if (parts.Length >= 1 && int.TryParse(parts[0], out var num))
                return num.ToString("000");
        }
        if (int.TryParse(trimmed, out var single))
            return single.ToString("000");
        return trimmed;
    }
}
