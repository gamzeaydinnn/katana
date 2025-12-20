# Apply EF Core Migrations to Database

Write-Host "=== APPLYING MIGRATIONS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if database container is running
Write-Host "1. Checking database container..." -ForegroundColor Yellow
$dbStatus = docker-compose ps db --format json 2>&1 | ConvertFrom-Json
if ($dbStatus.State -ne "running") {
    Write-Host "   ❌ Database container not running" -ForegroundColor Red
    Write-Host "   Starting database..." -ForegroundColor Yellow
    docker-compose up -d db
    Start-Sleep -Seconds 10
    Write-Host "   ✅ Database started" -ForegroundColor Green
} else {
    Write-Host "   ✅ Database is running" -ForegroundColor Green
}

# 2. Apply migrations using backend container
Write-Host ""
Write-Host "2. Applying EF Core migrations..." -ForegroundColor Yellow

# Stop backend if running (to avoid conflicts)
docker-compose stop backend 2>&1 | Out-Null

# Run migrations
$migrationResult = docker-compose run --rm backend dotnet ef database update --project /app/src/Katana.Data/Katana.Data.csproj --startup-project /app/src/Katana.API/Katana.API.csproj 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "   ❌ Migration failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error output:" -ForegroundColor Yellow
    Write-Host $migrationResult -ForegroundColor Red
    Write-Host ""
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
    
    # Alternative: Run migrations from within running backend
    docker-compose up -d backend
    Start-Sleep -Seconds 10
    
    $altResult = docker-compose exec backend dotnet ef database update --project /app/src/Katana.Data/Katana.Data.csproj --startup-project /app/src/Katana.API/Katana.API.csproj 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Migrations applied (alternative method)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Alternative method also failed" -ForegroundColor Red
        Write-Host $altResult -ForegroundColor Red
    }
}

# 3. Apply custom SQL scripts
Write-Host ""
Write-Host "3. Applying custom SQL scripts..." -ForegroundColor Yellow

$sqlScripts = @(
    "db/create_product_luca_mappings.sql",
    "db/populate_initial_mappings.sql",
    "db/apply_category_mappings_fixed.sql"
)

foreach ($script in $sqlScripts) {
    if (Test-Path $script) {
        Write-Host "   Applying: $script" -ForegroundColor Cyan
        
        $sql = Get-Content $script -Raw
        
        # Execute SQL via docker
        $result = $sql | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ Applied: $script" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Warning: $script may have failed" -ForegroundColor Yellow
            Write-Host "   $result" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️  Script not found: $script" -ForegroundColor Yellow
    }
}

# 4. Restart backend
Write-Host ""
Write-Host "4. Restarting backend..." -ForegroundColor Yellow
docker-compose restart backend
Start-Sleep -Seconds 5
Write-Host "   ✅ Backend restarted" -ForegroundColor Green

Write-Host ""
Write-Host "=== MIGRATIONS COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: Run .\quick-fix-check.ps1 to verify" -ForegroundColor Yellow
Write-Host ""
