-- apply_category_mappings.sql
-- Idempotent SQL to add CATEGORY mapping rows for observed source categories.
-- Adjust schema/column names if your MappingTables table uses different names.

SET NOCOUNT ON;

DECLARE @DefaultLucaCode NVARCHAR(200) = '001'; -- <- change if your default is different
DECLARE @CreatedBy NVARCHAR(200) = 'apply_category_mappings.sql';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

BEGIN TRANSACTION;

-- Insert if missing: MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt
IF OBJECT_ID('dbo.MappingTables') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='001')
    BEGIN
        INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt)
        VALUES ('CATEGORY','001', @DefaultLucaCode, 1, @CreatedBy, @Now);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='01')
    BEGIN
        INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt)
        VALUES ('CATEGORY','01', @DefaultLucaCode, 1, @CreatedBy, @Now);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='1')
    BEGIN
        INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt)
        VALUES ('CATEGORY','1', @DefaultLucaCode, 1, @CreatedBy, @Now);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='1MAMUL')
    BEGIN
        INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt)
        VALUES ('CATEGORY','1MAMUL', @DefaultLucaCode, 1, @CreatedBy, @Now);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='3YARI MAMUL')
    BEGIN
        INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt)
        VALUES ('CATEGORY','3YARI MAMUL', @DefaultLucaCode, 1, @CreatedBy, @Now);
    END
END
ELSE
BEGIN
    RAISERROR('Table dbo.MappingTables not found. Adjust the script to match your schema.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

COMMIT TRANSACTION;

PRINT 'apply_category_mappings.sql completed.';
