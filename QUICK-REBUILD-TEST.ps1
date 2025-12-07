#!/usr/bin/env pwsh
# Quick rebuild and test

Write-Host "Stopping containers..." -ForegroundColor Yellow
docker-compose down

Write-Host "Building API..." -ForegroundColor Yellow
docker-compose build api

if ($LASTEXITCODE -eq 0) {
    Write-Host "Starting containers..." -ForegroundColor Green
    docker-compose up -d
    
    Write-Host "Waiting for API to start..." -ForegroundColor Cyan
    Start-Sleep -Seconds 10
    
    Write-Host "Checking logs for DEBUG output..." -ForegroundColor Yellow
    docker logs katana-api-1 --tail 50
    
    Write-Host "`nDone! Check logs above for DEBUG DTO VALUES" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
