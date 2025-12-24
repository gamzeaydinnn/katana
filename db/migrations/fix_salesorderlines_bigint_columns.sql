-- Fix SalesOrderLines INT to BIGINT conversion
-- Bu migration KatanaRowId ve VariantId kolonlarını INT'den BIGINT'e çevirir

-- KatanaRowId kolonunu BIGINT yap
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'KatanaRowId' 
           AND DATA_TYPE = 'int')
BEGIN
    ALTER TABLE SalesOrderLines ALTER COLUMN KatanaRowId BIGINT NOT NULL;
    PRINT 'KatanaRowId converted to BIGINT';
END
ELSE
BEGIN
    PRINT 'KatanaRowId is already BIGINT or does not exist';
END

-- VariantId kolonunu BIGINT yap
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'VariantId' 
           AND DATA_TYPE = 'int')
BEGIN
    ALTER TABLE SalesOrderLines ALTER COLUMN VariantId BIGINT NOT NULL;
    PRINT 'VariantId converted to BIGINT';
END
ELSE
BEGIN
    PRINT 'VariantId is already BIGINT or does not exist';
END

-- TaxRateId kolonunu BIGINT yap (nullable)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'TaxRateId' 
           AND DATA_TYPE = 'int')
BEGIN
    ALTER TABLE SalesOrderLines ALTER COLUMN TaxRateId BIGINT NULL;
    PRINT 'TaxRateId converted to BIGINT';
END

-- LocationId kolonunu BIGINT yap (nullable)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'LocationId' 
           AND DATA_TYPE = 'int')
BEGIN
    ALTER TABLE SalesOrderLines ALTER COLUMN LocationId BIGINT NULL;
    PRINT 'LocationId converted to BIGINT';
END

-- KatanaOrderId kolonunu BIGINT yap (nullable)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'KatanaOrderId' 
           AND DATA_TYPE = 'int')
BEGIN
    ALTER TABLE SalesOrderLines ALTER COLUMN KatanaOrderId BIGINT NULL;
    PRINT 'KatanaOrderId converted to BIGINT';
END

PRINT 'Migration completed successfully';
