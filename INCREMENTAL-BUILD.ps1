# INCREMENTAL BUILD - Sadece degisen dosyalari compile eder
# En hizli gelistirme yontemi (Hot Reload destekli)

Write-Host "INCREMENTAL BUILD (Hot Reload)" -ForegroundColor Cyan
Write-Host ""

# Eski process'leri temizle
Write-Host "Eski process'ler temizleniyor..." -ForegroundColor Yellow
Get-Process -Name "Katana.API" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host ""
Write-Host "Hot Reload aktif - Kod degisikliklerinde otomatik yeniden yuklenir" -ForegroundColor Green
Write-Host ""
Write-Host "URL: http://localhost:5055" -ForegroundColor Yellow
Write-Host "Swagger: http://localhost:5055/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "CTRL+C ile durdurun" -ForegroundColor Yellow
Write-Host ""

# Environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5055"

# Watch mode ile calistir (Hot Reload)
Set-Location src/Katana.API
dotnet watch run --no-hot-reload
