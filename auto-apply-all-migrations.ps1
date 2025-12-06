# ============================================================================
# AUTO-APPLY ALL MIGRATIONS
# ============================================================================
# This script automatically detects and applies all missing migrations
# including EF Core migrations and custom SQL scripts
# ============================================================================

param(
    [switch]$Force,
    [switch]$SkipBackup,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"

# Colors
function Write-Header($text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }
function Write-Success($text) { Write-Host "✅ $text" -ForegroundColor Green }
function Write-Warning($text) { Write-Host "⚠️  $text" -ForegroundColor Yellow }
function Write-Error($text) { Write-Host "❌ $text" -ForegroundColor Red }
function Write-Info($text) { Write-Host "ℹ️  $text" -ForegroundColor Blue }
function Write-Step($text) { Write-Host "`n$text" -ForegroundColor Yellow }

# ============================================================================
# CONFIGURATION
# ============================================================================

$DB_SERVER = "localhost"
$DB_NAME = "KatanaDB"
$DB_USER = "sa"
$DB_PASS = "Admin00!S"

$SQL_SCRIPTS_DIR = "db"
$MIGRATION_TRACKING_TABLE = "__MigrationHistory"

# SQL Scripts to apply in order
$SQL_SCRIPTS = @(
    "create_product_luca_mappings.sql",
    "create_product_luca_mappings_table.sql",
    "populate_initial_mappings.sql",
    "insert_category_mappings.sql",
    "apply_category_mappings.sql",
    "apply_category_mappings_fixed.sql",
    "update_mapping_266220.sql",
    "update_mapping_266220_fix.sql",
    "update_mapping_266220_dbo.sql"
)

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Test-DockerRunning {
    try {
        $null = docker ps 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

function Test-ContainerRunning {
    param([string]$containerName)
    
    $status = docker-compose ps $containerName --format json 2>&1
    if ($LASTEXITCODE -ne 0) { return $false }
    
    try {
        $json = $status | ConvertFrom-Json
        return $json.State -eq "running"
    } catch {
        return $false
    }
}

function Start-DatabaseContainer {
    Write-Step "Starting database container..."
    
    if (Test-ContainerRunning "db") {
        Write-Success "Database container already running"
        return $true
    }
    
    Write-Info "Starting database..."
    docker-compose up -d db 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to start database container"
        return $false
    }
    
    Write-Info "Waiting for database to be ready..."
    $maxWait = 60
    $waited = 0
    
    while ($waited -lt $maxWait) {
        $dbCheck = docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -Q "SELECT 1" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Database is ready"
            return $true
        }
        Start-Sleep -Seconds 2
        $waited += 2
        if ($Verbose) {
            Write-Host "   Waiting... $waited / $maxWait seconds" -ForegroundColor Gray
        }
    }
    
    Write-Error "Database failed to become ready within timeout"
    return $false
}

function Invoke-SqlCommand {
    param(
        [string]$query,
        [string]$database = $DB_NAME
    )
    
    $result = $query | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $database -h -1 -W -b 2>&1
    
    return @{
        Success = $LASTEXITCODE -eq 0
        Output = $result
        ExitCode = $LASTEXITCODE
    }
}

function Test-DatabaseExists {
    $query = "SELECT DB_ID('$DB_NAME')"
    $result = Invoke-SqlCommand -query $query -database "master"
    
    if ($result.Success -and $result.Output -match "\d+") {
        return $true
    }
    return $false
}

function New-MigrationTrackingTable {
    Write-Step "Creating migration tracking table..."
    
    $tableName = $MIGRATION_TRACKING_TABLE
    $query = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '$tableName')
BEGIN
    CREATE TABLE [$tableName] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ScriptName NVARCHAR(500) NOT NULL,
        AppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        AppliedBy NVARCHAR(200) NOT NULL DEFAULT SYSTEM_USER,
        Success BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_ScriptName UNIQUE (ScriptName)
    );
    PRINT 'Migration tracking table created';
END
ELSE
BEGIN
    PRINT 'Migration tracking table already exists';
END
"@
    
    $result = Invoke-SqlCommand -query $query
    
    if ($result.Success) {
        Write-Success "Migration tracking table ready"
        return $true
    } else {
        Write-Warning "Could not create migration tracking table"
        if ($Verbose) {
            Write-Host $result.Output -ForegroundColor Gray
        }
        return $false
    }
}

