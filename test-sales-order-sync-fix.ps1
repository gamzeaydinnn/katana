# Test Sales Order Sync Fix
# Bu script, Katana siparişlerinin Luca'ya senkronize edilmesini test eder

Write-Host "=== Katana Sales Order Sync Test ===" -ForegroundColor Cyan
Write-Host ""

# Backend URL
$baseUrl = "http://localhost:5055"

Write-Host "1. Katana'dan siparişleri çekiyoruz..." -ForegroundColor Yellow
try {
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/sync/from-katana/sales-orders?days=7" -Method POST -ContentType "application/json"
    Write-Host "✅ Sync tamamlandı!" -ForegroundColor Green
    Write-Host "   - Yeni sipariş sayısı: $($syncResponse.newOrdersCount)" -ForegroundColor White
    Write-Host "   - Toplam satır sayısı: $($syncResponse.totalRowsCount)" -ForegroundColor White
    if ($syncResponse.message) {
        Write-Host "   - Mesaj: $($syncResponse.message)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Sync hatası: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Veritabanındaki siparişleri kontrol ediyoruz..." -ForegroundColor Yellow
try {
    $ordersResponse = Invoke-RestMethod -Uri "$baseUrl/api/sales-orders?page=1&pageSize=10" -Method GET
    Write-Host "✅ Sipariş listesi alındı!" -ForegroundColor Green
    Write-Host "   - Toplam sipariş sayısı: $($ordersResponse.Count)" -ForegroundColor White
    
    if ($ordersResponse.Count -gt 0) {
        Write-Host ""
        Write-Host "Son siparişler:" -ForegroundColor Cyan
        foreach ($order in $ordersResponse | Select-Object -First 5) {
            Write-Host "   - $($order.orderNo): $($order.customerName) - $($order.total) $($order.currency)" -ForegroundColor White
            Write-Host "     Status: $($order.status), Luca Sync: $($order.isSyncedToLuca)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  Henüz sipariş yok" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Sipariş listesi alınamadı: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Test Tamamlandı ===" -ForegroundColor Green
