# Check Katana products
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Get products
Write-Host "`nFetching products from Katana..." -ForegroundColor Cyan
$products = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get -Headers $headers

Write-Host "`nTotal products: $($products.Count)" -ForegroundColor Yellow

# Search for sill
$sillProducts = $products | Where-Object { $_.sku -like "*sill*" -or $_.name -like "*sill*" }
Write-Host "`nProducts with 'sill' in SKU or Name: $($sillProducts.Count)" -ForegroundColor Cyan

if ($sillProducts.Count -gt 0) {
    Write-Host "`nSill products:" -ForegroundColor Green
    $sillProducts | ForEach-Object {
        Write-Host "  SKU: $($_.sku), Name: $($_.name)" -ForegroundColor White
    }
} else {
    Write-Host "`n⚠️ No products found with 'sill' in SKU or Name" -ForegroundColor Yellow
    Write-Host "`nShowing first 10 products:" -ForegroundColor Cyan
    $products | Select-Object -First 10 | ForEach-Object {
        Write-Host "  SKU: $($_.sku), Name: $($_.name)" -ForegroundColor White
    }
}
