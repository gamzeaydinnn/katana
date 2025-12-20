#!/usr/bin/env pwsh
# Complete rebuild - This WILL work

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPLETE DOCKER REBUILD" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nStep 1: Stopping all containers..." -ForegroundColor Yellow
docker-compose down
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: docker-compose down failed, continuing..." -ForegroundColor Yellow
}

Write-Host "`nStep 2: Removing old images..." -ForegroundColor Yellow
docker rmi katana-api -f
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not remove old image, continuing..." -ForegroundColor Yellow
}

Write-Host "`nStep 3: Cleaning Docker build cache..." -ForegroundColor Yellow
docker builder prune -af
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not prune builder cache, continuing..." -ForegroundColor Yellow
}

Write-Host "`nStep 4: Building API (this will take 5-10 minutes)..." -ForegroundColor Yellow
Write-Host "Building with --no-cache to ensure fresh compilation..." -ForegroundColor Cyan
docker-compose build --no-cache --progress=plain api 2>&1 | Tee-Object -FilePath "build-log.txt"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nStep 5: Starting containers..." -ForegroundColor Green
    docker-compose up -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nStep 6: Waiting for API to start..." -ForegroundColor Cyan
        Start-Sleep -Seconds 15
        
        Write-Host "`nStep 7: Checking logs..." -ForegroundColor Yellow
        docker logs katana-api-1 --tail 50
        
        Write-Host "`n========================================" -ForegroundColor Green
        Write-Host "  REBUILD COMPLETE!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        
        Write-Host "`nNext steps:" -ForegroundColor Cyan
        Write-Host "1. Trigger a sync to test" -ForegroundColor White
        Write-Host "2. Check logs for 'LUCA JSON REQUEST' to see the new format" -ForegroundColor White
        Write-Host "3. Verify Luca returns success messages" -ForegroundColor White
        
        Write-Host "`nBuild log saved to: build-log.txt" -ForegroundColor Gray
    } else {
        Write-Host "`nERROR: Failed to start containers!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`nERROR: Build failed!" -ForegroundColor Red
    Write-Host "Check build-log.txt for details" -ForegroundColor Yellow
    exit 1
}
