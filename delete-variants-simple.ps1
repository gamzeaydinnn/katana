# VARIANT Ürünlerini Sil - Basit Versiyon
param([switch]$DryRun = $true)

Write-Host "=== VARIANT Ürün Silme ===" -ForegroundColor Cyan
Write-Host ""

# JSON'dan ürünleri oku
$allProducts = Get-Content "katana-all-products.json" -Raw | ConvertFrom-Json
$variants = $allProducts | Where-Object { $_.sku -like "VARIANT-*" }

Write-Host "Toplam ürün: $($allProducts.Count)" -ForegroundColor White
Write-Host "VARIANT ürünleri: $($variants.Count)" -ForegroundColor Yellow
Write-Host ""

if ($variants.Count -eq 0) {
    Write-Host "Silinecek VARIANT ürün yok!" -ForegroundColor Green
    exit 0
}

# İlk 20'yi göster
Write-Host "İlk 20 VARIANT ürün:" -ForegroundColor Yellow
$variants | Select-Object -First 20 | ForEach-Object {
    Write-Host "  SKU: $($_.sku) | ID: $($_.id)" -ForegroundColor Gray
}
if ($variants.Count -gt 20) {
    Write-Host "  ... ve $($variants.Count - 20) ürün daha" -ForegroundColor Gray
}
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODU - Hiçbir şey silinmeyecek" -ForegroundColor Yellow
    Write-Host "Gerçekten silmek için: -DryRun:`$false" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Silinecek ürün sayısı: $($variants.Count)" -ForegroundColor White
    exit 0
}

# Onay iste
Write-Host "UYARI: $($variants.Count) VARIANT ürünü silinecek!" -ForegroundColor Red
$confirm = Read-Host "Devam etmek için EVET yazın"
if ($confirm -ne "EVET") {
    Write-Host "İptal edildi" -ForegroundColor Yellow
    exit 0
}

# Backend'e bağlan
$baseUrl = "http://localhost:5055"
$loginBody = @{ username = "admin"; password = "Katana2025!" } | ConvertTo-Json
$loginResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResp.token

# Katana API key al
$appsettings = Get-Content "src/Katana.API/appsettings.Development.json" -Raw | ConvertFrom-Json
$katanaApiKey = $appsettings.KatanaApiSettings.ApiKey

Write-Host ""
Write-Host "Silme başlıyor..." -ForegroundColor Yellow
Write-Host ""

$success = 0
$fail = 0
$counter = 0

foreach ($product in $variants) {
    $counter++
    $pct = [math]::Round(($counter / $variants.Count) * 100, 1)
    Write-Host "[$counter/$($variants.Count)] $pct% - SKU: $($product.sku)" -NoNewline
    
    try {
        $url = "https://api.katanamrp.com/v1/products/$($product.id)"
        $headers = @{ "Authorization" = "Bearer $katanaApiKey" }
        Invoke-RestMethod -Uri $url -Method Delete -Headers $headers -ErrorAction Stop
        Write-Host " [OK]" -ForegroundColor Green
        $success++
        Start-Sleep -Milliseconds 650
    }
    catch {
        Write-Host " [HATA]" -ForegroundColor Red
        $fail++
    }
}

Write-Host ""
Write-Host "=== SONUÇ ===" -ForegroundColor Cyan
Write-Host "Başarılı: $success" -ForegroundColor Green
Write-Host "Başarısız: $fail" -ForegroundColor Red
