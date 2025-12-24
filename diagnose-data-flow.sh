#!/bin/bash

# ==========================================
# VERİ AKIŞI ANALİZİ - TEŞHİS SCRIPT
# ==========================================

# Database bağlantı bilgileri
DB_SERVER="localhost,1433"
DB_NAME="KatanaDB"
DB_USER="sa"
DB_PASSWORD="Admin00!S"

# SQL Server bağlantı komutu
SQLCMD="sqlcmd -S $DB_SERVER -d $DB_NAME -U $DB_USER -P $DB_PASSWORD -C"

echo "=========================================="
echo "🔍 VERİ AKIŞI ANALİZİ BAŞLIYOR..."
echo "=========================================="
echo ""

# ==========================================
# 1. ADMIN ONAYI SONRASI KATANA'YA GİDİŞ KONTROLÜ
# ==========================================
echo "📊 1. ADMIN ONAYI SONRASI KATANA'YA GİDİŞ KONTROLÜ"
echo "--------------------------------------------------"

echo ""
echo "1a. Onaylanmış ama KatanaOrderId olmayan siparişler (Katana'ya gitmemiş):"
$SQLCMD -Q "
SELECT 
    Id,
    OrderNo,
    Status,
    KatanaOrderId,
    CreatedAt,
    UpdatedAt,
    IsSyncedToLuca
FROM SalesOrders
WHERE Status = 'APPROVED'
   AND (KatanaOrderId IS NULL OR KatanaOrderId = 0)
ORDER BY CreatedAt DESC;
"

echo ""
echo "1b. Aynı OrderNo ile hem KatanaOrderId olan hem olmayan kayıtlar (çift kayıt):"
$SQLCMD -Q "
SELECT 
    OrderNo,
    COUNT(*) as KayitSayisi,
    COUNT(CASE WHEN KatanaOrderId > 0 THEN 1 END) as KatanaIleKayit,
    COUNT(CASE WHEN KatanaOrderId IS NULL OR KatanaOrderId = 0 THEN 1 END) as KatanasizKayit,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as Idler
FROM SalesOrders
GROUP BY OrderNo
HAVING COUNT(*) > 1;
"

# ==========================================
# 2. DUPLICATE SİPARİŞLERİN KATANA'DAKİ DURUMU
# ==========================================
echo ""
echo "📊 2. DUPLICATE SİPARİŞLERİN KATANA'DAKİ DURUMU"
echo "--------------------------------------------------"

echo ""
echo "2a. CANCELLED olmuş ama hala KatanaOrderId'si olan siparişler:"
$SQLCMD -Q "
SELECT 
    Id,
    OrderNo,
    Status,
    KatanaOrderId,
    CustomerId,
    Total,
    CreatedAt
FROM SalesOrders
WHERE Status = 'CANCELLED'
   AND KatanaOrderId > 0
ORDER BY OrderNo, CreatedAt;
"

echo ""
echo "2b. Son 7 gün içinde duplicate olabilecek siparişler:"
$SQLCMD -Q "
WITH DuplicateGroups AS (
    SELECT 
        UPPER(TRIM(OrderNo)) as CleanOrderNo,
        CustomerId,
        ROUND(CAST(Total AS FLOAT), 0) as RoundedTotal,
        COUNT(*) as Count
    FROM SalesOrders
    WHERE CreatedAt >= DATEADD(day, -7, GETUTCDATE())
    GROUP BY UPPER(TRIM(OrderNo)), CustomerId, ROUND(CAST(Total AS FLOAT), 0)
    HAVING COUNT(*) > 1
)
SELECT 
    so.Id,
    so.OrderNo,
    so.Status,
    so.KatanaOrderId,
    so.CustomerId,
    so.Total,
    so.CreatedAt,
    dg.Count as DuplicateCount
FROM SalesOrders so
INNER JOIN DuplicateGroups dg 
    ON UPPER(TRIM(so.OrderNo)) = dg.CleanOrderNo
    AND so.CustomerId = dg.CustomerId
    AND ROUND(CAST(so.Total AS FLOAT), 0) = dg.RoundedTotal
WHERE so.CreatedAt >= DATEADD(day, -7, GETUTCDATE())
ORDER BY so.OrderNo, so.CreatedAt;
"

# ==========================================
# 3. LUCA'YA VARYANTLI ÜRÜN GÖNDERİMİ KONTROLÜ
# ==========================================
echo ""
echo "📊 3. LUCA'YA VARYANTLI ÜRÜN GÖNDERİMİ KONTROLÜ"
echo "--------------------------------------------------"

