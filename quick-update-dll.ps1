# Hizli DLL guncelleme
Write-Host "Hizli guncelleme basliyor..." -ForegroundColor Cyan

# Build sadece Infrastructure projesi
Write-Host "Infrastructure build ediliyor..." -ForegroundColor Yellow
dotnet build src/Katana.Infrastructure/Katana.Infrastructure.csproj -c Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build basarisiz!" -ForegroundColor Red
    exit 1
}

# DLL'i container'a kopyala
Write-Host "DLL container'a kopyalaniyor..." -ForegroundColor Yellow
docker cp src/Katana.Infrastructure/bin/Release/net8.0/Katana.Infrastructure.dll katana-api-1:/app/Katana.Infrastructure.dll

# Container'i restart et
Write-Host "Container restart ediliyor..." -ForegroundColor Yellow
docker restart katana-api-1

Write-Host "Guncelleme tamamlandi!" -ForegroundColor Green
Write-Host "Container baslamasi icin 5 saniye bekleniyor..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

Write-Host "Container durumu:" -ForegroundColor Cyan
docker ps --filter 'name=katana-api-1'
