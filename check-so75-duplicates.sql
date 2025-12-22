-- SO-75 duplikasyonunu analiz et
SELECT 
    Id,
    OrderNo,
    Status,
    TotalAmount,
    Currency,
    OrderCreatedDate,
    KatanaOrderId,
    CreatedAt,
    UpdatedAt,
    LucaSyncStatus,
    LastSyncError
FROM SalesOrders
WHERE OrderNo = 'SO-75'
ORDER BY Id;
