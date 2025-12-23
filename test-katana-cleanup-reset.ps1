# Script to reset all approved orders back to pending status
# Bu script tüm onaylı siparişleri sıfırlar
# UYARI: Bu işlem geri alınamaz!

param(
    [switch]$DryRun = $true,  # Varsayılan olarak DRY RUN modunda
    [switch]$Force = $false    # Onay istemeden çalıştır
)

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║           SİPARİŞ SIFIRLAMA - TÜM SİPARİŞLER              ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 MOD: DRY RUN (Simülasyon)" -ForegroundColor Yellow
    Write-Host "   Hiçbir sipariş sıfırlanmayacak, sadece rapor gösterilecek" -ForegroundColor Gray
} else {
    Write-Host "⚠️  MOD: GERÇEK SIFIRLAMA" -ForegroundColor Red
    Write-Host "   Siparişler KALICI olarak sıfırlanacak!" -ForegroundColor Red
}
Write-Host ""

# Login
Write-Host "Giriş yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Giriş başarılı" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Giriş başarısız: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-API-Key" = $apiKey
}

# Reset orders
Write-Host "Siparişler kontrol ediliyor..." -ForegroundColor Yellow

$resetRequest = @{
    dryRun = $DryRun
} | ConvertTo-Json

try {
    $resetResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/katanacleanup/reset-orders" `
        -Method Post `
        -Headers $headers `
        -Body $resetRequest `
        -ContentType "application/json"
    
    Write-Host "✓ Kontrol tamamlandı" -ForegroundColor Green
    Write-Host ""
    
    if ($resetResult.ordersReset -eq 0) {
        Write-Host "ℹ️  Sıfırlanacak sipariş bulunamadı" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "📊 ÖZET:" -ForegroundColor Cyan
    Write-Host "  • Sıfırlanacak Sipariş: $($resetResult.ordersReset)" -ForegroundColor White
    Write-Host "  • Etkilenecek Satır  : $($resetResult.linesAffected)" -ForegroundColor White
    Write-Host "  • Silinecek Mapping  : $($resetResult.mappingsCleared)" -ForegroundColor White
    Write-Host ""
    
    if ($DryRun) {
        Write-Host "ℹ️  Bu bir DRY RUN - hiçbir değişiklik yapılmadı" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "💡 Gerçekten sıfırlamak için:" -ForegroundColor Cyan
        Write-Host "   .\test-katana-cleanup-reset.ps1 -DryRun:`$false" -ForegroundColor Gray
        Write-Host ""
        exit 0
    }
    
} catch {
    Write-Host "✗ Kontrol başarısız: $_" -ForegroundColor Red
    exit 1
}

# Confirmation for actual reset
if (-not $Force) {
    Write-Host "⚠️  UYARI: Bu işlem geri alınamaz!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Bu işlem şunları yapacak:" -ForegroundColor Yellow
    Write-Host "  • Sipariş durumunu 'Approved' → 'Pending' yapacak" -ForegroundColor White
    Write-Host "  • ApprovedDate, ApprovedBy, SyncStatus temizlenecek" -ForegroundColor White
    Write-Host "  • Tüm KatanaOrderId değerleri silinecek" -ForegroundColor White
    Write-Host "  • Tüm OrderMapping kayıtları silinecek" -ForegroundColor White
    Write-Host ""
    Write-Host "$($resetResult.ordersReset) sipariş sıfırlanacak." -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Devam etmek istiyor musunuz? (evet/hayır)"
    
    if ($confirmation -ne "evet") {
        Write-Host ""
        Write-Host "İşlem iptal edildi" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Execute actual reset
Write-Host "Sıfırlama işlemi başlatılıyor..." -ForegroundColor Yellow
Write-Host ""

$actualResetRequest = @{
    dryRun = $false
} | ConvertTo-Json

try {
    $actualResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/katanacleanup/reset-orders" `
        -Method Post `
        -Headers $headers `
        -Body $actualResetRequest `
        -ContentType "application/json"
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              SIFIRLAMA İŞLEMİ SONUÇLARI                    ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📊 SONUÇLAR:" -ForegroundColor Cyan
    Write-Host "  • Sıfırlanan Sipariş: $($actualResult.ordersReset)" -ForegroundColor Green
    Write-Host "  • Etkilenen Satır   : $($actualResult.linesAffected)" -ForegroundColor White
    Write-Host "  • Silinen Mapping   : $($actualResult.mappingsCleared)" -ForegroundColor White
    Write-Host "  • Süre              : $($actualResult.duration)" -ForegroundColor White
    Write-Host "  • Durum             : $(if ($actualResult.success) { '✓ Başarılı' } else { '✗ Hatalar var' })" -ForegroundColor $(if ($actualResult.success) { "Green" } else { "Red" })
    Write-Host ""
    
    if ($actualResult.errors -and $actualResult.errors.Count -gt 0) {
        Write-Host "⚠️  HATALAR:" -ForegroundColor Red
        foreach ($error in $actualResult.errors) {
            Write-Host "   • Sipariş $($error.orderId): $($error.message)" -ForegroundColor Red
            if ($error.details) {
                Write-Host "     Detay: $($error.details)" -ForegroundColor Gray
            }
        }
        Write-Host ""
    }
    
    Write-Host "✓ Siparişler sıfırlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 Sonraki adım:" -ForegroundColor Cyan
    Write-Host "   Siparişleri tekrar onaylamak için admin panelini kullanın" -ForegroundColor Gray
    Write-Host ""
    
    # Save results
    $actualResult | ConvertTo-Json -Depth 10 | Out-File "katana-cleanup-reset-result.json"
    Write-Host "📄 Detaylı rapor kaydedildi: katana-cleanup-reset-result.json" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "✗ Sıfırlama işlemi başarısız: $_" -ForegroundColor Red
    Write-Host "Hata detayları: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
