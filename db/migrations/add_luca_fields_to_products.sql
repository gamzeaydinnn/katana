-- Migration: Add Luca sync fields to Products table
-- Date: 2025-12-21
-- Description: Adds Barcode, KategoriAgacKod, PurchasePrice, GtipCode, UzunAdi columns to Products table
--              These fields are needed to persist Luca product data and prevent data loss on sync

-- Add Barcode column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Barcode')
BEGIN
    ALTER TABLE Products ADD Barcode NVARCHAR(100) NULL;
    PRINT 'Added Barcode column to Products table';
END
GO

-- Add KategoriAgacKod column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'KategoriAgacKod')
BEGIN
    ALTER TABLE Products ADD KategoriAgacKod NVARCHAR(50) NULL;
    PRINT 'Added KategoriAgacKod column to Products table';
END
GO

-- Add PurchasePrice column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PurchasePrice')
BEGIN
    ALTER TABLE Products ADD PurchasePrice DECIMAL(18,2) NULL;
    PRINT 'Added PurchasePrice column to Products table';
END
GO

-- Add GtipCode column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'GtipCode')
BEGIN
    ALTER TABLE Products ADD GtipCode NVARCHAR(50) NULL;
    PRINT 'Added GtipCode column to Products table';
END
GO

-- Add UzunAdi column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UzunAdi')
BEGIN
    ALTER TABLE Products ADD UzunAdi NVARCHAR(500) NULL;
    PRINT 'Added UzunAdi column to Products table';
END
GO

PRINT 'Migration completed: Luca sync fields added to Products table';
