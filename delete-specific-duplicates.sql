-- ============================================
-- SPESİFİK DUPLICATE ÜRÜNLERİ SİLME
-- ============================================
-- UYARI: Bu sorgular veriyi kalıcı olarak siler!
-- Önce SELECT sorgularını çalıştırıp kontrol edin!
-- ============================================

USE KatanaDB;
GO

-- ============================================
-- 1. ADIM: SİLİNECEK ÜRÜNLERİ LİSTELE
-- ============================================

-- "SILINECEK" ile başlayan tüm ürünleri listele
SELECT 
    p.Id as UrunID,
    p.Name as UrunIsmi,
    p.SKU,
    p.CategoryId,
    p.IsActive as Aktif,
    p.CreatedAt as OlusturmaTarihi,
    p.katana_order_id as KatanaSiparisID,
    (SELECT COUNT(*) FROM StockMovements WHERE ProductId = p.Id) as StokHareketSayisi,
    (SELECT COUNT(*) FROM Stocks WHERE ProductId = p.Id) as StokKayitSayisi,
    (SELECT COUNT(*) FROM BillOfMaterials WHERE ProductId = p.Id OR MaterialId = p.Id) as ReceteKayitSayisi
FROM products p
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1
ORDER BY p.Name, p.Id;

-- Toplam sayı
SELECT 
    COUNT(*) as ToplamSilinecekUrun,
    COUNT(DISTINCT Name) as FarkliUrunIsmi
FROM products
WHERE Name LIKE 'SILINECEK%'
  AND IsActive = 1;

-- ============================================
-- 2. ADIM: İLİŞKİLİ SİPARİŞLERİ LİSTELE
-- ============================================

-- Bu ürünleri içeren siparişleri bul
SELECT DISTINCT
    so.Id as SiparisID,
    so.OrderNo as SiparisNo,
    so.KatanaOrderId,
    so.Status as Durum,
    so.ApprovedDate as OnayTarihi,
    so.IsSyncedToLuca as LucayaSenkronEdildi,
    COUNT(sol.Id) as SatirSayisi
FROM SalesOrders so
INNER JOIN SalesOrderLines sol ON so.Id = sol.SalesOrderId
INNER JOIN products p ON sol.SKU = p.SKU
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1
GROUP BY so.Id, so.OrderNo, so.KatanaOrderId, so.Status, so.ApprovedDate, so.IsSyncedToLuca
ORDER BY so.Id;

-- ============================================
-- 3. ADIM: İLİŞKİLİ VERİLERİ DETAYLI KONTROL
-- ============================================

-- Stok hareketleri
SELECT 
    p.Name as UrunIsmi,
    p.SKU,
    sm.Id as HareketID,
    sm.MovementType as HareketTipi,
    sm.Quantity as Miktar,
    sm.CreatedAt as Tarih
FROM products p
INNER JOIN StockMovements sm ON p.Id = sm.ProductId
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1
ORDER BY p.Name, sm.CreatedAt DESC;

-- Stok kayıtları
SELECT 
    p.Name as UrunIsmi,
    p.SKU,
    s.Id as StokID,
    s.Quantity as Miktar,
    s.WarehouseId as DepoID
FROM products p
INNER JOIN Stocks s ON p.Id = s.ProductId
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1
ORDER BY p.Name;

-- Reçete kayıtları
SELECT 
    p.Name as UrunIsmi,
    p.SKU,
    bom.Id as ReceteID,
    bom.Quantity as Miktar
FROM products p
INNER JOIN BillOfMaterials bom ON p.Id = bom.ProductId OR p.Id = bom.MaterialId
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1
ORDER BY p.Name;

-- ============================================
-- 4. ADIM: SİLME İŞLEMİ
-- ============================================

-- UYARI: Bu sorgu veriyi kalıcı olarak değiştirir!
-- Önce yukarıdaki SELECT sorgularını çalıştırıp kontrol edin!

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'SPESİFİK ÜRÜN SİLME İŞLEMİ';
PRINT '========================================';
PRINT '';

-- Silinecek ürün ID'lerini geçici tabloya al
DECLARE @ProductsToDelete TABLE (
    ProductId INT,
    ProductName NVARCHAR(200),
    SKU NVARCHAR(50),
    KatanaOrderId BIGINT
);

INSERT INTO @ProductsToDelete (ProductId, ProductName, SKU, KatanaOrderId)
SELECT 
    p.Id,
    p.Name,
    p.SKU,
    p.katana_order_id
FROM products p
WHERE p.Name LIKE 'SILINECEK%'
  AND p.IsActive = 1;

-- Özet bilgi
DECLARE @ProductCount INT = (SELECT COUNT(*) FROM @ProductsToDelete);
DECLARE @UniqueNames INT = (SELECT COUNT(DISTINCT ProductName) FROM @ProductsToDelete);

PRINT 'Silinecek Ürün Sayısı: ' + CAST(@ProductCount AS NVARCHAR(10));
PRINT 'Farklı Ürün İsmi: ' + CAST(@UniqueNames AS NVARCHAR(10));
PRINT '';

