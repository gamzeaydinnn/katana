-- Idempotent mapping seed for CATEGORY mappings
-- This script sets CreatedAt and UpdatedAt to SYSUTCDATETIME() to satisfy NOT NULL constraints.
-- Edit the column list if your MappingTables schema differs.
SET NOCOUNT ON;

IF OBJECT_ID('dbo.MappingTables') IS NULL
BEGIN
    RAISERROR('Table dbo.MappingTables not found. Adjust the script to match your schema.', 16, 1);
    RETURN;
END

DECLARE @now DATETIME2 = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='001')
BEGIN
    INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedBy, CreatedAt, UpdatedAt)
    VALUES ('CATEGORY','001','001','Seed mapping for default 001',1,'migration', @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='01')
BEGIN
    INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedBy, CreatedAt, UpdatedAt)
    VALUES ('CATEGORY','01','001','Seed mapping: 01 -> 001',1,'migration', @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='1')
BEGIN
    INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedBy, CreatedAt, UpdatedAt)
    VALUES ('CATEGORY','1','001','Seed mapping: 1 -> 001 (numeric legacy)',1,'migration', @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='1MAMUL')
BEGIN
    INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedBy, CreatedAt, UpdatedAt)
    VALUES ('CATEGORY','1MAMUL','001','Seed mapping: 1MAMUL -> 001',1,'migration', @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.MappingTables WHERE MappingType='CATEGORY' AND SourceValue='3YARI MAMUL')
BEGIN
    INSERT INTO dbo.MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedBy, CreatedAt, UpdatedAt)
    VALUES ('CATEGORY','3YARI MAMUL','001','Seed mapping: 3YARI MAMUL -> 001',1,'migration', @now, @now);
END

PRINT 'apply_category_mappings_fixed.sql completed.';
