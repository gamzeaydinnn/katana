-- Product category mappings for Luca (MappingTables)
-- Safe to re-run due to NOT EXISTS checks.

IF NOT EXISTS (
    SELECT 1
    FROM MappingTables
    WHERE MappingType = 'PRODUCT_CATEGORY' AND SourceValue = '1RAPRO MAMUL'
)
BEGIN
    INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedAt, UpdatedAt)
    VALUES ('PRODUCT_CATEGORY', '1RAPRO MAMUL', '001', 1, GETUTCDATE(), GETUTCDATE());
END

IF NOT EXISTS (
    SELECT 1
    FROM MappingTables
    WHERE MappingType = 'PRODUCT_CATEGORY' AND SourceValue = '2BFM YARI MAMUL'
)
BEGIN
    INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedAt, UpdatedAt)
    VALUES ('PRODUCT_CATEGORY', '2BFM YARI MAMUL', '002', 1, GETUTCDATE(), GETUTCDATE());
END

IF NOT EXISTS (
    SELECT 1
    FROM MappingTables
    WHERE MappingType = 'PRODUCT_CATEGORY' AND SourceValue = 'YARI MAMUL'
)
BEGIN
    INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedAt, UpdatedAt)
    VALUES ('PRODUCT_CATEGORY', 'YARI MAMUL', '001', 1, GETUTCDATE(), GETUTCDATE());
END
