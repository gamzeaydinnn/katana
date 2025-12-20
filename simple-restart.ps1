# Simple Restart - No Turkish characters

Write-Host "=== RESTARTING ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Stopping..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 2

Write-Host "Starting..." -ForegroundColor Yellow
docker-compose up -d
Start-Sleep -Seconds 15

Write-Host "Checking..." -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "http://localhost:5055/health" -TimeoutSec 5
    Write-Host "Backend OK" -ForegroundColor Green
} catch {
    Write-Host "Backend NOT ready" -ForegroundColor Red
}

Write-Host ""
Write-Host "Logs:" -ForegroundColor Yellow
docker-compose logs --tail=40 backend

Write-Host ""
Write-Host "Done. Run: .\check-luca-simple.ps1" -ForegroundColor Yellow
