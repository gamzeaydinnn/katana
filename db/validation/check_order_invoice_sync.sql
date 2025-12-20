-- ============================================
-- Fatura/Sipariş Doğrulama SQL
-- ============================================
-- Amaç: Katana'dan gönderilen siparişlerin Luca'da olup olmadığını kontrol et

-- ============================================
-- 1. Katana Orders → Luca Mapping Kontrolü
-- ============================================
SELECT 
    o.Id AS KatanaOrderId,
    o.OrderNo AS KatanaOrderNo,
    o.OrderDate,
    o.Status,
    o.TotalAmount,
    o.IsSynced AS KatanaSyncFlag,
    om.LucaInvoiceId,
    om.EntityType AS LucaEntityType,
    om.ExternalOrderId,
    om.CreatedAt AS MappingCreatedAt,
    CASE 
        WHEN om.LucaInvoiceId IS NOT NULL THEN '✅ VAR'
        WHEN o.IsSynced = 1 THEN '⚠️ SYNC FLAG VAR AMA MAPPING YOK'
        ELSE '❌ YOK'
    END AS LucaDurum
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.Status IN ('Confirmed', 'Completed', 'Shipped')  -- Sadece gönderilmesi gereken siparişler
ORDER BY o.OrderDate DESC;

-- ============================================
-- 2. Sync Edilmiş Ama Mapping Olmayan Siparişler (SORUNLU)
-- ============================================
SELECT 
    o.Id,
    o.OrderNo,
    o.OrderDate,
    o.Status,
    o.IsSynced,
    o.UpdatedAt
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.IsSynced = 1 
  AND om.LucaInvoiceId IS NULL
ORDER BY o.OrderDate DESC;

-- ============================================
-- 3. Mapping Var Ama Sync Flag Yok (TUTARSIZLIK)
-- ============================================
SELECT 
    o.Id,
    o.OrderNo,
    o.IsSynced,
    om.LucaInvoiceId,
    om.EntityType,
    om.CreatedAt
FROM Orders o
INNER JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.IsSynced = 0 OR o.IsSynced IS NULL
ORDER BY om.CreatedAt DESC;

-- ============================================
-- 4. Son 24 Saatte Sync Edilen Siparişler
-- ============================================
SELECT 
    o.Id,
    o.OrderNo,
    o.OrderDate,
    o.Status,
    o.TotalAmount,
    om.LucaInvoiceId,
    om.EntityType,
    om.CreatedAt AS SyncTime,
    DATEDIFF(MINUTE, om.CreatedAt, GETUTCDATE()) AS MinutesAgo
FROM Orders o
INNER JOIN OrderMappings om ON o.Id = om.OrderId
WHERE om.CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY om.CreatedAt DESC;

-- ============================================
-- 5. Sync İstatistikleri
-- ============================================
SELECT 
    COUNT(*) AS TotalOrders,
    SUM(CASE WHEN o.IsSynced = 1 THEN 1 ELSE 0 END) AS SyncedOrders,
    SUM(CASE WHEN om.LucaInvoiceId IS NOT NULL THEN 1 ELSE 0 END) AS MappedOrders,
    SUM(CASE WHEN o.IsSynced = 1 AND om.LucaInvoiceId IS NULL THEN 1 ELSE 0 END) AS ProblematicOrders,
    SUM(CASE WHEN o.IsSynced = 0 AND om.LucaInvoiceId IS NOT NULL THEN 1 ELSE 0 END) AS InconsistentOrders
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.Status IN ('Confirmed', 'Completed', 'Shipped');

-- ============================================
-- 6. Entity Type Bazında Dağılım
-- ============================================
SELECT 
    om.EntityType,
    COUNT(*) AS Count,
    MIN(om.CreatedAt) AS FirstSync,
    MAX(om.CreatedAt) AS LastSync
FROM OrderMappings om
GROUP BY om.EntityType
ORDER BY Count DESC;

-- ============================================
-- 7. Sync Log Kontrolü (Son 100 kayıt)
-- ============================================
SELECT TOP 100
    sl.Id,
    sl.IntegrationName,
    sl.CreatedAt,
    sl.IsSuccess,
    sl.Details
FROM SyncLogs sl
WHERE sl.IntegrationName LIKE '%ORDER%' 
   OR sl.IntegrationName LIKE '%INVOICE%'
ORDER BY sl.CreatedAt DESC;

-- ============================================
-- 8. Başarısız Sync Denemeleri
-- ============================================
SELECT TOP 50
    sl.Id,
    sl.IntegrationName,
    sl.CreatedAt,
    sl.Details
FROM SyncLogs sl
WHERE sl.IsSuccess = 0
  AND (sl.IntegrationName LIKE '%ORDER%' OR sl.IntegrationName LIKE '%INVOICE%')
ORDER BY sl.CreatedAt DESC;

-- ============================================
-- 9. Duplicate Mapping Kontrolü (Aynı Order için birden fazla mapping)
-- ============================================
SELECT 
    om.OrderId,
    o.OrderNo,
    COUNT(*) AS MappingCount,
    STRING_AGG(CAST(om.LucaInvoiceId AS VARCHAR), ', ') AS LucaInvoiceIds
FROM OrderMappings om
INNER JOIN Orders o ON om.OrderId = o.Id
GROUP BY om.OrderId, o.OrderNo
HAVING COUNT(*) > 1
ORDER BY MappingCount DESC;

-- ============================================
-- 10. Eksik Mapping'ler (Sync edilmiş ama mapping yok)
-- ============================================
SELECT 
    o.Id,
    o.OrderNo,
    o.OrderDate,
    o.Status,
    o.TotalAmount,
    o.UpdatedAt AS LastUpdate,
    DATEDIFF(HOUR, o.UpdatedAt, GETUTCDATE()) AS HoursSinceUpdate
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.IsSynced = 1 
  AND om.LucaInvoiceId IS NULL
  AND o.Status IN ('Confirmed', 'Completed', 'Shipped')
ORDER BY o.OrderDate DESC;
