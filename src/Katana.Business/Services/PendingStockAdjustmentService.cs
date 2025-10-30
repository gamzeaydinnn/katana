using Katana.Business.Interfaces;
using Katana.Core.Entities;
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

        public PendingStockAdjustmentService(IntegrationDbContext context, ILogger<PendingStockAdjustmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PendingStockAdjustment> CreateAsync(PendingStockAdjustment creation)
        {
            creation.RequestedAt = creation.RequestedAt == default ? DateTimeOffset.UtcNow : creation.RequestedAt;
            creation.Status = "Pending";
            _context.PendingStockAdjustments.Add(creation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Pending stock adjustment created (Id: {Id}, ProductId: {ProductId}, Quantity: {Qty})", creation.Id, creation.ProductId, creation.Quantity);
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
            var item = await _context.PendingStockAdjustments.FindAsync(id);
            if (item == null || item.Status != "Pending") return false;

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        item.Status = "Failed";
                        item.RejectionReason = "Product not found";
                        await _context.SaveChangesAsync();
                        return;
                    }

                    // Apply adjustment. Quantity may be negative for sales.
                    product.Stock += item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Add a stock movement record for audit
                    var movement = new Katana.Core.Entities.StockMovement
                    {
                        ProductId = item.ProductId,
                        ProductSku = item.Sku ?? string.Empty,
                        ChangeQuantity = item.Quantity,
                        MovementType = item.Quantity < 0 ? Katana.Core.Enums.MovementType.Out : Katana.Core.Enums.MovementType.In,
                        SourceDocument = item.ExternalOrderId ?? string.Empty,
                        Timestamp = DateTime.UtcNow,
                        WarehouseCode = "MAIN",
                        IsSynced = false
                    };
                    _context.Add(movement);

                    item.Status = "Approved";
                    item.ApprovedBy = approvedBy;
                    item.ApprovedAt = DateTimeOffset.UtcNow;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    _logger.LogInformation("Pending stock adjustment {Id} approved by {User}", id, approvedBy);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve pending stock adjustment {Id}", id);
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
    }
}
