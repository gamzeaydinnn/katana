# Script to delete ALL products from Katana that were sent from orders
# Bu script Katana'daki TÜM sipariş ürünlerini siler
# UYARI: Bu işlem geri alınamaz!

param(
    [switch]$DryRun = $true,  # Varsayılan olarak DRY RUN modunda
    [switch]$Force = $false    # Onay istemeden çalıştır
)

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║        KATANA ÜRÜN TEMİZLEME - TÜM ÜRÜNLER                ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 MOD: DRY RUN (Simülasyon)" -ForegroundColor Yellow
    Write-Host "   Hiçbir ürün silinmeyecek, sadece rapor gösterilecek" -ForegroundColor Gray
} else {
    Write-Host "⚠️  MOD: GERÇEK SİLME" -ForegroundColor Red
    Write-Host "   Ürünler Katana'dan KALICI olarak silinecek!" -ForegroundColor Red
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

# First, analyze to get all SKUs
Write-Host "Ürünler analiz ediliyor..." -ForegroundColor Yellow
try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/analyze" -Method Get -Headers $headers
    
    $allSkus = $analysis.orderProducts | Select-Object -ExpandProperty sku -Unique | Sort-Object
    
    Write-Host "✓ Analiz tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 ÖZET:" -ForegroundColor Cyan
    Write-Host "  • Toplam Sipariş: $($analysis.totalApprovedOrders)" -ForegroundColor White
    Write-Host "  • Toplam Ürün  : $($analysis.totalProductsSentToKatana)" -ForegroundColor White
    Write-Host "  • Benzersiz SKU: $($allSkus.Count)" -ForegroundColor White
    Write-Host ""
    
    if ($allSkus.Count -eq 0) {
        Write-Host "ℹ️  Silinecek ürün bulunamadı" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "📋 SİLİNECEK SKU'LAR:" -ForegroundColor Yellow
    $allSkus | ForEach-Object {
        Write-Host "   • $_" -ForegroundColor White
    }
    Write-Host ""
    
} catch {
    Write-Host "✗ Analiz başarısız: $_" -ForegroundColor Red
    exit 1
}

# Confirmation
if (-not $DryRun -and -not $Force) {
    Write-Host "⚠️  UYARI: Bu işlem geri alınamaz!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Katana'dan $($allSkus.Count) adet ürün silinecek." -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Devam etmek istiyor musunuz? (evet/hayır)"
    
    if ($confirmation -ne "evet") {
        Write-Host ""
        Write-Host "İşlem iptal edildi" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Delete products
Write-Host "Silme işlemi başlatılıyor..." -ForegroundColor Yellow
Write-Host ""

$deleteRequest = @{
    skus = $allSkus
    dryRun = $DryRun
} | ConvertTo-Json

try {
    $deleteResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/katanacleanup/delete-from-katana" `
        -Method Post `
        -Headers $headers `
        -Body $deleteRequest `
        -ContentType "application/json"
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              SİLME İŞLEMİ SONUÇLARI                        ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📊 SONUÇLAR:" -ForegroundColor Cyan
    Write-Host "  • Toplam Deneme  : $($deleteResult.totalAttempted)" -ForegroundColor White
    Write-Host "  • Başarılı       : $($deleteResult.successCount)" -ForegroundColor Green
    Write-Host "  • Başarısız      : $($deleteResult.failCount)" -ForegroundColor $(if ($deleteResult.failCount -gt 0) { "Red" } else { "Green" })
    Write-Host "  • Süre           : $($deleteResult.duration)" -ForegroundColor White
    Write-Host "  • Durum          : $(if ($deleteResult.success) { '✓ Başarılı' } else { '✗ Hatalar var' })" -ForegroundColor $(if ($deleteResult.success) { "Green" } else { "Red" })
    Write-Host ""
    
    if ($deleteResult.errors -and $deleteResult.errors.Count -gt 0) {
        Write-Host "⚠️  HATALAR:" -ForegroundColor Red
        foreach ($error in $deleteResult.errors) {
            Write-Host "   • $($error.message)" -ForegroundColor Red
            if ($error.details) {
                Write-Host "     Detay: $($error.details)" -ForegroundColor Gray
            }
        }
        Write-Host ""
    }
    
    if ($DryRun) {
        Write-Host "ℹ️  Bu bir DRY RUN idi - hiçbir ürün silinmedi" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "💡 Gerçekten silmek için:" -ForegroundColor Cyan
        Write-Host "   .\test-katana-cleanup-delete-all.ps1 -DryRun:`$false" -ForegroundColor Gray
    } else {
        Write-Host "✓ Ürünler Katana'dan silindi" -ForegroundColor Green
        Write-Host ""
        Write-Host "💡 Sonraki adım:" -ForegroundColor Cyan
        Write-Host "   Siparişleri sıfırlamak için: .\test-katana-cleanup-reset.ps1" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Save results
    $deleteResult | ConvertTo-Json -Depth 10 | Out-File "katana-cleanup-delete-result.json"
    Write-Host "📄 Detaylı rapor kaydedildi: katana-cleanup-delete-result.json" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "✗ Silme işlemi başarısız: $_" -ForegroundColor Red
    Write-Host "Hata detayları: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
