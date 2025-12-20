# Test Luca API directly
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Test Luca stock cards endpoint
Write-Host "`nTesting Luca stock cards endpoint..." -ForegroundColor Cyan

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/luca/stock-cards" -Method Get -Headers $headers
    Write-Host "✅ Success! Got $($result.Count) cards" -ForegroundColor Green
    
    if ($result.Count -gt 0) {
        Write-Host "`nFirst 5 cards:" -ForegroundColor Yellow
        $result | Select-Object -First 5 | ForEach-Object {
            Write-Host "  Code: $($_.kartKodu), Name: $($_.kartAdi)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
    Write-Host "`nTrying alternative endpoint..." -ForegroundColor Cyan
    
    try {
        $result2 = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers
        Write-Host "✅ Alternative endpoint works! Got $($result2.Count) cards" -ForegroundColor Green
    } catch {
        Write-Host "❌ Alternative also failed: $_" -ForegroundColor Red
    }
}

# Force refresh Luca cache
Write-Host "`nForcing Luca cache refresh..." -ForegroundColor Cyan
try {
    $refreshResult = Invoke-RestMethod -Uri "$baseUrl/api/luca/refresh-cache" -Method Post -Headers $headers
    Write-Host "✅ Cache refreshed!" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Cache refresh endpoint not found (this is OK)" -ForegroundColor Yellow
}
