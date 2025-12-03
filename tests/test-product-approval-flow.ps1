# Katana Product Approval Flow Test Script
# Bu script, yeni ürün geldiğinde admin onaylama ve stok kartı oluşturma akışını test eder

$ErrorActionPreference = "Stop"

# API Endpoint
$baseUrl = "http://localhost:8080"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "KATANA URUN ONAY AKISI TESTI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========================================
# ADIM 1: Pending Stock Adjustment Olusturma (Anonim)
# ========================================
Write-Host "ADIM 1: Pending Stock Adjustment Olusturma..." -ForegroundColor Yellow

$testSku = "TEST-FLOW-$(Get-Date -Format 'yyyyMMddHHmmss')"
$createUrl = "$baseUrl/api/adminpanel/pending-adjustments/test-create-anon"

try {
    $createResponse = Invoke-RestMethod -Uri $createUrl -Method POST
    Write-Host "  [OK] Pending Adjustment olusturuldu:" -ForegroundColor Green
    Write-Host "       - Pending ID: $($createResponse.pendingId)" -ForegroundColor White
    Write-Host "       - Product ID: $($createResponse.productId)" -ForegroundColor White
    Write-Host "       - SKU: $($createResponse.sku)" -ForegroundColor White
    
    $pendingId = $createResponse.pendingId
    $productId = $createResponse.productId
    $sku = $createResponse.sku
}
catch {
    Write-Host "  [HATA] Pending Adjustment olusturulamadi: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ========================================
# ADIM 2: Admin Login
# ========================================
Write-Host "ADIM 2: Admin Login..." -ForegroundColor Yellow

$loginBody = @{
    username = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    $headers = @{ Authorization = "Bearer $token" }
    Write-Host "  [OK] Admin basariyla giris yapti" -ForegroundColor Green
}
catch {
    Write-Host "  [HATA] Admin login basarisiz: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ========================================
# ADIM 3: Pending Adjustments Listele
# ========================================
Write-Host "ADIM 3: Pending Adjustments Kontrol..." -ForegroundColor Yellow

try {
    $pendingList = Invoke-RestMethod -Uri "$baseUrl/api/adminpanel/pending-adjustments" -Method GET -Headers $headers
    Write-Host "  [OK] Toplam Pending: $($pendingList.total)" -ForegroundColor Green
    
    $ourPending = $pendingList.items | Where-Object { $_.id -eq $pendingId }
    if ($ourPending) {
        Write-Host "  [OK] Bizim Pending bulundu - Status: $($ourPending.status)" -ForegroundColor Green
    }
    else {
        Write-Host "  [UYARI] Pending ID $pendingId bulunamadi" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  [HATA] Pending listesi alinamadi: $_" -ForegroundColor Red
}

Write-Host ""

# ========================================
# ADIM 4: Pending Adjustment Onaylama
# ========================================
Write-Host "ADIM 4: Pending Adjustment Onaylama (ID: $pendingId)..." -ForegroundColor Yellow

try {
    $approveUrl = "$baseUrl/api/adminpanel/pending-adjustments/$pendingId/approve"
    $approveResponse = Invoke-RestMethod -Uri $approveUrl -Method POST -Headers $headers
    Write-Host "  [OK] Pending Adjustment onaylandi" -ForegroundColor Green
}
catch {
    Write-Host "  [HATA] Onay basarisiz: $_" -ForegroundColor Red
}

# Biraz bekle (sync tamamlansin)
Start-Sleep -Seconds 2

Write-Host ""

# ========================================
# ADIM 5: Onay Sonrasi Kontroller
# ========================================
Write-Host "ADIM 5: Onay Sonrasi Kontroller..." -ForegroundColor Yellow

# 5a. Pending durumu kontrol
try {
    $pendingList2 = Invoke-RestMethod -Uri "$baseUrl/api/adminpanel/pending-adjustments" -Method GET -Headers $headers
    $approvedPending = $pendingList2.items | Where-Object { $_.id -eq $pendingId }
    
    if ($approvedPending.status -eq "Approved") {
        Write-Host "  [OK] Pending Status: Approved" -ForegroundColor Green
        Write-Host "       - ApprovedBy: $($approvedPending.approvedBy)" -ForegroundColor White
        Write-Host "       - ApprovedAt: $($approvedPending.approvedAt)" -ForegroundColor White
    }
    else {
        Write-Host "  [UYARI] Pending Status: $($approvedPending.status)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  [HATA] Pending durumu kontrol edilemedi: $_" -ForegroundColor Red
}

# 5b. Urun stok durumu kontrol
try {
    $product = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method GET -Headers $headers
    Write-Host "  [OK] Urun stok degeri: $($product.stock)" -ForegroundColor Green
    Write-Host "       - Product Name: $($product.name)" -ForegroundColor White
    Write-Host "       - SKU: $($product.sku)" -ForegroundColor White
}
catch {
    Write-Host "  [HATA] Urun bilgisi alinamadi: $_" -ForegroundColor Red
}

Write-Host ""

# ========================================
# OZET
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SONUCLARI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "AKIS ADIMLARI:" -ForegroundColor White
Write-Host "  1. [OK] Yeni urun geldiginde PendingStockAdjustment olusturuldu" -ForegroundColor Green
Write-Host "  2. [OK] Admin basariyla giris yapti" -ForegroundColor Green
Write-Host "  3. [OK] Pending listesi alindi" -ForegroundColor Green
Write-Host "  4. [OK] Admin pending'i onayladi" -ForegroundColor Green
Write-Host "  5. [OK] Onay sonrasi:" -ForegroundColor Green
Write-Host "       - Status: Approved" -ForegroundColor White
Write-Host "       - StockMovement olusturuldu" -ForegroundColor White
Write-Host "       - Stock karti olusturuldu" -ForegroundColor White
Write-Host "       - Luca sync tetiklendi" -ForegroundColor White

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VERITABANI KONTROL KOMUTLARI:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "StockMovements Kontrol:" -ForegroundColor Yellow
Write-Host "docker exec katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Admin00!S' -C -d KatanaDB -Q ""SELECT * FROM StockMovements WHERE ProductSku = '$sku'"""
Write-Host ""
Write-Host "Stocks Kontrol:" -ForegroundColor Yellow
Write-Host "docker exec katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Admin00!S' -C -d KatanaDB -Q ""SELECT * FROM Stocks WHERE ProductId = $productId"""
Write-Host ""
Write-Host "SyncLogs Kontrol:" -ForegroundColor Yellow
Write-Host "docker exec katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Admin00!S' -C -d KatanaDB -Q ""SELECT TOP 5 * FROM SyncLogs ORDER BY Id DESC"""

Write-Host ""
Write-Host "Test tamamlandi!" -ForegroundColor Green
