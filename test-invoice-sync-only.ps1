# ========================================
# FATURA SYNC TEST SCRIPTI
# ========================================
# Mevcut bir siparisi Luca'ya fatura olarak gonderir

$baseUrl = "http://localhost:8080"
$apiBase = "$baseUrl/api"
$token = ""

# Siparis ID'sini buraya girin (onceki testten)
$orderId = 4003

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
Write-Host "LUCA FATURA SYNC TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========================================
# 1. LOGIN
# ========================================
Write-Host "[1/3] Login yapiliyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "OK Login basarili!" -ForegroundColor Green
    } else {
        Write-Host "HATA Token alinamadi!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "HATA Login hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# ========================================
# 2. SIPARIS DETAYLARINI KONTROL ET
# ========================================
Write-Host "[2/3] Siparis detaylari kontrol ediliyor (ID: $orderId)..." -ForegroundColor Yellow
try {
    $orderDetail = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId" -Method Get -Headers $headers
    
    Write-Host "OK Siparis bulundu!" -ForegroundColor Green
    Write-Host "   Siparis No: $($orderDetail.orderNo)" -ForegroundColor Gray
    Write-Host "   Durum: $($orderDetail.status)" -ForegroundColor Gray
    Write-Host "   Tedarikci: $($orderDetail.supplierName)" -ForegroundColor Gray
    Write-Host "   Toplam: $($orderDetail.totalAmount) TL" -ForegroundColor Gray
    Write-Host "   Luca Sync Durumu: $($orderDetail.isSyncedToLuca)" -ForegroundColor Gray
    Write-Host "   Son Sync Hatasi: $($orderDetail.lastSyncError)" -ForegroundColor Gray
    
    if ($orderDetail.status -ne "Received") {
        Write-Host ""
        Write-Host "UYARI: Siparis 'Received' durumunda degil!" -ForegroundColor Yellow
        Write-Host "   Mevcut Durum: $($orderDetail.status)" -ForegroundColor Yellow
        Write-Host "   Fatura gonderimi icin siparis 'Received' durumunda olmali" -ForegroundColor Yellow
        Write-Host ""
    }
    
} catch {
    Write-Host "HATA Siparis bulunamadi: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    exit 1
}

Write-Host ""

# ========================================
# 3. LUCA'YA FATURA GONDER
# ========================================
Write-Host "[3/3] Luca'ya fatura gonderiliyor..." -ForegroundColor Yellow
Write-Host "   Endpoint: POST $apiBase/purchase-orders/$orderId/sync" -ForegroundColor Cyan

try {
    Write-Host "   Istek gonderiliyor..." -ForegroundColor Gray
    
    $syncResponse = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId/sync" -Method Post -Headers $headers -ContentType "application/json" -TimeoutSec 120 -Verbose
    
    Write-Host ""
    if ($syncResponse.success) {
        Write-Host "OK Luca'ya fatura basariyla aktarildi!" -ForegroundColor Green
        Write-Host "   Luca Purchase Order ID: $($syncResponse.lucaPurchaseOrderId)" -ForegroundColor Gray
        Write-Host "   Luca Belge No: $($syncResponse.lucaDocumentNo)" -ForegroundColor Gray
        Write-Host "   Mesaj: $($syncResponse.message)" -ForegroundColor Gray
    } else {
        Write-Host "HATA Luca aktarimi basarisiz!" -ForegroundColor Red
        Write-Host "   Mesaj: $($syncResponse.message)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Sync Response (JSON):" -ForegroundColor Cyan
    $syncResponse | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "HATA Luca aktarimi hatasi: $($_.Exception.Message)" -ForegroundColor Red
    Write-ApiError $_
    
    Write-Host ""
    Write-Host "HTTP Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    Write-Host "HTTP Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Yellow
}

Write-Host ""

# ========================================
# 4. SYNC SONRASI DURUMU KONTROL ET
# ========================================
Write-Host "Sync sonrasi siparis durumu kontrol ediliyor..." -ForegroundColor Yellow
try {
    $orderDetailAfter = Invoke-RestMethod -Uri "$apiBase/purchase-orders/$orderId" -Method Get -Headers $headers
    
    Write-Host "   Luca Sync Durumu: $($orderDetailAfter.isSyncedToLuca)" -ForegroundColor Gray
    Write-Host "   Son Sync Zamani: $($orderDetailAfter.lastSyncAt)" -ForegroundColor Gray
    Write-Host "   Son Sync Hatasi: $($orderDetailAfter.lastSyncError)" -ForegroundColor Gray
    Write-Host "   Sync Retry Count: $($orderDetailAfter.syncRetryCount)" -ForegroundColor Gray
    Write-Host "   Luca Purchase Order ID: $($orderDetailAfter.lucaPurchaseOrderId)" -ForegroundColor Gray
    Write-Host "   Luca Document No: $($orderDetailAfter.lucaDocumentNo)" -ForegroundColor Gray
    
} catch {
    Write-Host "   Siparis durumu okunamadi" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
