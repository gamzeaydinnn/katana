using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Katana.Business.Services
{
    public class PendingStockAdjustmentService : IPendingStockAdjustmentService
    {
        private readonly IntegrationDbContext _context;
        private readonly ILogger<PendingStockAdjustmentService> _logger;
        private readonly Katana.Core.Interfaces.IPendingNotificationPublisher? _publisher;
        private readonly ISyncService? _syncService;
        private readonly IKatanaService? _katanaService;
        private readonly ILucaService? _lucaService;

        public PendingStockAdjustmentService(
            IntegrationDbContext context, 
            ILogger<PendingStockAdjustmentService> logger, 
            Katana.Core.Interfaces.IPendingNotificationPublisher? publisher = null,
            ISyncService? syncService = null,
            IKatanaService? katanaService = null,
            ILucaService? lucaService = null)
        {
            _context = context;
            _logger = logger;
            _publisher = publisher;
            _syncService = syncService;
            _katanaService = katanaService;
            _lucaService = lucaService;
        }

        public async Task<PendingStockAdjustment> CreateAsync(PendingStockAdjustment creation)
        {
            creation.RequestedAt = creation.RequestedAt == default ? DateTimeOffset.UtcNow : creation.RequestedAt;
            creation.Status = "Pending";
            _context.PendingStockAdjustments.Add(creation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pending stock adjustment created (Id: {Id}, ProductId: {ProductId}, Quantity: {Qty})", creation.Id, creation.ProductId, creation.Quantity);

            
            try
            {
                if (_publisher != null)
                {
                    var evt = new Katana.Core.Events.PendingStockAdjustmentCreatedEvent(creation.Id, creation.ExternalOrderId, creation.Sku, creation.Quantity, creation.RequestedBy, creation.RequestedAt);
                    await _publisher.PublishPendingCreatedAsync(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish pending-created notification for PendingStockAdjustment {Id}", creation.Id);
            }

            return creation;
        }

        public async Task<IEnumerable<PendingStockAdjustment>> GetAllAsync()
        {
            return await _context.PendingStockAdjustments.OrderByDescending(p => p.RequestedAt).ToListAsync();
        }

        public async Task<PendingStockAdjustment?> GetByIdAsync(long id)
        {
            return await _context.PendingStockAdjustments.FindAsync(id);
        }

        public async Task<bool> ApproveAsync(long id, string approvedBy)
        {
            
            
            if (!_context.Database.IsRelational())
            {
                try
                {
                    var item = await _context.PendingStockAdjustments.FindAsync(id);
                    if (item == null || item.Status != "Pending")
                    {
                        _logger.LogWarning("Pending stock adjustment {Id} could not be approved in non-relational mode: not found or not pending", id);
                        return false;
                    }

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        item.Status = "Failed";
                        item.RejectionReason = "Associated product was not found";
                        item.ApprovedBy = approvedBy;
                        item.ApprovedAt = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync();
                        return false;
                    }

                    
                    var movement = new Katana.Core.Entities.StockMovement
                    {
                        ProductId = item.ProductId,
                        ProductSku = item.Sku ?? string.Empty,
                        ChangeQuantity = item.Quantity,
                        MovementType = item.Quantity == 0 ? Katana.Core.Enums.MovementType.Adjustment : (item.Quantity > 0 ? Katana.Core.Enums.MovementType.In : Katana.Core.Enums.MovementType.Out),
                        SourceDocument = item.ExternalOrderId ?? "PendingAdjustment",
                        Timestamp = DateTime.UtcNow,
                        WarehouseCode = "MAIN",
                        IsSynced = false
                    };
                    _context.StockMovements.Add(movement);

                    
                    var stockEntry = new Stock
                    {
                        ProductId = item.ProductId,
                        Location = "MAIN",
                        Quantity = Math.Abs(item.Quantity),
                        Type = item.Quantity == 0 ? "ADJUSTMENT" : item.Quantity < 0 ? "OUT" : "IN",
                        Reason = !string.IsNullOrWhiteSpace(item.Notes) ? item.Notes : $"Pending adjustment approved by {approvedBy}",
                        Reference = item.ExternalOrderId ?? string.Empty,
                        Timestamp = DateTime.UtcNow,
                        IsSynced = false
                    };
                    _context.Stocks.Add(stockEntry);

                    item.Status = "Approved";
                    item.ApprovedBy = approvedBy;
                    item.ApprovedAt = DateTimeOffset.UtcNow;
                    item.RejectionReason = null;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pending stock adjustment {Id} approved by {User} (non-relational)", id, approvedBy);

                    // Luca'ya stok kartı senkronizasyonu tetikle
                    await TriggerLucaSyncAsync(item.Sku, "approve-non-relational");

                    try
                    {
                        if (_publisher != null)
                        {
                            var approvedEvent = new Katana.Core.Events.PendingStockAdjustmentApprovedEvent(item.Id, item.ExternalOrderId, item.Sku, item.Quantity, item.ApprovedBy, item.ApprovedAt ?? DateTimeOffset.UtcNow);
                            await _publisher.PublishPendingApprovedAsync(approvedEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish pending-approved notification for PendingStockAdjustment {Id}", id);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to approve pending stock adjustment {Id} in non-relational mode", id);
                    var item = await _context.PendingStockAdjustments.FindAsync(id);
                    if (item != null)
                    {
                        item.Status = "Failed";
                        item.RejectionReason = ex.Message;
                        await _context.SaveChangesAsync();
                    }
                    return false;
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                var success = false;
                string? failureReason = null;

                await strategy.ExecuteAsync(async () =>
                {
                    
                    var rows = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE PendingStockAdjustments SET Status = {"Approving"} WHERE Id = {id} AND Status = {"Pending"}");
                    if (rows == 0)
                    {
                        
                        failureReason = "Adjustment not in pending state or already processed";
                        return;
                    }

                    await using var tx = await _context.Database.BeginTransactionAsync();

                    
                    var item = await _context.PendingStockAdjustments.FindAsync(id);
                    if (item == null)
                    {
                        failureReason = "Pending adjustment not found after claiming";
                        await tx.CommitAsync();
                        return;
                    }

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        failureReason = "Associated product was not found";
                        item.Status = "Failed";
                        item.RejectionReason = failureReason;
                        item.ApprovedBy = approvedBy;
                        item.ApprovedAt = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        return;
                    }

                    
                    var movementRel = new Katana.Core.Entities.StockMovement
                    {
                        ProductId = item.ProductId,
                        ProductSku = item.Sku ?? string.Empty,
                        ChangeQuantity = item.Quantity,
                        MovementType = item.Quantity == 0
                            ? Katana.Core.Enums.MovementType.Adjustment
                            : item.Quantity > 0 ? Katana.Core.Enums.MovementType.In : Katana.Core.Enums.MovementType.Out,
                        SourceDocument = item.ExternalOrderId ?? "PendingAdjustment",
                        Timestamp = DateTime.UtcNow,
                        WarehouseCode = "MAIN",
                        IsSynced = false
                    };
                    _context.StockMovements.Add(movementRel);

                    
                    var stockEntryRel = new Stock
                    {
                        ProductId = item.ProductId,
                        Location = "MAIN",
                        Quantity = Math.Abs(item.Quantity),
                        Type = item.Quantity == 0
                            ? "ADJUSTMENT"
                            : item.Quantity < 0 ? "OUT" : "IN",
                        Reason = !string.IsNullOrWhiteSpace(item.Notes)
                            ? item.Notes
                            : $"Pending adjustment approved by {approvedBy}",
                        Reference = item.ExternalOrderId ?? string.Empty,
                        Timestamp = DateTime.UtcNow,
                        IsSynced = false
                    };
                    _context.Stocks.Add(stockEntryRel);

                    item.Status = "Approved";
                    item.ApprovedBy = approvedBy;
                    item.ApprovedAt = DateTimeOffset.UtcNow;
                    item.RejectionReason = null;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    _logger.LogInformation("Pending stock adjustment {Id} approved by {User}", id, approvedBy);
                    success = true;

                    // Luca'ya stok kartı senkronizasyonu tetikle
                    await TriggerLucaSyncAsync(item.Sku, "approve-relational");

                    try
                    {
                        if (_publisher != null)
                        {
                            var approvedEvent = new Katana.Core.Events.PendingStockAdjustmentApprovedEvent(item.Id, item.ExternalOrderId, item.Sku, item.Quantity, item.ApprovedBy, item.ApprovedAt ?? DateTimeOffset.UtcNow);
                            await _publisher.PublishPendingApprovedAsync(approvedEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish pending-approved notification for PendingStockAdjustment {Id}", id);
                    }
                });

                if (!success && failureReason != null)
                {
                    _logger.LogWarning("Pending stock adjustment {Id} could not be approved: {Reason}", id, failureReason);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve pending stock adjustment {Id}", id);
                
                var item = await _context.PendingStockAdjustments.FindAsync(id);
                if (item != null)
                {
                    item.Status = "Failed";
                    item.RejectionReason = ex.Message;
                    await _context.SaveChangesAsync();
                }
                return false;
            }
        }

        public async Task<bool> RejectAsync(long id, string rejectedBy, string? reason = null)
        {
            var item = await _context.PendingStockAdjustments.FindAsync(id);
            if (item == null || item.Status != "Pending") return false;

            item.Status = "Rejected";
            item.RejectionReason = reason;
            item.ApprovedBy = rejectedBy;
            item.ApprovedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Pending stock adjustment {Id} rejected by {User}: {Reason}", id, rejectedBy, reason);
            return true;
        }

        /// <summary>
        /// Onay sonrası tüm senkronizasyonları tetikler:
        /// 1. Luca'ya stok kartı oluşturma/güncelleme
        /// 2. Luca'ya stok hareketi gönderme
        /// 3. Katana'ya stok güncelleme
        /// </summary>
        private async Task TriggerLucaSyncAsync(string? sku, string source)
        {
            _logger.LogInformation("Starting full sync after pending approval. SKU: {SKU}, Source: {Source}", sku, source);

            // 1. Luca'ya stok kartı senkronizasyonu
            await SyncStockCardToLucaAsync(sku);

            // 2. Luca'ya stok hareketi gönder
            await SyncStockMovementToLucaAsync(sku);

            // 3. Katana'ya stok güncelleme
            await SyncStockToKatanaAsync(sku);

            // 4. IsSynced flag'lerini güncelle
            await MarkMovementsAsSyncedAsync(sku);
        }

        /// <summary>
        /// Luca'ya stok kartı senkronizasyonu - değişen ürünler yeni kart olarak oluşturulur
        /// </summary>
        private async Task SyncStockCardToLucaAsync(string? sku)
        {
            if (_syncService == null)
            {
                _logger.LogDebug("SyncService not available, skipping Luca stock card sync for SKU: {SKU}", sku);
                return;
            }

            try
            {
                var result = await _syncService.SyncProductsToLucaAsync(new SyncOptionsDto
                {
                    DryRun = false,
                    ForceSendDuplicates = false,
                    PreferBarcodeMatch = true
                });

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Luca stock card sync completed. New: {New}, Sent: {Sent}", 
                        result.NewCreated, result.SentRecords);
                }
                else
                {
                    _logger.LogWarning("Luca stock card sync completed with issues: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync stock card to Luca for SKU: {SKU}", sku);
            }
        }

        /// <summary>
        /// Luca'ya stok hareketi gönderir
        /// </summary>
        private async Task SyncStockMovementToLucaAsync(string? sku)
        {
            if (_lucaService == null || string.IsNullOrEmpty(sku))
            {
                _logger.LogDebug("LucaService not available or SKU empty, skipping stock movement sync");
                return;
            }

            try
            {
                // Senkronize edilmemiş stok hareketlerini al
                var unsyncedMovements = await _context.Stocks
                    .Where(s => !s.IsSynced && (string.IsNullOrEmpty(sku) || (s.Reference != null && s.Reference.Contains(sku))))
                    .OrderByDescending(s => s.Timestamp)
                    .Take(50) // Son 50 hareket
                    .ToListAsync();

                if (!unsyncedMovements.Any())
                {
                    _logger.LogDebug("No unsynced stock movements found for SKU: {SKU}", sku);
                    return;
                }

                // Luca formatına dönüştür - LucaStockDto [JsonIgnore] properties kullanılır
                var lucaStocks = unsyncedMovements.Select(s => new LucaStockDto
                {
                    ProductCode = s.Reference ?? string.Empty,
                    Quantity = s.Quantity,
                    MovementType = s.Type ?? "ADJUSTMENT",
                    MovementDate = s.Timestamp
                }).ToList();

                var result = await _lucaService.SendStockMovementsAsync(lucaStocks);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Sent {Count} stock movements to Luca", result.SuccessfulRecords);
                    
                    // Başarılı olanları işaretle
                    foreach (var movement in unsyncedMovements)
                    {
                        movement.IsSynced = true;
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to send stock movements to Luca: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync stock movements to Luca for SKU: {SKU}", sku);
            }
        }

        /// <summary>
        /// Katana'ya stok güncelleme gönderir
        /// </summary>
        private async Task SyncStockToKatanaAsync(string? sku)
        {
            if (_katanaService == null || string.IsNullOrEmpty(sku))
            {
                _logger.LogDebug("KatanaService not available or SKU empty, skipping Katana sync");
                return;
            }

            try
            {
                // SKU ile ürünü bul
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.SKU == sku);

                if (product == null)
                {
                    _logger.LogDebug("Product not found for SKU: {SKU}, skipping Katana sync", sku);
                    return;
                }

                // Toplam stok miktarını hesapla
                var totalStock = await _context.Stocks
                    .Where(s => s.ProductId == product.Id)
                    .SumAsync(s => s.Type == "IN" ? s.Quantity : (s.Type == "OUT" ? -s.Quantity : 0));

                // Katana'ya gönder
                var success = await _katanaService.UpdateProductAsync(
                    product.Id,
                    product.Name ?? sku,
                    product.SalesPrice,
                    (int)totalStock
                );

                if (success)
                {
                    _logger.LogInformation("Successfully updated stock in Katana for SKU: {SKU}, Stock: {Stock}", sku, totalStock);
                }
                else
                {
                    _logger.LogWarning("Failed to update stock in Katana for SKU: {SKU}", sku);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync stock to Katana for SKU: {SKU}", sku);
            }
        }

        /// <summary>
        /// StockMovement kayıtlarını senkronize edilmiş olarak işaretler
        /// </summary>
        private async Task MarkMovementsAsSyncedAsync(string? sku)
        {
            try
            {
                var unsyncedMovements = await _context.StockMovements
                    .Where(m => !m.IsSynced && (string.IsNullOrEmpty(sku) || m.ProductSku == sku))
                    .ToListAsync();

                foreach (var movement in unsyncedMovements)
                {
                    movement.IsSynced = true;
                }

                if (unsyncedMovements.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marked {Count} stock movements as synced for SKU: {SKU}", unsyncedMovements.Count, sku);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark movements as synced for SKU: {SKU}", sku);
            }
        }
    }
}
