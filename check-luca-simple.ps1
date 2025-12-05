# Simple Luca status check
$baseUrl = "http://localhost:5055"

# Login
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

# Get stock cards
Write-Host "Fetching stock cards..." -ForegroundColor Cyan
$stockCards = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers

Write-Host "`nTotal Cards: $($stockCards.Count)" -ForegroundColor Yellow

# Count versions
$withV = $stockCards | Where-Object { $_.kartKodu -match "-V\d+$" }
Write-Host "With -V suffix: $($withV.Count)" -ForegroundColor Cyan

# Find duplicates
$dupes = $stockCards | Group-Object -Property kartAdi | Where-Object { $_.Count -gt 1 }
Write-Host "Duplicate groups: $($dupes.Count)" -ForegroundColor Cyan

# Export
$stockCards | Select-Object skartId, kartKodu, kartAdi | Export-Csv "luca-cards.csv" -NoTypeInformation
Write-Host "`nExported to luca-cards.csv" -ForegroundColor Green
