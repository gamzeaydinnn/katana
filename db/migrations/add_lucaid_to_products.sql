-- Migration: Add LucaId column to Products table
-- Date: 2024-12-17
-- Purpose: Store Luca/Koza skartId for fast delete operations (avoids 90-second lookup)

-- Check if column exists and add if not
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LucaId')
BEGIN
    ALTER TABLE Products ADD LucaId BIGINT NULL;
    
    PRINT 'LucaId column added to Products table.';
END
ELSE
BEGIN
    PRINT 'LucaId column already exists.';
END
GO

-- Create index for fast lookup by LucaId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_LucaId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_LucaId ON Products(LucaId) WHERE LucaId IS NOT NULL;
    
    PRINT 'Index IX_Products_LucaId created.';
END
GO

-- Backfill existing products with their LucaId from Luca (manual step)
-- After running sync, this will be populated automatically.
-- For immediate use, you can run this query to see products without LucaId:
-- SELECT SKU, Name FROM Products WHERE LucaId IS NULL;

PRINT 'Migration complete. Run Koza Sync to populate LucaId values.';
