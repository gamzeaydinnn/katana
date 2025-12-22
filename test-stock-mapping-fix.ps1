# Test Stock Mapping Fix
# Bu script ExtractorService'deki stock mapping duzeltmesini test eder

param(
    [string]$BackendUrl = "http://localhost:5055",
    [string]$AdminToken = "admin-token"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stock Mapping Fix Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Urunleri Katana'dan cek
Write-Host "[TEST 1] Katana'dan urunleri cekme" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BackendUrl/api/Products/katana?sync=true&page=1&pageSize=100" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Content-Type" = "application/json"
        }
    
    Write-Host "OK: Katana'dan $($response.products.Count) urun cekild" -ForegroundColor Green
    
    # Stok bilgisini kontrol et
    $productsWithStock = $response.products | Where-Object { $_.stock -gt 0 }
    Write-Host "  - Stok > 0 olan urunler: $($productsWithStock.Count)" -ForegroundColor Green
    
    if ($productsWithStock.Count -gt 0) {
        Write-Host "  - Ornek urun:" -ForegroundColor Green
        $sample = $productsWithStock[0]
        Write-Host "    SKU: $($sample.sku)" -ForegroundColor Green
        Write-Host "    Stok: $($sample.stock)" -ForegroundColor Green
        Write-Host "    Fiyat: $($sample.price)" -ForegroundColor Green
    }
} catch {
    Write-Host "HATA: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Urunleri senkronize et
Write-Host "[TEST 2] Urunleri Luca'ya senkronize etme" -ForegroundColor Yellow
try {
    $syncResponse = Invoke-RestMethod -Uri "$BackendUrl/api/sync/products" `
        -Method POST `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Content-Type" = "application/json"
        }
    
    Write-Host "OK: Senkronizasyon baslatildi" -ForegroundColor Green
    Write-Host "  - Sonuc: $($syncResponse.message)" -ForegroundColor Green
    
    if ($syncResponse.success) {
        Write-Host "  - Senkronize edilen: $($syncResponse.syncedCount)" -ForegroundColor Green
        Write-Host "  - Basarisiz: $($syncResponse.failedCount)" -ForegroundColor Green
    }
} catch {
    Write-Host "HATA: $_" -ForegroundColor Red
}

Write-Host ""

# Test 3: Luca'da urunleri kontrol et
Write-Host "[TEST 3] Luca'da urunleri kontrol etme" -ForegroundColor Yellow
try {
    $lucaProducts = Invoke-RestMethod -Uri "$BackendUrl/api/Products/luca?page=1&pageSize=100" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Content-Type" = "application/json"
        }
    
    Write-Host "OK: Luca'da $($lucaProducts.products.Count) urun bulundu" -ForegroundColor Green
    
    # Stok bilgisini kontrol et
    $lucaWithStock = $lucaProducts.products | Where-Object { $_.stock -gt 0 }
    Write-Host "  - Stok > 0 olan urunler: $($lucaWithStock.Count)" -ForegroundColor Green
    
    if ($lucaWithStock.Count -gt 0) {
        Write-Host "  - Ornek urun:" -ForegroundColor Green
        $sample = $lucaWithStock[0]
        Write-Host "    SKU: $($sample.sku)" -ForegroundColor Green
        Write-Host "    Stok: $($sample.stock)" -ForegroundColor Green
    }
} catch {
    Write-Host "HATA: $_" -ForegroundColor Red
}

Write-Host ""

# Test 4: Stok Mapping Dogrulugu
Write-Host "[TEST 4] Stok Mapping Dogrulugu" -ForegroundColor Yellow
try {
    # Katana ve Luca'daki urunleri karsilastir
    $katanaProds = $response.products | Where-Object { $_.stock -gt 0 } | Select-Object -First 5
    
    $matchCount = 0
    $mismatchCount = 0
    
    foreach ($katanaProd in $katanaProds) {
        $lucaProd = $lucaProducts.products | Where-Object { $_.sku -eq $katanaProd.sku }
        
        if ($lucaProd) {
            if ($lucaProd.stock -eq $katanaProd.stock) {
                Write-Host "  OK: $($katanaProd.sku) - Stok eslesti ($($katanaProd.stock))" -ForegroundColor Green
                $matchCount++
            } else {
                Write-Host "  HATA: $($katanaProd.sku) - Stok uyusmadi (Katana: $($katanaProd.stock), Luca: $($lucaProd.stock))" -ForegroundColor Red
                $mismatchCount++
            }
        } else {
            Write-Host "  UYARI: $($katanaProd.sku) - Luca'da bulunamadi" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "  Sonuc: $matchCount eslesti, $mismatchCount uyusmadi" -ForegroundColor Cyan
} catch {
    Write-Host "HATA: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Tamamlandi" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
