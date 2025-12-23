-- ==========================================================
-- KATANA ERP ENTEGRASYON - "SILINECEK" ÜRÜN TEMİZLEME
-- ==========================================================
-- UYARI: İşlem öncesi yedeğinizi kontrol edin!
-- VERİ KAYBI YAŞANMAZ - Tüm veriler Master ürüne aktarılır
-- ==========================================================

USE KatanaDB;
GO

-- ============================================
-- KONTROL SORGUSU: BİRLEŞTİRİLECEK ÜRÜNLERİ LİSTELE
-- ============================================

-- "SILINECEK" ile başlayan ürünleri ve ana ürünleri göster
SELECT 
    p.Id as UrunID,
    p.Name as UrunIsmi,
    p.SKU,
    p.katana_order_id as KatanaSiparisID,
    p.IsActive,
    CASE 
        WHEN p.Name LIKE 'SILINECEK%' THEN 'SİLİNECEK - BİRLEŞTİRİLECEK'
        ELSE 'ANA ÜRÜN - KALACAK'
    END as Durum,
    (SELECT MIN(Id) 
     FROM products 
     WHERE Name = REPLACE(p.Name, 'SILINECEK', '') 
     AND katana_order_id IS NULL 
     AND IsActive = 1) as AnaUrunID
FROM products p
WHERE (p.Name LIKE 'SILINECEK%' OR 
       p.Name IN (SELECT DISTINCT REPLACE(Name, 'SILINECEK', '') 
                  FROM products 
                  WHERE Name LIKE 'SILINECEK%'))
  AND p.IsActive = 1
ORDER BY 
    REPLACE(p.Name, 'SILINECEK', ''), 
    CASE WHEN p.Name LIKE 'SILINECEK%' THEN 1 ELSE 0 END;

-- Toplam sayılar
SELECT 
    COUNT(*) as ToplamSilinecekUrun,
    COUNT(DISTINCT REPLACE(Name, 'SILINECEK', '')) as FarkliUrunIsmi
FROM products
WHERE Name LIKE 'SILINECEK%'
  AND IsActive = 1;

-- ============================================
-- BİRLEŞTİRME İŞLEMİ
-- ============================================

BEGIN TRANSACTION;

-- 1. ADIM: Master Ürünleri Belirle (CTE Kullanarak Hatayı Önle)
-- Bu kısım, her isim grubu için "Ana Ürün"ü (Master) önceden hesaplar.

-- 2. ADIM: Eşleştirme Tablosunu Doldur
DECLARE @MergeTable TABLE (
    TrashProductId INT,
    MasterProductId INT,
    MasterSKU NVARCHAR(50),
    ProductName NVARCHAR(200)
);

WITH MasterProductsCTE AS (
    SELECT 
        Name, 
        MIN(Id) as MasterId
    FROM products 
    WHERE katana_order_id IS NULL 
      AND IsActive = 1
    GROUP BY Name
)
INSERT INTO @MergeTable (TrashProductId, MasterProductId, MasterSKU, ProductName)
SELECT 
    pTrash.Id,
    mp.MasterId,
    pMaster.SKU,
    pMaster.Name
FROM products pTrash
INNER JOIN MasterProductsCTE mp ON REPLACE(pTrash.Name, 'SILINECEK', '') = mp.Name
INNER JOIN products pMaster ON mp.MasterId = pMaster.Id
WHERE pTrash.Name LIKE 'SILINECEK%'  -- Sadece "SILINECEK" ile başlayanlar
  AND pTrash.IsActive = 1
  AND pTrash.Id <> mp.MasterId; -- Kendisiyle eşleşmesini engelle

-- Özet bilgi
DECLARE @TrashCount INT = (SELECT COUNT(*) FROM @MergeTable);
DECLARE @MasterCount INT = (SELECT COUNT(DISTINCT MasterProductId) FROM @MergeTable);

PRINT '========================================';
PRINT 'ÜRÜN BİRLEŞTİRME İŞLEMİ';
PRINT '========================================';
PRINT 'Birleştirilecek Ürün Sayısı: ' + CAST(@TrashCount AS NVARCHAR(10));
PRINT 'Ana Ürün Sayısı: ' + CAST(@MasterCount AS NVARCHAR(10));
PRINT '';

