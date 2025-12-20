-- Katana → Luca Append-Only Senkronizasyon için Mapping Tablosu
-- Luca'da güncelleme endpoint'i olmadığından, her ürün değişikliğinde yeni versiyon oluşturulur
-- Örnek: SKU → SKU-V2 → SKU-V3 ...

-- Eğer tablo varsa önce sil (development ortamı için)
IF OBJECT_ID('dbo.ProductLucaMappings', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ProductLucaMappings;
END
GO

CREATE TABLE ProductLucaMappings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    
    -- Katana bilgileri
    KatanaProductId NVARCHAR(100) NOT NULL,
    KatanaSku NVARCHAR(100) NOT NULL,
    
    -- Luca bilgileri
    LucaStockCode NVARCHAR(100) NOT NULL,  -- Versiyonlu SKU: SKU-V2, SKU-V3...
    LucaStockId BIGINT NULL,               -- Luca'dan dönen ID
    
    -- Versiyon yönetimi
    Version INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,       -- Sadece 1 aktif kayıt olur
    
    -- Sync durumu
    SyncStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING',  -- PENDING/SYNCED/FAILED
    
    -- Değişiklik kontrolü için son senkronize edilen veriler
    SyncedProductName NVARCHAR(500) NULL,
    SyncedPrice DECIMAL(18,2) NULL,
    SyncedVatRate INT NULL,
    SyncedBarcode NVARCHAR(100) NULL,
    
    -- Hata yönetimi
    LastSyncError NVARCHAR(MAX) NULL,
    SyncedAt DATETIME2 NULL,
    
    -- Audit
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100) NULL,
    
    -- Indexler
    INDEX IX_KatanaProductId NONCLUSTERED (KatanaProductId),
    INDEX IX_SyncStatus NONCLUSTERED (SyncStatus)
);
GO

-- Aktif kayıtlar için filtered index
CREATE UNIQUE NONCLUSTERED INDEX IX_Active_KatanaProductId 
ON ProductLucaMappings (KatanaProductId) 
WHERE IsActive = 1;
GO

-- LucaStockCode unique olmalı
CREATE UNIQUE NONCLUSTERED INDEX IX_LucaStockCode 
ON ProductLucaMappings (LucaStockCode);
GO

PRINT 'ProductLucaMappings tablosu başarıyla oluşturuldu.';
GO
