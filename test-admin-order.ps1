# Admin Login + Purchase Order Test
$baseUrl = "http://localhost:5055"
$headers = @{ "Content-Type" = "application/json" }

Write-Host "=== ADMIN LOGIN + PURCHASE ORDER TEST ===" -ForegroundColor Cyan

# 1. ADMIN LOGIN
Write-Host "`n1. Admin ile giris yapiliyor..." -ForegroundColor Yellow
$loginPayload = @{
    username = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginPayload -Headers $headers
    $token = $loginResponse.token
    Write-Host "LOGIN BASARILI! Token alindi." -ForegroundColor Green
    
    # Token'i header'a ekle
    $authHeaders = @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $token"
    }
} catch {
    Write-Host "LOGIN HATASI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# 2. TEST URUNU OLUSTUR
Write-Host "`n2. Test urunu olusturuluyor..." -ForegroundColor Yellow
$productPayload = @{
    name = "TEST-PRODUCT-$(Get-Date -Format 'yyyyMMddHHmmss')"
    sku = "TEST-SKU-$(Get-Random -Maximum 99999)"
    price = 100.00
    categoryId = 1
    isActive = $true
} | ConvertTo-Json

try {
    $productResponse = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body $productPayload -Headers $authHeaders
    $productId = $productResponse.id
    $productSku = $productResponse.sku
    Write-Host "URUN OLUSTURULDU: ID=$productId, SKU=$productSku, Stok=$($productResponse.stock)" -ForegroundColor Green
} catch {
    Write-Host "URUN OLUSTURMA HATASI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# 2.5 SUPPLIER KONTROL ET
Write-Host "`n2.5. Supplier kontrol ediliyor..." -ForegroundColor Yellow
try {
    $suppliers = Invoke-RestMethod -Uri "$baseUrl/api/suppliers?pageSize=1" -Method Get -Headers $authHeaders
    if ($suppliers.items.Count -gt 0) {
        $supplierId = $suppliers.items[0].id
        Write-Host "Supplier bulundu: ID=$supplierId, Name=$($suppliers.items[0].name)" -ForegroundColor Green
    } else {
        Write-Host "UYARI: Supplier bulunamadi, manuel supplier olusturun" -ForegroundColor Yellow
        
        # Supplier olustur
        $supplierPayload = @{
            code = "SUP-$(Get-Random -Maximum 9999)"
            name = "Test Supplier"
            isActive = $true
        } | ConvertTo-Json
        
        $newSupplier = Invoke-RestMethod -Uri "$baseUrl/api/suppliers" -Method Post -Body $supplierPayload -Headers $authHeaders
        $supplierId = $newSupplier.id
        Write-Host "Yeni supplier olusturuldu: ID=$supplierId" -ForegroundColor Green
    }
} catch {
    Write-Host "Supplier hatasi, ID=1 kullaniliyor: $($_.Exception.Message)" -ForegroundColor Yellow
    $supplierId = 1
}

Start-Sleep -Seconds 1

# 3. SATIN ALMA SIPARISI OLUSTUR
Write-Host "`n3. Satin alma siparisi olusturuluyor..." -ForegroundColor Yellow
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

Write-Host "Request Payload: $orderPayload" -ForegroundColor Gray

try {
    $orderResponse = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders" -Method Post -Body $orderPayload -Headers $authHeaders
    Write-Host "Response: $($orderResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    $orderId = $orderResponse.id
    $orderNo = $orderResponse.orderNo
    Write-Host "SIPARIS OLUSTURULDU: ID=$orderId, No=$orderNo, Status=$($orderResponse.status)" -ForegroundColor Green
    
    if (-not $orderId) {
        Write-Host "HATA: Order ID bos dondu!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "SIPARIS OLUSTURMA HATASI: $($_.Exception.Message)" -ForegroundColor Red
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Hata detayi: $errorBody" -ForegroundColor Red
    }
    exit 1
}

Start-Sleep -Seconds 1

# 4. STOK KONTROLU (0 OLMALI)
Write-Host "`n4. Stok kontrolu (0 olmali)..." -ForegroundColor Yellow
try {
    $productCheck1 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get -Headers $authHeaders
    Write-Host "Mevcut Stok: $($productCheck1.stock)" -ForegroundColor $(if ($productCheck1.stock -eq 0) { "Green" } else { "Red" })
} catch {
    Write-Host "STOK KONTROLU HATASI" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# 5. SIPARISI ONAYLA (Pending -> Approved)
Write-Host "`n5. Siparis ONAYLANIYOR (Pending -> Approved)..." -ForegroundColor Yellow
$statusPayload = @{ newStatus = "Approved" } | ConvertTo-Json

try {
    $statusResponse1 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload -Headers $authHeaders
    Write-Host "SIPARIS ONAYLANDI: $($statusResponse1.oldStatus) -> $($statusResponse1.newStatus)" -ForegroundColor Green
} catch {
    Write-Host "ONAYLAMA HATASI: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# 6. STOK KONTROLU (HALA 0 OLMALI)
Write-Host "`n6. Stok kontrolu (hala 0 olmali)..." -ForegroundColor Yellow
try {
    $productCheck2 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get -Headers $authHeaders
    Write-Host "Mevcut Stok: $($productCheck2.stock)" -ForegroundColor $(if ($productCheck2.stock -eq 0) { "Green" } else { "Red" })
} catch {
    Write-Host "STOK KONTROLU HATASI" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# 7. SIPARISI TESLIM AL (Approved -> Received) - KRITIK!
Write-Host "`n7. Siparis TESLIM ALINIYOR (Approved -> Received) - STOK ARTMALI..." -ForegroundColor Yellow
$statusPayload2 = @{ newStatus = "Received" } | ConvertTo-Json

try {
    $statusResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/purchase-orders/$orderId/status" -Method Patch -Body $statusPayload2 -Headers $authHeaders
    Write-Host "SIPARIS TESLIM ALINDI: $($statusResponse2.oldStatus) -> $($statusResponse2.newStatus)" -ForegroundColor Green
    Write-Host "Stok guncellendi mi: $($statusResponse2.stockUpdated)" -ForegroundColor $(if ($statusResponse2.stockUpdated) { "Green" } else { "Red" })
} catch {
    Write-Host "TESLIM ALMA HATASI: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 3

# 8. FINAL STOK KONTROLU (50 OLMALI!)
Write-Host "`n8. FINAL STOK KONTROLU (50 olmali)..." -ForegroundColor Yellow
try {
    $productCheck3 = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method Get -Headers $authHeaders
    Write-Host "FINAL STOK: $($productCheck3.stock)" -ForegroundColor $(if ($productCheck3.stock -eq 50) { "Green" } else { "Red" })
    
    if ($productCheck3.stock -eq 50) {
        Write-Host "`n========================================" -ForegroundColor Green
        Write-Host "TEST BASARILI!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    } else {
        Write-Host "`n========================================" -ForegroundColor Red
        Write-Host "TEST BASARISIZ!" -ForegroundColor Red
        Write-Host "Beklenen: 50, Gercek: $($productCheck3.stock)" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
    }
} catch {
    Write-Host "STOK KONTROLU HATASI" -ForegroundColor Red
}

# 9. STOK HAREKETLERI
Write-Host "`n9. Stok hareketleri kontrol ediliyor..." -ForegroundColor Yellow
try {
    $movements = Invoke-RestMethod -Uri "$baseUrl/api/stock-movements?productId=$productId" -Method Get -Headers $authHeaders
    Write-Host "Toplam hareket sayisi: $($movements.items.Count)" -ForegroundColor Gray
    foreach ($movement in $movements.items) {
        Write-Host "  - Tip: $($movement.movementType), Miktar: $($movement.changeQuantity), Kaynak: $($movement.sourceDocument)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Stok hareketleri alinamadi" -ForegroundColor Yellow
}

Write-Host "`n=== OZET ===" -ForegroundColor Cyan
Write-Host "Urun: $productSku (ID: $productId)" -ForegroundColor Gray
Write-Host "Siparis: $orderNo (ID: $orderId)" -ForegroundColor Gray
Write-Host "Baslangic Stok: 0" -ForegroundColor Gray
Write-Host "Siparis Miktari: 50" -ForegroundColor Gray
Write-Host "Final Stok: $($productCheck3.stock)" -ForegroundColor Gray
Write-Host ""
