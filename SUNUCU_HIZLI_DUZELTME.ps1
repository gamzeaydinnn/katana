# Sunucu Admin Onay Sorunu - Hızlı Düzeltme Scripti
# Bu script sunucudaki sorunu hızlıca düzeltir

Write-Host "🔧 Sunucu Admin Onay Sorunu - Hızlı Düzeltme" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. Mevcut durumu kontrol et
Write-Host "`n📋 1. Mevcut konfigürasyon kontrol ediliyor..." -ForegroundColor Yellow

$appsettingsPath = "publish_test/appsettings.json"

if (Test-Path $appsettingsPath) {
    $content = Get-Content $appsettingsPath -Raw
    
    if ($content -match '"ManualSessionCookie":\s*"JSESSIONID=FILL_ME"') {
        Write-Host "   ❌ SORUN BULUNDU: ManualSessionCookie = 'JSESSIONID=FILL_ME'" -ForegroundColor Red
        Write-Host "   Bu geçersiz bir cookie değeri ve authentication'ı engelliyor" -ForegroundColor Red
    }
    elseif ($content -match '"ManualSessionCookie":\s*""') {
        Write-Host "   ✅ Konfigürasyon zaten düzeltilmiş" -ForegroundColor Green
        Write-Host "   ManualSessionCookie boş - otomatik login kullanılıyor" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️  ManualSessionCookie farklı bir değere sahip" -ForegroundColor Yellow
        Write-Host "   Mevcut değer kontrol edilmeli" -ForegroundColor Yellow
    }
}
else {
    Write-Host "   ❌ appsettings.json dosyası bulunamadı: $appsettingsPath" -ForegroundColor Red
    exit 1
}

# 2. Yedek al
Write-Host "`n💾 2. Yedek alınıyor..." -ForegroundColor Yellow

$backupPath = "publish_test/appsettings.json.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $appsettingsPath $backupPath
Write-Host "   ✅ Yedek oluşturuldu: $backupPath" -ForegroundColor Green

# 3. Düzeltmeyi uygula
Write-Host "`n🔧 3. Düzeltme uygulanıyor..." -ForegroundColor Yellow

$content = Get-Content $appsettingsPath -Raw
$originalContent = $content

# ManualSessionCookie'yi temizle
$content = $content -replace '"ManualSessionCookie":\s*"JSESSIONID=FILL_ME"', '"ManualSessionCookie": ""'
$content = $content -replace '"ManualSessionCookie":\s*"[^"]*FILL_ME[^"]*"', '"ManualSessionCookie": ""'

if ($content -ne $originalContent) {
    Set-Content -Path $appsettingsPath -Value $content -NoNewline
    Write-Host "   ✅ ManualSessionCookie temizlendi" -ForegroundColor Green
    Write-Host "   Artık otomatik login kullanılacak" -ForegroundColor Green
}
else {
    Write-Host "   ℹ️  Değişiklik gerekmedi" -ForegroundColor Cyan
}

# 4. Değişiklikleri doğrula
Write-Host "`n✅ 4. Değişiklikler doğrulanıyor..." -ForegroundColor Yellow

$newContent = Get-Content $appsettingsPath -Raw

if ($newContent -match '"ManualSessionCookie":\s*""') {
    Write-Host "   ✅ Doğrulama başarılı!" -ForegroundColor Green
    Write-Host "   ManualSessionCookie artık boş" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Doğrulama başarısız!" -ForegroundColor Red
    Write-Host "   Yedekten geri yükleniyor..." -ForegroundColor Yellow
    Copy-Item $backupPath $appsettingsPath -Force
    Write-Host "   ⚠️  Geri yükleme tamamlandı. Manuel kontrol gerekli." -ForegroundColor Yellow
    exit 1
}

# 5. Sonraki adımlar
Write-Host "`n📝 5. Sonraki Adımlar:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   1️⃣  Uygulamayı yeniden başlat:" -ForegroundColor White
Write-Host "      docker-compose restart katana-api" -ForegroundColor Gray
Write-Host "      # veya" -ForegroundColor Gray
Write-Host "      systemctl restart katana-api" -ForegroundColor Gray
Write-Host ""
Write-Host "   2️⃣  Logları kontrol et:" -ForegroundColor White
Write-Host "      docker-compose logs -f katana-api | Select-String 'Authentication'" -ForegroundColor Gray
Write-Host ""
Write-Host "   3️⃣  Admin panelinde test et:" -ForegroundColor White
Write-Host "      - Bir satış siparişini onayla" -ForegroundColor Gray
Write-Host "      - Kozaya senkronize et" -ForegroundColor Gray
Write-Host ""
Write-Host "   4️⃣  Başarılı authentication logları:" -ForegroundColor White
Write-Host "      '✅ Koza Authentication Complete'" -ForegroundColor Gray
Write-Host "      'IsAuthenticated=True'" -ForegroundColor Gray
Write-Host ""

Write-Host "`n🎉 Düzeltme tamamlandı!" -ForegroundColor Green
Write-Host "=" * 60
Write-Host ""
Write-Host "⚠️  NOT: Değişikliklerin etkili olması için uygulamayı yeniden başlatmanız gerekiyor" -ForegroundColor Yellow
Write-Host ""

# Opsiyonel: Docker restart
$restart = Read-Host "`nUygulamayı şimdi yeniden başlatmak ister misiniz? (E/H)"

if ($restart -eq "E" -or $restart -eq "e") {
    Write-Host "`n🔄 Uygulama yeniden başlatılıyor..." -ForegroundColor Yellow
    
    # Docker Compose kontrolü
    if (Test-Path "docker-compose.yml") {
        try {
            docker-compose restart katana-api
            Write-Host "   ✅ Docker container yeniden başlatıldı" -ForegroundColor Green
            
            Write-Host "`n⏳ 10 saniye bekleniyor..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
            
            Write-Host "`n📊 Container durumu:" -ForegroundColor Cyan
            docker-compose ps katana-api
            
            Write-Host "`n📋 Son loglar:" -ForegroundColor Cyan
            docker-compose logs --tail=50 katana-api | Select-String -Pattern "Authentication|Session|Login" | Select-Object -Last 10
        }
        catch {
            Write-Host "   ❌ Docker restart hatası: $_" -ForegroundColor Red
            Write-Host "   Manuel olarak yeniden başlatın" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   ⚠️  docker-compose.yml bulunamadı" -ForegroundColor Yellow
        Write-Host "   Manuel olarak yeniden başlatın" -ForegroundColor Yellow
    }
}
else {
    Write-Host "`nℹ️  Uygulamayı manuel olarak yeniden başlatmayı unutmayın!" -ForegroundColor Cyan
}

Write-Host "`n✨ Script tamamlandı!" -ForegroundColor Green
