# Katana API basit test
$katanaApiKey = "ed8c38d1-4015-45e5-9c28-381d3fe148b6"
$katanaBaseUrl = "https://api.katanamrp.com/v1"

Write-Host "Katana API Test..." -ForegroundColor Cyan

try {
    # Curl ile test
    $curlCommand = "curl -X GET `"$katanaBaseUrl/products?limit=10`" -H `"X-Api-Key: $katanaApiKey`" -H `"Accept: application/json`""
    Write-Host "`nKomut: $curlCommand`n" -ForegroundColor Yellow
    
    $result = Invoke-Expression $curlCommand
    Write-Host $result
    
} catch {
    Write-Host "Hata: $($_.Exception.Message)" -ForegroundColor Red
}
