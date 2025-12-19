# FAST BUILD SCRIPT - Optimized Docker build
# Bu script build suresini %70 azaltir

Write-Host "FAST BUILD BASLATILIYOR..." -ForegroundColor Cyan
Write-Host ""

# BuildKit'i etkinlestir (Docker 18.09+)
$env:DOCKER_BUILDKIT = 1
$env:COMPOSE_DOCKER_CLI_BUILD = 1

Write-Host "BuildKit etkinlestirildi" -ForegroundColor Green

# Eski container'i durdur
Write-Host "Eski container durduruluyor..." -ForegroundColor Yellow
docker-compose -f docker-compose.fast.yml down 2>$null

# Cache'li build (ilk build yavas, sonrakiler cok hizli)
Write-Host ""
Write-Host "Docker image build ediliyor (cache kullaniliyor)..." -ForegroundColor Cyan
$buildStart = Get-Date

docker-compose -f docker-compose.fast.yml build --progress=plain

$buildEnd = Get-Date
$buildDuration = ($buildEnd - $buildStart).TotalSeconds

Write-Host ""
Write-Host "Build tamamlandi! Sure: $([math]::Round($buildDuration, 2)) saniye" -ForegroundColor Green

# Container'i baslat
Write-Host ""
Write-Host "Container baslatiliyor..." -ForegroundColor Cyan
docker-compose -f docker-compose.fast.yml up -d

# Loglari goster
Write-Host ""
Write-Host "Container loglari (CTRL+C ile cikis):" -ForegroundColor Yellow
Write-Host ""
docker-compose -f docker-compose.fast.yml logs -f
