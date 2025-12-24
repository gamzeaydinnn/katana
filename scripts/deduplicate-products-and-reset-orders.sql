/*
    Product deduplication and order reset script
    - Merges duplicate Products by Name
    - Keeps canonical product (prefers LucaId, then oldest CreatedAt/Id)
    - Repoints FK references to canonical ProductId
    - Updates SalesOrderLines SKU/ProductName to canonical SKU/name
    - Sets affected sales orders to Pending state (OnayFlag = 0, IsSyncedToLuca = 0) for re-approval
    - Deletes duplicate product rows

    Usage: run on the Katana SQL Server database
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ts VARCHAR(14) = CONVERT(VARCHAR(8), GETDATE(), 112) + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '');

PRINT '=== Starting product deduplication @ ' + @ts + ' ===';

BEGIN TRAN;

-------------------------------------------------------------------------------
-- 1) Backups
-------------------------------------------------------------------------------
DECLARE @prodBackup NVARCHAR(128) = QUOTENAME('Products_Backup_' + @ts);
DECLARE @variantBackup NVARCHAR(128) = QUOTENAME('ProductVariants_Backup_' + @ts);
DECLARE @salesOrderLinesBackup NVARCHAR(128) = QUOTENAME('SalesOrderLines_Backup_' + @ts);

DECLARE @sql NVARCHAR(MAX);

-- Products backup
IF OBJECT_ID(@prodBackup, 'U') IS NOT NULL
BEGIN
    RAISERROR('Backup table %s already exists. Aborting to keep data safe.', 16, 1, @prodBackup);
    ROLLBACK;
    RETURN;
END

SET @sql = N'SELECT * INTO ' + @prodBackup + N' FROM Products;';
EXEC sp_executesql @sql;
PRINT 'Products backed up to ' + @prodBackup;

-- ProductVariants backup
IF OBJECT_ID(@variantBackup, 'U') IS NULL
BEGIN
    SET @sql = N'SELECT * INTO ' + @variantBackup + N' FROM ProductVariants;';
    EXEC sp_executesql @sql;
    PRINT 'ProductVariants backed up to ' + @variantBackup;
END
ELSE
BEGIN
    PRINT 'WARNING: ProductVariants backup table already exists, skipping backup.';
END

-- SalesOrderLines backup (only structure/data, not FK linked)
IF OBJECT_ID(@salesOrderLinesBackup, 'U') IS NULL
BEGIN
    SET @sql = N'SELECT * INTO ' + @salesOrderLinesBackup + N' FROM SalesOrderLines;';
    EXEC sp_executesql @sql;
    PRINT 'SalesOrderLines backed up to ' + @salesOrderLinesBackup;
END
ELSE
BEGIN
    PRINT 'WARNING: SalesOrderLines backup table already exists, skipping backup.';
END

-------------------------------------------------------------------------------
-- 2) Identify duplicates by Name
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#DuplicateProducts') IS NOT NULL DROP TABLE #DuplicateProducts;
IF OBJECT_ID('tempdb..#ProductCanonicalMap') IS NOT NULL DROP TABLE #ProductCanonicalMap;

-- All products in duplicated name groups
SELECT p.*,
       ROW_NUMBER() OVER (
           PARTITION BY p.Name
           ORDER BY CASE WHEN p.LucaId IS NOT NULL THEN 0 ELSE 1 END, p.CreatedAt, p.Id
       ) AS CanonicalRank
INTO #DuplicateProducts
FROM Products p
WHERE p.Name IN (
    SELECT Name FROM Products GROUP BY Name HAVING COUNT(*) > 1
);

-- Map duplicate -> canonical
SELECT
    dp.Name,
    CanonicalProductId = MIN(CASE WHEN dp.CanonicalRank = 1 THEN dp.Id END),
    CanonicalSKU = MIN(CASE WHEN dp.CanonicalRank = 1 THEN dp.SKU END),
    DuplicateProductId = dp.Id,
    DuplicateSKU = dp.SKU
INTO #ProductCanonicalMap
FROM #DuplicateProducts dp
GROUP BY dp.Name, dp.Id, dp.SKU;

DECLARE @duplicateGroups INT = (SELECT COUNT(DISTINCT Name) FROM #ProductCanonicalMap);
DECLARE @duplicateRows INT = (SELECT COUNT(*) FROM #ProductCanonicalMap WHERE DuplicateProductId <> CanonicalProductId);

PRINT CONCAT('Found ', @duplicateGroups, ' duplicate name groups, ', @duplicateRows, ' duplicate product rows.');

IF @duplicateRows = 0
BEGIN
    PRINT 'No duplicates to process. Rolling back to leave database untouched.';
    ROLLBACK;
    RETURN;
END

-------------------------------------------------------------------------------
-- 3) Repoint FK references to canonical ProductId
-------------------------------------------------------------------------------
-- Helper: updates a single table with ProductId FK
DECLARE @updates TABLE(TableName NVARCHAR(128), RowsAffected INT);

-- ProductVariants
UPDATE pv
SET ProductId = m.CanonicalProductId
FROM ProductVariants pv
JOIN #ProductCanonicalMap m ON pv.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('ProductVariants', @@ROWCOUNT);

-- VariantMappings
UPDATE vm
SET ProductId = m.CanonicalProductId
FROM VariantMappings vm
JOIN #ProductCanonicalMap m ON vm.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('VariantMappings', @@ROWCOUNT);

-- Stocks
UPDATE s
SET ProductId = m.CanonicalProductId
FROM Stocks s
JOIN #ProductCanonicalMap m ON s.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('Stocks', @@ROWCOUNT);

-- StockMovements
UPDATE sm
SET ProductId = m.CanonicalProductId,
    ProductSku = m.CanonicalSKU
FROM StockMovements sm
JOIN #ProductCanonicalMap m ON sm.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('StockMovements', @@ROWCOUNT);

-- InventoryMovements
UPDATE im
SET ProductId = m.CanonicalProductId
FROM InventoryMovements im
JOIN #ProductCanonicalMap m ON im.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('InventoryMovements', @@ROWCOUNT);

-- Batches
UPDATE b
SET ProductId = m.CanonicalProductId
FROM Batches b
JOIN #ProductCanonicalMap m ON b.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('Batches', @@ROWCOUNT);

-- BillOfMaterials
UPDATE bom
SET ProductId = m.CanonicalProductId
FROM BillOfMaterials bom
JOIN #ProductCanonicalMap m ON bom.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('BillOfMaterials', @@ROWCOUNT);

-- StockTransfers
UPDATE st
SET ProductId = m.CanonicalProductId
FROM StockTransfers st
JOIN #ProductCanonicalMap m ON st.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('StockTransfers', @@ROWCOUNT);

-- PurchaseOrderItems
UPDATE poi
SET ProductId = m.CanonicalProductId
FROM PurchaseOrderItems poi
JOIN #ProductCanonicalMap m ON poi.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('PurchaseOrderItems', @@ROWCOUNT);

-- ManufacturingOrders
UPDATE mo
SET ProductId = m.CanonicalProductId
FROM ManufacturingOrders mo
JOIN #ProductCanonicalMap m ON mo.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('ManufacturingOrders', @@ROWCOUNT);

-- SupplierPrices
UPDATE sp
SET ProductId = m.CanonicalProductId
FROM SupplierPrices sp
JOIN #ProductCanonicalMap m ON sp.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('SupplierPrices', @@ROWCOUNT);

-- InvoiceItems
UPDATE ii
SET ProductId = m.CanonicalProductId
FROM InvoiceItems ii
JOIN #ProductCanonicalMap m ON ii.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('InvoiceItems', @@ROWCOUNT);

-- OrderItems
UPDATE oi
SET ProductId = m.CanonicalProductId
FROM OrderItems oi
JOIN #ProductCanonicalMap m ON oi.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('OrderItems', @@ROWCOUNT);

-- PendingStockAdjustments
UPDATE psa
SET ProductId = m.CanonicalProductId,
    Sku = m.CanonicalSKU
FROM PendingStockAdjustments psa
JOIN #ProductCanonicalMap m ON psa.ProductId = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
INSERT INTO @updates VALUES('PendingStockAdjustments', @@ROWCOUNT);

-------------------------------------------------------------------------------
-- 4) Update SalesOrderLines SKU/ProductName to canonical
-------------------------------------------------------------------------------
UPDATE sol
SET SKU = m.CanonicalSKU,
    ProductName = COALESCE(NULLIF(sol.ProductName, ''), m.Name)
FROM SalesOrderLines sol
JOIN #ProductCanonicalMap m ON sol.SKU = m.DuplicateSKU
WHERE m.DuplicateProductId <> m.CanonicalProductId;
DECLARE @updatedSalesLines INT = @@ROWCOUNT;
INSERT INTO @updates VALUES('SalesOrderLines (SKU/ProductName)', @updatedSalesLines);

-------------------------------------------------------------------------------
-- 5) Reset affected sales orders to pending/unapproved for re-approval
-------------------------------------------------------------------------------
UPDATE so
SET OnayFlag = 0,
    IsSyncedToLuca = 0,
    LastSyncAt = NULL,
    LastSyncError = NULL
FROM SalesOrders so
WHERE EXISTS (
    SELECT 1
    FROM SalesOrderLines sol
    JOIN #ProductCanonicalMap m ON sol.SKU IN (m.DuplicateSKU, m.CanonicalSKU)
    WHERE sol.SalesOrderId = so.Id
);
DECLARE @ordersReset INT = @@ROWCOUNT;
INSERT INTO @updates VALUES('SalesOrders reset to Pending', @ordersReset);

-------------------------------------------------------------------------------
-- 6) Remove duplicate product rows (keep canonical)
-------------------------------------------------------------------------------
DELETE p
FROM Products p
JOIN #ProductCanonicalMap m ON p.Id = m.DuplicateProductId
WHERE m.DuplicateProductId <> m.CanonicalProductId;
DECLARE @deletedProducts INT = @@ROWCOUNT;
PRINT CONCAT('Deleted duplicate products: ', @deletedProducts);

-------------------------------------------------------------------------------
-- 7) Summary
-------------------------------------------------------------------------------
PRINT '--- Rows affected ---';
SELECT TableName, RowsAffected FROM @updates WHERE RowsAffected > 0 ORDER BY TableName;

PRINT '--- Remaining duplicate groups after cleanup (should be 0) ---';
SELECT Name, COUNT(*) AS Cnt
FROM Products
GROUP BY Name
HAVING COUNT(*) > 1;

COMMIT;
PRINT '=== Product deduplication completed successfully ===';
