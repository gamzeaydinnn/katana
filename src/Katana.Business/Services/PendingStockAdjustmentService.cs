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
        private readonly Katana.Core.Interfaces.IPendingNotificationPublisher? _publisher;

        public PendingStockAdjustmentService(IntegrationDbContext context, ILogger<PendingStockAdjustmentService> logger, Katana.Core.Interfaces.IPendingNotificationPublisher? publisher = null)
        {
            _context = context;
            _logger = logger;
            _publisher = publisher;
        }

        public async Task<PendingStockAdjustment> CreateAsync(PendingStockAdjustment creation)
        {
            creation.RequestedAt = creation.RequestedAt == default ? DateTimeOffset.UtcNow : creation.RequestedAt;
            creation.Status = "Pending";
            _context.PendingStockAdjustments.Add(creation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pending stock adjustment created (Id: {Id}, ProductId: {ProductId}, Quantity: {Qty})", creation.Id, creation.ProductId, creation.Quantity);

            // Publish lightweight pending-created notification if a publisher is available
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
            // If the provider is non-relational (e.g., InMemory for tests),
            // fallback to a simpler, transaction-less path that preserves semantics.
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

                    product.Stock += item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

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
                    // First, attempt an atomic state transition from Pending -> Approving
                    var rows = await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE PendingStockAdjustments SET Status = {"Approving"} WHERE Id = {id} AND Status = {"Pending"}");
                    if (rows == 0)
                    {
                        // Nothing to do (already processed or not pending)
                        failureReason = "Adjustment not in pending state or already processed";
                        return;
                    }

                    await using var tx = await _context.Database.BeginTransactionAsync();

                    // Reload the item now that we have claimed it
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

                    // Apply adjustment. Quantity may be negative for sales.
                    product.Stock += item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Mirror the adjustment into the Stocks table so admin stock movement reports stay accurate
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

                    // Publish approved event (best-effort)
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
                // Try to mark as failed if possible
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
    }
}
