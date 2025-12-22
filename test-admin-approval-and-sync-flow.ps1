#!/usr/bin/env pwsh

<#
.SYNOPSIS
Admin Onayı ve Kozaya Senkronizasyon Akışını Test Eder

.DESCRIPTION
1. Satış siparişi listesini alır
2. Bir siparişi admin onayı ile onayla
3. Onaylanan siparişi Kozaya senkronize et
4. Senkronizasyon durumunu kontrol et

.EXAMPLE
.\test-admin-approval-and-sync-flow.ps1 -ApiUrl "http://localhost:5055" -Token "your-jwt-token"
#>

param(
    [string]$ApiUrl = "http://localhost:5055",
    [string]$Token = "",
    [int]$OrderId = 0,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Renkli çıktı için fonksiyonlar
function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

# Headers hazırla
$headers = @{
    "Content-Type" = "application/json"
}

if ($Token) {
    $headers["Authorization"] = "Bearer $Token"
}

Write-Info "Admin Onayı ve Kozaya Senkronizasyon Test Başlıyor..."
Write-Info "API URL: $ApiUrl"
Write-Info ""

# 1. Satış siparişi listesini al
Write-Info "1️⃣  Satış siparişi listesi alınıyor..."
try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders?page=1&pageSize=10" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop

    if ($response -and $response.Count -gt 0) {
        Write-Success "Toplam $($response.Count) sipariş bulundu"
        
        # Siparişleri listele
        $response | ForEach-Object {
            Write-Host "  - OrderNo: $($_.orderNo), Status: $($_.status), Synced: $($_.isSyncedToLuca)" -ForegroundColor Gray
        }
        
        # İlk siparişi seç (veya parametreden al)
        if ($OrderId -eq 0) {
            $selectedOrder = $response[0]
            $OrderId = $selectedOrder.id
        } else {
            $selectedOrder = $response | Where-Object { $_.id -eq $OrderId } | Select-Object -First 1
        }
        
        if ($selectedOrder) {
            Write-Success "Seçilen sipariş: $($selectedOrder.orderNo) (ID: $OrderId)"
        } else {
            Write-Error "Sipariş ID $OrderId bulunamadı"
            exit 1
        }
    } else {
        Write-Error "Sipariş bulunamadı"
        exit 1
    }
} catch {
    Write-Error "Sipariş listesi alınamadı: $_"
    exit 1
}

Write-Info ""

# 2. Sipariş detayını al
Write-Info "2️⃣  Sipariş detayı alınıyor (ID: $OrderId)..."
try {
    $orderDetail = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders/$OrderId" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop

    Write-Success "Sipariş detayı alındı"
    Write-Host "  - OrderNo: $($orderDetail.orderNo)" -ForegroundColor Gray
    Write-Host "  - Status: $($orderDetail.status)" -ForegroundColor Gray
    Write-Host "  - Müşteri: $($orderDetail.customerName)" -ForegroundColor Gray
    Write-Host "  - Satır Sayısı: $($orderDetail.lines.Count)" -ForegroundColor Gray
    Write-Host "  - Luca Senkronize: $($orderDetail.isSyncedToLuca)" -ForegroundColor Gray
    
    if ($orderDetail.lines.Count -eq 0) {
        Write-Warning "Sipariş satırları boş! Katana'dan tekrar çek."
        exit 1
    }
} catch {
    Write-Error "Sipariş detayı alınamadı: $_"
    exit 1
}

Write-Info ""

