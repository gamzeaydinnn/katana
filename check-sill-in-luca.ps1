# Check sill products in Luca
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Get Luca stock cards
Write-Host "`nFetching Luca stock cards..." -ForegroundColor Cyan
$lucaCards = Invoke-RestMethod -Uri "$baseUrl/api/luca/stock-cards" -Method Get -Headers $headers

Write-Host "`nTotal Luca cards: $($lucaCards.Count)" -ForegroundColor Yellow

# Search for sill
$sillCards = $lucaCards | Where-Object { $_.kartKodu -like "*sill*" -or $_.kartAdi -like "*sill*" }
Write-Host "`nLuca cards with 'sill' in code or name: $($sillCards.Count)" -ForegroundColor Cyan

if ($sillCards.Count -gt 0) {
    Write-Host "`nSill cards in Luca:" -ForegroundColor Green
    $sillCards | ForEach-Object {
        Write-Host "  Code: $($_.kartKodu), Name: $($_.kartAdi), ID: $($_.skartId)" -ForegroundColor White
    }
} else {
    Write-Host "`n⚠️ No cards found with 'sill' in Luca" -ForegroundColor Yellow
}

# Check specific SKUs
Write-Host "`n=== Checking specific SKUs ===" -ForegroundColor Cyan
$sku1 = "silll12344"
$sku2 = "sillll3l3ll3l3l"

$card1 = $lucaCards | Where-Object { $_.kartKodu -eq $sku1 }
$card2 = $lucaCards | Where-Object { $_.kartKodu -eq $sku2 }

Write-Host "`nSKU: $sku1" -ForegroundColor Yellow
if ($card1) {
    Write-Host "  ✅ Found in Luca - Name: $($card1.kartAdi), ID: $($card1.skartId)" -ForegroundColor Green
} else {
    Write-Host "  ❌ NOT found in Luca" -ForegroundColor Red
}

Write-Host "`nSKU: $sku2" -ForegroundColor Yellow
if ($card2) {
    Write-Host "  ✅ Found in Luca - Name: $($card2.kartAdi), ID: $($card2.skartId)" -ForegroundColor Green
} else {
    Write-Host "  ❌ NOT found in Luca" -ForegroundColor Red
}
