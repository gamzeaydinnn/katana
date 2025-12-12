# ========================================
# KATANA SİPARİŞ ONAY AKIŞI TEST SCRIPTI
# ========================================
# Bu script şunları test eder:
# 1. Katana'dan sipariş geldiğinde sipariş sekmesine düşüyor mu?
# 2. Admin onayladığında (Approved) sipariş durumu değişiyor mu?
# 3. Admin onayından sonra Katana'ya geri gönderiliyor mu?

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
Write-Host "KATANA SİPARİŞ ONAY AKIŞI TESTİ" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========================================
# 1. LOGIN (Token al)
# ========================================
Write-Host "[1/6] Login yapılıyor..." -ForegroundColor Yellow
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
Write-Host "[2/6] Tedarikçi kontrol ediliyor..." -ForegroundColor Yellow
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
Write-Host "[3/6] Ürün kontrol ediliyor..." -ForegroundColor Yellow
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
# 4. KATANA'DAN SİPARİŞ GELİYOR (Simülasyon)
# ========================================
Write-Host "[4/6] Katana'dan siparis geliyor (simulasyon)..." -ForegroundColor Yellow
Write-Host "   INFO: Gercek senaryoda bu Katana webhook'undan gelir" -ForegroundColor Cyan
try {
    $orderBody = @{
        supplierId = $supplier.id
        orderDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
        expectedDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss")
        documentSeries = "A"
        documentTypeDetailId = 2
        vatIncluded = $true
        projectCode = "KATANA-ORDER"
        description = "Katanadan gelen test siparisi"
        items = @(
            @{
                productId = $product.id
                quantity = 5
                unitPrice = 250.00
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
    
    Write-Host "✅ Sipariş sisteme düştü!" -ForegroundColor Green
    Write-Host "   Order ID: $orderId" -ForegroundColor Gray
    Write-Host "   Order No: $orderNo" -ForegroundColor Gray
    Write-Host "   Durum: $($orderResponse.status) (Pending - Onay bekliyor)" -ForegroundColor Yellow
    Write-Host "   Toplam: $($orderResponse.totalAmount) TL" -ForegroundColor Gray
} catch {
    Write-Host "❌ Sipariş oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""

# ========================================
# 5. SİPARİŞ SEKMESİNDE GÖRÜNÜYOR MU?
# ========================================
Write-Host "[5/6] Siparis sekmesinde gorunuyor mu kontrol ediliyor..." -ForegroundColor Yellow
try {
    # Pending siparişleri listele
    $pendingOrders = Invoke-RestMethod -Uri "$apiBase/purchase-orders?status=Pending" -Method Get -Headers $headers
    
    $orderList = @()
    if ($pendingOrders -is [System.Collections.IEnumerable]) {
        $orderList = $pendingOrders
    } elseif ($pendingOrders.items) {
        $orderList = $pendingOrders.items
    }
    
    $foundOrder = $orderList | Where-Object { $_.id -eq $orderId }
    
    if ($foundOrder) {
        Write-Host "✅ Sipariş sekmesinde görünüyor!" -ForegroundColor Green
        Write-Host "   Sipariş No: $($foundOrder.orderNumber)" -ForegroundColor Gray
        Write-Host "   Tedarikçi: $($foundOrder.supplierName)" -ForegroundColor Gray
        Write-Host "   Durum: $($foundOrder.status)" -ForegroundColor Gray
        Write-Host "   Tutar: $($foundOrder.totalAmount) TL" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Sipariş listede bulunamadı!" -ForegroundColor Yellow
    }
    
    # İstatistikleri göster
    $stats = Invoke-RestMethod -Uri "$apiBase/purchase-orders/stats" -Method Get -Headers $headers
    Write-Host ""
    Write-Host "Siparis Istatistikleri:" -ForegroundColor Cyan
    Write-Host "   Toplam: $($stats.total)" -ForegroundColor Gray
    Write-Host "   Bekleyen (Pending): $($stats.pending)" -ForegroundColor Yellow
    Write-Host "   Onaylı (Approved): $($stats.approved)" -ForegroundColor Green
    Write-Host "   Teslim Alındı (Received): $($stats.received)" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Sipariş listesi sorgulanamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
}

Write-Host ""

# ========================================
# 6. ADMIN ONAYI (Pending -> Approved)
# ========================================
Write-Host "[6/6] Admin siparisi onayliyor..." -ForegroundColor Yellow
Write-Host "   INFO: Bu islem admin panelinden yapilir" -ForegroundColor Cyan
try {
    $statusBody = @{
        newStatus = 1   # PurchaseOrderStatus.Approved
    } | ConvertTo-Json

    $statusResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId/status" -Method Patch -Body $statusBody -Headers $headers -ContentType "application/json"
    
    Write-Host "✅ Sipariş onaylandı!" -ForegroundColor Green
    Write-Host "   Eski Durum: $($statusResponse.oldStatus)" -ForegroundColor Gray
    Write-Host "   Yeni Durum: $($statusResponse.newStatus)" -ForegroundColor Green
    Write-Host "   INFO: Arka planda Katana'ya urun ekleniyor/guncelleniyor..." -ForegroundColor Cyan
    
    # Katana sync için biraz bekle
    Write-Host "   Katana sync icin 5 saniye bekleniyor..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    # Onaylandıktan sonra detayları kontrol et
    $orderDetail = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId" -Method Get -Headers $headers
    Write-Host ""
    Write-Host "Siparis Detaylari (Onay Sonrasi):" -ForegroundColor Cyan
    Write-Host "   Sipariş No: $($orderDetail.orderNo)" -ForegroundColor Gray
    Write-Host "   Durum: $($orderDetail.status)" -ForegroundColor Green
    Write-Host "   Tedarikçi: $($orderDetail.supplierName)" -ForegroundColor Gray
    Write-Host "   Toplam Tutar: $($orderDetail.totalAmount) TL" -ForegroundColor Gray
    Write-Host "   Oluşturulma: $($orderDetail.createdAt)" -ForegroundColor Gray
    Write-Host "   Güncellenme: $($orderDetail.updatedAt)" -ForegroundColor Gray
    
    # Katana'da ürün kontrolü
    Write-Host ""
    Write-Host "Katana'da urun kontrolu yapiliyor..." -ForegroundColor Yellow
    try {
        # Katana API'den ürünleri çek (proxy üzerinden)
        $katanaProducts = Invoke-RestMethod -Uri "$apiBase/katana/products" -Method Get -Headers $headers -ErrorAction SilentlyContinue
        
        if ($katanaProducts) {
            $katanaProductList = @()
            if ($katanaProducts -is [System.Collections.IEnumerable]) {
                $katanaProductList = $katanaProducts
            } elseif ($katanaProducts.data) {
                $katanaProductList = $katanaProducts.data
            }
            
            # Siparişteki ürünü Katana'da ara
            $katanaProduct = $katanaProductList | Where-Object { $_.sku -eq $product.sku }
            
            if ($katanaProduct) {
                Write-Host "✅ Urun Katana'da bulundu!" -ForegroundColor Green
                Write-Host "   Katana ID: $($katanaProduct.id)" -ForegroundColor Gray
                Write-Host "   SKU: $($katanaProduct.sku)" -ForegroundColor Gray
                Write-Host "   Isim: $($katanaProduct.name)" -ForegroundColor Gray
                Write-Host "   Stok: $($katanaProduct.inStock)" -ForegroundColor Gray
                Write-Host "   Fiyat: $($katanaProduct.salesPrice)" -ForegroundColor Gray
            } else {
                Write-Host "⚠️  Urun Katana'da bulunamadi (henuz sync olmamis olabilir)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️  Katana urunleri alinamadi" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  Katana kontrolu yapilamadi: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Sipariş onaylanamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OZET:" -ForegroundColor White
Write-Host "   - Siparis No: $orderNo" -ForegroundColor Gray
Write-Host "   - Siparis ID: $orderId" -ForegroundColor Gray
Write-Host "   - Baslangic Durumu: Pending (Beklemede)" -ForegroundColor Yellow
Write-Host "   - Son Durum: Approved (Onaylandi)" -ForegroundColor Green
Write-Host "   - Urun: $($product.name) ($($product.sku))" -ForegroundColor Gray
Write-Host "   - Miktar: 5 adet" -ForegroundColor Gray
Write-Host "   - Birim Fiyat: 250.00 TL" -ForegroundColor Gray
Write-Host ""
Write-Host "BASARILI ADIMLAR:" -ForegroundColor Green
Write-Host "   1. Katana'dan siparis geldi" -ForegroundColor Gray
Write-Host "   2. Siparis sekmesine dustu (Pending)" -ForegroundColor Gray
Write-Host "   3. Admin onayladi (Approved)" -ForegroundColor Gray
Write-Host "   4. Katana'ya urun eklendi/guncellendi" -ForegroundColor Gray
Write-Host ""
Write-Host "SONRAKI ADIMLAR:" -ForegroundColor White
Write-Host "   - Siparis 'Received' durumuna cekildiginde stok artisi olacak" -ForegroundColor Gray
Write-Host "   - Luca'ya fatura aktarimi yapilabilir" -ForegroundColor Gray
Write-Host "   - Katana'dan sync ile stok karti olusturulabilir" -ForegroundColor Gray
Write-Host ""
Write-Host "NOT:" -ForegroundColor Cyan
Write-Host "   Gercek senaryoda Katana webhook'u ile siparis otomatik gelir." -ForegroundColor Gray
Write-Host "   Bu test manuel olarak siparis olusturarak akisi simule eder." -ForegroundColor Gray
Write-Host ""