-- Etkilenecek siparişleri bul
DECLARE @AffectedOrders TABLE (OrderId INT, OrderNo NVARCHAR(100));

INSERT INTO @AffectedOrders (OrderId, OrderNo)
SELECT DISTINCT
    so.Id,
    so.OrderNo
FROM SalesOrders so
INNER JOIN SalesOrderLines sol ON so.Id = sol.SalesOrderId
INNER JOIN @ProductsToDelete ptd ON sol.SKU = ptd.SKU;

DECLARE @OrderCount INT = (SELECT COUNT(*) FROM @AffectedOrders);
PRINT 'Etkilenecek Sipariş Sayısı: ' + CAST(@OrderCount AS NVARCHAR(10));
PRINT '';

-- ADIM 1: Siparişlerin onayını kaldır ve durumunu sıfırla
UPDATE SalesOrders
SET 
    Status = 'Pending',
    ApprovedDate = NULL,
    ApprovedBy = NULL,
    IsSyncedToLuca = 0,
    LucaOrderId = NULL,
    UpdatedAt = GETDATE()
WHERE Id IN (SELECT OrderId FROM @AffectedOrders);

PRINT 'Sipariş onayları kaldırıldı: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 2: İlişkili InvoiceItems kayıtlarını sil
DELETE FROM InvoiceItems
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Fatura kalemleri silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 3: İlişkili StockMovements kayıtlarını sil
DELETE FROM StockMovements
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Stok hareketleri silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 4: İlişkili Stocks kayıtlarını sil
DELETE FROM Stocks
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Stok kayıtları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 5: İlişkili BillOfMaterials kayıtlarını sil
DELETE FROM BillOfMaterials
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete)
   OR MaterialId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Reçete kayıtları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 6: İlişkili ProductVariants kayıtlarını sil
DELETE FROM ProductVariants
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Varyant kayıtları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 7: İlişkili Batches kayıtlarını sil
DELETE FROM Batches
WHERE ProductId IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Parti kayıtları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 8: İlişkili SalesOrderLines kayıtlarını sil
DELETE FROM SalesOrderLines
WHERE SKU IN (SELECT SKU FROM @ProductsToDelete);

PRINT 'Sipariş satırları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 9: İlişkili PurchaseOrderLines kayıtlarını sil
DELETE FROM PurchaseOrderLines
WHERE SKU IN (SELECT SKU FROM @ProductsToDelete);

PRINT 'Satın alma sipariş satırları silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- ADIM 10: Ürünleri sil
DELETE FROM products
WHERE Id IN (SELECT ProductId FROM @ProductsToDelete);

PRINT 'Ürünler silindi: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- Silme işlemi sonrası kontrol
PRINT '========================================';
PRINT 'İŞLEM TAMAMLANDI';
PRINT '========================================';
PRINT '';
PRINT 'Etkilenen Siparişler:';

SELECT 
    OrderNo as SiparisNo,
    'Onay kaldırıldı - Yeni sipariş gibi işlenecek' as Durum
FROM @AffectedOrders;

PRINT '';
PRINT 'Silinen Ürünler (İlk 50):';

SELECT TOP 50
    ProductName as UrunIsmi,
    SKU,
    KatanaOrderId as KatanaSiparisID
FROM @ProductsToDelete
ORDER BY ProductName;

-- İşlemi onayla veya geri al
PRINT '';
PRINT '⚠️ KONTROL ET! Eğer sonuç doğruysa:';
PRINT '   COMMIT TRANSACTION;  -- Bu satırı çalıştır';
PRINT '';
PRINT 'Eğer hata varsa geri almak için:';
PRINT '   ROLLBACK TRANSACTION;  -- Bu satırı çalıştır';

-- COMMIT TRANSACTION;  -- Onaylamak için bu satırı çalıştır
-- ROLLBACK TRANSACTION;  -- Geri almak için bu satırı çalıştır

-- ============================================
-- 5. ADIM: SİLME SONRASI KONTROL
-- ============================================

-- Hala "SILINECEK" ile başlayan ürün var mı?
SELECT 
    COUNT(*) as KalanSilinecekUrun
FROM products
WHERE Name LIKE 'SILINECEK%'
  AND IsActive = 1;

-- Toplam ürün sayısı
SELECT 
    COUNT(*) as ToplamAktifUrun,
    COUNT(DISTINCT Name) as FarkliUrunIsmi,
    SUM(CASE WHEN katana_order_id IS NOT NULL THEN 1 ELSE 0 END) as SiparistenGelenUrunler,
    SUM(CASE WHEN katana_order_id IS NULL THEN 1 ELSE 0 END) as ManuelUrunler
FROM products
WHERE IsActive = 1;

-- Pending durumundaki siparişler
SELECT 
    Id as SiparisID,
    OrderNo as SiparisNo,
    Status as Durum,
    CreatedAt as OlusturmaTarihi,
    'Yeni sipariş gibi işlenecek' as Aciklama
FROM SalesOrders
WHERE Status = 'Pending'
ORDER BY CreatedAt DESC;

GO
