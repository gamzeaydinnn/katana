#!/bin/bash

# =====================================================
# Script: Update BelgeSeri from "A" to "EFA2025"
# Description: Updates database and restarts Docker containers
# Platform: macOS/Linux
# Date: 2024-12-15
# =====================================================

set -e  # Exit on error

echo "=========================================="
echo "BelgeSeri Update Script"
echo "Updating from 'A' to 'EFA2025'"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running. Please start Docker first.${NC}"
    exit 1
fi

echo -e "${YELLOW}Step 1: Checking Docker containers...${NC}"
docker-compose ps

echo ""
echo -e "${YELLOW}Step 2: Updating database...${NC}"

# Execute SQL script in the database container
docker-compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Admin00!S' -d KatanaDB -C << 'EOF'
-- Update SalesOrders table
PRINT 'Updating SalesOrders table...';
GO

UPDATE SalesOrders
SET BelgeSeri = 'EFA2025'
WHERE BelgeSeri = 'A' OR BelgeSeri IS NULL;
GO

PRINT 'SalesOrders updated.';
GO

-- Update PurchaseOrders table (if DocumentSeries column exists)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'PurchaseOrders' AND COLUMN_NAME = 'DocumentSeries')
BEGIN
    PRINT 'Updating PurchaseOrders table...';
    
    UPDATE PurchaseOrders
    SET DocumentSeries = 'EFA2025'
    WHERE DocumentSeries = 'A' OR DocumentSeries IS NULL;
    
    PRINT 'PurchaseOrders updated.';
END
GO

-- Verification
PRINT '';
PRINT '=== VERIFICATION ===';
GO

SELECT COUNT(*) AS 'SalesOrders with EFA2025' 
FROM SalesOrders 
WHERE BelgeSeri = 'EFA2025';
GO

SELECT COUNT(*) AS 'SalesOrders with A (should be 0)' 
FROM SalesOrders 
WHERE BelgeSeri = 'A';
GO

PRINT 'Database update complete!';
GO
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Database updated successfully${NC}"
else
    echo -e "${RED}✗ Database update failed${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 3: Rebuilding and restarting Docker containers...${NC}"

# Stop containers
echo "Stopping containers..."
docker-compose down

# Rebuild the api container (where code changes are)
echo "Rebuilding api container..."
docker-compose build api

# Start all containers
echo "Starting containers..."
docker-compose up -d

# Wait for containers to be healthy
echo "Waiting for containers to start..."
sleep 10

# Check container status
echo ""
echo -e "${YELLOW}Step 4: Checking container status...${NC}"
docker-compose ps

# Check api logs for any startup errors
echo ""
echo -e "${YELLOW}Step 5: Checking api logs...${NC}"
docker-compose logs --tail=50 api | grep -i "error\|exception\|fail" || echo -e "${GREEN}No errors found in recent logs${NC}"

echo ""
echo -e "${GREEN}=========================================="
echo "✓ Update Complete!"
echo "==========================================${NC}"
echo ""
echo "Summary:"
echo "  - Database BelgeSeri updated: A → EFA2025"
echo "  - Docker containers rebuilt and restarted"
echo "  - Application is now using EFA2025 as default"
echo ""
echo "Next steps:"
echo "  1. Test invoice creation: curl http://localhost:8080/api/sync/to-luca/sales-invoice"
echo "  2. Check logs: docker-compose logs -f api"
echo "  3. Verify in Luca that new invoices have BelgeSeri = 'EFA2025'"
echo ""
