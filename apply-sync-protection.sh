#!/bin/bash

echo "============================================================================"
echo "SYNC PROTECTION CONSTRAINTS - 24 Aralık 2024"
echo "============================================================================"
echo ""

# SQL Server connection details
SERVER="localhost"
DB="KatanaDB"
USER="sa"
PASS='Admin00!S'

# Function to run SQL query with QUOTED_IDENTIFIER ON
run_query() {
    local query="$1"
    docker exec katana-mssql /opt/mssql-tools18/bin/sqlcmd \
        -S "$SERVER" -U "$USER" -P "$PASS" -d "$DB" \
        -C -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; $query" -W 2>&1
}

echo "1️⃣ PRODUCTS — SKU UNIQUE CONSTRAINT"
echo "------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Products_SKU' AND object_id = OBJECT_ID('Products'))
BEGIN
    ALTER TABLE Products ADD CONSTRAINT UQ_Products_SKU UNIQUE (SKU);
    PRINT 'UQ_Products_SKU eklendi';
END
ELSE
    PRINT 'UQ_Products_SKU zaten mevcut';
"
echo ""

echo "2️⃣ PRODUCTS — KatanaProductId UNIQUE INDEX"
echo "--------------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Products_KatanaProductId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE UNIQUE INDEX UX_Products_KatanaProductId ON Products (katana_product_id) WHERE katana_product_id IS NOT NULL;
    PRINT 'UX_Products_KatanaProductId eklendi';
END
ELSE
    PRINT 'UX_Products_KatanaProductId zaten mevcut';
"
echo ""

echo "3️⃣ PRODUCTS — IMMUTABLE KEYS TRIGGER"
echo "--------------------------------------"
run_query "
CREATE OR ALTER TRIGGER TR_Products_Immutable_Keys ON Products AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE i.SKU <> d.SKU OR ISNULL(i.katana_product_id, -1) <> ISNULL(d.katana_product_id, -1))
    BEGIN
        RAISERROR('BLOCKED: SKU and KatanaProductId are IMMUTABLE', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
"
echo "TR_Products_Immutable_Keys oluşturuldu"
echo ""

echo "4️⃣ PRODUCTS — BLOCK LUCA SOURCE TRIGGER"
echo "-----------------------------------------"
run_query "
CREATE OR ALTER TRIGGER TR_Block_Luca_Product_Create ON Products AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE Source IN ('LUCA', 'Luca', 'luca'))
    BEGIN
        RAISERROR('BLOCKED: Luca-sourced products cannot be created. Katana is Source of Truth.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
"
echo "TR_Block_Luca_Product_Create oluşturuldu"
echo ""

echo "5️⃣ ORDER MAPPINGS — ExternalOrderId UNIQUE INDEX"
echo "--------------------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_OrderMappings_ExternalOrderId' AND object_id = OBJECT_ID('OrderMappings'))
BEGIN
    CREATE UNIQUE INDEX UQ_OrderMappings_ExternalOrderId ON OrderMappings (ExternalOrderId, EntityType) WHERE ExternalOrderId IS NOT NULL;
    PRINT 'UQ_OrderMappings_ExternalOrderId eklendi';
END
ELSE
    PRINT 'UQ_OrderMappings_ExternalOrderId zaten mevcut';
"
echo ""

echo "6️⃣ ORDER MAPPINGS — LucaInvoiceId UNIQUE INDEX"
echo "------------------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_OrderMappings_LucaInvoiceId' AND object_id = OBJECT_ID('OrderMappings'))
BEGIN
    CREATE UNIQUE INDEX UQ_OrderMappings_LucaInvoiceId ON OrderMappings (LucaInvoiceId) WHERE LucaInvoiceId IS NOT NULL AND LucaInvoiceId > 0;
    PRINT 'UQ_OrderMappings_LucaInvoiceId eklendi';
END
ELSE
    PRINT 'UQ_OrderMappings_LucaInvoiceId zaten mevcut';
"
echo ""

echo "7️⃣ SALES ORDERS — KatanaOrderId UNIQUE INDEX"
echo "----------------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SalesOrders_KatanaOrderId' AND object_id = OBJECT_ID('SalesOrders'))
BEGIN
    CREATE UNIQUE INDEX UX_SalesOrders_KatanaOrderId ON SalesOrders (KatanaOrderId) WHERE KatanaOrderId IS NOT NULL AND KatanaOrderId > 0;
    PRINT 'UX_SalesOrders_KatanaOrderId eklendi';
END
ELSE
    PRINT 'UX_SalesOrders_KatanaOrderId zaten mevcut';
"
echo ""

echo "8️⃣ SALES ORDER LINES — FK to SalesOrders"
echo "------------------------------------------"
run_query "
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesOrderLines_SalesOrder')
BEGIN
    ALTER TABLE SalesOrderLines ADD CONSTRAINT FK_SalesOrderLines_SalesOrder FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id);
    PRINT 'FK_SalesOrderLines_SalesOrder eklendi';
END
ELSE
    PRINT 'FK_SalesOrderLines_SalesOrder zaten mevcut';
"
echo ""

echo "============================================================================"
echo "✅ TÜM SYNC PROTECTION CONSTRAINTS UYGULANDI"
echo "============================================================================"
