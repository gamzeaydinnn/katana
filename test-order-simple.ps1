# Test Script - Purchase Order Flow
$baseUrl = "http://localhost:5000"
$headers = @{ "Content-Type" = "application/json" }

Write-Host "=== Purchase Order Flow Test ===" -ForegroundColor Cyan

# 1. Create Product
Write-Host "`n1. Creating test product..." -ForegroundColor Yellow
$productPayload = @{
    name = "TEST-PRODUCT-$(Get-Date -Format 'yyyyMMddHHmmss')"
    sku = "TEST-SKU-$(Get-Random -Maximum 99999)"
    price = 100.00
    categoryId = 1
    isActive = $true
} | ConvertTo-Json

$productResponse = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body $productPayload -Headers $headers
$productId = $productResponse.id
$productSku = $productResponse.sku
Write-Host "Product created: ID=$productId, SKU=$productSku, Stock=$($productResponse.stock)" -ForegroundColor Green

Start-Sleep -Seconds 1

# 2. Create Purchase Order
Write-Host "`n2. Creating purchase order..." -ForegroundColor Yellow
$orderPayload = @{
    supplierId = 1
    orderDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    items = @(
        @{
            productId = $productId
            quantity = 50
            unitPrice = 80.00
            vatRate = 20
            unitCode = "AD"
        }
    )
} | ConvertTo-Json -Depth 3

$orderResponse = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders" -Method Post -Body $orderPayload -Headers $headers
$orderId = $orderResponse.id
$orderNo = $orderResponse.orderNo
Write-Host "Order created: ID=$orderId, No=$orderNo, Status=$($orderResponse.status)" -ForegroundColor Green

Start-Sleep -Seconds 1

# 3. Check Stock (should be 0)
Write-Host "`n3. Checking stock (should be 0)..." -ForegroundColor Yellow
$productCheck1 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
Write-Host "Current Stock: $($productCheck1.stock)" -ForegroundColor $(if ($productCheck1.stock -eq 0) { "Green" } else { "Red" })

Start-Sleep -Seconds 1

# 4. Approve Order
Write-Host "`n4. Approving order (Pending -> Approved)..." -ForegroundColor Yellow
$statusPayload = @{ newStatus = "Approved" } | ConvertTo-Json
$statusResponse1 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload -Headers $headers
Write-Host "Order approved: $($statusResponse1.oldStatus) -> $($statusResponse1.newStatus)" -ForegroundColor Green

Start-Sleep -Seconds 1

# 5. Check Stock (should still be 0)
Write-Host "`n5. Checking stock (should still be 0)..." -ForegroundColor Yellow
$productCheck2 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
Write-Host "Current Stock: $($productCheck2.stock)" -ForegroundColor $(if ($productCheck2.stock -eq 0) { "Green" } else { "Red" })

Start-Sleep -Seconds 1

# 6. Receive Order (CRITICAL STEP)
Write-Host "`n6. Receiving order (Approved -> Received) - STOCK SHOULD INCREASE..." -ForegroundColor Yellow
$statusPayload2 = @{ newStatus = "Received" } | ConvertTo-Json
$statusResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload2 -Headers $headers
Write-Host "Order received: $($statusResponse2.oldStatus) -> $($statusResponse2.newStatus)" -ForegroundColor Green
Write-Host "Stock updated: $($statusResponse2.stockUpdated)" -ForegroundColor $(if ($statusResponse2.stockUpdated) { "Green" } else { "Red" })

Start-Sleep -Seconds 2

# 7. FINAL STOCK CHECK (should be 50)
Write-Host "`n7. FINAL STOCK CHECK..." -ForegroundColor Yellow
$productCheck3 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
Write-Host "FINAL STOCK: $($productCheck3.stock) (Expected: 50)" -ForegroundColor $(if ($productCheck3.stock -eq 50) { "Green" } else { "Red" })

# Summary
Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Cyan
Write-Host "Product: $productSku (ID: $productId)"
Write-Host "Order: $orderNo (ID: $orderId)"
Write-Host "Initial Stock: 0"
Write-Host "Order Quantity: 50"
Write-Host "Final Stock: $($productCheck3.stock)"

if ($productCheck3.stock -eq 50) {
    Write-Host "`nTEST PASSED!" -ForegroundColor Green
} else {
    Write-Host "`nTEST FAILED!" -ForegroundColor Red
}
