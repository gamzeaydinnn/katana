-- Product Name Deduplication Tables Migration
-- Created: 2024-12-23
-- Purpose: Add tables for product merge history, keep separate groups, and rollback data
-- Database: SQL Server

-- Merge history table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'merge_history')
BEGIN
    CREATE TABLE merge_history (
        id INT IDENTITY(1,1) PRIMARY KEY,
        canonical_product_id INT NOT NULL,
        canonical_product_name NVARCHAR(500) NOT NULL,
        canonical_product_sku NVARCHAR(200) NOT NULL,
        merged_product_ids NVARCHAR(MAX) NOT NULL, -- JSON array of product IDs
        sales_orders_updated INT DEFAULT 0,
        boms_updated INT DEFAULT 0,
        stock_movements_updated INT DEFAULT 0,
        admin_user_id NVARCHAR(100) NOT NULL,
        admin_user_name NVARCHAR(200) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        status NVARCHAR(50) NOT NULL,
        reason NVARCHAR(MAX),
        CONSTRAINT FK_merge_history_canonical_product 
            FOREIGN KEY (canonical_product_id) 
            REFERENCES products(id)
    );
END
GO

-- Keep separate groups table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'keep_separate_groups')
BEGIN
    CREATE TABLE keep_separate_groups (
        id INT IDENTITY(1,1) PRIMARY KEY,
        product_name NVARCHAR(500) NOT NULL UNIQUE,
        reason NVARCHAR(MAX) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        created_by NVARCHAR(100) NOT NULL,
        removed_at DATETIME2,
        removed_by NVARCHAR(100)
    );
END
GO

-- Merge rollback data table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'merge_rollback_data')
BEGIN
    CREATE TABLE merge_rollback_data (
        id INT IDENTITY(1,1) PRIMARY KEY,
        merge_history_id INT NOT NULL,
        entity_type NVARCHAR(50) NOT NULL, -- 'SalesOrder', 'BOM', 'StockMovement'
        entity_id INT NOT NULL,
        original_product_id INT NOT NULL,
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_merge_rollback_merge_history 
            FOREIGN KEY (merge_history_id) 
            REFERENCES merge_history(id) 
            ON DELETE CASCADE
    );
END
GO

-- Indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_merge_history_canonical')
    CREATE INDEX IX_merge_history_canonical ON merge_history(canonical_product_id);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_merge_history_created_at')
    CREATE INDEX IX_merge_history_created_at ON merge_history(created_at DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_merge_history_status')
    CREATE INDEX IX_merge_history_status ON merge_history(status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_merge_rollback_merge_id')
    CREATE INDEX IX_merge_rollback_merge_id ON merge_rollback_data(merge_history_id);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_merge_rollback_entity')
    CREATE INDEX IX_merge_rollback_entity ON merge_rollback_data(entity_type, entity_id);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_keep_separate_name')
    CREATE INDEX IX_keep_separate_name ON keep_separate_groups(product_name);
GO

PRINT 'Product deduplication tables created successfully';
GO