function Test-MigrationApplied {
    param([string]$scriptName)
    
    $tableName = $MIGRATION_TRACKING_TABLE
    $query = "SELECT COUNT(*) FROM [$tableName] WHERE ScriptName = '$scriptName' AND Success = 1"
    $result = Invoke-SqlCommand -query $query
    
    if ($result.Success -and $result.Output -match "^\s*(\d+)") {
        return [int]$matches[1] -gt 0
    }
    return $false
}

function Register-MigrationApplied {
    param(
        [string]$scriptName,
        [bool]$success = $true,
        [string]$errorMessage = $null
    )
    
    $errorMsg = if ($errorMessage) { "'$($errorMessage -replace "'", "''")'" } else { "NULL" }
    $successBit = if ($success) { "1" } else { "0" }
    $tableName = $MIGRATION_TRACKING_TABLE
    
    $query = @"
IF EXISTS (SELECT 1 FROM [$tableName] WHERE ScriptName = '$scriptName')
BEGIN
    UPDATE [$tableName] 
    SET AppliedAt = GETDATE(), 
        Success = $successBit, 
        ErrorMessage = $errorMsg
    WHERE ScriptName = '$scriptName';
END
ELSE
BEGIN
    INSERT INTO [$tableName] (ScriptName, Success, ErrorMessage)
    VALUES ('$scriptName', $successBit, $errorMsg);
END
"@
    
    $null = Invoke-SqlCommand -query $query
}

function Invoke-SqlScript {
    param([string]$scriptPath)
    
    if (-not (Test-Path $scriptPath)) {
        Write-Warning "Script not found: $scriptPath"
        return $false
    }
    
    $scriptName = Split-Path $scriptPath -Leaf
    
    # Check if already applied
    if (-not $Force -and (Test-MigrationApplied -scriptName $scriptName)) {
        Write-Info "Already applied: $scriptName (use -Force to reapply)"
        return $true
    }
    
    Write-Info "Applying: $scriptName"
    
    try {
        $sqlContent = Get-Content $scriptPath -Raw -ErrorAction Stop
        
        # Execute the script
        $result = Invoke-SqlCommand -query $sqlContent
        
        if ($result.Success) {
            Write-Success "Applied: $scriptName"
            Register-MigrationApplied -scriptName $scriptName -success $true
            
            if ($Verbose -and $result.Output) {
                Write-Host "   Output: $($result.Output)" -ForegroundColor Gray
            }
            return $true
        } else {
            Write-Error "Failed: $scriptName"
            if ($result.Output) {
                Write-Host "   Error: $($result.Output)" -ForegroundColor Red
            }
            Register-MigrationApplied -scriptName $scriptName -success $false -errorMessage $result.Output
            return $false
        }
    } catch {
        Write-Error "Exception applying $scriptName : $_"
        Register-MigrationApplied -scriptName $scriptName -success $false -errorMessage $_.Exception.Message
        return $false
    }
}

function Invoke-EFCoreMigrations {
    Write-Step "Applying EF Core migrations..."
    
    # Check if backend container exists
    $backendExists = docker-compose ps backend 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Backend container not configured, skipping EF migrations"
        return $true
    }
    
    # Stop backend to avoid conflicts
    Write-Info "Stopping backend container..."
    docker-compose stop backend 2>&1 | Out-Null
    
    # Try to apply migrations
    Write-Info "Running EF Core database update..."
    
    $migrationCmd = "dotnet ef database update --project /app/src/Katana.Data/Katana.Data.csproj --startup-project /app/src/Katana.API/Katana.API.csproj"
    
    $result = docker-compose run --rm backend $migrationCmd 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "EF Core migrations applied"
        return $true
    } else {
        Write-Warning "EF Core migrations may have failed"
        
        if ($Verbose) {
            Write-Host "   Output:" -ForegroundColor Gray
            Write-Host $result -ForegroundColor Gray
        }
        
        # Try alternative method
        Write-Info "Trying alternative method (running backend)..."
        docker-compose up -d backend 2>&1 | Out-Null
        Start-Sleep -Seconds 10
        
        $altResult = docker-compose exec backend $migrationCmd 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "EF Core migrations applied (alternative method)"
            return $true
        } else {
            Write-Warning "Alternative method also had issues"
            if ($Verbose) {
                Write-Host $altResult -ForegroundColor Gray
            }
            return $false
        }
    }
}

