# Test script to verify old orders (SO-41, SO-47) are being synced
# This tests the fix for date filtering in GetSalesOrdersBatchedAsync

$baseUrl = "http://localhost:5000"
$apiKey = "your-api-key-here"  # Replace with actual API key

Write-Host "🧪 Testing Old Orders Sync Fix" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check current database state
Write-Host "📊 Test 1: Checking current SalesOrders in database..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/salesorders" `
        -Method GET `
        -Headers @{ "X-API-Key" = $apiKey }
    
    $existingOrders = $response | Where-Object { $_.orderNo -in @("SO-41", "SO-47") }
    
    if ($existingOrders.Count -gt 0) {
        Write-Host "✅ Found existing orders in database:" -ForegroundColor Green
        $existingOrders | ForEach-Object {
            Write-Host "   - $($_.orderNo) (Status: $($_.status), Created: $($_.orderCreatedDate))" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  SO-41 and SO-47 not found in database yet" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Failed to check database: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Trigger manual sync (without days parameter = fetch all open orders)
Write-Host "🔄 Test 2: Triggering manual sync (all open orders)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sync/from-katana/sales-orders" `
        -Method POST `
        -Headers @{ "X-API-Key" = $apiKey } `
        -ContentType "application/json"
    
    Write-Host "✅ Sync completed:" -ForegroundColor Green
    Write-Host "   - Message: $($response.message)" -ForegroundColor Gray
    Write-Host "   - Processed: $($response.processedRecords)" -ForegroundColor Gray
    Write-Host "   - Successful: $($response.successfulRecords)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Sync failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Verify SO-41 and SO-47 are now in database
Write-Host "🔍 Test 3: Verifying old orders are now synced..." -ForegroundColor Yellow
Start-Sleep -Seconds 2  # Wait for database to update

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/salesorders" `
        -Method GET `
        -Headers @{ "X-API-Key" = $apiKey }
    
    $targetOrders = $response | Where-Object { $_.orderNo -in @("SO-41", "SO-47") }
    
    if ($targetOrders.Count -eq 2) {
        Write-Host "✅ SUCCESS! Both old orders found in database:" -ForegroundColor Green
        $targetOrders | ForEach-Object {
            Write-Host "   - $($_.orderNo)" -ForegroundColor Green
            Write-Host "     Status: $($_.status)" -ForegroundColor Gray
            Write-Host "     Customer ID: $($_.customerId)" -ForegroundColor Gray
            Write-Host "     Created: $($_.orderCreatedDate)" -ForegroundColor Gray
            Write-Host "     Total: $($_.total) $($_.currency)" -ForegroundColor Gray
        }
    } elseif ($targetOrders.Count -eq 1) {
        Write-Host "⚠️  PARTIAL: Only 1 order found:" -ForegroundColor Yellow
        Write-Host "   - $($targetOrders[0].orderNo)" -ForegroundColor Yellow
    } else {
        Write-Host "❌ FAILED: Old orders still not in database" -ForegroundColor Red
        Write-Host "   This means the fix didn't work or orders don't exist in Katana" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Failed to verify: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 To test directly against Katana API:" -ForegroundColor Cyan
Write-Host "   curl -H 'Authorization: Bearer YOUR_KATANA_API_KEY' \" -ForegroundColor Gray
Write-Host "        'https://api.katanamrp.com/v1/sales_orders?status=NOT_SHIPPED&limit=100'" -ForegroundColor Gray
