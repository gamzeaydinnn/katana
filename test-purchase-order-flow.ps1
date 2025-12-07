# SatÄ±nalma SipariÅŸi AkÄ±ÅŸ Testi
# Test senaryosu: SipariÅŸ oluÅŸtur -> Onayla -> Teslim al -> Stok kontrolÃ¼

$baseUrl = "http://localhost:5000"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ğŸ“¦ SATINALMA SÄ°PARÄ°ÅÄ° AKIÅ TESTÄ°" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Test Ã¼rÃ¼nÃ¼ oluÅŸtur
Write-Host "1ï¸âƒ£ Test Ã¼rÃ¼nÃ¼ oluÅŸturuluyor..." -ForegroundColor Yellow
$productPayload = @{
    name = "TEST-PRODUCT-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    sku = "TEST-SKU-$(Get-Random -Maximum 99999)"
    price = 100.00
    categoryId = 1
    isActive = $true
} | ConvertTo-Json

try {
    $productResponse = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body $productPayload -Headers $headers
    $productId = $productResponse.id
    $productSku = $productResponse.sku
    Write-Host "   âœ… ÃœrÃ¼n oluÅŸturuldu: ID=$productId, SKU=$productSku" -ForegroundColor Green
    Write-Host "   ğŸ“Š BaÅŸlangÄ±Ã§ Stok: $($productResponse.stock)" -ForegroundColor Gray
} catch {
    Write-Host "   âŒ ÃœrÃ¼n oluÅŸturulamadÄ±: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# 2. TedarikÃ§i listesini al (ilk tedarikÃ§iyi kullanacaÄŸÄ±z)
Write-Host ""
Write-Host "2ï¸âƒ£ TedarikÃ§i bilgisi alÄ±nÄ±yor..." -ForegroundColor Yellow
try {
    $suppliersResponse = Invoke-RestMethod -Uri "$baseUrl/api/suppliers?pageSize=1" -Method Get
    $supplierId = $suppliersResponse.items[0].id
    Write-Host "   âœ… TedarikÃ§i: ID=$supplierId, AdÄ±=$($suppliersResponse.items[0].name)" -ForegroundColor Green
} catch {
    Write-Host "   âš ï¸ TedarikÃ§i bulunamadÄ±, varsayÄ±lan ID=1 kullanÄ±lÄ±yor" -ForegroundColor Yellow
    $supplierId = 1
}

Start-Sleep -Seconds 1

# 3. SatÄ±nalma sipariÅŸi oluÅŸtur
Write-Host ""
Write-Host "3ï¸âƒ£ SatÄ±nalma sipariÅŸi oluÅŸturuluyor..." -ForegroundColor Yellow
$orderPayload = @{
    supplierId = $supplierId
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

try {
    $orderResponse = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders" -Method Post -Body $orderPayload -Headers $headers
    $orderId = $orderResponse.id
    $orderNo = $orderResponse.orderNo
    Write-Host "   âœ… SipariÅŸ oluÅŸturuldu: ID=$orderId, No=$orderNo" -ForegroundColor Green
    Write-Host "   ğŸ“Š Durum: $($orderResponse.status)" -ForegroundColor Gray
} catch {
    Write-Host "   âŒ SipariÅŸ oluÅŸturulamadÄ±: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# 4. Stok kontrolÃ¼ (0 olmalÄ±)
Write-Host ""
Write-Host "4ï¸âƒ£ Stok kontrolÃ¼ (sipariÅŸ oluÅŸturuldu, henÃ¼z teslim alÄ±nmadÄ±)..." -ForegroundColor Yellow
try {
    $productCheck1 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
    Write-Host "   ğŸ“Š Mevcut Stok: $($productCheck1.stock) (Beklenen: 0)" -ForegroundColor $(if ($productCheck1.stock -eq 0) { "Green" } else { "Red" })
} catch {
    Write-Host "   âŒ Stok kontrolÃ¼ baÅŸarÄ±sÄ±z" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 5. SipariÅŸi ONAYLA (Pending -> Approved)
Write-Host ""
Write-Host "5ï¸âƒ£ SipariÅŸ onaylanÄ±yor (Pending -> Approved)..." -ForegroundColor Yellow
$statusPayload = @{
    newStatus = "Approved"
} | ConvertTo-Json

try {
    $statusResponse1 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload -Headers $headers
    Write-Host "   âœ… SipariÅŸ onaylandÄ±: $($statusResponse1.oldStatus) -> $($statusResponse1.newStatus)" -ForegroundColor Green
} catch {
    Write-Host "   âŒ SipariÅŸ onaylanamadÄ±: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 6. Stok kontrolÃ¼ (hala 0 olmalÄ±)
Write-Host ""
Write-Host "6ï¸âƒ£ Stok kontrolÃ¼ (onaylandÄ±, henÃ¼z teslim alÄ±nmadÄ±)..." -ForegroundColor Yellow
try {
    $productCheck2 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
    Write-Host "   ğŸ“Š Mevcut Stok: $($productCheck2.stock) (Beklenen: 0)" -ForegroundColor $(if ($productCheck2.stock -eq 0) { "Green" } else { "Red" })
} catch {
    Write-Host "   âŒ Stok kontrolÃ¼ baÅŸarÄ±sÄ±z" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 7. SipariÅŸi TESLÄ°M AL (Approved -> Received) - ğŸ”¥ KRÄ°TÄ°K ADIM
Write-Host ""
Write-Host "7ï¸âƒ£ SipariÅŸ teslim alÄ±nÄ±yor (Approved -> Received) - ğŸ”¥ STOK ARTIÅI BEKLENÄ°YOR..." -ForegroundColor Yellow
$statusPayload2 = @{
    newStatus = "Received"
} | ConvertTo-Json

try {
    $statusResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload2 -Headers $headers
    Write-Host "   âœ… SipariÅŸ teslim alÄ±ndÄ±: $($statusResponse2.oldStatus) -> $($statusResponse2.newStatus)" -ForegroundColor Green
    Write-Host "   ğŸ“¦ Stok gÃ¼ncellendi mi: $($statusResponse2.stockUpdated)" -ForegroundColor $(if ($statusResponse2.stockUpdated) { "Green" } else { "Red" })
} catch {
    Write-Host "   âŒ SipariÅŸ teslim alÄ±namadÄ±: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 3

# 8. FINAL STOK KONTROLÃœ (50 olmalÄ±!)
Write-Host ""
Write-Host "8ï¸âƒ£ FINAL STOK KONTROLÃœ..." -ForegroundColor Yellow
try {
    $productCheck3 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get
    Write-Host "   ğŸ“Š FINAL STOK: $($productCheck3.stock) (Beklenen: 50)" -ForegroundColor $(if ($productCheck3.stock -eq 50) { "Green" } else { "Red" })
    
    if ($productCheck3.stock -eq 50) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "âœ… TEST BAÅARILI!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "   SipariÅŸ No: $orderNo" -ForegroundColor Gray
        Write-Host "   ÃœrÃ¼n SKU: $productSku" -ForegroundColor Gray
        Write-Host "   BaÅŸlangÄ±Ã§ Stok: 0" -ForegroundColor Gray
        Write-Host "   SipariÅŸ MiktarÄ±: 50" -ForegroundColor Gray
        Write-Host "   Final Stok: $($productCheck3.stock)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "âœ… Stok artÄ±ÅŸÄ± doÄŸru Ã§alÄ±ÅŸtÄ±!" -ForegroundColor Green
        Write-Host "âœ… SipariÅŸ -> Onay -> Teslim akÄ±ÅŸÄ± baÅŸarÄ±lÄ±!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "âŒ TEST BAÅARISIZ!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "   Beklenen Stok: 50" -ForegroundColor Gray
        Write-Host "   GerÃ§ek Stok: $($productCheck3.stock)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   âŒ Stok kontrolÃ¼ baÅŸarÄ±sÄ±z" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ğŸ“‹ Ã–ZET" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ÃœrÃ¼n ID: $productId" -ForegroundColor Gray
Write-Host "ÃœrÃ¼n SKU: $productSku" -ForegroundColor Gray
Write-Host "SipariÅŸ ID: $orderId" -ForegroundColor Gray
Write-Host "SipariÅŸ No: $orderNo" -ForegroundColor Gray
Write-Host ""

# 9. StockMovements kontrolÃ¼
Write-Host "9ï¸âƒ£ Stok Hareketleri KontrolÃ¼..." -ForegroundColor Yellow
try {
    $movements = Invoke-RestMethod -Uri "$baseUrl/api/stock-movements?productId=$productId" -Method Get
    Write-Host "   ğŸ“Š Toplam Hareket SayÄ±sÄ±: $($movements.items.Count)" -ForegroundColor Gray
    foreach ($movement in $movements.items) {
        Write-Host "   - Tip: $($movement.movementType), Miktar: $($movement.changeQuantity), Kaynak: $($movement.sourceDocument)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   âš ï¸ Stok hareketleri alÄ±namadÄ±" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Test tamamlandÄ±!" -ForegroundColor Cyan
