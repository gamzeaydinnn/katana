Write-Host "🚀 Katana UAT Test Başlatılıyor...`n" -ForegroundColor Cyan

Write-Host "🔐 Login yapılıyor..." -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "http://localhost:5055/api/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"Katana2025!"}'
$token = $response.token
Write-Host "✅ Token alındı`n" -ForegroundColor Green

Write-Host "🧪 UAT Test Paketi çalıştırılıyor...`n" -ForegroundColor Yellow
$headers = @{ "Authorization" = "Bearer $token" }
$result = Invoke-RestMethod -Uri "http://localhost:5055/api/IntegrationTest/uat-suite" -Method Post -Headers $headers

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✅ UAT TESTİ TAMAMLANDI" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

Write-Host "📊 ÖZET:" -ForegroundColor Cyan
Write-Host "  Toplam Test: $($result.totalTests)"
Write-Host "  ✅ Başarılı: $($result.passedTests)" -ForegroundColor Green
Write-Host "  ❌ Başarısız: $($result.failedTests)"
Write-Host "  Genel Durum: $(if($result.success){'✅ BAŞARILI'}else{'❌ BAŞARISIZ'})`n"

foreach ($test in $result.results) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "🔍 $($test.testName)" -ForegroundColor Cyan
    Write-Host "  Durum: $(if($test.success){'✅ BAŞARILI'}else{'❌ BAŞARISIZ'})"
    Write-Host "  Test Edilen: $($test.recordsTested)"
    Write-Host "  Geçen: $($test.recordsPassed)"
    Write-Host "  Kalan: $($test.recordsFailed)"
    Write-Host "  Ortam: $($test.environment)"
    Write-Host ""
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
$result | ConvertTo-Json -Depth 10 | Out-File "uat-test-result.json"
Write-Host "📄 Detaylı sonuç uat-test-result.json dosyasına kaydedildi" -ForegroundColor Yellow
