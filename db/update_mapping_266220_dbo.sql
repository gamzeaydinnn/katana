SET NOCOUNT ON;

-- Temizlik ve etkilenen satır sayısını göster
DELETE FROM dbo.MappingTables
WHERE TargetValue = '266220'
  AND MappingType = 'CATEGORY';
PRINT 'DELETED_ROWS:' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Kural 1: Katana'dan '1MAMUL' gelirse -> Gerçek Luca ID'si 266220
INSERT INTO dbo.MappingTables (SourceValue, TargetValue, MappingType, IsActive, CreatedAt, UpdatedAt)
VALUES ('1MAMUL', '266220', 'CATEGORY', 1, GETUTCDATE(), GETUTCDATE());
PRINT 'INSERTED_ROWS_AFTER_1:' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Kural 2: Katana'dan '3MAMUL' gelirse -> Gerçek Luca ID'si 266220
INSERT INTO dbo.MappingTables (SourceValue, TargetValue, MappingType, IsActive, CreatedAt, UpdatedAt)
VALUES ('3MAMUL', '266220', 'CATEGORY', 1, GETUTCDATE(), GETUTCDATE());
PRINT 'INSERTED_ROWS_AFTER_2:' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Son durumu göster
SELECT SourceValue, TargetValue, IsActive FROM dbo.MappingTables
WHERE MappingType = 'CATEGORY' AND TargetValue = '266220';

-- Toplam sayıyı da göster
SELECT COUNT(*) AS TotalMatches FROM dbo.MappingTables WHERE MappingType = 'CATEGORY' AND TargetValue = '266220';
