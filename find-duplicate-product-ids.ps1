# Duplicate urunlerin ID'lerini bul
param(
    [string]$BaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!"
)

Write-Host "=== DUPLICATE URUN ID BULMA ===" -ForegroundColor Cyan
Write-Host ""

# Login
$loginBody = @{ username = $Username; password = $Password } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
$headers = @{ "Authorization" = "Bearer $token" }

# Tum urunleri al
$products = Invoke-RestMethod -Uri "$BaseUrl/api/products" -Method Get -Headers $headers

Write-Host "Toplam urun sayisi: $($products.Count)" -ForegroundColor Green
Write-Host ""

# GRUP 1: Ayni SKU'ya sahip urunler
Write-Host "=== GRUP 1: AYNI SKU, FARKLI ISIM ===" -ForegroundColor Yellow
Write-Host ""

$skusToCheck = @("81.06301-8212", "81.06301-8211", "9855411580", "CL-29 02 00347 01")

foreach ($sku in $skusToCheck) {
    $matches = $products | Where-Object { $_.sku -eq $sku }
    if ($matches.Count -gt 1) {
        Write-Host "SKU: $sku - $($matches.Count) kayit bulundu" -ForegroundColor Cyan
        foreach ($m in $matches) {
            Write-Host "  ID: $($m.id) | Name: $($m.name)" -ForegroundColor White
        }
        Write-Host ""
    } elseif ($matches.Count -eq 1) {
        Write-Host "SKU: $sku - Tek kayit (duplicate yok)" -ForegroundColor Gray
        Write-Host "  ID: $($matches.id) | Name: $($matches.name)" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "SKU: $sku - Bulunamadi!" -ForegroundColor Red
        Write-Host ""
    }
}

# GRUP 2: Ayni isme sahip urunler
Write-Host "=== GRUP 2: AYNI ISIM, FARKLI SKU ===" -ForegroundColor Yellow
Write-Host ""

$bakir = $products | Where-Object { $_.name -match "BAKIR BORU" }
if ($bakir.Count -gt 0) {
    Write-Host "BAKIR BORU iceren urunler: $($bakir.Count)" -ForegroundColor Cyan
    foreach ($b in $bakir | Sort-Object sku) {
        Write-Host "  ID: $($b.id) | SKU: $($b.sku) | Name: $($b.name)" -ForegroundColor White
    }
} else {
    Write-Host "BAKIR BORU iceren urun bulunamadi" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== OZET ===" -ForegroundColor Cyan
Write-Host "Yukaridaki bilgileri kullanarak archive-duplicate-products.ps1 scriptini guncelleyin" -ForegroundColor White
