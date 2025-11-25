using System;

namespace Katana.Core.Entities;

/// <summary>
/// Envanter hareketi (stocktake/adjustment vb.) için temel temsil.
/// </summary>
public class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public long? VariantId { get; set; }
    public long? LocationId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
}
