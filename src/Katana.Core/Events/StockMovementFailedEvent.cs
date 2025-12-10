using System;

namespace Katana.Core.Events;

/// <summary>
/// Stok hareketi Luca aktarımı başarısız olduğunda tetiklenen event
/// </summary>
public class StockMovementFailedEvent
{
    public int MovementId { get; }
    public string MovementType { get; } // "TRANSFER" veya "ADJUSTMENT"
    public string DocumentNo { get; }
    public string? ErrorMessage { get; }
    public DateTimeOffset FailedAt { get; }

    public StockMovementFailedEvent(int movementId, string movementType, string documentNo, string? errorMessage, DateTimeOffset failedAt)
    {
        MovementId = movementId;
        MovementType = movementType;
        DocumentNo = documentNo;
        ErrorMessage = errorMessage;
        FailedAt = failedAt;
    }
}
