# Master script for complete Katana cleanup
# Bu script tüm temizlik işlemlerini sırayla yapar
# 1. Analiz yapar
# 2. Katana'dan ürünleri siler
# 3. Siparişleri sıfırlar

param(
    [switch]$DryRun = $true,  # Varsayılan olarak DRY RUN modunda
    [switch]$Force = $false    # Onay istemeden çalıştır
)

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║          KATANA TAM TEMİZLİK - TÜM İŞLEMLER               ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 MOD: DRY RUN (Simülasyon)" -ForegroundColor Yellow
    Write-Host "   Hiçbir değişiklik yapılmayacak, sadece rapor gösterilecek" -ForegroundColor Gray
} else {
    Write-Host "⚠️  MOD: GERÇEK TEMİZLİK" -ForegroundColor Red
    Write-Host "   TÜM veriler KALICI olarak temizlenecek!" -ForegroundColor Red
}
Write-Host ""

Write-Host "Bu script şunları yapacak:" -ForegroundColor Cyan
Write-Host "  1️⃣  Mevcut durumu analiz et" -ForegroundColor White
Write-Host "  2️⃣  Katana'dan tüm ürünleri sil" -ForegroundColor White
Write-Host "  3️⃣  Tüm siparişleri sıfırla" -ForegroundColor White
Write-Host ""

# Login
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host "GİRİŞ" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

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

# STEP 1: Analyze
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host "1️⃣  ANALİZ" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/analyze" -Method Get -Headers $headers
    
    $allSkus = $analysis.orderProducts | Select-Object -ExpandProperty sku -Unique | Sort-Object
    
    Write-Host "✓ Analiz tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 MEVCUT DURUM:" -ForegroundColor Cyan
    Write-Host "  • Onaylı Sipariş      : $($analysis.totalApprovedOrders)" -ForegroundColor White
    Write-Host "  • Katana'daki Ürün    : $($analysis.totalProductsSentToKatana)" -ForegroundColor White
    Write-Host "  • Benzersiz SKU       : $($allSkus.Count)" -ForegroundColor White
    Write-Host "  • Tekrarlanan SKU     : $($analysis.skuDuplicates.Count)" -ForegroundColor $(if ($analysis.skuDuplicates.Count -gt 0) { "Red" } else { "Green" })
    Write-Host ""
    
    if ($allSkus.Count -eq 0) {
        Write-Host "ℹ️  Temizlenecek veri bulunamadı" -ForegroundColor Yellow
        exit 0
    }
    
    # Save analysis
    $analysis | ConvertTo-Json -Depth 10 | Out-File "katana-full-cleanup-analysis.json"
    
} catch {
    Write-Host "✗ Analiz başarısız: $_" -ForegroundColor Red
    exit 1
}