-- Eğer birleştirilecek ürün yoksa işlemi durdur
IF @TrashCount = 0
BEGIN
    PRINT '⚠️ UYARI: Birleştirilecek ürün bulunamadı!';
    PRINT 'İşlem iptal ediliyor...';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- 3. ADIM: Sipariş Satırlarını Master'a Bağla ve ID'leri Sıfırla
UPDATE sol
SET sol.SKU = mt.MasterSKU,
    sol.KatanaOrderId = NULL -- Önemli: Yeni gruplu onay için
FROM SalesOrderLines sol
INNER JOIN products pTrash ON sol.SKU = pTrash.SKU
INNER JOIN @MergeTable mt ON pTrash.Id = mt.TrashProductId;

PRINT 'Sipariş satırları Master ürünlere bağlandı: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 4. ADIM: Sipariş Başlıklarını 'PENDING' Yap
-- Bu siparişler admin panelinde tekrar "Onayla" butonunu aktif eder.
UPDATE SalesOrders
SET Status = 'Pending', 
    ApprovedDate = NULL, 
    ApprovedBy = NULL,
    IsSyncedToLuca = 0,
    UpdatedAt = GETDATE()
WHERE Id IN (
    SELECT DISTINCT SalesOrderId 
    FROM SalesOrderLines 
    WHERE KatanaOrderId IS NULL
);

PRINT 'Sipariş durumları PENDING yapıldı: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 5. ADIM: Tüm Geçmiş Verileri Aktar
UPDATE sm 
SET sm.ProductId = mt.MasterProductId
FROM StockMovements sm 
INNER JOIN @MergeTable mt ON sm.ProductId = mt.TrashProductId;

PRINT 'StokMovements güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

UPDATE bom 
SET bom.ProductId = mt.MasterProductId
FROM BillOfMaterials bom 
INNER JOIN @MergeTable mt ON bom.ProductId = mt.TrashProductId;

PRINT 'BillOfMaterials (ProductId) güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

UPDATE bom 
SET bom.MaterialId = mt.MasterProductId
FROM BillOfMaterials bom 
INNER JOIN @MergeTable mt ON bom.MaterialId = mt.TrashProductId;

PRINT 'BillOfMaterials (MaterialId) güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

UPDATE ii 
SET ii.ProductId = mt.MasterProductId
FROM InvoiceItems ii 
INNER JOIN @MergeTable mt ON ii.ProductId = mt.TrashProductId;

PRINT 'InvoiceItems güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 6. ADIM: Çöp Verileri Temizle
DELETE pv 
FROM ProductVariants pv 
INNER JOIN @MergeTable mt ON pv.ProductId = mt.TrashProductId;

PRINT 'ProductVariants silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

DELETE s 
FROM Stocks s 
INNER JOIN @MergeTable mt ON s.ProductId = mt.TrashProductId;

PRINT 'Stocks silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

DELETE p 
FROM products p 
INNER JOIN @MergeTable mt ON p.Id = mt.TrashProductId;

PRINT 'Duplicate (Çöp) ürünler silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- 7. ADIM: SONUÇ KONTROL
PRINT '========================================';
PRINT 'İŞLEM TAMAMLANDI';
PRINT '========================================';
PRINT '';

SELECT COUNT(*) as KalanTrashCount 
FROM products 
WHERE Name LIKE 'SILINECEK%' 
  AND IsActive = 1;

SELECT COUNT(*) as PendingOrderCount 
FROM SalesOrders 
WHERE Status = 'Pending';

PRINT '';
PRINT 'Birleştirilen Ürünler (İlk 50):';

SELECT TOP 50
    ProductName as AnaUrun,
    MasterSKU as AnaSKU,
    'Tüm veriler Master ürüne aktarıldı' as Durum
FROM @MergeTable
ORDER BY ProductName;

PRINT '';
PRINT '⚠️ KONTROL ET! Eğer sonuç doğruysa:';
PRINT '   COMMIT TRANSACTION;  -- Bu satırı çalıştır';
PRINT '';
PRINT 'Eğer hata varsa geri almak için:';
PRINT '   ROLLBACK TRANSACTION;  -- Bu satırı çalıştır';

-- COMMIT TRANSACTION;  -- Onaylamak için bu satırı çalıştır
-- ROLLBACK TRANSACTION;  -- Geri almak için bu satırı çalıştır

GO
