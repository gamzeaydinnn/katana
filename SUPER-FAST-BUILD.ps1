# ⚡⚡⚡ SUPER FAST BUILD - Docker olmadan direkt çalıştır
# Bu yöntem Docker build'den 10x daha hızlı!

Write-Host "⚡⚡⚡ SUPER FAST BUILD (Docker'sız)" -ForegroundColor Cyan
Write-Host ""

# Eski process'leri temizle
Write-Host "🧹 Eski process'ler temizleniyor..." -ForegroundColor Yellow
Get-Process -Name "Katana.API" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Build (sadece değişen dosyalar compile edilir)
Write-Host ""
Write-Host "🔨 Build başlatılıyor..." -ForegroundColor Cyan
$buildStart = Get-Date

dotnet build src/Katana.API/Katana.API.csproj -c Release --no-incremental

$buildEnd = Get-Date
$buildDuration = ($buildEnd - $buildStart).TotalSeconds

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Build BAŞARISIZ!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Build tamamlandı! Süre: $([math]::Round($buildDuration, 2)) saniye" -ForegroundColor Green

# Çalıştır
Write-Host ""
Write-Host "🚀 Uygulama başlatılıyor..." -ForegroundColor Cyan
Write-Host ""
Write-Host "📍 URL: http://localhost:5055" -ForegroundColor Yellow
Write-Host "📍 Swagger: http://localhost:5055/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  CTRL+C ile durdurun" -ForegroundColor Yellow
Write-Host ""

# Environment variables
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5055"

# Çalıştır
dotnet run --project src/Katana.API/Katana.API.csproj --no-build -c Release
