using System;

namespace Katana.Core.Events;

/// <summary>
/// Ürün güncellendiğinde tetiklenen event
/// </summary>
public class ProductUpdatedEvent
{
    public int ProductId { get; }
    public string? Sku { get; }
    public string? Name { get; }
    public string? ChangedFields { get; }
    public DateTimeOffset UpdatedAt { get; }

    public ProductUpdatedEvent(int productId, string? sku, string? name, string? changedFields, DateTimeOffset updatedAt)
    {
        ProductId = productId;
        Sku = sku;
        Name = name;
        ChangedFields = changedFields;
        UpdatedAt = updatedAt;
    }
}
