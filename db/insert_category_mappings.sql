-- =============================================
-- Katana -> Luca Kategori Mapping'leri
-- =============================================
-- Bu script, Katana ürün kategorilerinin Luca kategori kodlarına mapping'ini yapar
-- 
-- Luca Kategori Kodları:
-- 001 = Hammadde
-- 002 = Yarı Mamul
-- 003 = Mamul
-- 004 = Ticari Mal
-- 005 = Yedek Parça
-- 006 = Ambalaj Malzemesi
-- 007 = Demirbaş
-- 008 = Sarf Malzemesi
-- 009 = Hurda
-- 010 = Diğer

USE IntegrationDb;
GO

-- Önce mevcut PRODUCT_CATEGORY mapping'lerini temizle (opsiyonel)
-- DELETE FROM MappingTables WHERE MappingType = 'PRODUCT_CATEGORY';

-- Katana kategorilerini Luca kategorilerine eşle
INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedAt, UpdatedAt, CreatedBy)
VALUES 
    -- Genel Ürün Kategorileri
    ('PRODUCT_CATEGORY', 'Electronics', '004', 'Elektronik ürünler - Ticari Mal', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Clothing', '004', 'Giyim ürünleri - Ticari Mal', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Food', '004', 'Gıda ürünleri - Ticari Mal', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Furniture', '004', 'Mobilya ürünleri - Ticari Mal', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Books', '004', 'Kitap ve yayınlar - Ticari Mal', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    
    -- Üretim İlgili Kategoriler
    ('PRODUCT_CATEGORY', 'Raw Material', '001', 'Ham maddeler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Semi-Finished', '002', 'Yarı mamul ürünler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Finished Goods', '003', 'Bitmiş mamul ürünler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    
    -- Yardımcı Kategoriler
    ('PRODUCT_CATEGORY', 'Spare Parts', '005', 'Yedek parçalar', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Packaging', '006', 'Ambalaj malzemeleri', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Consumables', '008', 'Sarf malzemeleri', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Fixed Assets', '007', 'Demirbaş ürünler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    
    -- Diğer
    ('PRODUCT_CATEGORY', 'Scrap', '009', 'Hurda malzemeler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Other', '010', 'Diğer ürünler', 1, GETDATE(), GETDATE(), 'SYSTEM'),
    ('PRODUCT_CATEGORY', 'Uncategorized', '010', 'Kategorize edilmemiş', 1, GETDATE(), GETDATE(), 'SYSTEM');

GO

-- Eklenen kayıtları kontrol et
SELECT 
    Id,
    MappingType,
    SourceValue AS 'Katana Kategorisi',
    TargetValue AS 'Luca Kodu',
    Description AS 'Açıklama',
    IsActive AS 'Aktif',
    CreatedAt AS 'Oluşturma Tarihi'
FROM MappingTables
WHERE MappingType = 'PRODUCT_CATEGORY'
ORDER BY TargetValue, SourceValue;

GO

-- İstatistik
SELECT 
    COUNT(*) AS 'Toplam Mapping',
    COUNT(CASE WHEN IsActive = 1 THEN 1 END) AS 'Aktif',
    COUNT(CASE WHEN IsActive = 0 THEN 1 END) AS 'Pasif'
FROM MappingTables
WHERE MappingType = 'PRODUCT_CATEGORY';

GO
