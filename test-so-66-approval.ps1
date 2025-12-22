# SO-SO-66 ONAY VE SENKRONIZASYON TESTI
$baseUrl = "http://localhost:5055"
$orderNumber = "SO-SO-66"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "SO-SO-66 ONAY VE SENKRONIZASYON TESTI" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "1. Login yapiliyor..." -ForegroundColor Yellow
$loginBody = @{
    orgCode = "100"
    userName = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "   ✓ Login basarili!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "   ✗ Login hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Katana'dan SO-SO-66'yi bul
Write-Host "2. Katana'dan $orderNumber aranıyor..." -ForegroundColor Yellow
try {
    $katanaInvoices = Invoke-RestMethod -Uri "$baseUrl/api/debug/katana/katana-invoices" -Method GET -Headers $headers
    $targetOrder = $katanaInvoices | Where-Object { $_.number -eq $orderNumber }
    
    if ($targetOrder) {
        Write-Host "   ✓ Siparis bulundu!" -ForegroundColor Green
        Write-Host "     - Number: $($targetOrder.number)" -ForegroundColor Gray
        Write-Host "     - Status: $($targetOrder.status)" -ForegroundColor Gray
        Write-Host "     - Approved: $($targetOrder.approved)" -ForegroundColor Gray
        Write-Host "     - ID: $($targetOrder.id)" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "   ✗ $orderNumber bulunamadi!" -ForegroundColor Red
        Write-Host "   Mevcut siparisler:" -ForegroundColor Yellow
        $katanaInvoices | Select-Object -First 5 | ForEach-Object {
            Write-Host "     - $($_.number) (Status: $($_.status), Approved: $($_.approved))" -ForegroundColor Gray
        }
        exit 1
    }
}
catch {
    Write-Host "   ✗ Katana API hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. Eğer onaylanmamışsa onayla
if (-not $targetOrder.approved) {
    Write-Host "3. Siparis onaylanıyor..." -ForegroundColor Yellow
    
    # Katana API'ye direkt onay isteği gönder
    $katanaApiUrl = "https://api.katanamrp.com/v1/sales_order/$($targetOrder.id)"
    $katanaApiKey = "YOUR_KATANA_API_KEY"  # appsettings'den alınmalı
    
    $approveBody = @{
        approved = $true
    } | ConvertTo-Json
    
    try {
        # Bu kısım Katana API key'e ihtiyaç duyar, şimdilik backend üzerinden yapalım
        Write-Host "   ! Katana'da manuel onay gerekiyor veya backend endpoint kullanılmalı" -ForegroundColor Yellow
        Write-Host ""
    }
    catch {
        Write-Host "   ✗ Onay hatasi: $($_.Exception.Message)" -ForegroundColor Red
    }
}
else {
    Write-Host "3. Siparis zaten onaylanmis!" -ForegroundColor Green
    Write-Host ""
}

# 4. Luca'ya senkronize et
Write-Host "4. Luca'ya senkronizasyon baslatiliyor..." -ForegroundColor Yellow
try {
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/sync/from-luca/dispatch" -Method POST -Headers $headers
    Write-Host "   ✓ Senkronizasyon baslatildi!" -ForegroundColor Green
    Write-Host ""
    $syncResponse | ConvertTo-Json -Depth 3
    Write-Host ""
}
catch {
    Write-Host "   ✗ Senkronizasyon hatasi: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# 5. Sonucu kontrol et
Write-Host "5. Sonuc kontrol ediliyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

try {
    $katanaInvoicesAfter = Invoke-RestMethod -Uri "$baseUrl/api/debug/katana/katana-invoices" -Method GET -Headers $headers
    $targetOrderAfter = $katanaInvoicesAfter | Where-Object { $_.number -eq $orderNumber }
    
    if ($targetOrderAfter) {
        Write-Host "   Siparis durumu:" -ForegroundColor Gray
        Write-Host "     - Number: $($targetOrderAfter.number)" -ForegroundColor Gray
        Write-Host "     - Status: $($targetOrderAfter.status)" -ForegroundColor Gray
        Write-Host "     - Approved: $($targetOrderAfter.approved)" -ForegroundColor Gray
        Write-Host ""
        
        if ($targetOrderAfter.approved -and $targetOrderAfter.status -eq "completed") {
            Write-Host "   ✓ BASARILI! Siparis onaylandi ve tamamlandi!" -ForegroundColor Green
        }
        elseif ($targetOrderAfter.approved) {
            Write-Host "   ⚠ Siparis onaylandi ama henuz tamamlanmadi (Status: $($targetOrderAfter.status))" -ForegroundColor Yellow
        }
        else {
            Write-Host "   ✗ Siparis henuz onaylanmadi!" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "   ✗ Kontrol hatasi: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
