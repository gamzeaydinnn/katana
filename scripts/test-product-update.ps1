# Test script for product update endpoint
param(
    [string]$ApiUrl = "http://52.90.142.107",
    [int]$ProductId = 1
)

Write-Host "=== Testing Product Update Endpoint ===" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "Product ID: $ProductId" -ForegroundColor Yellow

# First, get the product to see its current state
Write-Host "`n1. Getting current product state..." -ForegroundColor Green
try {
    $getResponse = Invoke-RestMethod -Uri "$ApiUrl/api/Products/$ProductId" -Method Get -ErrorAction Stop
    Write-Host "Current Product:" -ForegroundColor Green
    $getResponse | ConvertTo-Json -Depth 3
} catch {
    Write-Host "ERROR getting product: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response | Out-String)" -ForegroundColor Red
    exit 1
}

# Prepare update payload (Luca format)
$updatePayload = @{
    productCode = $getResponse.sku
    productName = "$($getResponse.name) (Updated)"
    unit = "Adet"
    quantity = $getResponse.stock + 5
    unitPrice = $getResponse.price + 1.5
    vatRate = 20
} | ConvertTo-Json

Write-Host "`n2. Sending update request..." -ForegroundColor Green
Write-Host "Payload: $updatePayload" -ForegroundColor Yellow

try {
    $updateResponse = Invoke-RestMethod -Uri "$ApiUrl/api/Products/luca/$ProductId" `
        -Method Put `
        -Body $updatePayload `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Write-Host "SUCCESS! Updated Product:" -ForegroundColor Green
    $updateResponse | ConvertTo-Json -Depth 3
} catch {
    Write-Host "ERROR updating product: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
        
        Write-Host "`nStatus Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
    exit 1
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
