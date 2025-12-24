-- ==========================================================
-- KATANA ERP - AKILLI DUPLICATE TEMİZLEME
-- ==========================================================
-- Deduplication mantığı ile aynı isme sahip ürünleri birleştirir
-- UYARI: İşlem öncesi yedeğinizi kontrol edin!
-- ==========================================================

USE KatanaDB;
GO

-- ============================================
-- ADIM 1: DUPLICATE GRUPLARI ANALİZİ
-- ============================================

PRINT '========================================';
PRINT 'DUPLICATE GRUPLARI ANALİZİ';
PRINT '========================================';
PRINT '';

-- Aynı isme sahip ürün gruplarını bul
SELECT 
    TRIM(p.Name) as UrunIsmi,
    COUNT(*) as ToplamAdet,
    COUNT(CASE WHEN p.katana_order_id IS NULL THEN 1 END) as ManuelUrunSayisi,
    COUNT(CASE WHEN p.katana_order_id IS NOT NULL THEN 1 END) as SiparistenGelenSayisi,
    MIN(p.Id) as EnKucukID,
    MIN(CASE WHEN p.katana_order_id IS NULL THEN p.Id END) as ManuelEnKucukID,
    STRING_AGG(p.SKU, ', ') as TumSKUlar
FROM products p
WHERE p.IsActive = 1
GROUP BY TRIM(p.Name)
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, TRIM(p.Name);

PRINT '';
PRINT 'Toplam Duplicate Grup Sayısı:';
SELECT COUNT(*) as DuplicateGrupSayisi
FROM (
    SELECT TRIM(Name) as UrunIsmi
    FROM products
    WHERE IsActive = 1
    GROUP BY TRIM(Name)
    HAVING COUNT(*) > 1
) as DuplicateGroups;

PRINT '';
PRINT '========================================';
PRINT 'CANONICAL SEÇİM KURALLARI';
PRINT '========================================';
PRINT '1. Manuel oluşturulan ürünü tercih et (katana_order_id IS NULL)';
PRINT '2. En küçük ID''yi tercih et';
PRINT '3. En kısa SKU''yu tercih et';
PRINT '';

-- ============================================
-- ADIM 2: BİRLEŞTİRME İŞLEMİ
-- ============================================

BEGIN TRANSACTION;

-- Merge tablosu oluştur
DECLARE @MergeTable TABLE (
    TrashProductId INT,
    MasterProductId INT,
    MasterSKU NVARCHAR(50),
    ProductName NVARCHAR(200),
    Reason NVARCHAR(500)
);

-- Her grup için canonical (master) ürünü seç
WITH ProductGroups AS (
    SELECT 
        TRIM(p.Name) as CleanName,
        p.Id,
        p.SKU,
        p.Name,
        p.katana_order_id,
        p.CreatedAt,
        -- Canonical seçim skoru (düşük = daha iyi)
        ROW_NUMBER() OVER (
            PARTITION BY TRIM(p.Name)
            ORDER BY 
                CASE WHEN p.katana_order_id IS NULL THEN 0 ELSE 1 END, -- Manuel önce
                p.Id,                                                    -- En küçük ID
                LEN(p.SKU)                                              -- En kısa SKU
        ) as SelectionRank
    FROM products p
    WHERE p.IsActive = 1
),
DuplicateGroups AS (
    SELECT CleanName
    FROM ProductGroups
    GROUP BY CleanName
    HAVING COUNT(*) > 1
)
INSERT INTO @MergeTable (TrashProductId, MasterProductId, MasterSKU, ProductName, Reason)
SELECT 
    pg_trash.Id as TrashProductId,
    pg_master.Id as MasterProductId,
    pg_master.SKU as MasterSKU,
    pg_master.Name as ProductName,
    CASE 
        WHEN pg_master.katana_order_id IS NULL AND pg_trash.katana_order_id IS NOT NULL 
            THEN 'Manuel ürün tercih edildi (sipariş ürünü birleştirildi)'
        WHEN pg_master.Id < pg_trash.Id 
            THEN 'En küçük ID tercih edildi'
        ELSE 'En kısa SKU tercih edildi'
    END as Reason
FROM ProductGroups pg_trash
INNER JOIN DuplicateGroups dg ON pg_trash.CleanName = dg.CleanName
INNER JOIN ProductGroups pg_master ON pg_trash.CleanName = pg_master.CleanName 
    AND pg_master.SelectionRank = 1
WHERE pg_trash.SelectionRank > 1;  -- Sadece duplicate olanlar

-- İstatistikler
DECLARE @TrashCount INT = (SELECT COUNT(*) FROM @MergeTable);
DECLARE @MasterCount INT = (SELECT COUNT(DISTINCT MasterProductId) FROM @MergeTable);

PRINT '';
PRINT '========================================';
PRINT 'BİRLEŞTİRME PLANI';
PRINT '========================================';
PRINT 'Birleştirilecek Ürün Sayısı: ' + CAST(@TrashCount AS NVARCHAR(10));
PRINT 'Ana (Master) Ürün Sayısı: ' + CAST(@MasterCount AS NVARCHAR(10));
PRINT '';

