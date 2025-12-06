# Fix Database Connection Issue
# SQL Server container is not running

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║          🔥 DATABASE CONNECTION ERROR! 🔥                 ║" -ForegroundColor Red
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""

Write-Host "🔴 PROBLEM:" -ForegroundColor Red
Write-Host "   SQL Server container is NOT running!" -ForegroundColor Red
Write-Host "   Error: Hedef makine etkin olarak reddettiğinden bağlantı kurulamadı" -ForegroundColor Red
Write-Host ""

Write-Host "✅ SOLUTION:" -ForegroundColor Green
Write-Host "   Starting ALL containers properly..." -ForegroundColor Green
Write-Host ""

Write-Host "📋 Checking current containers..." -ForegroundColor Yellow
docker-compose ps

Write-Host ""
Write-Host "🛑 Stopping all containers..." -ForegroundColor Yellow
docker-compose down

Write-Host ""
Write-Host "🚀 Starting all containers..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

Write-Host ""
Write-Host "📊 Checking container status..." -ForegroundColor Cyan
docker-compose ps

Write-Host ""
Write-Host "✅ All containers should be running now!" -ForegroundColor Green
Write-Host ""

Write-Host "🔍 Check backend logs:" -ForegroundColor Cyan
Write-Host "   docker-compose logs -f backend" -ForegroundColor Gray
Write-Host ""
