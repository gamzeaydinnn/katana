using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class DataCorrectionService : IDataCorrectionService
{
    private readonly IntegrationDbContext _context;
    private readonly IKatanaService _katanaService;
    private readonly IProductService _productService;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<DataCorrectionService> _logger;

    public DataCorrectionService(
        IntegrationDbContext context,
        IKatanaService katanaService,
        IProductService productService,
        ILoggingService loggingService,
        ILogger<DataCorrectionService> logger)
    {
        _context = context;
        _katanaService = katanaService;
        _productService = productService;
        _loggingService = loggingService;
        _logger = logger;
    }

    public async Task<List<ComparisonProductDto>> CompareKatanaAndLucaProductsAsync()
    {
        _loggingService.LogInfo("Starting Katana ↔ Luca product comparison", null, null, LogCategory.Business);

        // Fetch from both systems
        var katanaProducts = await _katanaService.GetProductsAsync();
        var lucaProducts = await _productService.GetAllProductsAsync();

        var comparisons = new List<ComparisonProductDto>();

        // Compare by SKU
        var allSkus = katanaProducts.Select(k => k.SKU)
            .Union(lucaProducts.Select(l => l.SKU))
            .Distinct()
            .ToList();

        foreach (var sku in allSkus)
        {
            var katana = katanaProducts.FirstOrDefault(k => k.SKU == sku);
            var luca = lucaProducts.FirstOrDefault(l => l.SKU == sku);

            var comparison = new ComparisonProductDto
            {
                SKU = sku,
                Name = katana?.Name ?? luca?.Name ?? "",
                KatanaData = katana != null ? new KatanaProductData
                {
                    Id = katana.Id ?? "",
                    SKU = katana.SKU,
                    Name = katana.Name,
                    SalesPrice = katana.SalesPrice ?? katana.Price,
                    CostPrice = katana.CostPrice,
                    OnHand = katana.OnHand,
                    Available = katana.Available,
                    Committed = katana.Committed,
                    Category = katana.Category,
                    IsActive = katana.IsActive
                } : null,
                LucaData = luca != null ? new LucaProductData
                {
                    Id = luca.Id,
                    SKU = luca.SKU,
                    Name = luca.Name,
                    Price = luca.Price,
                    Stock = luca.Stock,
                    IsActive = luca.IsActive
                } : null,
                Issues = new List<DataIssue>()
            };

            // Detect issues
            if (katana == null)
            {
                comparison.Issues.Add(new DataIssue
                {
                    Field = "Existence",
                    Issue = "Ürün Katana'da bulunamadı",
                    LucaValue = "Var",
                    KatanaValue = "Yok",
                    Severity = "Warning"
                });
            }
            else if (luca == null)
            {
                comparison.Issues.Add(new DataIssue
                {
                    Field = "Existence",
                    Issue = "Ürün Luca'da bulunamadı",
                    KatanaValue = "Var",
                    LucaValue = "Yok",
                    Severity = "Warning"
                });
            }
            else
            {
                // Compare prices (use SalesPrice if available, fallback to Price)
                var katanaPrice = katana.SalesPrice ?? katana.Price;
                if (Math.Abs(katanaPrice - luca.Price) > 0.01m)
                {
                    comparison.Issues.Add(new DataIssue
                    {
                        Field = "Price",
                        Issue = "Fiyat uyuşmazlığı",
                        KatanaValue = katanaPrice.ToString("F2"),
                        LucaValue = luca.Price.ToString("F2"),
                        Severity = "Critical"
                    });
                }

                // Compare names
                if (!string.Equals(katana.Name.Trim(), luca.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    comparison.Issues.Add(new DataIssue
                    {
                        Field = "Name",
                        Issue = "İsim farklılığı",
                        KatanaValue = katana.Name,
                        LucaValue = luca.Name,
                        Severity = "Warning"
                    });
                }

                // Compare active status
                if (katana.IsActive != luca.IsActive)
                {
                    comparison.Issues.Add(new DataIssue
                    {
                        Field = "IsActive",
                        Issue = "Aktiflik durumu farklı",
                        KatanaValue = katana.IsActive ? "Aktif" : "Pasif",
                        LucaValue = luca.IsActive ? "Aktif" : "Pasif",
                        Severity = "Warning"
                    });
                }
            }

            if (comparison.Issues.Any())
            {
                comparisons.Add(comparison);
            }
        }

        _loggingService.LogInfo($"Comparison complete: {comparisons.Count} products with issues", null, null, LogCategory.Business);
        return comparisons;
    }

    public async Task<List<DataCorrectionDto>> GetPendingCorrectionsAsync()
    {
        var corrections = await _context.DataCorrectionLogs
            .Where(c => !c.IsApproved && !c.IsSynced)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return corrections.Select(c => new DataCorrectionDto
        {
            Id = c.Id,
            SourceSystem = c.SourceSystem,
            EntityType = c.EntityType,
            EntityId = c.EntityId,
            FieldName = c.FieldName,
            OriginalValue = c.OriginalValue,
            CorrectedValue = c.CorrectedValue,
            ValidationError = c.ValidationError,
            CorrectionReason = c.CorrectionReason,
            IsApproved = c.IsApproved,
            ApprovedBy = c.ApprovedBy,
            ApprovedAt = c.ApprovedAt,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<DataCorrectionDto> CreateCorrectionAsync(CreateCorrectionDto dto, string userId)
    {
        var correction = new DataCorrectionLog
        {
            SourceSystem = dto.SourceSystem,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            FieldName = dto.FieldName,
            OriginalValue = dto.OriginalValue,
            CorrectedValue = dto.CorrectedValue,
            CorrectionReason = dto.CorrectionReason,
            ValidationError = string.Empty,
            IsApproved = false,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.DataCorrectionLogs.Add(correction);
        await _context.SaveChangesAsync(default);

        _loggingService.LogInfo($"Data correction created: {dto.EntityType} {dto.EntityId}", userId, null, LogCategory.Business);

        return new DataCorrectionDto
        {
            Id = correction.Id,
            SourceSystem = correction.SourceSystem,
            EntityType = correction.EntityType,
            EntityId = correction.EntityId,
            FieldName = correction.FieldName,
            OriginalValue = correction.OriginalValue,
            CorrectedValue = correction.CorrectedValue,
            CorrectionReason = correction.CorrectionReason,
            IsApproved = correction.IsApproved,
            CreatedAt = correction.CreatedAt
        };
    }

    public async Task<bool> ApproveCorrectionAsync(int correctionId, string userId)
    {
        var correction = await _context.DataCorrectionLogs.FindAsync(correctionId);
        if (correction == null) return false;

        correction.IsApproved = true;
        correction.ApprovedBy = userId;
        correction.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(default);
        _loggingService.LogInfo($"Correction approved: {correctionId}", userId, null, LogCategory.Business);

        return true;
    }

    public async Task<bool> RejectCorrectionAsync(int correctionId, string userId)
    {
        var correction = await _context.DataCorrectionLogs.FindAsync(correctionId);
        if (correction == null) return false;

        _context.DataCorrectionLogs.Remove(correction);
        await _context.SaveChangesAsync(default);

        _loggingService.LogInfo($"Correction rejected and deleted: {correctionId}", userId, null, LogCategory.Business);
        return true;
    }

    public async Task<bool> ApplyCorrectionToLucaAsync(int correctionId)
    {
        var correction = await _context.DataCorrectionLogs.FindAsync(correctionId);
        if (correction == null || !correction.IsApproved || correction.IsSynced)
            return false;

        try
        {
            if (correction.EntityType == "Product")
            {
                var product = await _productService.GetProductBySkuAsync(correction.EntityId);
                if (product == null) return false;

                // Apply correction based on field
                switch (correction.FieldName.ToLower())
                {
                    case "price":
                        if (decimal.TryParse(correction.CorrectedValue, out var price))
                        {
                            await _productService.UpdateProductAsync(product.Id, new UpdateProductDto
                            {
                                Name = product.Name,
                                SKU = product.SKU,
                                Price = price,
                                CategoryId = product.CategoryId
                            });
                        }
                        break;

                    case "name":
                        await _productService.UpdateProductAsync(product.Id, new UpdateProductDto
                        {
                            Name = correction.CorrectedValue ?? product.Name,
                            SKU = product.SKU,
                            Price = product.Price,
                            CategoryId = product.CategoryId
                        });
                        break;

                    case "stock":
                        if (int.TryParse(correction.CorrectedValue, out var stock))
                        {
                            await _productService.UpdateStockAsync(product.Id, stock);
                        }
                        break;
                }

                correction.IsSynced = true;
                await _context.SaveChangesAsync(default);

                _loggingService.LogInfo($"Correction applied to Luca: {correctionId}", correction.ApprovedBy, null, LogCategory.Business);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply correction {CorrectionId} to Luca", correctionId);
            return false;
        }
    }

    public async Task<bool> ApplyCorrectionToKatanaAsync(int correctionId)
    {
        // NOTE: Katana API'ye write işlemi için endpoint olup olmadığını kontrol edin
        // Şimdilik placeholder
        _logger.LogWarning("Katana API write operations not implemented yet");
        return false;
    }
}
