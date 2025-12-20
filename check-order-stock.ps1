# Sipariş 3002'nin stok hareketlerini kontrol et
$baseUrl = "http://localhost:8080"
$apiBase = "$baseUrl/api"

# Login
$loginBody = @{
    username = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SİPARİŞ 3002 - STOK HAREKETLERİ" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Sipariş detayı
Write-Host "[1] Sipariş Detayı:" -ForegroundColor Yellow
$order = Invoke-RestMethod -Uri "$apiBase/purchase-orders/3002" -Method Get -Headers $headers
Write-Host "   Order No: $($order.orderNo)" -ForegroundColor Gray
Write-Host "   Status: $($order.status)" -ForegroundColor Gray
Write-Host "   Total: $($order.totalAmount) TL" -ForegroundColor Gray
Write-Host "   Luca Synced: $($order.isSyncedToLuca)" -ForegroundColor Gray
Write-Host "   Last Sync Error: $($order.lastSyncError)" -ForegroundColor Gray
Write-Host ""

# Stok hareketleri
Write-Host "[2] Stok Hareketleri:" -ForegroundColor Yellow
try {
    $movements = Invoke-RestMethod -Uri "$apiBase/stock-movements?sourceDocument=PO-20251211-894508A9" -Method Get -Headers $headers
    if ($movements.Count -gt 0) {
        Write-Host "   ✅ $($movements.Count) stok hareketi bulundu!" -ForegroundColor Green
        foreach ($m in $movements) {
            Write-Host "      • SKU: $($m.productSku), Miktar: $($m.changeQuantity), Tip: $($m.movementType)" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ Stok hareketi bulunamadı!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ⚠️  Stok hareketi sorgulanamadı: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Ürün stoğu
Write-Host "[3] Ürün Stok Durumu (HIZ01):" -ForegroundColor Yellow
try {
    $product = Invoke-RestMethod -Uri "$apiBase/products?sku=HIZ01" -Method Get -Headers $headers
    if ($product) {
        $p = $product[0]
        Write-Host "   Ürün: $($p.name)" -ForegroundColor Gray
        Write-Host "   Mevcut Stok: $($p.stock)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ⚠️  Ürün sorgulanamadı" -ForegroundColor Yellow
}
Write-Host ""
