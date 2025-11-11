#!/bin/bash

##############################################
# Production Fix: Product Update 400/500 Errors
# Issue: DTO mismatch + CategoryId validation
# Safe deployment with rollback capability
##############################################

set -e  # Exit on error

DEPLOY_USER="azureuser"
DEPLOY_HOST="bfmmrp.com"
API_PATH="/var/www/katana-api"
FRONTEND_PATH="/var/www/katana-web"
SERVICE_NAME="katana-api"

echo "🔧 Production Fix Deployment"
echo "=============================="
echo "Target: $DEPLOY_HOST"
echo "API Path: $API_PATH"
echo ""

# Step 1: Backup current version
echo "📦 Step 1: Creating backup..."
ssh $DEPLOY_USER@$DEPLOY_HOST << 'ENDSSH'
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    sudo mkdir -p /var/backups/katana-api
    sudo cp -r /var/www/katana-api /var/backups/katana-api/backup_$TIMESTAMP
    sudo cp -r /var/www/katana-web /var/backups/katana-web/backup_$TIMESTAMP
    echo "✅ Backup created: backup_$TIMESTAMP"
ENDSSH

# Step 2: Build backend locally
echo ""
echo "🔨 Step 2: Building backend..."
cd "$(dirname "$0")/.."
dotnet publish src/Katana.API/Katana.API.csproj \
    -c Release \
    -o publish \
    --self-contained false

if [ $? -ne 0 ]; then
    echo "❌ Backend build failed!"
    exit 1
fi
echo "✅ Backend build successful"

# Step 3: Build frontend locally
echo ""
echo "🎨 Step 3: Building frontend..."
cd frontend/katana-web
npm install
npm run build

if [ $? -ne 0 ]; then
    echo "❌ Frontend build failed!"
    exit 1
fi
echo "✅ Frontend build successful"
cd ../..

# Step 4: Deploy backend
echo ""
echo "🚀 Step 4: Deploying backend..."
rsync -avz --delete \
    --exclude 'appsettings.json' \
    --exclude 'appsettings.Production.json' \
    publish/ $DEPLOY_USER@$DEPLOY_HOST:$API_PATH/

# Step 5: Deploy frontend
echo ""
echo "🚀 Step 5: Deploying frontend..."
cd frontend/katana-web
rsync -avz --delete \
    build/ $DEPLOY_USER@$DEPLOY_HOST:$FRONTEND_PATH/

cd ../..

# Step 6: Restart services
echo ""
echo "🔄 Step 6: Restarting services..."
ssh $DEPLOY_USER@$DEPLOY_HOST << 'ENDSSH'
    sudo systemctl restart katana-api
    sudo systemctl restart nginx
    sleep 3
    
    # Check service status
    if sudo systemctl is-active --quiet katana-api; then
        echo "✅ API service started successfully"
    else
        echo "❌ API service failed to start!"
        sudo journalctl -u katana-api -n 50 --no-pager
        exit 1
    fi
ENDSSH

# Step 7: Test the fix
echo ""
echo "🧪 Step 7: Testing product update endpoint..."
sleep 2

# Test update endpoint
TEST_RESULT=$(curl -s -o /dev/null -w "%{http_code}" \
    -X PUT https://bfmmrp.com/api/Products/luca/1001 \
    -H "Content-Type: application/json" \
    -d '{
        "productCode": "SKU-1001",
        "productName": "Test Product Fix",
        "unit": "Adet",
        "quantity": 100,
        "unitPrice": 15.50,
        "vatRate": 20
    }')

echo "Test response code: $TEST_RESULT"

if [ "$TEST_RESULT" = "200" ]; then
    echo "✅ Update endpoint working correctly!"
elif [ "$TEST_RESULT" = "404" ]; then
    echo "⚠️  Product not found (expected if demo products not in DB)"
    echo "   Try updating an existing product from admin panel"
else
    echo "❌ Unexpected response: $TEST_RESULT"
    echo ""
    echo "Checking logs..."
    ssh $DEPLOY_USER@$DEPLOY_HOST 'sudo journalctl -u katana-api -n 30 --no-pager'
    exit 1
fi

# Step 8: Display logs
echo ""
echo "📋 Step 8: Recent API logs..."
ssh $DEPLOY_USER@$DEPLOY_HOST 'sudo journalctl -u katana-api -n 20 --no-pager | grep -i "update\|error\|luca"'

echo ""
echo "✅ Deployment complete!"
echo ""
echo "Next steps:"
echo "1. Open https://bfmmrp.com/admin/luca-products"
echo "2. Try updating a product"
echo "3. Check browser console for errors"
echo "4. If issues persist, check logs: sudo journalctl -u katana-api -f"
echo ""
echo "Rollback if needed:"
echo "  ssh $DEPLOY_USER@$DEPLOY_HOST"
echo "  sudo systemctl stop katana-api"
echo "  sudo cp -r /var/backups/katana-api/backup_* $API_PATH/"
echo "  sudo systemctl start katana-api"
