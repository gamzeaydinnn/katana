#!/bin/bash

echo "=== Checking Database Tables ==="
echo ""

# SQL Server connection details
SERVER="localhost,1433"
DB="KatanaDB"
USER="sa"
PASS="Admin00!S"

# Function to run SQL query
run_query() {
    local query="$1"
    docker exec katana-db-1 /opt/mssql-tools18/bin/sqlcmd \
        -S "$SERVER" -U "$USER" -P "$PASS" -d "$DB" \
        -C -Q "$query" -W -s"|" 2>&1
}

echo "1. Checking SalesOrders table..."
run_query "SELECT COUNT(*) AS TotalOrders FROM SalesOrders;"
echo ""

echo "2. Checking for specific orders (SO-41, SO-47, SO-56)..."
run_query "SELECT OrderNo, KatanaOrderId, CustomerId, Status, Total FROM SalesOrders WHERE OrderNo IN ('SO-41', 'SO-47', 'SO-56');"
echo ""

echo "3. Checking PendingStockAdjustments for these orders..."
run_query "SELECT ExternalOrderId, Sku, Quantity, Status FROM PendingStockAdjustments WHERE ExternalOrderId IN ('SO-41', 'SO-47', 'SO-56');"
echo ""

echo "4. Checking all SalesOrders (last 10)..."
run_query "SELECT TOP 10 Id, OrderNo, KatanaOrderId, CustomerId, Status, Total, OrderCreatedDate FROM SalesOrders ORDER BY CreatedAt DESC;"
echo ""

echo "5. Checking all PendingStockAdjustments (last 10)..."
run_query "SELECT TOP 10 Id, ExternalOrderId, Sku, Quantity, Status, RequestedBy FROM PendingStockAdjustments ORDER BY RequestedAt DESC;"
echo ""

echo "Done!"
