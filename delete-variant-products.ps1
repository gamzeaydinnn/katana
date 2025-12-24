# Katana'dan VARIANT Ürünlerini Sil
# Sadece siparişlerden gelen VARIANT- ile başlayan ürünleri siler

param(
    [switch]$DryRun = $true
)

Write-Host "=== Katana VARIANT Ürün Silme ===" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "⚠️  DRY RUN MODU - Hiçbir şey silinmeyecek" -ForegroundColor Yellow
    Write-Host "Gerçekten silmek için: -DryRun:`$false parametresi ekleyin" -ForegroundColor Yellow
    Write-Host ""
}

# JSON dosyasını oku
if (-not (Test-Path "katana-all-products.json")) {
    Write-Host "HATA: katana-all-products.json bulunamadı!" -ForegroundColor Red
    Write-Host "Önce şunu çalıştırın: .\test-katana-products-direct.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "Katana ürünleri okunuyor..." -ForegroundColor Yellow
$allProducts = Get-Content "katana-all-products.json" -Raw | ConvertFrom-Json

# VARIANT ürünlerini filtrele
$variantProducts = $allProducts | Where-Object { $_.sku -like "VARIANT-*" }

Write-Host "Toplam ürün: $($allProducts.Count)" -ForegroundColor White
Write-Host "VARIANT ürünleri: $($variantProducts.Count)" -ForegroundColor Yellow
Write-Host ""

if ($variantProducts.Count -eq 0) {
    Write-Host "✓ Silinecek VARIANT ürün bulunamadı!" -ForegroundColor Green
    exit 0
}

Write-Host "=== SİLİNECEK VARIANT ÜRÜNLER ===" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Tarihe göre grupla
$byDate = $variantProducts | Group-Object { 
    if ($_.created_at) { 
        [DateTime]::Parse($_.created_at).ToString("yyyy-MM-dd") 
    } else { 
        "Bilinmiyor" 
    }
} | Sort-Object Name

foreach ($group in $byDate) {
    Write-Host "$($group.Name): $($group.Count) ürün" -ForegroundColor White
}

Write-Host ""
Write-Host "İlk 20 VARIANT ürün:" -ForegroundColor Yellow
foreach ($product in ($variantProducts | Select-Object -First 20)) {
    $createdDate = if ($product.created_at) { 
        [DateTime]::Parse($product.created_at).ToString("yyyy-MM-dd HH:mm") 
    } else { 
        "Bilinmiyor" 
    }
    Write-Host "  SKU: $($product.sku) | ID: $($product.id) | Tarih: $createdDate" -ForegroundColor Gray
}

if ($variantProducts.Count -gt 20) {
    Write-Host "  ... ve $($variantProducts.Count - 20) ürün daha" -ForegroundColor Gray
}

Write-Host ""

# Onay iste (DryRun değilse)
if (-not $DryRun) {
    Write-Host "⚠️  UYARI: $($variantProducts.Count) VARIANT ürünü silinecek!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Bu işlem:" -ForegroundColor Yellow
    Write-Host "  - Katana'daki tüm VARIANT ürünlerini silecek" -ForegroundColor Yellow
    Write-Host "  - Siparişleri sıfırlamayacak (siparişler Pending kalacak)" -ForegroundColor Yellow
    Write-Host "  - Siparişleri tekrar onaylayarak temiz ürünler oluşturabilirsiniz" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Devam etmek istiyor musunuz? (EVET yazın)"
    
    if ($confirmation -ne "EVET") {
        Write-Host "İşlem iptal edildi" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Katana API ayarlarını al
$baseUrl = "http://localhost:5055"
$username = "admin"
$password = "Katana2025!"

Write-Host "Backend'e bağlanılıyor..." -ForegroundColor Yellow

# Giriş yap
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

# Katana API key'i appsettings'den al
$appsettings = Get-Content "src/Katana.API/appsettings.Development.json" -Raw | ConvertFrom-Json
$katanaApiKey = $appsettings.KatanaApiSettings.ApiKey

if (-not $katanaApiKey) {
    Write-Host "HATA: Katana API key bulunamadı!" -ForegroundColor Red
    exit 1
}

Write-Host "Katana API key alındı" -ForegroundColor Green
Write-Host ""

# Silme işlemi
$successCount = 0
$failCount = 0
$errors = @()

Write-Host "Silme işlemi başlıyor..." -ForegroundColor Yellow
Write-Host ""

$counter = 0
foreach ($product in $variantProducts) {
    $counter++
    $progress = [math]::Round(($counter / $variantProducts.Count) * 100, 1)
    
    $total = $variantProducts.Count
    $progressText = "$progress%"
    Write-Host "[$counter/$total] ($progressText) SKU: $($product.sku) | ID: $($product.id)" -NoNewline
    
    if ($DryRun) {
        Write-Host " [DRY RUN - Silinmedi]" -ForegroundColor Yellow
        $successCount++
    } else {
        try {
            # Katana API'den ürünü sil
            $deleteUrl = "https://api.katanamrp.com/v1/products/$($product.id)"
            
            $katanaHeaders = @{
                "Authorization" = "Bearer $katanaApiKey"
                "Content-Type" = "application/json"
            }
            
            Invoke-RestMethod -Uri $deleteUrl -Method Delete -Headers $katanaHeaders -ErrorAction Stop
            
            Write-Host " [✓ Silindi]" -ForegroundColor Green
            $successCount++
            
            # Rate limiting için bekle (Katana API limiti: 100 req/min)
            Start-Sleep -Milliseconds 650
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
    TotalVariantProducts = $variantProducts.Count
    SuccessCount = $successCount
    FailCount = $failCount
    Errors = $errors
    DeletedProducts = if (-not $DryRun -and $successCount -gt 0) { 
        $variantProducts | Select-Object -First $successCount | ForEach-Object {
            [PSCustomObject]@{
                Id = $_.id
                SKU = $_.sku
                Name = $_.name
                CreatedAt = $_.created_at
            }
        }
    } else { 
        @() 
    }
}

$resultJson = $result | ConvertTo-Json -Depth 10
$resultJson | Out-File -FilePath "variant-delete-result.json" -Encoding UTF8

Write-Host "Sonuç raporu kaydedildi: variant-delete-result.json" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "Gerçekten silmek için komutu tekrar çalıştırın:" -ForegroundColor Yellow
    Write-Host "  .\delete-variant-products.ps1 -DryRun:`$false" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "✓ İşlem tamamlandı!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
    Write-Host "  1. Siparişleri tekrar onaylayın (temiz ürünler oluşturulacak)" -ForegroundColor White
    Write-Host "  2. Kontrol edin: .\test-katana-products-direct.ps1" -ForegroundColor White
}
