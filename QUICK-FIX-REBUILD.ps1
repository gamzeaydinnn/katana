#!/usr/bin/env pwsh
Write-Host "Quick rebuild after fixing hardcoded values..." -ForegroundColor Cyan

docker-compose down
docker-compose build api
docker-compose up -d

Write-Host "Waiting for API..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "`nChecking logs..." -ForegroundColor Cyan
docker logs katana-api-1 --tail 30

Write-Host "`nDone! Now test sync." -ForegroundColor Green