echo ""
echo "3a. Varyantlı ürün satırlarının durumu (son 30 gün, APPROVED):"
$SQLCMD -Q "
SELECT TOP 50
    sol.Id,
    sol.SalesOrderId,
    so.OrderNo,
    so.Status,
    so.IsSyncedToLuca,
    sol.SKU,
    sol.ProductName,
    sol.VariantCode,
    sol.VariantName,
    sol.Barcode,
    CASE 
        WHEN sol.VariantCode IS NOT NULL THEN 'Varyantli'
        ELSE 'Normal'
    END as UrunTipi
FROM SalesOrderLines sol
INNER JOIN SalesOrders so ON sol.SalesOrderId = so.Id
WHERE so.Status = 'APPROVED'
  AND so.CreatedAt >= DATEADD(day, -30, GETUTCDATE())
ORDER BY so.OrderNo, sol.Id;
"

echo ""
echo "3b. APPROVED + IsSyncedToLuca false olan siparişler (Luca'ya gitmemiş):"
$SQLCMD -Q "
SELECT 
    Id,
    OrderNo,
    Status,
    KatanaOrderId,
    IsSyncedToLuca,
    Total,
    CreatedAt
FROM SalesOrders
WHERE Status = 'APPROVED'
  AND (IsSyncedToLuca = 0 OR IsSyncedToLuca IS NULL)
ORDER BY CreatedAt DESC;
"

# ==========================================
# 4. FATURA OLUŞUMU KONTROLÜ
# ==========================================
echo ""
echo "📊 4. FATURA OLUŞUMU KONTROLÜ"
echo "--------------------------------------------------"

echo ""
echo "4a. APPROVED ama fatura oluşmamış siparişler (OrderMappings kaydı yok):"
$SQLCMD -Q "
SELECT 
    so.Id,
    so.OrderNo,
    so.Status,
    so.KatanaOrderId,
    so.IsSyncedToLuca,
    so.Total,
    so.CreatedAt,
    COUNT(om.Id) as MappingKayitSayisi
FROM SalesOrders so
LEFT JOIN OrderMappings om ON so.Id = om.LocalOrderId AND om.EntityType = 'SalesOrder'
WHERE so.Status = 'APPROVED'
  AND so.CreatedAt >= DATEADD(day, -30, GETUTCDATE())
GROUP BY so.Id, so.OrderNo, so.Status, so.KatanaOrderId, so.IsSyncedToLuca, so.Total, so.CreatedAt
HAVING COUNT(om.Id) = 0
ORDER BY so.CreatedAt DESC;
"

echo ""
echo "4b. OrderMappings tablosundaki fatura kayıtları:"
$SQLCMD -Q "
SELECT TOP 50
    om.Id,
    om.LocalOrderId,
    om.ExternalOrderId,
    om.EntityType,
    om.CreatedAt,
    so.OrderNo,
    so.Status
FROM OrderMappings om
INNER JOIN SalesOrders so ON om.LocalOrderId = so.Id
WHERE om.EntityType = 'SalesOrder'
ORDER BY om.CreatedAt DESC;
"

# ==========================================
# 5. ÖZET İSTATİSTİKLER
# ==========================================
echo ""
echo "📊 5. ÖZET İSTATİSTİKLER"
echo "--------------------------------------------------"

$SQLCMD -Q "
SELECT 
    'Toplam Siparis' as Metrik, COUNT(*) as Deger FROM SalesOrders
UNION ALL
SELECT 'APPROVED Siparis', COUNT(*) FROM SalesOrders WHERE Status = 'APPROVED'
UNION ALL
SELECT 'CANCELLED Siparis', COUNT(*) FROM SalesOrders WHERE Status = 'CANCELLED'
UNION ALL
SELECT 'PENDING Siparis', COUNT(*) FROM SalesOrders WHERE Status = 'PENDING'
UNION ALL
SELECT 'KatanaOrderId Olan', COUNT(*) FROM SalesOrders WHERE KatanaOrderId > 0
UNION ALL
SELECT 'KatanaOrderId Olmayan', COUNT(*) FROM SalesOrders WHERE KatanaOrderId IS NULL OR KatanaOrderId = 0
UNION ALL
SELECT 'Luca Sync Basarili', COUNT(*) FROM SalesOrders WHERE IsSyncedToLuca = 1
UNION ALL
SELECT 'Luca Sync Basarisiz', COUNT(*) FROM SalesOrders WHERE IsSyncedToLuca = 0 OR IsSyncedToLuca IS NULL;
"

echo ""
echo "=========================================="
echo "✅ ANALİZ TAMAMLANDI"
echo "=========================================="
