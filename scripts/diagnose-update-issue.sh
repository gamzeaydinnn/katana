#!/bin/bash

##############################################
# Diagnostic Script for Product Update Errors
# Collects logs and tests endpoints
##############################################

DEPLOY_USER="azureuser"
DEPLOY_HOST="bfmmrp.com"

echo "🔍 Product Update Diagnostic Tool"
echo "=================================="
echo ""

# Test 1: Check API service status
echo "1️⃣ API Service Status"
echo "---------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST 'sudo systemctl status katana-api --no-pager | head -20'
echo ""

# Test 2: Check recent errors
echo "2️⃣ Recent API Errors (last 50 lines)"
echo "------------------------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST 'sudo journalctl -u katana-api -n 50 --no-pager | grep -i "error\|exception\|fail"'
echo ""

# Test 3: Test GET endpoint
echo "3️⃣ Testing GET /api/Products/luca"
echo "---------------------------------"
GET_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" https://bfmmrp.com/api/Products/luca)
echo "$GET_RESPONSE"
echo ""

# Test 4: Test PUT with camelCase
echo "4️⃣ Testing PUT with camelCase JSON"
echo "----------------------------------"
CAMEL_TEST=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
    -X PUT https://bfmmrp.com/api/Products/luca/1001 \
    -H "Content-Type: application/json" \
    -d '{
        "productCode": "SKU-1001",
        "productName": "Test camelCase",
        "unit": "Adet",
        "quantity": 100,
        "unitPrice": 10.50,
        "vatRate": 20
    }')
echo "$CAMEL_TEST"
echo ""

# Test 5: Test PUT with PascalCase
echo "5️⃣ Testing PUT with PascalCase JSON"
echo "-----------------------------------"
PASCAL_TEST=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
    -X PUT https://bfmmrp.com/api/Products/luca/1001 \
    -H "Content-Type: application/json" \
    -d '{
        "ProductCode": "SKU-1001",
        "ProductName": "Test PascalCase",
        "Unit": "Adet",
        "Quantity": 100,
        "UnitPrice": 10.50,
        "VatRate": 20
    }')
echo "$PASCAL_TEST"
echo ""

# Test 6: Check nginx logs
echo "6️⃣ Nginx Error Logs (last 20 lines)"
echo "-----------------------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST 'sudo tail -20 /var/log/nginx/error.log'
echo ""

# Test 7: Check .NET runtime
echo "7️⃣ .NET Runtime Version"
echo "-----------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST 'dotnet --version'
echo ""

# Test 8: Check database connection
echo "8️⃣ Database Connectivity"
echo "------------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST << 'ENDSSH'
    sudo docker exec katana-sqlserver /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P 'Admin00!S' \
        -Q "SELECT TOP 5 Id, Name, SKU, CategoryId FROM Products ORDER BY Id DESC" \
        -W || echo "⚠️ Database connection failed"
ENDSSH
echo ""

# Test 9: Check appsettings
echo "9️⃣ Current Logging Level"
echo "------------------------"
ssh $DEPLOY_USER@$DEPLOY_HOST 'cat /var/www/katana-api/appsettings.json | grep -A 5 "Logging"'
echo ""

echo "✅ Diagnostic complete!"
echo ""
echo "Common issues to check:"
echo "  - HTTP 400: DTO validation failed (missing CategoryId or invalid fields)"
echo "  - HTTP 500: Database connection issue or unhandled exception"
echo "  - Case sensitivity: Check if camelCase vs PascalCase matters"
echo "  - Nginx proxy: Check if Content-Type header is preserved"
