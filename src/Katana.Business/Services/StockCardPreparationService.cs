using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for preparing stock cards in Luca before order approval.
/// Ensures all SKUs in an order have corresponding stock cards in Luca.
/// </summary>
public class StockCardPreparationService : IStockCardPreparationService
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<StockCardPreparationService> _logger;

    public StockCardPreparationService(
        ILucaService lucaService,
        ILogger<StockCardPreparationService> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StockCardPreparationResult> PrepareStockCardsForOrderAsync(
        SalesOrder order,
        CancellationToken ct = default)
    {
        var result = new StockCardPreparationResult
        {
            TotalLines = order.Lines?.Count ?? 0
        };

        if (order.Lines == null || !order.Lines.Any())
        {
            _logger.LogWarning("Order {OrderNo} has no lines to process", order.OrderNo);
            result.AllSucceeded = true;
            return result;
        }

        _logger.LogInformation(
            "Starting stock card preparation for order {OrderNo} with {LineCount} lines",
            order.OrderNo, result.TotalLines);

        foreach (var line in order.Lines)
        {
            var operationResult = await CheckAndCreateStockCardAsync(line, ct);
            result.Results.Add(operationResult);

            switch (operationResult.Action)
            {
                case "exists":
                case "created":
                    result.SuccessCount++;
                    break;
                case "failed":
                    result.FailedCount++;
                    break;
                case "skipped":
                    result.SkippedCount++;
                    break;
            }
        }

        result.AllSucceeded = result.FailedCount == 0;

        _logger.LogInformation(
            "Stock card preparation completed for order {OrderNo}: " +
            "Total={Total}, Success={Success}, Failed={Failed}, Skipped={Skipped}",
            order.OrderNo, result.TotalLines, result.SuccessCount, 
            result.FailedCount, result.SkippedCount);

        return result;
    }


    /// <summary>
    /// Checks if stock card exists for the SKU and creates if missing.
    /// Requirements: 1.2, 1.3, 4.1, 4.3
    /// </summary>
    private async Task<StockCardOperationResult> CheckAndCreateStockCardAsync(
        SalesOrderLine line,
        CancellationToken ct)
    {
        var result = new StockCardOperationResult
        {
            SKU = line.SKU ?? string.Empty,
            ProductName = line.ProductName ?? string.Empty,
            ProcessedAt = DateTime.UtcNow
        };

        // Skip empty SKUs
        if (string.IsNullOrWhiteSpace(line.SKU))
        {
            result.Action = "skipped";
            result.Message = "Empty or null SKU";
            _logger.LogWarning("Skipping line with empty SKU for product: {ProductName}", line.ProductName);
            return result;
        }

        try
        {
            // Check if stock card exists
            var existingSkartId = await _lucaService.FindStockCardBySkuAsync(line.SKU);

            if (existingSkartId.HasValue)
            {
                result.Action = "exists";
                result.SkartId = existingSkartId.Value;
                result.Message = $"Stock card already exists with skartId: {existingSkartId.Value}";
                _logger.LogDebug("Stock card exists for SKU {SKU}: skartId={SkartId}", line.SKU, existingSkartId.Value);
                return result;
            }

            // Create new stock card
            _logger.LogInformation("Creating stock card for SKU: {SKU}", line.SKU);
            var createRequest = MapFromOrderLine(line);
            var createResult = await _lucaService.UpsertStockCardAsync(createRequest);

            if (createResult.IsSuccess)
            {
                result.Action = "created";
                result.SkartId = createResult.NewCreated > 0 ? createResult.NewCreated : null;
                result.Message = createResult.Message ?? "Stock card created successfully";
                
                // If it was a duplicate, treat as exists
                if (createResult.DuplicateRecords > 0)
                {
                    result.Action = "exists";
                    result.Message = "Stock card already exists (duplicate detected)";
                }
                
                _logger.LogInformation("Stock card created for SKU {SKU}: {Message}", line.SKU, result.Message);
            }
            else
            {
                result.Action = "failed";
                result.Error = createResult.Message ?? "Unknown error during stock card creation";
                _logger.LogError("Failed to create stock card for SKU {SKU}: {Error}", line.SKU, result.Error);
            }
        }
        catch (Exception ex)
        {
            result.Action = "failed";
            result.Error = ex.Message;
            _logger.LogError(ex, "Exception while processing stock card for SKU {SKU}", line.SKU);
        }

        return result;
    }

    #region Mapping Methods

    /// <summary>
    /// Maps a SalesOrderLine to LucaCreateStokKartiRequest.
    /// Requirements: 3.1, 3.2, 3.4, 3.5
    /// </summary>
    /// <param name="line">The order line to map</param>
    /// <returns>Stock card creation request</returns>
    public static LucaCreateStokKartiRequest MapFromOrderLine(SalesOrderLine line)
    {
        var cleanedSku = CleanSpecialChars(line.SKU);
        var cleanedName = CleanSpecialChars(line.ProductName ?? line.SKU);
        var kdvRate = CalculateKdvRate(line.TaxRate);

        return new LucaCreateStokKartiRequest
        {
            KartKodu = cleanedSku,
            KartAdi = cleanedName,
            KartTuru = 1, // Stok kartı
            KartTipi = 1,
            KartAlisKdvOran = kdvRate,
            KartSatisKdvOran = kdvRate,
            KartToptanAlisKdvOran = kdvRate,
            KartToptanSatisKdvOran = kdvRate,
            OlcumBirimiId = 1, // ADET (default)
            BaslangicTarihi = DateTime.Now.ToString("dd/MM/yyyy"),
            Barkod = cleanedSku,
            UzunAdi = cleanedName,
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            MaliyetHesaplanacakFlag = true
        };
    }

    /// <summary>
    /// Calculates KDV rate from TaxRate field.
    /// Requirements: 3.3
    /// - If TaxRate > 1: treat as percentage (e.g., 20 → 0.20)
    /// - If TaxRate <= 1: treat as decimal (e.g., 0.20 → 0.20)
    /// - If TaxRate is null: default to 0.20 (20%)
    /// </summary>
    /// <param name="taxRate">Tax rate from order line</param>
    /// <returns>KDV rate as decimal (0.0 - 1.0)</returns>
    public static double CalculateKdvRate(decimal? taxRate)
    {
        if (!taxRate.HasValue)
            return 0.20; // Default 20%

        // If > 1, treat as percentage (e.g., 20 → 0.20)
        if (taxRate.Value > 1)
            return (double)taxRate.Value / 100.0;

        // Already decimal format
        return (double)taxRate.Value;
    }

    /// <summary>
    /// Cleans special characters from input string.
    /// Requirements: 3.1, 3.2
    /// - Replaces Ø with O, ø with o
    /// - Trims whitespace
    /// </summary>
    /// <param name="input">Input string to clean</param>
    /// <returns>Cleaned string</returns>
    public static string CleanSpecialChars(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("Ø", "O")
            .Replace("ø", "o")
            .Trim();
    }

    #endregion
}