function Backup-Database {
    if ($SkipBackup) {
        Write-Info "Skipping backup (--SkipBackup flag)"
        return $true
    }
    
    Write-Step "Creating database backup..."
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = "/var/opt/mssql/backup/" + $DB_NAME + "_" + $timestamp + ".bak"
    
    $backupName = $DB_NAME + "-Full Database Backup"
    $backupQuery = @"
BACKUP DATABASE [$DB_NAME] 
TO DISK = N'$backupFile' 
WITH FORMAT, INIT, 
NAME = N'$backupName', 
SKIP, NOREWIND, NOUNLOAD, STATS = 10
"@
    
    $result = Invoke-SqlCommand -query $backupQuery
    
    if ($result.Success) {
        $successMsg = "Database backed up to: $backupFile"
        Write-Success $successMsg
        return $true
    } else {
        Write-Warning "Backup failed, but continuing..."
        if ($Verbose) {
            Write-Host $result.Output -ForegroundColor Gray
        }
        return $false
    }
}

function Get-MigrationStatus {
    Write-Step "Migration Status Report"
    
    $tableName = $MIGRATION_TRACKING_TABLE
    $query = @"
SELECT 
    ScriptName,
    AppliedAt,
    AppliedBy,
    CASE WHEN Success = 1 THEN 'Success' ELSE 'Failed' END AS Status,
    ErrorMessage
FROM [$tableName]
ORDER BY AppliedAt DESC
"@
    
    $result = Invoke-SqlCommand -query $query
    
    if ($result.Success) {
        Write-Host "`nApplied Migrations:" -ForegroundColor Cyan
        Write-Host $result.Output
    } else {
        Write-Warning "Could not retrieve migration status"
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Header "AUTO-APPLY ALL MIGRATIONS"

# 1. Check Docker
Write-Step "1. Checking Docker..."
if (-not (Test-DockerRunning)) {
    Write-Error "Docker is not running. Please start Docker Desktop."
    exit 1
}
Write-Success "Docker is running"

# 2. Start Database
Write-Step "2. Ensuring database is running..."
if (-not (Start-DatabaseContainer)) {
    Write-Error "Could not start database container"
    exit 1
}

# 3. Check Database Exists
Write-Step "3. Checking database exists..."
if (-not (Test-DatabaseExists)) {
    $warnMsg = "Database '$DB_NAME' does not exist. It will be created by EF migrations."
    Write-Warning $warnMsg
}

# 4. Backup Database
if (Test-DatabaseExists) {
    Backup-Database
}

# 5. Create Migration Tracking Table
New-MigrationTrackingTable

# 6. Apply EF Core Migrations
Invoke-EFCoreMigrations

# 7. Apply Custom SQL Scripts
Write-Header "APPLYING CUSTOM SQL SCRIPTS"

$successCount = 0
$failCount = 0
$skipCount = 0

foreach ($scriptFile in $SQL_SCRIPTS) {
    $scriptPath = Join-Path $SQL_SCRIPTS_DIR $scriptFile
    
    if (-not (Test-Path $scriptPath)) {
        Write-Warning "Script not found: $scriptPath"
        $skipCount++
        continue
    }
    
    $result = Invoke-SqlScript -scriptPath $scriptPath
    
    if ($result) {
        $successCount++
    } else {
        $failCount++
    }
}

# 8. Show Migration Status
Get-MigrationStatus

# 9. Restart Backend
Write-Step "Restarting backend service..."
docker-compose restart backend 2>&1 | Out-Null
Start-Sleep -Seconds 5
Write-Success "Backend restarted"

# 10. Summary
Write-Header "MIGRATION SUMMARY"
Write-Host ""
$totalScripts = $SQL_SCRIPTS.Count
Write-Host "  Total Scripts: $totalScripts" -ForegroundColor Cyan
Write-Host "  ✅ Success: $successCount" -ForegroundColor Green
Write-Host "  ❌ Failed: $failCount" -ForegroundColor Red
Write-Host "  ⏭️  Skipped: $skipCount" -ForegroundColor Yellow
Write-Host ""

if ($failCount -eq 0) {
    Write-Success "All migrations completed successfully!"
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "  - Run: .\quick-fix-check.ps1" -ForegroundColor White
    Write-Host "  - Check backend logs: docker-compose logs backend" -ForegroundColor White
    Write-Host ""
    exit 0
} else {
    Write-Warning "Some migrations failed. Review the errors above."
    Write-Host ""
    Write-Info "Troubleshooting:"
    Write-Host "  - Check database logs: docker-compose logs db" -ForegroundColor White
    Write-Host "  - Run with -Verbose flag for more details" -ForegroundColor White
    Write-Host "  - Use -Force to reapply migrations" -ForegroundColor White
    Write-Host ""
    exit 1
}
