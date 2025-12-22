# Variant stok kontrolü
$ErrorActionPreference = "Stop"

$variantId = 37627732
$katanaApiKey = "YOUR_KATANA_API_KEY_HERE"  # appsettings.json'dan alınmalı

Write-Host "Variant $variantId stok durumu kontrol ediliyor..."

try {
    $headers = @{
        "X-API-KEY" = $katanaApiKey
        "Accept" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri "https://api.katanamrp.com/v1/variants/$variantId" -Headers $headers -Method Get
    
    Write-Host "Variant Bilgileri:" -ForegroundColor Green
    Write-Host "  SKU: $($response.sku)"
    Write-Host "  Name: $($response.name)"
    Write-Host "  In Stock: $($response.in_stock)"
    Write-Host "  Committed: $($response.committed_stock)"
    Write-Host "  Available: $($response.available_stock)"
    
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Detay: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}
