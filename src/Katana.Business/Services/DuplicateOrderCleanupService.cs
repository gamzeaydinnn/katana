using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Katana.Business.Services;

/// <summary>
/// Duplike sipariş temizleme servisi implementasyonu
/// </summary>
public class DuplicateOrderCleanupService : IDuplicateOrderCleanupService
{
    private readonly IntegrationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<DuplicateOrderCleanupService> _logger;

    // Bozuk OrderNo pattern'leri
    // SO-SO-84 → SO-84
    // SO-SO-SO-56 → SO-56
    private static readonly Regex MalformedPattern = new(
        @"^(SO-)+SO-(\d+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Genel tekrarlı prefix pattern
    // ABC-ABC-123 → ABC-123
    private static readonly Regex RepeatedPrefixPattern = new(
        @"^([A-Z]+-)\1+(\d+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DuplicateOrderCleanupService(
        IntegrationDbContext context,
        IAuditService auditService,
        ILoggingService loggingService,
        ILogger<DuplicateOrderCleanupService> logger)
    {
        _context = context;
        _auditService = auditService;
        _loggingService = loggingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ExtractBaseOrderNo(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return orderNo;

        // SO-SO-84 → SO-84
        var match = MalformedPattern.Match(orderNo);
        if (match.Success)
        {
            return $"SO-{match.Groups[2].Value}";
        }

        // Genel tekrarlı prefix: ABC-ABC-123 → ABC-123
        var repeatedMatch = RepeatedPrefixPattern.Match(orderNo);
        if (repeatedMatch.Success)
        {
            return $"{repeatedMatch.Groups[1].Value}{repeatedMatch.Groups[2].Value}";
        }

        return orderNo;
    }

    /// <inheritdoc />
    public bool IsMalformedOrderNo(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return false;

        return MalformedPattern.IsMatch(orderNo) || RepeatedPrefixPattern.IsMatch(orderNo);
    }


    /// <inheritdoc />
    public async Task<DuplicateOrderAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing duplicate orders...");

        var result = new DuplicateOrderAnalysisResult();

        // Tüm siparişleri OrderNo'ya göre grupla
        var orders = await _context.SalesOrders
            .Include(o => o.Customer)
            .AsNoTracking()
            .ToListAsync(ct);

        result.TotalOrders = orders.Count;

        // ✅ GELİŞTİRİLMİŞ DUPLICATE KRİTERLERİ:
        // 1. OrderNo aynı olmalı
        // 2. CustomerId aynı olmalı (farklı müşterilerin aynı OrderNo'su olabilir)
        // 3. TotalAmount aynı veya çok yakın olmalı (±%1 tolerans)
        // 4. Son 7 gün içinde oluşturulmuş olmalı (yanlış pozitif önleme)
        // 5. ID farkı < 1000 olmalı (aynı batch'te oluşturulmuş olmalı)
        var recentCutoff = DateTime.UtcNow.AddDays(-7);
        
        var groups = orders
            .Where(o => o.CreatedAt >= recentCutoff) // Son 7 gün
            .GroupBy(o => new 
            { 
                OrderNo = o.OrderNo?.Trim().ToUpperInvariant() ?? $"NULL-{o.Id}",
                CustomerId = o.CustomerId,
                // TotalAmount'u yuvarlayarak grupla (küçük farkları tolere et)
                TotalRounded = Math.Round((double)(o.Total ?? 0), 0)
            })
            .Where(g => g.Count() > 1)
            .Where(g => 
            {
                // ID farkı kontrolü - aynı batch'te oluşturulmuş olmalı
                var ids = g.Select(o => o.Id).ToList();
                var idDiff = ids.Max() - ids.Min();
                return idDiff < 1000;
            })
            .ToList();

        result.DuplicateGroups = groups.Count;

        foreach (var group in groups)
        {
            var orderedList = group
                .OrderByDescending(o => GetStatusPriority(o.Status))
                .ThenBy(o => o.CreatedAt)
                .ToList();

            var orderToKeep = orderedList.First();
            var ordersToDelete = orderedList.Skip(1).ToList();

            var duplicateGroup = new DuplicateOrderGroup
            {
                OrderNo = group.Key.OrderNo,
                Count = group.Count(),
                OrderToKeep = MapToInfo(orderToKeep, GetKeepReason(orderToKeep, orderedList)),
                OrdersToDelete = ordersToDelete.Select(o => MapToInfo(o, null)).ToList()
            };

            result.Groups.Add(duplicateGroup);
            result.OrdersToDelete += ordersToDelete.Count;
        }

        _logger.LogInformation("Duplicate analysis complete: {Groups} groups, {ToDelete} orders to delete (criteria: same OrderNo+CustomerId+Total, last 7 days, ID diff < 1000)",
            result.DuplicateGroups, result.OrdersToDelete);

        return result;
    }

    /// <inheritdoc />
    public async Task<OrderCleanupResult> CleanupDuplicatesAsync(bool dryRun = true, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new OrderCleanupResult { WasDryRun = dryRun };

        try
        {
            var analysis = await AnalyzeDuplicatesAsync(ct);

            if (dryRun)
            {
                result.Success = true;
                result.OrdersDeleted = analysis.OrdersToDelete;
                result.Log.Add(new OrderCleanupLogEntry
                {
                    Action = "DryRun",
                    Details = $"Would delete {analysis.OrdersToDelete} duplicate orders from {analysis.DuplicateGroups} groups"
                });
                sw.Stop();
                result.Duration = sw.Elapsed;
                return result;
            }

            _logger.LogInformation("Starting duplicate cleanup: {Groups} groups, {ToDelete} orders",
                analysis.DuplicateGroups, analysis.OrdersToDelete);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    foreach (var group in analysis.Groups)
                    {
                        foreach (var orderInfo in group.OrdersToDelete)
                        {
                            try
                            {
                                var order = await _context.SalesOrders
                                    .Include(o => o.Lines)
                                    .FirstOrDefaultAsync(o => o.Id == orderInfo.Id, ct);

                                if (order != null)
                                {
                                    var lineCount = order.Lines?.Count ?? 0;
                                    
                                    if (order.Lines != null && order.Lines.Any())
                                    {
                                        _context.SalesOrderLines.RemoveRange(order.Lines);
                                        result.LinesDeleted += lineCount;
                                    }

                                    _context.SalesOrders.Remove(order);
                                    result.OrdersDeleted++;

                                    result.Log.Add(new OrderCleanupLogEntry
                                    {
                                        OrderId = order.Id,
                                        OrderNo = order.OrderNo ?? "",
                                        Action = "Deleted",
                                        Details = $"Deleted duplicate order (kept {group.OrderToKeep.Id}), {lineCount} lines removed"
                                    });

                                    _auditService.LogDelete(
                                        "SalesOrder",
                                        order.Id.ToString(),
                                        "DuplicateCleanup",
                                        $"Duplicate cleanup: kept order {group.OrderToKeep.Id} ({group.OrderToKeep.OrderNo})");
                                }
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add($"Failed to delete order {orderInfo.Id}: {ex.Message}");
                                _logger.LogError(ex, "Failed to delete duplicate order {OrderId}", orderInfo.Id);
                            }
                        }
                    }

                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    result.Success = true;
                    _logger.LogInformation("Duplicate cleanup complete: {Deleted} orders deleted, {Lines} lines removed",
                        result.OrdersDeleted, result.LinesDeleted);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    result.Success = false;
                    result.Errors.Add($"Transaction failed: {ex.Message}");
                    _logger.LogError(ex, "Duplicate cleanup transaction failed");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Cleanup failed: {ex.Message}");
            _logger.LogError(ex, "Duplicate cleanup failed");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        _loggingService.LogInfo(
            $"Duplicate order cleanup: {result.OrdersDeleted} deleted, {result.Errors.Count} errors",
            "DuplicateCleanup",
            null,
            LogCategory.Business);

        return result;
    }


    /// <inheritdoc />
    public async Task<MalformedOrderAnalysisResult> AnalyzeMalformedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing malformed order numbers...");

        var result = new MalformedOrderAnalysisResult();

        var orders = await _context.SalesOrders
            .Include(o => o.Customer)
            .AsNoTracking()
            .ToListAsync(ct);

        // Bozuk OrderNo'ları bul
        var malformedOrders = orders
            .Where(o => IsMalformedOrderNo(o.OrderNo ?? ""))
            .ToList();

        result.TotalMalformed = malformedOrders.Count;

        // Her bozuk sipariş için doğru OrderNo'yu hesapla
        foreach (var order in malformedOrders)
        {
            var correctOrderNo = ExtractBaseOrderNo(order.OrderNo ?? "");

            // Doğru OrderNo'ya sahip mevcut sipariş var mı?
            var existingOrder = orders.FirstOrDefault(o =>
                o.Id != order.Id &&
                string.Equals(o.OrderNo?.Trim(), correctOrderNo, StringComparison.OrdinalIgnoreCase));

            var info = new MalformedOrderInfo
            {
                Id = order.Id,
                CurrentOrderNo = order.OrderNo ?? "",
                CorrectOrderNo = correctOrderNo,
                CustomerName = order.Customer?.Title,
                Total = order.Total,
                Status = order.Status ?? ""
            };

            if (existingOrder != null)
            {
                info.Action = "Merge";
                info.MergeTargetId = existingOrder.Id;
                result.CanMerge++;
            }
            else
            {
                info.Action = "Rename";
                result.CanRename++;
            }

            result.Orders.Add(info);
        }

        _logger.LogInformation("Malformed analysis complete: {Total} malformed, {Merge} can merge, {Rename} can rename",
            result.TotalMalformed, result.CanMerge, result.CanRename);

        return result;
    }

    /// <inheritdoc />
    public async Task<OrderCleanupResult> CleanupMalformedAsync(bool dryRun = true, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new OrderCleanupResult { WasDryRun = dryRun };

        try
        {
            var analysis = await AnalyzeMalformedAsync(ct);

            if (dryRun)
            {
                result.Success = true;
                result.OrdersMerged = analysis.CanMerge;
                result.OrdersRenamed = analysis.CanRename;
                result.Log.Add(new OrderCleanupLogEntry
                {
                    Action = "DryRun",
                    Details = $"Would merge {analysis.CanMerge} and rename {analysis.CanRename} malformed orders"
                });
                sw.Stop();
                result.Duration = sw.Elapsed;
                return result;
            }

            _logger.LogInformation("Starting malformed cleanup: {Merge} to merge, {Rename} to rename",
                analysis.CanMerge, analysis.CanRename);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    foreach (var info in analysis.Orders)
                    {
                        try
                        {
                            if (info.Action == "Merge" && info.MergeTargetId.HasValue)
                            {
                                // Merge: Bozuk siparişi sil, satırları hedef siparişe taşı
                                var sourceOrder = await _context.SalesOrders
                                    .Include(o => o.Lines)
                                    .FirstOrDefaultAsync(o => o.Id == info.Id, ct);

                                var targetOrder = await _context.SalesOrders
                                    .Include(o => o.Lines)
                                    .FirstOrDefaultAsync(o => o.Id == info.MergeTargetId.Value, ct);

                                if (sourceOrder != null && targetOrder != null)
                                {
                                    // Satırları hedef siparişe taşı (eğer yoksa)
                                    if (sourceOrder.Lines != null)
                                    {
                                        foreach (var line in sourceOrder.Lines.ToList())
                                        {
                                            // Aynı SKU ve VariantId ile satır var mı kontrol et
                                            var existingLine = targetOrder.Lines?.FirstOrDefault(l =>
                                                l.SKU == line.SKU && l.VariantId == line.VariantId);

                                            if (existingLine == null)
                                            {
                                                line.SalesOrderId = targetOrder.Id;
                                            }
                                            else
                                            {
                                                // Miktarı topla
                                                existingLine.Quantity += line.Quantity;
                                                _context.SalesOrderLines.Remove(line);
                                                result.LinesDeleted++;
                                            }
                                        }
                                    }

                                    _context.SalesOrders.Remove(sourceOrder);
                                    result.OrdersMerged++;

                                    result.Log.Add(new OrderCleanupLogEntry
                                    {
                                        OrderId = info.Id,
                                        OrderNo = info.CurrentOrderNo,
                                        Action = "Merged",
                                        Details = $"Merged into order {info.MergeTargetId} ({info.CorrectOrderNo})"
                                    });

                                    _auditService.LogDelete(
                                        "SalesOrder",
                                        info.Id.ToString(),
                                        "MalformedCleanup",
                                        $"Merged malformed order into {info.MergeTargetId}");
                                }
                            }
                            else if (info.Action == "Rename")
                            {
                                // Rename: OrderNo'yu düzelt
                                var order = await _context.SalesOrders
                                    .FirstOrDefaultAsync(o => o.Id == info.Id, ct);

                                if (order != null)
                                {
                                    var oldOrderNo = order.OrderNo;
                                    order.OrderNo = info.CorrectOrderNo;
                                    order.UpdatedAt = DateTime.UtcNow;
                                    result.OrdersRenamed++;

                                    result.Log.Add(new OrderCleanupLogEntry
                                    {
                                        OrderId = info.Id,
                                        OrderNo = info.CurrentOrderNo,
                                        Action = "Renamed",
                                        Details = $"Renamed from '{oldOrderNo}' to '{info.CorrectOrderNo}'"
                                    });

                                    _auditService.LogUpdate(
                                        "SalesOrder",
                                        info.Id.ToString(),
                                        "MalformedCleanup",
                                        oldOrderNo,
                                        $"Renamed OrderNo to {info.CorrectOrderNo}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Failed to process order {info.Id}: {ex.Message}");
                            _logger.LogError(ex, "Failed to process malformed order {OrderId}", info.Id);
                        }
                    }

                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    result.Success = true;
                    _logger.LogInformation("Malformed cleanup complete: {Merged} merged, {Renamed} renamed",
                        result.OrdersMerged, result.OrdersRenamed);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    result.Success = false;
                    result.Errors.Add($"Transaction failed: {ex.Message}");
                    _logger.LogError(ex, "Malformed cleanup transaction failed");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Cleanup failed: {ex.Message}");
            _logger.LogError(ex, "Malformed cleanup failed");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        _loggingService.LogInfo(
            $"Malformed order cleanup: {result.OrdersMerged} merged, {result.OrdersRenamed} renamed, {result.Errors.Count} errors",
            "MalformedCleanup",
            null,
            LogCategory.Business);

        return result;
    }

    #region Private Helpers

    private static int GetStatusPriority(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "SHIPPED" => 4,
            "APPROVED" => 3,
            "PARTIALLY_SHIPPED" => 2,
            "NOT_SHIPPED" => 1,
            "PENDING" => 0,
            _ => -1
        };
    }

    private static string GetKeepReason(SalesOrder order, List<SalesOrder> allInGroup)
    {
        var maxStatus = allInGroup.Max(o => GetStatusPriority(o.Status));
        var orderStatus = GetStatusPriority(order.Status);

        if (orderStatus == maxStatus && allInGroup.Count(o => GetStatusPriority(o.Status) == maxStatus) == 1)
        {
            return "Most Advanced Status";
        }

        return "Oldest";
    }

    private static DuplicateOrderInfo MapToInfo(SalesOrder order, string? keepReason)
    {
        return new DuplicateOrderInfo
        {
            Id = order.Id,
            OrderNo = order.OrderNo ?? "",
            CustomerName = order.Customer?.Title,
            Total = order.Total,
            Currency = order.Currency,
            Status = order.Status ?? "",
            CreatedAt = order.CreatedAt,
            KatanaOrderId = order.KatanaOrderId,
            LucaOrderId = order.LucaOrderId,
            IsSyncedToLuca = order.IsSyncedToLuca,
            KeepReason = keepReason
        };
    }

    #endregion
}
