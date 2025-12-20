# Find sill product index in Katana
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Get Katana products
Write-Host "`nFetching Katana products..." -ForegroundColor Cyan
$products = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get -Headers $headers

Write-Host "`nTotal products: $($products.Count)" -ForegroundColor Yellow

# Find sill products and their indices
$sillProducts = @()
for ($i = 0; $i -lt $products.Count; $i++) {
    if ($products[$i].sku -like "*sill*" -or $products[$i].name -like "*sill*") {
        $sillProducts += [PSCustomObject]@{
            Index = $i
            SKU = $products[$i].sku
            Name = $products[$i].name
        }
    }
}

Write-Host "`nFound $($sillProducts.Count) sill products:" -ForegroundColor Green
$sillProducts | ForEach-Object {
    Write-Host "  Index: $($_.Index), SKU: $($_.SKU), Name: $($_.Name)" -ForegroundColor White
}

if ($sillProducts.Count -gt 0) {
    $minIndex = ($sillProducts | Measure-Object -Property Index -Minimum).Minimum
    Write-Host "`n💡 To sync sill products, use limit >= $($minIndex + 1)" -ForegroundColor Cyan
}
