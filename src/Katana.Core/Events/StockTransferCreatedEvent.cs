using System;

namespace Katana.Core.Events;

/// <summary>
/// Yeni stok transferi oluşturulduğunda tetiklenen event
/// </summary>
public class StockTransferCreatedEvent
{
    public int TransferId { get; }
    public string DocumentNo { get; }
    public string? FromWarehouse { get; }
    public string? ToWarehouse { get; }
    public decimal Quantity { get; }
    public string? ProductSku { get; }
    public DateTimeOffset CreatedAt { get; }

    public StockTransferCreatedEvent(int transferId, string documentNo, string? fromWarehouse, string? toWarehouse, decimal quantity, string? productSku, DateTimeOffset createdAt)
    {
        TransferId = transferId;
        DocumentNo = documentNo;
        FromWarehouse = fromWarehouse;
        ToWarehouse = toWarehouse;
        Quantity = quantity;
        ProductSku = productSku;
        CreatedAt = createdAt;
    }
}
