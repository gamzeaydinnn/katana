-- Stok Hareketleri Hatalarını Temizle
-- Bu script tüm hatalı stok hareketlerini Pending durumuna alır

-- Transfer hatalarını temizle
UPDATE StockTransfers
SET Status = 'Pending'
WHERE Status = 'Error';

-- Adjustment hatalarını temizle  
UPDATE PendingStockAdjustments
SET Status = 'Pending',
    RejectionReason = NULL
WHERE Status = 'Error';

-- Sonuçları göster
SELECT 
    'Transfer' as Type,
    COUNT(*) as PendingCount
FROM StockTransfers
WHERE Status = 'Pending'
UNION ALL
SELECT 
    'Adjustment' as Type,
    COUNT(*) as PendingCount
FROM PendingStockAdjustments
WHERE Status = 'Pending';