# Confirmation
if (-not $DryRun -and -not $Force) {
    Write-Host "⚠️  UYARI: Bu işlem geri alınamaz!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Silinecek:" -ForegroundColor Yellow
    Write-Host "  • $($allSkus.Count) adet ürün (Katana'dan)" -ForegroundColor White
    Write-Host "  • $($analysis.totalApprovedOrders) adet sipariş (sıfırlanacak)" -ForegroundColor White
    Write-Host ""
    $confirmation = Read-Host "Devam etmek istiyor musunuz? (evet/hayır)"
    
    if ($confirmation -ne "evet") {
        Write-Host ""
        Write-Host "İşlem iptal edildi" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# STEP 2: Delete from Katana
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host "2️⃣  KATANA'DAN SİLME" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

$deleteRequest = @{
    skus = $allSkus
    dryRun = $DryRun
} | ConvertTo-Json

try {
    Write-Host "Ürünler siliniyor..." -ForegroundColor Yellow
    $deleteResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/katanacleanup/delete-from-katana" `
        -Method Post `
        -Headers $headers `
        -Body $deleteRequest `
        -ContentType "application/json"
    
    Write-Host "✓ Silme işlemi tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 SİLME SONUÇLARI:" -ForegroundColor Cyan
    Write-Host "  • Toplam Deneme: $($deleteResult.totalAttempted)" -ForegroundColor White
    Write-Host "  • Başarılı     : $($deleteResult.successCount)" -ForegroundColor Green
    Write-Host "  • Başarısız    : $($deleteResult.failCount)" -ForegroundColor $(if ($deleteResult.failCount -gt 0) { "Red" } else { "Green" })
    Write-Host "  • Süre         : $($deleteResult.duration)" -ForegroundColor White
    Write-Host ""
    
    if ($deleteResult.errors -and $deleteResult.errors.Count -gt 0) {
        Write-Host "⚠️  Silme hataları:" -ForegroundColor Red
        $deleteResult.errors | Select-Object -First 5 | ForEach-Object {
            Write-Host "   • $($_.message)" -ForegroundColor Red
        }
        if ($deleteResult.errors.Count -gt 5) {
            Write-Host "   ... ve $($deleteResult.errors.Count - 5) hata daha" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Save delete results
    $deleteResult | ConvertTo-Json -Depth 10 | Out-File "katana-full-cleanup-delete.json"
    
} catch {
    Write-Host "✗ Silme işlemi başarısız: $_" -ForegroundColor Red
    Write-Host "Hata: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️  Silme başarısız oldu, sipariş sıfırlama atlanıyor" -ForegroundColor Yellow
    exit 1
}

# STEP 3: Reset Orders
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host "3️⃣  SİPARİŞ SIFIRLAMA" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

$resetRequest = @{
    dryRun = $DryRun
} | ConvertTo-Json

try {
    Write-Host "Siparişler sıfırlanıyor..." -ForegroundColor Yellow
    $resetResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/katanacleanup/reset-orders" `
        -Method Post `
        -Headers $headers `
        -Body $resetRequest `
        -ContentType "application/json"
    
    Write-Host "✓ Sıfırlama işlemi tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 SIFIRLAMA SONUÇLARI:" -ForegroundColor Cyan
    Write-Host "  • Sıfırlanan Sipariş: $($resetResult.ordersReset)" -ForegroundColor Green
    Write-Host "  • Etkilenen Satır   : $($resetResult.linesAffected)" -ForegroundColor White
    Write-Host "  • Silinen Mapping   : $($resetResult.mappingsCleared)" -ForegroundColor White
    Write-Host "  • Süre              : $($resetResult.duration)" -ForegroundColor White
    Write-Host ""
    
    if ($resetResult.errors -and $resetResult.errors.Count -gt 0) {
        Write-Host "⚠️  Sıfırlama hataları:" -ForegroundColor Red
        $resetResult.errors | Select-Object -First 5 | ForEach-Object {
            Write-Host "   • Sipariş $($_.orderId): $($_.message)" -ForegroundColor Red
        }
        if ($resetResult.errors.Count -gt 5) {
            Write-Host "   ... ve $($resetResult.errors.Count - 5) hata daha" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Save reset results
    $resetResult | ConvertTo-Json -Depth 10 | Out-File "katana-full-cleanup-reset.json"
    
} catch {
    Write-Host "✗ Sıfırlama işlemi başarısız: $_" -ForegroundColor Red
    Write-Host "Hata: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Final Summary
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              TEMİZLİK İŞLEMİ TAMAMLANDI                   ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "ℹ️  Bu bir DRY RUN idi - hiçbir değişiklik yapılmadı" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Gerçekten temizlemek için:" -ForegroundColor Cyan
    Write-Host "   .\test-katana-full-cleanup.ps1 -DryRun:`$false" -ForegroundColor Gray
} else {
    Write-Host "✓ Tüm işlemler başarıyla tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 ÖZET:" -ForegroundColor Cyan
    Write-Host "  • Silinen Ürün        : $($deleteResult.successCount)/$($deleteResult.totalAttempted)" -ForegroundColor White
    Write-Host "  • Sıfırlanan Sipariş  : $($resetResult.ordersReset)" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Sonraki adımlar:" -ForegroundColor Cyan
    Write-Host "   1. Siparişleri admin panelden tekrar onaylayın" -ForegroundColor Gray
    Write-Host "   2. Ürünler otomatik olarak Katana'ya gönderilecek" -ForegroundColor Gray
}
Write-Host ""

Write-Host "📄 Raporlar kaydedildi:" -ForegroundColor Gray
Write-Host "   • katana-full-cleanup-analysis.json" -ForegroundColor White
Write-Host "   • katana-full-cleanup-delete.json" -ForegroundColor White
Write-Host "   • katana-full-cleanup-reset.json" -ForegroundColor White
Write-Host ""
