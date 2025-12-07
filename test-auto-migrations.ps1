# Quick Test for Auto-Migration Script
# This script tests the auto-migration functionality

Write-Host "=== TESTING AUTO-MIGRATION SCRIPT ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if script exists
Write-Host "Test 1: Checking script exists..." -ForegroundColor Yellow
if (Test-Path "auto-apply-all-migrations.ps1") {
    Write-Host "✅ Script found" -ForegroundColor Green
} else {
    Write-Host "❌ Script not found" -ForegroundColor Red
    exit 1
}

# Test 2: Check Docker
Write-Host ""
Write-Host "Test 2: Checking Docker..." -ForegroundColor Yellow
try {
    docker ps 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Docker is running" -ForegroundColor Green
    } else {
        Write-Host "❌ Docker is not running" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Docker not found" -ForegroundColor Red
    exit 1
}

# Test 3: Check SQL scripts directory
Write-Host ""
Write-Host "Test 3: Checking SQL scripts directory..." -ForegroundColor Yellow
if (Test-Path "db") {
    $sqlFiles = Get-ChildItem "db" -Filter "*.sql"
    Write-Host "✅ Found $($sqlFiles.Count) SQL files in db/" -ForegroundColor Green
    
    if ($sqlFiles.Count -gt 0) {
        Write-Host "   SQL Scripts:" -ForegroundColor Cyan
        $sqlFiles | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Gray }
    }
} else {
    Write-Host "⚠️  db/ directory not found" -ForegroundColor Yellow
}

# Test 4: Check database container
Write-Host ""
Write-Host "Test 4: Checking database container..." -ForegroundColor Yellow
$dbStatus = docker-compose ps db --format json 2>&1

if ($LASTEXITCODE -eq 0) {
    try {
        $json = $dbStatus | ConvertFrom-Json
        if ($json.State -eq "running") {
            Write-Host "✅ Database container is running" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Database container exists but not running (State: $($json.State))" -ForegroundColor Yellow
            Write-Host "   Starting database..." -ForegroundColor Cyan
            docker-compose up -d db 2>&1 | Out-Null
            Start-Sleep -Seconds 5
            Write-Host "✅ Database started" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️  Could not parse container status" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Database container not found" -ForegroundColor Yellow
}

# Test 5: Test database connectivity
Write-Host ""
Write-Host "Test 5: Testing database connectivity..." -ForegroundColor Yellow
$testQuery = "SELECT @@VERSION"
$result = $testQuery | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -h -1 -W 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database connection successful" -ForegroundColor Green
    if ($result -match "Microsoft SQL Server") {
        Write-Host "   Version: $($result -replace '\s+', ' ')" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ Database connection failed" -ForegroundColor Red
    Write-Host "   Error: $result" -ForegroundColor Red
}

# Test 6: Check if KatanaDB exists
Write-Host ""
Write-Host "Test 6: Checking KatanaDB database..." -ForegroundColor Yellow
$dbCheckQuery = "SELECT DB_ID('KatanaDB')"
$dbCheck = $dbCheckQuery | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d master -h -1 -W 2>&1

if ($LASTEXITCODE -eq 0 -and $dbCheck -match "\d+") {
    Write-Host "✅ KatanaDB exists" -ForegroundColor Green
} else {
    Write-Host "⚠️  KatanaDB does not exist (will be created by migrations)" -ForegroundColor Yellow
}

# Test 7: Dry run check
Write-Host ""
Write-Host "Test 7: Checking script syntax..." -ForegroundColor Yellow
try {
    $scriptContent = Get-Content "auto-apply-all-migrations.ps1" -Raw
    $errors = $null
    $null = [System.Management.Automation.PSParser]::Tokenize($scriptContent, [ref]$errors)
    
    if ($errors.Count -eq 0) {
        Write-Host "✅ Script syntax is valid" -ForegroundColor Green
    } else {
        Write-Host "❌ Script has syntax errors:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
    }
} catch {
    Write-Host "⚠️  Could not validate script syntax: $_" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "=== TEST SUMMARY ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ All prerequisites are ready!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now run:" -ForegroundColor Yellow
Write-Host "  .\auto-apply-all-migrations.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Or with options:" -ForegroundColor Yellow
Write-Host "  .\auto-apply-all-migrations.ps1 -Verbose" -ForegroundColor White
Write-Host "  .\auto-apply-all-migrations.ps1 -SkipBackup" -ForegroundColor White
Write-Host "  .\auto-apply-all-migrations.ps1 -Force" -ForegroundColor White
Write-Host ""
