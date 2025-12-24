using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for preparing stock cards in Luca before order approval.
/// Ensures all SKUs in an order have corresponding stock cards in Luca.
/// </summary>
public interface IStockCardPreparationService
{
    /// <summary>
    /// Prepares stock cards for all lines in a sales order.
    /// For each line, checks if stock card exists in Luca and creates if missing.
    /// </summary>
    /// <param name="order">The sales order with lines to process</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing success/failure status for each SKU</returns>
    Task<StockCardPreparationResult> PrepareStockCardsForOrderAsync(
        SalesOrder order,
        CancellationToken ct = default);
}
