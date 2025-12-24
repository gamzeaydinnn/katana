# Script to check ALL products in Katana directly
# Katana'daki TÜM ürünleri doğrudan kontrol eder

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host "=== Katana'daki Tüm Ürünler ===" -ForegroundColor Cyan
Write-Host ""

# Login
Write-Host "Giriş yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "Giriş başarılı" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Giriş başarısız: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-API-Key" = $apiKey
}

# Get all products from Katana
Write-Host "Katana'daki ürünler getiriliyor..." -ForegroundColor Yellow

try {
    # Katana API'den tüm ürünleri çek
    $products = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get -Headers $headers
    
    Write-Host ""
    Write-Host "KATANA'DAKİ TÜM ÜRÜNLER" -ForegroundColor Cyan
    Write-Host "======================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($products.Count -eq 0) {
        Write-Host "Katana'da hiç ürün yok" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Toplam Ürün Sayısı: $($products.Count)" -ForegroundColor White
    Write-Host ""
    
    # VARIANT ile başlayanları filtrele
    $variantProducts = $products | Where-Object { $_.sku -like "VARIANT-*" }
    
    if ($variantProducts.Count -gt 0) {
        Write-Host "VARIANT ÜRÜNLER ($($variantProducts.Count) adet)" -ForegroundColor Yellow
        $variantProducts | ForEach-Object {
            Write-Host "  SKU: $($_.sku) | ID: $($_.id) | Ad: $($_.name)" -ForegroundColor White
        }
        Write-Host ""
    }
    
    # Diğer ürünler
    $otherProducts = $products | Where-Object { $_.sku -notlike "VARIANT-*" }
    
    if ($otherProducts.Count -gt 0) {
        Write-Host "DİĞER ÜRÜNLER ($($otherProducts.Count) adet)" -ForegroundColor Yellow
        $otherProducts | ForEach-Object {
            Write-Host "  SKU: $($_.sku) | ID: $($_.id) | Ad: $($_.name)" -ForegroundColor White
        }
        Write-Host ""
    }
    
    # Tüm SKU'ları listele
    Write-Host "TÜM SKU'LAR" -ForegroundColor Yellow
    $products | ForEach-Object {
        Write-Host "  $($_.sku)" -ForegroundColor White
    }
    Write-Host ""
    
    # Save to file
    $products | ConvertTo-Json -Depth 10 | Out-File "katana-all-products.json"
    Write-Host "Detaylı rapor kaydedildi: katana-all-products.json" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Sonraki Adım:" -ForegroundColor Cyan
    Write-Host "  Bu ürünleri silmek için: .\test-katana-cleanup-delete-all.ps1 -DryRun:`$false" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "Hata: $_" -ForegroundColor Red
    Write-Host "Detay: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
