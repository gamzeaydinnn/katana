# URGENT: Backend Restart Required!
# Changes were made but backend wasn't restarted

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║              ⚠️  URGENT BACKEND RESTART  ⚠️               ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

Write-Host "🔴 PROBLEM:" -ForegroundColor Red
Write-Host "   Backend is still using OLD code!" -ForegroundColor Red
Write-Host "   JSON request is MISSING required fields:" -ForegroundColor Red
Write-Host "   - kategoriAgacKod" -ForegroundColor Red
Write-Host "   - alisTevkifatOran" -ForegroundColor Red
Write-Host "   - satisTevkifatOran" -ForegroundColor Red
Write-Host "   - alisTevkifatKod" -ForegroundColor Red
Write-Host "   - satisTevkifatKod" -ForegroundColor Red
Write-Host ""

Write-Host "✅ SOLUTION:" -ForegroundColor Green
Write-Host "   Restarting backend NOW to apply all changes..." -ForegroundColor Green
Write-Host ""

Write-Host "🔄 Stopping backend..." -ForegroundColor Yellow
docker-compose stop backend

Write-Host ""
Write-Host "🔄 Starting backend..." -ForegroundColor Yellow
docker-compose start backend

Write-Host ""
Write-Host "⏳ Waiting for backend to fully start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "✅ Backend restarted!" -ForegroundColor Green
Write-Host ""

Write-Host "📊 NOW CHECK LOGS:" -ForegroundColor Cyan
Write-Host "   docker-compose logs -f backend | Select-String 'LUCA JSON REQUEST'" -ForegroundColor Gray
Write-Host ""
Write-Host "🎯 LOOK FOR:" -ForegroundColor Cyan
Write-Host '   "kategoriAgacKod":null' -ForegroundColor Green
Write-Host '   "alisTevkifatOran":"0"' -ForegroundColor Green
Write-Host '   "satisTevkifatOran":"0"' -ForegroundColor Green
Write-Host '   "alisTevkifatKod":0' -ForegroundColor Green
Write-Host '   "satisTevkifatKod":0' -ForegroundColor Green
Write-Host ""
