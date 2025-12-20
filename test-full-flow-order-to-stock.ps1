# 🧪 FULL FLOW TEST: Sipariş → Admin Onay → Katana → Luca Stok Kartı
# Test: Yeni ürün siparişi oluştur, admin onaylasın, Katana'ya gelsin, Luca'da stok kartı oluşsun

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:5147"

Write-Host "🧪 FULL FLOW TEST BAŞLIYOR..." -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Gray

# Test ürünü için unique SKU
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testSku = "TEST-FLOW-$timestamp"
$testName = "Test Flow Product $timestamp"

Write-Host ""
Write-Host "📦 TEST ÜRÜNÜ:" -ForegroundColor Yellow
Write-Host "   SKU: $testSku" -ForegroundColor White
Write-Host "   Name: $testName" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 1: Katana'da Sipariş Oluştur
# ============================================================================
Write-Host "STEP 1: Katana'da sipariş oluşturuluyor..." -ForegroundColor Cyan

$orderPayload = @{
    title = "Test Order - $timestamp"
    notes = "Full flow test order"
    line_items = @(
        @{
            product = @{
                name = $testName
                sku = $testSku
            }
            quantity = 10
            unit_price = 100.50
        }
    )
} | ConvertTo-Json -Depth 10

Write-Host "📤 Sipariş gönderiliyor..." -ForegroundColor Gray
try {
    $orderResponse = Invoke-RestMethod -Uri "$baseUrl/api/katana/orders" `
        -Method Post `
        -Body $orderPayload `
        -ContentType "application/json"
    
    $orderId = $orderResponse.id
    Write-Host "✅ Sipariş oluşturuldu: Order ID = $orderId" -ForegroundColor Green
    Write-Host "   SKU: $testSku" -ForegroundColor White
} catch {
    Write-Host "❌ Sipariş oluşturulamadı: $_" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# ============================================================================
# STEP 2: Admin Siparişi Onaylasın
# ============================================================================
Write-Host ""
Write-Host "STEP 2: Admin siparişi onaylıyor..." -ForegroundColor Cyan

try {
    $approveResponse = Invoke-RestMethod -Uri "$baseUrl/api/katana/orders/$orderId/approve" `
        -Method Post `
        -ContentType "application/json"
    
    Write-Host "✅ Sipariş onaylandı!" -ForegroundColor Green
} catch {
    Write-Host "❌ Sipariş onaylanamadı: $_" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 3

# ============================================================================
# STEP 3: Katana'da Ürün Var mı Kontrol Et
# ============================================================================
Write-Host ""
Write-Host "STEP 3: Katana'da ürün kontrol ediliyor..." -ForegroundColor Cyan

try {
    $katanaProducts = Invoke-RestMethod -Uri "$baseUrl/api/katana/products" -Method Get
    $testProduct = $katanaProducts | Where-Object { $_.sku -eq $testSku }
    
    if ($testProduct) {
        Write-Host "✅ Ürün Katana'da bulundu!" -ForegroundColor Green
        Write-Host "   SKU: $($testProduct.sku)" -ForegroundColor White
        Write-Host "   Name: $($testProduct.name)" -ForegroundColor White
        Write-Host "   ID: $($testProduct.id)" -ForegroundColor White
    } else {
        Write-Host "⚠️ Ürün henüz Katana'da görünmüyor, 5 saniye bekleniyor..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
        
        $katanaProducts = Invoke-RestMethod -Uri "$baseUrl/api/katana/products" -Method Get
        $testProduct = $katanaProducts | Where-Object { $_.sku -eq $testSku }
        
        if ($testProduct) {
            Write-Host "✅ Ürün Katana'da bulundu!" -ForegroundColor Green
        } else {
            Write-Host "❌ Ürün Katana'da bulunamadı!" -ForegroundColor Red
            exit 1
        }
    }
} catch {
    Write-Host "❌ Katana ürün kontrolü başarısız: $_" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# ============================================================================
# STEP 4: Luca'da Stok Kartı Oluştur (Sync)
# ============================================================================
Write-Host ""
Write-Host "STEP 4: Luca'da stok kartı oluşturuluyor (Sync)..." -ForegroundColor Cyan

$syncPayload = @{
    syncType = "PRODUCT_STOCK_CARD"
    options = @{
        dryRun = $false
        limit = 1000
    }
} | ConvertTo-Json -Depth 10

Write-Host "📤 Sync başlatılıyor..." -ForegroundColor Gray
try {
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/sync/start" `
        -Method Post `
        -Body $syncPayload `
        -ContentType "application/json"
    
    Write-Host "✅ Sync tamamlandı!" -ForegroundColor Green
    Write-Host "   Processed: $($syncResponse.processedRecords)" -ForegroundColor White
    Write-Host "   Success: $($syncResponse.successfulRecords)" -ForegroundColor White
    Write-Host "   Failed: $($syncResponse.failedRecords)" -ForegroundColor White
    
    if ($syncResponse.errors -and $syncResponse.errors.Count -gt 0) {
        Write-Host "   ⚠️ Hatalar:" -ForegroundColor Yellow
        $syncResponse.errors | ForEach-Object {
            Write-Host "      - $_" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "❌ Sync başarısız: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 3

# ============================================================================
# STEP 5: Luca'da Stok Kartı Var mı Kontrol Et
# ============================================================================
Write-Host ""
Write-Host "STEP 5: Luca'da stok kartı kontrol ediliyor..." -ForegroundColor Cyan

try {
    $lucaCards = Invoke-RestMethod -Uri "$baseUrl/api/admin/koza/stock-cards" -Method Get
    
    # SKU'yu normalize et (Ø → O gibi)
    $normalizedSku = $testSku -replace 'Ø', 'O' -replace 'ø', 'o'
    
    $lucaCard = $lucaCards | Where-Object { 
        $_.kartKodu -eq $testSku -or 
        $_.kartKodu -eq $normalizedSku -or
        $_.kartAdi -like "*$testName*"
    }
    
    if ($lucaCard) {
        Write-Host "✅ Stok kartı Luca'da bulundu!" -ForegroundColor Green
        Write-Host "   Kart Kodu: $($lucaCard.kartKodu)" -ForegroundColor White
        Write-Host "   Kart Adı: $($lucaCard.kartAdi)" -ForegroundColor White
        Write-Host "   Kart ID: $($lucaCard.skartId)" -ForegroundColor White
        Write-Host "   Ölçüm Birimi: $($lucaCard.olcumBirimiId)" -ForegroundColor White
    } else {
        Write-Host "⚠️ Stok kartı Luca'da bulunamadı!" -ForegroundColor Yellow
        Write-Host "   Aranan SKU: $testSku" -ForegroundColor White
        Write-Host "   Normalized: $normalizedSku" -ForegroundColor White
        Write-Host ""
        Write-Host "   Luca'daki son 5 kart:" -ForegroundColor Gray
        $lucaCards | Select-Object -Last 5 | ForEach-Object {
            $kod = $_.kartKodu
            $adi = $_.kartAdi
            Write-Host "      - $kod - $adi" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ Luca kontrol başarısız: $_" -ForegroundColor Red
}

# ============================================================================
# ÖZET
# ============================================================================
Write-Host ""
Write-Host "=" * 80 -ForegroundColor Gray
Write-Host "📊 TEST ÖZET" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Gray
Write-Host ""
Write-Host "Test Ürünü:" -ForegroundColor Yellow
Write-Host "  SKU: $testSku" -ForegroundColor White
Write-Host "  Name: $testName" -ForegroundColor White
Write-Host ""
Write-Host "Adımlar:" -ForegroundColor Yellow
Write-Host "  ✅ 1. Sipariş oluşturuldu (Order ID: $orderId)" -ForegroundColor Green
Write-Host "  ✅ 2. Admin onayladı" -ForegroundColor Green
Write-Host "  ✅ 3. Katana'da ürün bulundu" -ForegroundColor Green
Write-Host "  ✅ 4. Sync çalıştırıldı" -ForegroundColor Green
if ($lucaCard) {
    Write-Host "  ✅ 5. Luca'da stok kartı oluşturuldu" -ForegroundColor Green
} else {
    Write-Host "  ⚠️ 5. Luca'da stok kartı bulunamadı" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "=" * 80 -ForegroundColor Gray

if ($lucaCard) {
    Write-Host ""
    Write-Host "🎉 FULL FLOW TEST BAŞARILI!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "⚠️ Test tamamlandı ama Luca'da kart bulunamadı" -ForegroundColor Yellow
    Write-Host "   Manuel kontrol gerekebilir" -ForegroundColor Yellow
    Write-Host ""
}