-- Eğer birleştirilecek ürün yoksa işlemi durdur
IF @TrashCount = 0
BEGIN
    PRINT '⚠️ UYARI: Birleştirilecek duplicate ürün bulunamadı!';
    PRINT 'İşlem iptal ediliyor...';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Detaylı plan göster
PRINT 'Birleştirme Detayları (İlk 20):';
SELECT TOP 20
    ProductName as UrunIsmi,
    MasterSKU as AnaSKU,
    COUNT(*) as BirlestirilenAdet,
    MAX(Reason) as Sebep
FROM @MergeTable
GROUP BY ProductName, MasterSKU
ORDER BY COUNT(*) DESC, ProductName;

PRINT '';
PRINT '========================================';
PRINT 'VERİ AKTARIMI BAŞLIYOR';
PRINT '========================================';

-- 3. ADIM: Sipariş Satırlarını Master'a Bağla
UPDATE sol
SET sol.SKU = mt.MasterSKU,
    sol.KatanaOrderId = NULL
FROM SalesOrderLines sol
INNER JOIN products pTrash ON sol.SKU = pTrash.SKU
INNER JOIN @MergeTable mt ON pTrash.Id = mt.TrashProductId;

PRINT 'SalesOrderLines güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 4. ADIM: Sipariş Başlıklarını PENDING Yap
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

PRINT 'SalesOrders PENDING yapıldı: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 5. ADIM: StockMovements Aktar
UPDATE sm 
SET sm.ProductId = mt.MasterProductId
FROM StockMovements sm 
INNER JOIN @MergeTable mt ON sm.ProductId = mt.TrashProductId;

PRINT 'StockMovements güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 6. ADIM: BillOfMaterials Aktar (ProductId)
UPDATE bom 
SET bom.ProductId = mt.MasterProductId
FROM BillOfMaterials bom 
INNER JOIN @MergeTable mt ON bom.ProductId = mt.TrashProductId;

PRINT 'BillOfMaterials (ProductId) güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 7. ADIM: BillOfMaterials Aktar (MaterialId)
UPDATE bom 
SET bom.MaterialId = mt.MasterProductId
FROM BillOfMaterials bom 
INNER JOIN @MergeTable mt ON bom.MaterialId = mt.TrashProductId;

PRINT 'BillOfMaterials (MaterialId) güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 8. ADIM: InvoiceItems Aktar
UPDATE ii 
SET ii.ProductId = mt.MasterProductId
FROM InvoiceItems ii 
INNER JOIN @MergeTable mt ON ii.ProductId = mt.TrashProductId;

PRINT 'InvoiceItems güncellendi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 9. ADIM: ProductVariants Sil
DELETE pv 
FROM ProductVariants pv 
INNER JOIN @MergeTable mt ON pv.ProductId = mt.TrashProductId;

PRINT 'ProductVariants silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 10. ADIM: Stocks Sil
DELETE s 
FROM Stocks s 
INNER JOIN @MergeTable mt ON s.ProductId = mt.TrashProductId;

PRINT 'Stocks silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 11. ADIM: Duplicate Ürünleri Sil
DELETE p 
FROM products p 
INNER JOIN @MergeTable mt ON p.Id = mt.TrashProductId;

PRINT 'Duplicate ürünler silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- ============================================
-- SONUÇ RAPORU
-- ============================================

PRINT '========================================';
PRINT 'İŞLEM TAMAMLANDI';
PRINT '========================================';
PRINT '';

-- Kalan duplicate kontrolü
DECLARE @RemainingDuplicates INT = (
    SELECT COUNT(*)
    FROM (
        SELECT TRIM(Name) as CleanName
        FROM products
        WHERE IsActive = 1
        GROUP BY TRIM(Name)
        HAVING COUNT(*) > 1
    ) as remaining
);

PRINT 'Kalan Duplicate Grup Sayısı: ' + CAST(@RemainingDuplicates AS NVARCHAR(10));

-- Pending sipariş sayısı
DECLARE @PendingOrders INT = (
    SELECT COUNT(*) 
    FROM SalesOrders 
    WHERE Status = 'Pending'
);

PRINT 'Pending Sipariş Sayısı: ' + CAST(@PendingOrders AS NVARCHAR(10));
PRINT '';

-- Birleştirilen ürünler özeti
PRINT 'Birleştirilen Ürünler Özeti (İlk 50):';
SELECT TOP 50
    ProductName as AnaUrun,
    MasterSKU as AnaSKU,
    COUNT(*) as BirlestirilenAdet,
    MAX(Reason) as Sebep
FROM @MergeTable
GROUP BY ProductName, MasterSKU
ORDER BY COUNT(*) DESC, ProductName;

PRINT '';
PRINT '========================================';
PRINT 'ONAY GEREKLİ';
PRINT '========================================';
PRINT '';
PRINT '⚠️ KONTROL ET! Eğer sonuç doğruysa:';
PRINT '   COMMIT TRANSACTION;  -- Bu satırı çalıştır';
PRINT '';
PRINT 'Eğer hata varsa geri almak için:';
PRINT '   ROLLBACK TRANSACTION;  -- Bu satırı çalıştır';
PRINT '';

-- COMMIT TRANSACTION;  -- Onaylamak için bu satırı çalıştır
-- ROLLBACK TRANSACTION;  -- Geri almak için bu satırı çalıştır

GO
