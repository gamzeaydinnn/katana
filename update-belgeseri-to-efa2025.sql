-- =====================================================
-- Script: Update BelgeSeri from "A" to "EFA2025"
-- Description: Updates all hardcoded "A" values in database
-- Date: 2024-12-15
-- =====================================================

USE KatanaDB;
GO

-- Backup existing data (optional but recommended)
PRINT 'Starting BelgeSeri update from "A" to "EFA2025"...';
GO

-- 1. Update SalesOrders table
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrders' AND COLUMN_NAME = 'BelgeSeri')
BEGIN
    PRINT 'Updating SalesOrders table...';
    
    UPDATE SalesOrders
    SET BelgeSeri = 'EFA2025'
    WHERE BelgeSeri = 'A' OR BelgeSeri IS NULL;
    
    PRINT 'SalesOrders updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows affected.';
END
ELSE
BEGIN
    PRINT 'SalesOrders.BelgeSeri column not found - skipping.';
END
GO

-- 2. Update PurchaseOrders table (if exists)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'PurchaseOrders' AND COLUMN_NAME = 'DocumentSeries')
BEGIN
    PRINT 'Updating PurchaseOrders table...';
    
    UPDATE PurchaseOrders
    SET DocumentSeries = 'EFA2025'
    WHERE DocumentSeries = 'A' OR DocumentSeries IS NULL;
    
    PRINT 'PurchaseOrders updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows affected.';
END
ELSE
BEGIN
    PRINT 'PurchaseOrders.DocumentSeries column not found - skipping.';
END
GO

-- 3. Check for any mapping tables that might store BelgeSeri
-- Note: Based on code analysis, BelgeSeri is stored in memory/cache, not in a dedicated mapping table
-- But we'll check common table names just in case

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderMappings')
BEGIN
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'OrderMappings' AND COLUMN_NAME = 'BelgeSeri')
    BEGIN
        PRINT 'Updating OrderMappings table...';
        
        UPDATE OrderMappings
        SET BelgeSeri = 'EFA2025'
        WHERE BelgeSeri = 'A';
        
        PRINT 'OrderMappings updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows affected.';
    END
END
GO

-- 4. Update any audit/log tables that might have captured the old value
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SyncOperationLogs')
BEGIN
    PRINT 'Note: SyncOperationLogs table exists but typically stores JSON/text data.';
    PRINT 'Manual review recommended if BelgeSeri values are logged.';
END
GO

-- 5. Verification queries
PRINT '';
PRINT '=== VERIFICATION ===';
PRINT '';

-- Check SalesOrders
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalesOrders' AND COLUMN_NAME = 'BelgeSeri')
BEGIN
    DECLARE @SalesOrderCount INT;
    SELECT @SalesOrderCount = COUNT(*) FROM SalesOrders WHERE BelgeSeri = 'EFA2025';
    PRINT 'SalesOrders with BelgeSeri = "EFA2025": ' + CAST(@SalesOrderCount AS VARCHAR(10));
    
    DECLARE @SalesOrderOldCount INT;
    SELECT @SalesOrderOldCount = COUNT(*) FROM SalesOrders WHERE BelgeSeri = 'A';
    PRINT 'SalesOrders with BelgeSeri = "A" (should be 0): ' + CAST(@SalesOrderOldCount AS VARCHAR(10));
END
GO

-- Check PurchaseOrders
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'PurchaseOrders' AND COLUMN_NAME = 'DocumentSeries')
BEGIN
    DECLARE @POCount INT;
    SELECT @POCount = COUNT(*) FROM PurchaseOrders WHERE DocumentSeries = 'EFA2025';
    PRINT 'PurchaseOrders with DocumentSeries = "EFA2025": ' + CAST(@POCount AS VARCHAR(10));
    
    DECLARE @POOldCount INT;
    SELECT @POOldCount = COUNT(*) FROM PurchaseOrders WHERE DocumentSeries = 'A';
    PRINT 'PurchaseOrders with DocumentSeries = "A" (should be 0): ' + CAST(@POOldCount AS VARCHAR(10));
END
GO

PRINT '';
PRINT '=== UPDATE COMPLETE ===';
PRINT 'All BelgeSeri values have been updated from "A" to "EFA2025".';
PRINT 'Please restart the application to ensure all cached values are refreshed.';
GO
