# Test date format in invoice JSON

$baseUrl = "http://localhost:5000"

Write-Host "🧪 Testing date format in invoice creation..." -ForegroundColor Cyan

# Get a sales order
$orders = Invoke-RestMethod -Uri "$baseUrl/api/salesorders" -Method Get
$order = $orders | Select-Object -First 1

if (-not $order) {
    Write-Host "❌ No sales orders found" -ForegroundColor Red
    exit 1
}

Write-Host "📦 Testing with order: $($order.orderNo)" -ForegroundColor Yellow

# Try to sync to Luca
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/orderinvoicesync/sales/$($order.id)" -Method Post
    Write-Host "✅ Sync result: $($result.message)" -ForegroundColor Green
    
    if ($result.success) {
        Write-Host "✅ Invoice created successfully with ID: $($result.lucaFaturaId)" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Sync failed: $($result.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host "`n📋 Check backend logs for JSON format:" -ForegroundColor Cyan
Write-Host "docker logs katana-backend 2>&1 | Select-String 'siparisTarihi' | Select-Object -Last 5" -ForegroundColor Gray
