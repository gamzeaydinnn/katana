using System;

namespace Katana.Core.Events;

/// <summary>
/// Stok hareketi Luca'ya başarıyla aktarıldığında tetiklenen event
/// </summary>
public class StockMovementSyncedEvent
{
    public int MovementId { get; }
    public string MovementType { get; } // "TRANSFER" veya "ADJUSTMENT"
    public string DocumentNo { get; }
    public int LucaDocumentId { get; }
    public DateTimeOffset SyncedAt { get; }

    public StockMovementSyncedEvent(int movementId, string movementType, string documentNo, int lucaDocumentId, DateTimeOffset syncedAt)
    {
        MovementId = movementId;
        MovementType = movementType;
        DocumentNo = documentNo;
        LucaDocumentId = lucaDocumentId;
        SyncedAt = syncedAt;
    }
}
