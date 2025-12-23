# Test script for Katana cleanup analysis endpoint
# Bu script Katana'ya gönderilmiş sipariş ürünlerini analiz eder

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host "=== Katana Temizlik Analizi ===" -ForegroundColor Cyan
Write-Host "Bu analiz Katana'ya gönderilmiş tüm sipariş ürünlerini gösterir" -ForegroundColor Gray
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
    Write-Host "Giriş başarılı" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Giriş başarısız: $_" -ForegroundColor Red
    exit 1
}

# Analyze
Write-Host "Sipariş ürünleri analiz ediliyor..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $token"
    "X-API-Key" = $apiKey
}

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/analyze" -Method Get -Headers $headers
    
    Write-Host ""
    Write-Host "KATANA ÜRÜN ANALİZ RAPORU" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "GENEL İSTATİSTİKLER" -ForegroundColor Yellow
    Write-Host "Onaylı Sipariş Sayısı      : $($result.totalApprovedOrders)" -ForegroundColor White
    Write-Host "Katana'ya Gönderilen Ürün  : $($result.totalProductsSentToKatana)" -ForegroundColor White
    Write-Host "Benzersiz SKU Sayısı        : $($result.uniqueSkuCount)" -ForegroundColor White
    Write-Host "Tekrarlanan SKU Sayısı      : $($result.skuDuplicates.Count)" -ForegroundColor White
    Write-Host ""
    
    if ($result.skuDuplicates.Count -gt 0) {
        Write-Host "TEKRARLANAN SKU'LAR" -ForegroundColor Yellow
        $sortedDuplicates = $result.skuDuplicates.PSObject.Properties | Sort-Object -Property Value -Descending
        foreach ($dup in $sortedDuplicates) {
            Write-Host "  $($dup.Name) -> $($dup.Value) kez" -ForegroundColor White
        }
        Write-Host ""
    }
    
    if ($result.orderProducts.Count -gt 0) {
        Write-Host "SİPARİŞ ÜRÜNLERİ (İlk 20)" -ForegroundColor Yellow
        $displayCount = [Math]::Min(20, $result.orderProducts.Count)
        
        $result.orderProducts | Select-Object -First $displayCount | ForEach-Object {
            Write-Host "  Sipariş: $($_.orderNo) | SKU: $($_.sku) | Ürün: $($_.productName)" -ForegroundColor White
        }
        
        if ($result.orderProducts.Count -gt 20) {
            Write-Host "  ... ve $($result.orderProducts.Count - 20) ürün daha" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Tüm SKU'ları listele
    $allSkus = $result.orderProducts | Select-Object -ExpandProperty sku -Unique | Sort-Object
    Write-Host "TÜM BENZERSIZ SKU'LAR ($($allSkus.Count) adet)" -ForegroundColor Yellow
    $allSkus | ForEach-Object {
        Write-Host "  $_" -ForegroundColor White
    }
    Write-Host ""
    
    Write-Host "Analiz tamamlandı" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sonraki Adımlar:" -ForegroundColor Cyan
    Write-Host "  1. Katana'dan silmek için: .\test-katana-cleanup-delete-all.ps1" -ForegroundColor Gray
    Write-Host "  2. Siparişleri sıfırlamak için: .\test-katana-cleanup-reset.ps1" -ForegroundColor Gray
    Write-Host ""
    
    # Save results
    $result | ConvertTo-Json -Depth 10 | Out-File "katana-cleanup-analysis-result.json"
    Write-Host "Detaylı rapor kaydedildi: katana-cleanup-analysis-result.json" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "Analiz başarısız: $_" -ForegroundColor Red
    Write-Host "Hata: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
