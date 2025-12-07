# REBUILD DOCKER CONTAINER
# Restart is not enough - need to rebuild!

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║          🔥 DOCKER REBUILD REQUIRED! 🔥                   ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

Write-Host "🔴 PROBLEM:" -ForegroundColor Red
Write-Host "   Docker container is running OLD CODE!" -ForegroundColor Red
Write-Host "   Restart is NOT enough - need to REBUILD!" -ForegroundColor Red
Write-Host ""

Write-Host "✅ SOLUTION:" -ForegroundColor Green
Write-Host "   Rebuilding Docker container with new code..." -ForegroundColor Green
Write-Host ""

Write-Host "🛑 Stopping containers..." -ForegroundColor Yellow
docker-compose down

Write-Host ""
Write-Host "🔨 Rebuilding backend..." -ForegroundColor Yellow
docker-compose build backend

Write-Host ""
Write-Host "🚀 Starting all containers..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "⏳ Waiting for backend to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host ""
Write-Host "✅ Docker containers rebuilt and started!" -ForegroundColor Green
Write-Host ""

Write-Host "📊 CHECK LOGS NOW:" -ForegroundColor Cyan
Write-Host "   docker-compose logs -f backend | Select-String 'LUCA JSON REQUEST'" -ForegroundColor Gray
Write-Host ""
Write-Host "🎯 YOU SHOULD NOW SEE:" -ForegroundColor Cyan
Write-Host '   "kategoriAgacKod":null' -ForegroundColor Green
Write-Host '   "alisTevkifatOran":"0"' -ForegroundColor Green
Write-Host '   "satisTevkifatOran":"0"' -ForegroundColor Green
Write-Host '   "alisTevkifatKod":0' -ForegroundColor Green
Write-Host '   "satisTevkifatKod":0' -ForegroundColor Green
Write-Host ""
