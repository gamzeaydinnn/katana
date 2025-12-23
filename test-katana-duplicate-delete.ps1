# Katana Duplicate Ürün Silme
# Bu script duplicate ürünleri Katana'dan siler (en eski olanı korur)

param(
    [switch]$DryRun = $true
)

$baseUrl = "http://localhost:5055"
$username = "admin"
$password = "Katana2025!"

Write-Host "=== Katana Duplicate Ürün Silme ===" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "⚠️  DRY RUN MODU - Hiçbir şey silinmeyecek" -ForegroundColor Yellow
    Write-Host "Gerçekten silmek için: -DryRun:`$false parametresi ekleyin" -ForegroundColor Yellow
    Write-Host ""
}

# Giriş yap
Write-Host "Giriş yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Giriş başarılı" -ForegroundColor Green
Write-Host ""

# Katana'dan tüm ürünleri çek
Write-Host "Katana'dan ürünler çekiliyor..." -ForegroundColor Yellow
$allProducts = Invoke-RestMethod -Uri "$baseUrl/api/katana-cleanup/all-products" -Method Get -Headers $headers

Write-Host "Toplam $($allProducts.Count) ürün bulundu" -ForegroundColor Green
Write-Host ""

# Duplicate analizi
Write-Host "Duplicate analizi yapılıyor..." -ForegroundColor Yellow

# SKU'ya göre grupla
$skuGroups = $allProducts | Group-Object -Property sku

# Duplicate SKU'ları bul (2'den fazla olan)
$duplicateSkus = $skuGroups | Where-Object { $_.Count -gt 1 }

# Silinecek ürünleri belirle
$productsToDelete = @()
$productsToKeep = @()

foreach ($group in $duplicateSkus) {
    $products = $group.Group | Sort-Object created_at
    
    # İlk ürünü koru (en eski)
    $keepProduct = $products[0]
    $productsToKeep += $keepProduct
    
    # Geri kalanları sil
    for ($i = 1; $i -lt $products.Count; $i++) {
        $productsToDelete += $products[$i]
    }
}

Write-Host ""
Write-Host "=== SİLME PLANI ===" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Korunacak Ürünler : $($productsToKeep.Count)" -ForegroundColor Green
Write-Host "Silinecek Ürünler : $($productsToDelete.Count)" -ForegroundColor Red
Write-Host ""

if ($productsToDelete.Count -eq 0) {
    Write-Host "✓ Silinecek duplicate ürün bulunamadı!" -ForegroundColor Green
    exit 0
}

# Onay iste (DryRun değilse)
if (-not $DryRun) {
    Write-Host "⚠️  UYARI: $($productsToDelete.Count) ürün silinecek!" -ForegroundColor Red
    Write-Host ""
    $confirmation = Read-Host "Devam etmek istiyor musunuz? (EVET yazın)"
    
    if ($confirmation -ne "EVET") {
        Write-Host "İşlem iptal edildi" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Silme işlemi
$successCount = 0
$failCount = 0
$errors = @()

Write-Host "Silme işlemi başlıyor..." -ForegroundColor Yellow
Write-Host ""

$counter = 0
foreach ($product in $productsToDelete) {
    $counter++
    $progress = [math]::Round(($counter / $productsToDelete.Count) * 100, 1)
    
    Write-Host "[$counter/$($productsToDelete.Count)] ($progress%) SKU: $($product.sku) | ID: $($product.id)" -NoNewline
    
    if ($DryRun) {
        Write-Host " [DRY RUN - Silinmedi]" -ForegroundColor Yellow
        $successCount++
    } else {
        try {
            # Katana API'den ürünü sil
            $deleteUrl = "https://api.katanamrp.com/v1/products/$($product.id)"
            
            # Katana API key'i appsettings'den al
            $katanaApiKey = "YOUR_KATANA_API_KEY" # Bu değer appsettings'den gelecek
            
            $katanaHeaders = @{
                "Authorization" = "Bearer $katanaApiKey"
                "Content-Type" = "application/json"
            }
            
            Invoke-RestMethod -Uri $deleteUrl -Method Delete -Headers $katanaHeaders -ErrorAction Stop
            
            Write-Host " [✓ Silindi]" -ForegroundColor Green
            $successCount++
            
            # Rate limiting için bekle
            Start-Sleep -Milliseconds 100
        }
        catch {
            Write-Host " [✗ HATA]" -ForegroundColor Red
            $failCount++
            $errors += [PSCustomObject]@{
                ProductId = $product.id
                SKU = $product.sku
                Error = $_.Exception.Message
            }
        }
    }
}

Write-Host ""
Write-Host "=== SONUÇ ===" -ForegroundColor Cyan
Write-Host "=============" -ForegroundColor Cyan
Write-Host ""
Write-Host "Başarılı : $successCount" -ForegroundColor Green
Write-Host "Başarısız: $failCount" -ForegroundColor Red
Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "HATALAR:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  SKU: $($error.SKU) | ID: $($error.ProductId)" -ForegroundColor Gray
        Write-Host "  Hata: $($error.Error)" -ForegroundColor Gray
        Write-Host ""
    }
}

# Sonuç raporunu kaydet
$result = @{
    Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    DryRun = $DryRun
    TotalToDelete = $productsToDelete.Count
    SuccessCount = $successCount
    FailCount = $failCount
    Errors = $errors
    DeletedProducts = if (-not $DryRun) { 
        $productsToDelete | Select-Object -First $successCount | ForEach-Object {
            [PSCustomObject]@{
                Id = $_.id
                SKU = $_.sku
                Name = $_.name
            }
        }
    } else { 
        @() 
    }
}

$resultJson = $result | ConvertTo-Json -Depth 10
$resultJson | Out-File -FilePath "katana-duplicate-delete-result.json" -Encoding UTF8

Write-Host "Sonuç raporu kaydedildi: katana-duplicate-delete-result.json" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "Gerçekten silmek için komutu tekrar çalıştırın:" -ForegroundColor Yellow
    Write-Host "  .\test-katana-duplicate-delete.ps1 -DryRun:`$false" -ForegroundColor White
}
