# Simple Migration Application

Write-Host "=== APPLYING MIGRATIONS (SIMPLE) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Ensure containers are running
Write-Host "1. Starting containers..." -ForegroundColor Yellow
docker-compose up -d
Start-Sleep -Seconds 10
Write-Host "   ✅ Containers started" -ForegroundColor Green

# 2. Wait for database
Write-Host ""
Write-Host "2. Waiting for database to be ready..." -ForegroundColor Yellow
$maxWait = 30
$waited = 0

while ($waited -lt $maxWait) {
    $dbCheck = docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -Q "SELECT 1" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Database ready" -ForegroundColor Green
        break
    }
    Start-Sleep -Seconds 2
    $waited += 2
    Write-Host "   ⏳ Waiting... ($waited/$maxWait)" -ForegroundColor Gray
}

# 3. Check if migrations are already applied
Write-Host ""
Write-Host "3. Checking migration status..." -ForegroundColor Yellow

$checkMigrations = @"
SELECT COUNT(*) as TableCount 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
AND TABLE_NAME IN ('Products', 'Customers', 'MappingTables')
"@

$tableCount = $checkMigrations | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -h -1 -W 2>&1

if ($tableCount -match "3") {
    Write-Host "   ✅ Core tables exist - migrations likely applied" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Core tables missing - migrations needed" -ForegroundColor Yellow
    
    # Try to apply migrations
    Write-Host ""
    Write-Host "4. Applying migrations from backend..." -ForegroundColor Yellow
    
    # The backend should auto-apply migrations on startup
    # Check Program.cs for auto-migration code
    Write-Host "   Backend should auto-apply migrations on startup" -ForegroundColor Cyan
    Write-Host "   Restarting backend to trigger migration..." -ForegroundColor Cyan
    
    docker-compose restart backend
    Start-Sleep -Seconds 15
    
    # Check again
    $tableCountAfter = $checkMigrations | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -h -1 -W 2>&1
    
    if ($tableCountAfter -match "3") {
        Write-Host "   ✅ Migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Migrations may not have applied" -ForegroundColor Red
        Write-Host "   Check backend logs: docker-compose logs backend | grep -i migration" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== MIGRATION CHECK COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend logs (last 20 lines):" -ForegroundColor Yellow
docker-compose logs --tail=20 backend

Write-Host ""
Write-Host "Run: .\quick-fix-check.ps1" -ForegroundColor Yellow
Write-Host ""
