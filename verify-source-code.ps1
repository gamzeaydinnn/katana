#!/usr/bin/env pwsh
# Verify that source code has the correct changes

Write-Host "=== Verifying Source Code Changes ===" -ForegroundColor Cyan

Write-Host "`n1. Checking DTO definition for MaliyetHesaplanacakFlag..." -ForegroundColor Yellow
$dtoCheck = Select-String -Path "src/Katana.Core/DTOs/LucaDtos.cs" -Pattern "public int MaliyetHesaplanacakFlag" -Context 0,0
if ($dtoCheck) {
    Write-Host "   OK: MaliyetHesaplanacakFlag is defined as int" -ForegroundColor Green
    $dtoCheck | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Gray }
} else {
    Write-Host "   ERROR: MaliyetHesaplanacakFlag is NOT int!" -ForegroundColor Red
}

Write-Host "`n2. Checking mapper for tevkifat fields..." -ForegroundColor Yellow
$mapperCheck = Select-String -Path "src/Katana.Business/Mappers/KatanaToLucaMapper.cs" -Pattern "AlisTevkifatOran|SatisTevkifatOran" -Context 0,0
if ($mapperCheck) {
    Write-Host "   OK: Tevkifat fields found in mapper" -ForegroundColor Green
    $mapperCheck | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Gray }
} else {
    Write-Host "   ERROR: Tevkifat fields NOT found in mapper!" -ForegroundColor Red
}

Write-Host "`n3. Checking serialization settings..." -ForegroundColor Yellow
$serCheck = Select-String -Path "src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs" -Pattern "DefaultIgnoreCondition.*Never" -Context 0,0
if ($serCheck) {
    Write-Host "   OK: DefaultIgnoreCondition.Never is set" -ForegroundColor Green
    $serCheck | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Gray }
} else {
    Write-Host "   ERROR: DefaultIgnoreCondition.Never NOT found!" -ForegroundColor Red
}

Write-Host "`n4. Checking .dockerignore..." -ForegroundColor Yellow
if (Test-Path ".dockerignore") {
    Write-Host "   .dockerignore contents:" -ForegroundColor Gray
    Get-Content ".dockerignore" | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
} else {
    Write-Host "   No .dockerignore file found" -ForegroundColor Gray
}

Write-Host "`n=== Verification Complete ===" -ForegroundColor Cyan
Write-Host "`nIf all checks passed, the source code is correct." -ForegroundColor Green
Write-Host "The issue is that the Docker image doesn't contain this code." -ForegroundColor Yellow
Write-Host "`nNext step: Run a complete rebuild with --no-cache" -ForegroundColor Cyan
