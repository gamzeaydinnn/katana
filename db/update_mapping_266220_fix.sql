SET NOCOUNT ON;

-- Eğer (CATEGORY, '1MAMUL') zaten varsa güncelle, yoksa ekle
IF EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType = 'CATEGORY' AND SourceValue = '1MAMUL')
BEGIN
    UPDATE dbo.MappingTables
    SET TargetValue = '266220', IsActive = 1, UpdatedAt = GETUTCDATE()
    WHERE MappingType = 'CATEGORY' AND SourceValue = '1MAMUL';
    PRINT 'UPDATED_ROWS:' + CAST(@@ROWCOUNT AS VARCHAR(10));
END
ELSE
BEGIN
    INSERT INTO dbo.MappingTables (SourceValue, TargetValue, MappingType, IsActive, CreatedAt, UpdatedAt)
    VALUES ('1MAMUL', '266220', 'CATEGORY', 1, GETUTCDATE(), GETUTCDATE());
    PRINT 'INSERTED_ROWS:' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

-- Doğrulama sorgusu
SELECT SourceValue, TargetValue, IsActive, CreatedAt, UpdatedAt FROM dbo.MappingTables
WHERE MappingType = 'CATEGORY' AND TargetValue = '266220';

SELECT COUNT(*) AS TotalMatches FROM dbo.MappingTables WHERE MappingType = 'CATEGORY' AND TargetValue = '266220';
