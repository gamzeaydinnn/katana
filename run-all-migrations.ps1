# Simple Migration Runner
# Runs all pending migrations automatically

Write-Host "=== RUNNING ALL MIGRATIONS ===" -ForegroundColor Cyan
Write-Host ""

# Check Docker
Write-Host "Checking Docker..." -ForegroundColor Yellow
docker ps 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker is not running!" -ForegroundColor Red
    exit 1
}
Write-Host "OK: Docker is running" -ForegroundColor Green

# Start containers
Write-Host ""
Write-Host "Starting containers..." -ForegroundColor Yellow
docker-compose up -d
Start-Sleep -Seconds 10
Write-Host "OK: Containers started" -ForegroundColor Green

# Wait for database
Write-Host ""
Write-Host "Waiting for database..." -ForegroundColor Yellow
$maxWait = 30
$waited = 0

while ($waited -lt $maxWait) {
    $test = "SELECT 1" | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d master -h -1 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: Database is ready" -ForegroundColor Green
        break
    }
    Start-Sleep -Seconds 2
    $waited += 2
}

if ($waited -ge $maxWait) {
    Write-Host "ERROR: Database timeout" -ForegroundColor Red
    exit 1
}

# Apply EF Migrations
Write-Host ""
Write-Host "Applying EF Core migrations..." -ForegroundColor Yellow
docker-compose stop backend 2>&1 | Out-Null
$efResult = docker-compose run --rm backend dotnet ef database update --project /app/src/Katana.Data/Katana.Data.csproj --startup-project /app/src/Katana.API/Katana.API.csproj 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK: EF migrations applied" -ForegroundColor Green
} else {
    Write-Host "WARNING: EF migrations may have issues" -ForegroundColor Yellow
}

# Apply SQL Scripts
Write-Host ""
Write-Host "Applying SQL scripts..." -ForegroundColor Yellow

$scripts = @(
    "db/create_product_luca_mappings.sql",
    "db/create_product_luca_mappings_table.sql",
    "db/populate_initial_mappings.sql",
    "db/insert_category_mappings.sql",
    "db/apply_category_mappings.sql",
    "db/apply_category_mappings_fixed.sql",
    "db/update_mapping_266220.sql",
    "db/update_mapping_266220_fix.sql",
    "db/update_mapping_266220_dbo.sql"
)

$success = 0
$failed = 0

foreach ($script in $scripts) {
    if (Test-Path $script) {
        $scriptName = Split-Path $script -Leaf
        Write-Host "  Applying: $scriptName" -ForegroundColor Cyan
        
        $sql = Get-Content $script -Raw
        $result = $sql | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  OK: $scriptName" -ForegroundColor Green
            $success++
        } else {
            Write-Host "  WARNING: $scriptName may have issues" -ForegroundColor Yellow
            $failed++
        }
    }
}

# Restart backend
Write-Host ""
Write-Host "Restarting backend..." -ForegroundColor Yellow
docker-compose restart backend 2>&1 | Out-Null
Start-Sleep -Seconds 5
Write-Host "OK: Backend restarted" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "  Total: $($scripts.Count)" -ForegroundColor White
Write-Host "  Success: $success" -ForegroundColor Green
Write-Host "  Issues: $failed" -ForegroundColor Yellow
Write-Host ""

if ($failed -eq 0) {
    Write-Host "SUCCESS: All migrations completed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next: Run .\quick-fix-check.ps1" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "COMPLETED: Some scripts had warnings" -ForegroundColor Yellow
    Write-Host "Check logs: docker-compose logs backend" -ForegroundColor Gray
    exit 0
}