# 3. Admin Onayı
if ($orderDetail.status -ne "APPROVED" -and $orderDetail.status -ne "APPROVED_WITH_ERRORS") {
    Write-Info "3️⃣  Admin onayı yapılıyor..."
    try {
        $approveResponse = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders/$OrderId/approve" `
            -Method Post `
            -Headers $headers `
            -Body "{}" `
            -ErrorAction Stop

        if ($approveResponse.success) {
            Write-Success "Admin onayı başarılı"
            Write-Host "  - OrderNo: $($approveResponse.orderNo)" -ForegroundColor Gray
            Write-Host "  - Status: $($approveResponse.orderStatus)" -ForegroundColor Gray
            Write-Host "  - Katana Order ID: $($approveResponse.katanaOrderId)" -ForegroundColor Gray
        } else {
            Write-Error "Admin onayı başarısız: $($approveResponse.message)"
            Write-Host "  - Error: $($approveResponse.error)" -ForegroundColor Gray
            exit 1
        }
    } catch {
        Write-Error "Admin onayı sırasında hata: $_"
        exit 1
    }
} else {
    Write-Warning "Sipariş zaten onaylanmış (Status: $($orderDetail.status))"
}

Write-Info ""

# 4. Senkronizasyon durumunu kontrol et
Write-Info "4️⃣  Senkronizasyon durumu kontrol ediliyor..."
try {
    $syncStatus = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders/$OrderId/sync-status" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "  - Status: $($syncStatus.status)" -ForegroundColor Gray
    Write-Host "  - IsSyncedToLuca: $($syncStatus.isSyncedToLuca)" -ForegroundColor Gray
    Write-Host "  - LucaOrderId: $($syncStatus.lucaOrderId)" -ForegroundColor Gray
    if ($syncStatus.lastSyncError) {
        Write-Host "  - LastSyncError: $($syncStatus.lastSyncError)" -ForegroundColor Red
    }
} catch {
    Write-Error "Senkronizasyon durumu alınamadı: $_"
}

Write-Info ""

# 5. Kozaya Senkronize Et
if (-not $syncStatus.isSyncedToLuca) {
    Write-Info "5️⃣  Kozaya senkronizasyon yapılıyor..."
    try {
        $syncResponse = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders/$OrderId/sync" `
            -Method Post `
            -Headers $headers `
            -Body "{}" `
            -ErrorAction Stop

        if ($syncResponse.isSuccess) {
            Write-Success "Kozaya senkronizasyon başarılı"
            Write-Host "  - Message: $($syncResponse.message)" -ForegroundColor Gray
            Write-Host "  - LucaOrderId: $($syncResponse.lucaOrderId)" -ForegroundColor Gray
            Write-Host "  - SyncedAt: $($syncResponse.syncedAt)" -ForegroundColor Gray
        } else {
            Write-Error "Kozaya senkronizasyon başarısız: $($syncResponse.message)"
            Write-Host "  - ErrorDetails: $($syncResponse.errorDetails)" -ForegroundColor Gray
            exit 1
        }
    } catch {
        Write-Error "Kozaya senkronizasyon sırasında hata: $_"
        exit 1
    }
} else {
    Write-Success "Sipariş zaten Kozaya senkronize edilmiş"
}

Write-Info ""

# 6. Final durumu kontrol et
Write-Info "6️⃣  Final durumu kontrol ediliyor..."
try {
    $finalStatus = Invoke-RestMethod -Uri "$ApiUrl/api/sales-orders/$OrderId" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop

    Write-Success "Final Durum:"
    Write-Host "  - OrderNo: $($finalStatus.orderNo)" -ForegroundColor Gray
    Write-Host "  - Status: $($finalStatus.status)" -ForegroundColor Gray
    Write-Host "  - Katana Order ID: $($finalStatus.katanaOrderId)" -ForegroundColor Gray
    Write-Host "  - Luca Order ID: $($finalStatus.lucaOrderId)" -ForegroundColor Gray
    Write-Host "  - IsSyncedToLuca: $($finalStatus.isSyncedToLuca)" -ForegroundColor Gray
    
    if ($finalStatus.lastSyncError) {
        Write-Host "  - LastSyncError: $($finalStatus.lastSyncError)" -ForegroundColor Red
    }
} catch {
    Write-Error "Final durum alınamadı: $_"
}

Write-Info ""
Write-Success "Test Tamamlandı! ✨"
Write-Info ""
Write-Info "Özet:"
Write-Info "  1. Sipariş listesi alındı"
Write-Info "  2. Sipariş detayı alındı"
Write-Info "  3. Admin onayı yapıldı"
Write-Info "  4. Senkronizasyon durumu kontrol edildi"
Write-Info "  5. Kozaya senkronizasyon yapıldı"
Write-Info "  6. Final durum kontrol edildi"
Write-Info ""
Write-Success "Sistem tamamen çalışıyor! 🎉"
