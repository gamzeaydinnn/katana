# Stok Hareketleri Hata Düzeltme Script'i
# Bu script hatalı stok hareketlerini tespit edip düzeltir

$baseUrl = "http://localhost:8080"
$username = "admin"
$password = "Katana2025!"

Write-Host "🔐 Giriş yapılıyor..." -ForegroundColor Cyan

# Login
$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.token
    Write-Host "✅ Giriş başarılı" -ForegroundColor Green
}
catch {
    Write-Host "❌ Giriş başarısız: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Hatalı hareketleri listele
Write-Host "`n📊 Hatalı stok hareketleri kontrol ediliyor..." -ForegroundColor Cyan

try {
    $errorMovements = Invoke-RestMethod -Uri "$baseUrl/api/StockMovementSync/movements?syncStatus=ERROR" `
        -Method Get `
        -Headers $headers
    
    $totalErrors = $errorMovements.Count
    Write-Host "📋 Toplam $totalErrors hatalı kayıt bulundu" -ForegroundColor Yellow
    
    if ($totalErrors -eq 0) {
        Write-Host "✅ Hatalı kayıt yok!" -ForegroundColor Green
        exit 0
    }
    
    # Hata tiplerini kategorize et
    $transferErrors = $errorMovements | Where-Object { $_.movementType -eq "TRANSFER" }
    $adjustmentErrors = $errorMovements | Where-Object { $_.movementType -eq "ADJUSTMENT" }
    
    Write-Host "`n📊 Hata Dağılımı:" -ForegroundColor Cyan
    Write-Host "  - Transfer Hataları: $($transferErrors.Count)" -ForegroundColor Yellow
    Write-Host "  - Düzeltme Hataları: $($adjustmentErrors.Count)" -ForegroundColor Yellow
    
    # İlk 10 hatayı göster
    Write-Host "`n📝 İlk 10 Hata:" -ForegroundColor Cyan
    $errorMovements | Select-Object -First 10 | ForEach-Object {
        $errorMsg = if ($_.errorMessage) { $_.errorMessage } else { "Hata mesajı yok" }
        Write-Host "  [$($_.documentNo)] $($_.movementType) - $errorMsg" -ForegroundColor Red
    }
    
    # Kullanıcıya sor
    Write-Host "`n❓ Hatalı kayıtları düzeltmek ister misiniz?" -ForegroundColor Yellow
    Write-Host "   1) Tüm hataları yeniden dene (Retry All)" -ForegroundColor White
    Write-Host "   2) Sadece Transfer hatalarını yeniden dene" -ForegroundColor White
    Write-Host "   3) Sadece Düzeltme hatalarını yeniden dene" -ForegroundColor White
    Write-Host "   4) Hata durumunu temizle (Pending'e al)" -ForegroundColor White
    Write-Host "   5) İptal" -ForegroundColor White
    
    $choice = Read-Host "`nSeçiminiz (1-5)"
    
    switch ($choice) {
        "1" {
            Write-Host "`n🔄 Tüm hatalı kayıtlar yeniden deneniyor..." -ForegroundColor Cyan
            
            $successCount = 0
            $failCount = 0
            
            foreach ($movement in $errorMovements) {
                try {
                    Write-Host "  🔄 $($movement.documentNo) işleniyor..." -ForegroundColor Gray
                    
                    $syncUrl = "$baseUrl/api/StockMovementSync/sync-movement/$($movement.movementType)/$($movement.id)"
                    $result = Invoke-RestMethod -Uri $syncUrl `
                        -Method Post `
                        -Headers $headers
                    
                    if ($result.success) {
                        Write-Host "    ✅ Başarılı" -ForegroundColor Green
                        $successCount++
                    }
                    else {
                        Write-Host "    ❌ Başarısız: $($result.message)" -ForegroundColor Red
                        $failCount++
                    }
                }
                catch {
                    Write-Host "    ❌ Hata: $_" -ForegroundColor Red
                    $failCount++
                }
                
                Start-Sleep -Milliseconds 500
            }
            
            Write-Host "`n📊 Sonuç:" -ForegroundColor Cyan
            Write-Host "  ✅ Başarılı: $successCount" -ForegroundColor Green
            Write-Host "  ❌ Başarısız: $failCount" -ForegroundColor Red
        }
        
        "2" {
            Write-Host "`n🔄 Transfer hataları yeniden deneniyor..." -ForegroundColor Cyan
            
            $successCount = 0
            $failCount = 0
            
            foreach ($movement in $transferErrors) {
                try {
                    Write-Host "  🔄 $($movement.documentNo) işleniyor..." -ForegroundColor Gray
                    
                    $syncUrl = "$baseUrl/api/StockMovementSync/sync/transfer/$($movement.id)"
                    $result = Invoke-RestMethod -Uri $syncUrl `
                        -Method Post `
                        -Headers $headers
                    
                    if ($result.success) {
                        Write-Host "    ✅ Başarılı" -ForegroundColor Green
                        $successCount++
                    }
                    else {
                        Write-Host "    ❌ Başarısız: $($result.errorMessage)" -ForegroundColor Red
                        $failCount++
                    }
                }
                catch {
                    Write-Host "    ❌ Hata: $_" -ForegroundColor Red
                    $failCount++
                }
                
                Start-Sleep -Milliseconds 500
            }
            
            Write-Host "`n📊 Sonuç:" -ForegroundColor Cyan
            Write-Host "  ✅ Başarılı: $successCount" -ForegroundColor Green
            Write-Host "  ❌ Başarısız: $failCount" -ForegroundColor Red
        }
        
        "3" {
            Write-Host "`n🔄 Düzeltme hataları yeniden deneniyor..." -ForegroundColor Cyan
            
            $successCount = 0
            $failCount = 0
            
            foreach ($movement in $adjustmentErrors) {
                try {
                    Write-Host "  🔄 $($movement.documentNo) işleniyor..." -ForegroundColor Gray
                    
                    $syncUrl = "$baseUrl/api/StockMovementSync/sync/adjustment/$($movement.id)"
                    $result = Invoke-RestMethod -Uri $syncUrl `
                        -Method Post `
                        -Headers $headers
                    
                    if ($result.success) {
                        Write-Host "    ✅ Başarılı" -ForegroundColor Green
                        $successCount++
                    }
                    else {
                        Write-Host "    ❌ Başarısız: $($result.errorMessage)" -ForegroundColor Red
                        $failCount++
                    }
                }
                catch {
                    Write-Host "    ❌ Hata: $_" -ForegroundColor Red
                    $failCount++
                }
                
                Start-Sleep -Milliseconds 500
            }
            
            Write-Host "`n📊 Sonuç:" -ForegroundColor Cyan
            Write-Host "  ✅ Başarılı: $successCount" -ForegroundColor Green
            Write-Host "  ❌ Başarısız: $failCount" -ForegroundColor Red
        }
        
        "4" {
            Write-Host "`n⚠️  Bu özellik henüz implement edilmedi" -ForegroundColor Yellow
            Write-Host "Hata durumunu temizlemek için veritabanında manuel güncelleme gerekiyor" -ForegroundColor Yellow
        }
        
        "5" {
            Write-Host "`n❌ İptal edildi" -ForegroundColor Yellow
            exit 0
        }
        
        default {
            Write-Host "`n❌ Geçersiz seçim" -ForegroundColor Red
            exit 1
        }
    }
}
catch {
    Write-Host "❌ Hata: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ İşlem tamamlandı" -ForegroundColor Green
