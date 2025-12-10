using System;

namespace Katana.Core.Events;

/// <summary>
/// Yeni ürün oluşturulduğunda tetiklenen event
/// </summary>
public class ProductCreatedEvent
{
    public int ProductId { get; }
    public string? Sku { get; }
    public string? Name { get; }
    public string Source { get; } // "Katana" veya "Manual"
    public DateTimeOffset CreatedAt { get; }

    public ProductCreatedEvent(int productId, string? sku, string? name, string source, DateTimeOffset createdAt)
    {
        ProductId = productId;
        Sku = sku;
        Name = name;
        Source = source;
        CreatedAt = createdAt;
    }
}
