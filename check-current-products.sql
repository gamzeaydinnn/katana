-- ==========================================================
-- MEVCUT ÜRÜN LİSTESİ KONTROLÜ
-- ==========================================================
-- Database'deki tüm ürünleri kontrol et
-- ==========================================================

USE KatanaDB;
GO

PRINT '========================================';
PRINT 'DATABASE ÜRÜN DURUMU';
PRINT '========================================';
PRINT '';

-- 1. Genel İstatistikler
PRINT '1. GENEL İSTATİSTİKLER:';
PRINT '----------------------------------------';
SELECT 
    COUNT(*) as ToplamUrunSayisi,
    COUNT(CASE WHEN IsActive = 1 THEN 1 END) as AktifUrunler,
    COUNT(CASE WHEN IsActive = 0 THEN 1 END) as PasifUrunler,
    COUNT(CASE WHEN katana_order_id IS NULL THEN 1 END) as ManuelUrunler,
    COUNT(CASE WHEN katana_order_id IS NOT NULL THEN 1 END) as KatanaUrunleri,
    COUNT(CASE WHEN LucaId IS NOT NULL THEN 1 END) as LucayaGonderilmis
FROM products;

PRINT '';
PRINT '2. DUPLICATE KONTROLÜ:';
PRINT '----------------------------------------';
-- Hala duplicate var mı?
SELECT 
    COUNT(DISTINCT Name) as DuplicateGrupSayisi
FROM products
WHERE Name IN (
    SELECT Name 
    FROM products 
    GROUP BY Name 
    HAVING COUNT(*) > 1
);

PRINT '';
PRINT '3. İLK 20 ÜRÜN (Alfabetik):';
PRINT '----------------------------------------';
SELECT TOP 20
    Id,
    Name as UrunIsmi,
    SKU,
    Price as Fiyat,
    CASE WHEN IsActive = 1 THEN 'Aktif' ELSE 'Pasif' END as Durum,
    CASE 
        WHEN katana_order_id IS NULL THEN 'Manuel' 
        ELSE 'Katana-' + CAST(katana_order_id AS VARCHAR) 
    END as Kaynak,
    CASE 
        WHEN LucaId IS NOT NULL THEN 'Evet (' + CAST(LucaId AS VARCHAR) + ')' 
        ELSE 'Hayır' 
    END as LucadaVar,
    CreatedAt as OlusturmaTarihi
FROM products
ORDER BY Name;

PRINT '';
PRINT '4. KATANA ORDER ID BAZLI ÜRÜNLER:';
PRINT '----------------------------------------';
SELECT 
    katana_order_id as KatanaOrderId,
    COUNT(*) as UrunSayisi,
    STRING_AGG(SKU, ', ') as SKUlar
FROM products
WHERE katana_order_id IS NOT NULL
GROUP BY katana_order_id
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT '5. SON EKLENEN 10 ÜRÜN:';
PRINT '----------------------------------------';
SELECT TOP 10
    Id,
    Name as UrunIsmi,
    SKU,
    CASE 
        WHEN katana_order_id IS NULL THEN 'Manuel' 
        ELSE 'Katana' 
    END as Kaynak,
    CreatedAt as EklenmeTarihi
FROM products
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '6. LUCA SENKRONIZASYON DURUMU:';
PRINT '----------------------------------------';
SELECT 
    CASE 
        WHEN LucaId IS NOT NULL THEN 'Lucaya Gönderilmiş'
        ELSE 'Henüz Gönderilmemiş'
    END as LucaDurumu,
    COUNT(*) as UrunSayisi
FROM products
GROUP BY 
    CASE 
        WHEN LucaId IS NOT NULL THEN 'Lucaya Gönderilmiş'
        ELSE 'Henüz Gönderilmemiş'
    END;

PRINT '';
PRINT '========================================';
PRINT 'KONTROL TAMAMLANDI';
PRINT '========================================';

GO
