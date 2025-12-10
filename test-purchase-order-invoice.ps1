# ========================================
# SATIN ALMA SİPARİŞİ VE FATURA AKTARIMI TEST SCRIPTI
# ========================================
# Bu script şunları test eder:
# 1. Satınalma siparişi oluşturma
# 2. Sipariş durumunu Approved'a çekme
# 3. Sipariş durumunu Received'a çekme (stok artışı tetiklenir)
# 4. Luca'ya fatura aktarımı
# 5. Stok hareketlerinin doğruluğu

$baseUrl = "http://localhost:8080"
$apiBase = "$baseUrl/api"
$token = ""

function Write-ApiError {
    param([Parameter(Mandatory=$true)] $ErrorObject)

    if ($ErrorObject.ErrorDetails.Message) {
        Write-Host "   Detay: $($ErrorObject.ErrorDetails.Message)" -ForegroundColor Red
        return
    }

    $resp = $ErrorObject.Exception.Response
    if ($resp -and $resp.GetResponseStream) {
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $body = $reader.ReadToEnd()
        if ($body) {
            Write-Host "   Response Body: $body" -ForegroundColor Red
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SATIN ALMA SİPARİŞİ VE FATURA TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========================================
# 1. LOGIN (Token al)
# ========================================
Write-Host "[1/7] Login yapılıyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "✅ Login başarılı!" -ForegroundColor Green
        Write-Host "   Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
    } else {
        Write-Host "❌ Token alınamadı!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Login hatası: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# ========================================
# 2. TEDARİKÇİ KONTROL (Supplier)
# ========================================
Write-Host "[2/7] Tedarikçi kontrol ediliyor..." -ForegroundColor Yellow
try {
    $suppliersResponse = Invoke-RestMethod -Uri "$apiBase/suppliers" -Method Get -Headers $headers
    $supplierList = @()
    if ($suppliersResponse -is [System.Collections.IEnumerable]) {
        $supplierList = $suppliersResponse
    } elseif ($suppliersResponse.items) {
        $supplierList = $suppliersResponse.items
    }

    if ($supplierList.Count -gt 0) {
        $supplier = $supplierList | Select-Object -First 1
        Write-Host "✅ Tedarikçi bulundu: $($supplier.name) (ID: $($supplier.id))" -ForegroundColor Green
    } else {
        Write-Host "❌ Tedarikçi bulunamadı! Önce tedarikçi oluşturun." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Tedarikçi sorgulanamadı: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ========================================
# 3. ÜRÜN KONTROL (Product)
# ========================================
Write-Host "[3/7] Ürün kontrol ediliyor..." -ForegroundColor Yellow
try {
    $productsResponse = Invoke-RestMethod -Uri "$apiBase/products" -Method Get -Headers $headers
    $productList = @()
    if ($productsResponse -is [System.Collections.IEnumerable]) {
        $productList = $productsResponse
    } elseif ($productsResponse.data) {
        $productList = $productsResponse.data
    }

    if ($productList.Count -gt 0) {
        $product = $productList | Select-Object -First 1
        Write-Host "✅ Ürün bulundu: $($product.name) (SKU: $($product.sku), ID: $($product.id))" -ForegroundColor Green
    } else {
        Write-Host "❌ Ürün bulunamadı! Önce ürün oluşturun." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Ürün sorgulanamadı: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ========================================
# 4. SATIN ALMA SİPARİŞİ OLUŞTUR
# ========================================
Write-Host "[4/7] Satınalma siparişi oluşturuluyor..." -ForegroundColor Yellow
try {
    $orderBody = @{
        supplierId = $supplier.id
        orderDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
        expectedDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss")
        documentSeries = "A"
        documentTypeDetailId = 2
        vatIncluded = $true
        projectCode = "TEST-PROJECT"
        description = "Test PO - invoice sync"
        items = @(
            @{
                productId = $product.id
                quantity = 10
                unitPrice = 100.50
                lucaStockCode = $product.sku
                warehouseCode = "01"
                vatRate = 20
                unitCode = "AD"
                discountAmount = 0
            }
        )
    } | ConvertTo-Json -Depth 10

    $orderResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders" -Method Post -Body $orderBody -Headers $headers -ContentType "application/json"
    $orderId = $orderResponse.id
    $orderNo = $orderResponse.orderNo
    
    Write-Host "✅ Sipariş oluşturuldu!" -ForegroundColor Green
    Write-Host "   Order ID: $orderId" -ForegroundColor Gray
    Write-Host "   Order No: $orderNo" -ForegroundColor Gray
    Write-Host "   Durum: $($orderResponse.status)" -ForegroundColor Gray
    Write-Host "   Toplam: $($orderResponse.totalAmount) TL" -ForegroundColor Gray
} catch {
    Write-Host "❌ Sipariş oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""

# ========================================
# 5. SİPARİŞ DURUMUNU APPROVED'A ÇEK
# ========================================
Write-Host "[5/7] Sipariş onaylanıyor (Pending -> Approved)..." -ForegroundColor Yellow
try {
    $statusBody = @{
        newStatus = 1   # PurchaseOrderStatus.Approved
    } | ConvertTo-Json

    $statusResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId/status" -Method Patch -Body $statusBody -Headers $headers -ContentType "application/json"
    
    Write-Host "✅ Sipariş onaylandı!" -ForegroundColor Green
    Write-Host "   Eski Durum: $($statusResponse.oldStatus)" -ForegroundColor Gray
    Write-Host "   Yeni Durum: $($statusResponse.newStatus)" -ForegroundColor Gray
    
    Start-Sleep -Seconds 2
} catch {
    Write-Host "❌ Sipariş onaylanamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""

# ========================================
# 6. SİPARİŞ DURUMUNU RECEIVED'A ÇEK (STOK ARTIŞI)
# ========================================
Write-Host "[6/7] Sipariş teslim alınıyor (Approved -> Received)..." -ForegroundColor Yellow
Write-Host "   ⚠️  Bu işlem STOK ARTIŞI tetikler!" -ForegroundColor Magenta
try {
    $statusBody = @{
        newStatus = 2   # PurchaseOrderStatus.Received
    } | ConvertTo-Json

    $statusResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId/status" -Method Patch -Body $statusBody -Headers $headers -ContentType "application/json"
    
    Write-Host "✅ Sipariş teslim alındı!" -ForegroundColor Green
    Write-Host "   Eski Durum: $($statusResponse.oldStatus)" -ForegroundColor Gray
    Write-Host "   Yeni Durum: $($statusResponse.newStatus)" -ForegroundColor Gray
    Write-Host "   Stok Güncellendi: $($statusResponse.stockUpdated)" -ForegroundColor Gray
    
    Start-Sleep -Seconds 3
} catch {
    Write-Host "❌ Sipariş teslim alınamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""

# ========================================
# 7. LUCA'YA FATURA AKTARIMI
# ========================================
Write-Host "[7/7] Luca'ya fatura aktarımı yapılıyor..." -ForegroundColor Yellow
try {
    $syncResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId/sync" -Method Post -Headers $headers -ContentType "application/json"
    
    if ($syncResponse.success) {
        Write-Host "✅ Luca'ya fatura başarıyla aktarıldı!" -ForegroundColor Green
        Write-Host "   Luca Purchase Order ID: $($syncResponse.lucaPurchaseOrderId)" -ForegroundColor Gray
        Write-Host "   Luca Belge No: $($syncResponse.lucaDocumentNo)" -ForegroundColor Gray
        Write-Host "   Mesaj: $($syncResponse.message)" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Luca aktarımı başarısız!" -ForegroundColor Yellow
        Write-Host "   Mesaj: $($syncResponse.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Luca aktarımı hatası: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
} finally {
    # Sync sonrası durumu mutlaka oku
    try {
        $orderDetail = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId" -Method Get -Headers $headers
        Write-Host "   Luca Sync State -> IsSynced: $($orderDetail.isSyncedToLuca) / LastError: $($orderDetail.lastSyncError)" -ForegroundColor Gray
        Write-Host "   Luca IDs -> PurchaseOrderId: $($orderDetail.lucaPurchaseOrderId) / BelgeNo: $($orderDetail.lucaDocumentNo)" -ForegroundColor Gray
    } catch {
        Write-Host "   Luca sync durumunu okuyamadım" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 ÖZET:" -ForegroundColor White
Write-Host "   • Sipariş No: $orderNo" -ForegroundColor Gray
Write-Host "   • Sipariş ID: $orderId" -ForegroundColor Gray
Write-Host "   • Ürün: $($product.name) ($($product.sku))" -ForegroundColor Gray
Write-Host "   • Miktar: 10 adet" -ForegroundColor Gray
Write-Host "   • Birim Fiyat: 100.50 TL" -ForegroundColor Gray
Write-Host ""
Write-Host "🔍 KONTROL EDİLECEKLER:" -ForegroundColor White
Write-Host "   1. Stok hareketi oluştu mu? (StockMovements tablosu)" -ForegroundColor Gray
Write-Host "   2. Stock tablosuna kayıt düştü mü?" -ForegroundColor Gray
Write-Host "   3. Luca'da fatura görünüyor mu?" -ForegroundColor Gray
Write-Host "   4. Bildirim (Notification) oluştu mu?" -ForegroundColor Gray
Write-Host ""
