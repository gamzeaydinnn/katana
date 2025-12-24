-- Migration: Add KatanaProductId and KatanaOrderId to Products table
-- Purpose: Enable Katana API integration and order-based product grouping
-- Requirements: 5.5, 11.1, 11.2
-- Database: SQL Server

SET QUOTED_IDENTIFIER ON;
GO

-- Add KatanaProductId column for Katana API integration
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.products') AND name = 'katana_product_id')
BEGIN
    ALTER TABLE dbo.products ADD katana_product_id INT NULL;
END
GO

-- Add KatanaOrderId column for order-based grouping
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.products') AND name = 'katana_order_id')
BEGIN
    ALTER TABLE dbo.products ADD katana_order_id BIGINT NULL;
END
GO

-- Create index for faster lookups by KatanaOrderId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.products') AND name = 'idx_products_katana_order_id')
BEGIN
    CREATE NONCLUSTERED INDEX idx_products_katana_order_id 
    ON dbo.products(katana_order_id) 
    WHERE katana_order_id IS NOT NULL;
END
GO

-- Create index for faster lookups by KatanaProductId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.products') AND name = 'idx_products_katana_product_id')
BEGIN
    CREATE NONCLUSTERED INDEX idx_products_katana_product_id 
    ON dbo.products(katana_product_id) 
    WHERE katana_product_id IS NOT NULL;
END
GO

-- Add extended properties (SQL Server equivalent of comments)
IF NOT EXISTS (SELECT * FROM sys.extended_properties WHERE major_id = OBJECT_ID('dbo.products') AND name = 'MS_Description' AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.products') AND name = 'katana_product_id'))
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Katana API product ID - used for deleting products from Katana API during merge', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE',  @level1name = 'products',
        @level2type = N'COLUMN', @level2name = 'katana_product_id';
END
GO

IF NOT EXISTS (SELECT * FROM sys.extended_properties WHERE major_id = OBJECT_ID('dbo.products') AND name = 'MS_Description' AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.products') AND name = 'katana_order_id'))
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Katana order ID - products with same order ID are treated as variants of single product', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE',  @level1name = 'products',
        @level2type = N'COLUMN', @level2name = 'katana_order_id';
END
GO

PRINT 'Migration completed successfully!';
