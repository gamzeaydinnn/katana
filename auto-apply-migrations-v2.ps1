# ============================================================================
# AUTO-APPLY ALL MIGRATIONS - Simplified Version
# ============================================================================

param(
    [switch]$Force,
    [switch]$SkipBackup,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"

# Configuration
$DB_SERVER = "localhost"
$DB_NAME = "KatanaDB"
$DB_USER = "sa"
$DB_PASS = "Admin00!S"
$SQL_SCRIPTS_DIR = "db"
$MIGRATION_TABLE = "__MigrationHistory"

# SQL Scripts in order
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

# Helper Functions
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Invoke-DbCommand {
    param([string]$Query, [string]$Database = $DB_NAME)
    
    $result = $Query | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $Database -h -1 -W -b 2>&1
    
    return @{
        Success = ($LASTEXITCODE -eq 0)
        Output = $result
    }
}

# Main Script
Write-ColorOutput "`n=== AUTO-APPLY ALL MIGRATIONS ===" "Cyan"

# 1. Check Docker
Write-ColorOutput "`n1. Checking Docker..." "Yellow"
try {
    $null = docker ps 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Docker is not running" "Red"
        exit 1
    }
    Write-ColorOutput "✅ Docker is running" "Green"
} catch {
    Write-ColorOutput "❌ Docker not found" "Red"
    exit 1
}

# 2. Start Database
Write-ColorOutput "`n2. Starting database container..." "Yellow"
$dbStatus = docker-compose ps db --format json 2>&1

if ($LASTEXITCODE -eq 0) {
    try {
        $json = $dbStatus | ConvertFrom-Json
        if ($json.State -ne "running") {
            Write-ColorOutput "Starting database..." "Cyan"
            docker-compose up -d db 2>&1 | Out-Null
            Start-Sleep -Seconds 10
        }
    } catch {
        docker-compose up -d db 2>&1 | Out-Null
        Start-Sleep -Seconds 10
    }
} else {
    docker-compose up -d db 2>&1 | Out-Null
    Start-Sleep -Seconds 10
}

# Wait for database
Write-ColorOutput "Waiting for database..." "Cyan"
$maxWait = 30
$waited = 0

while ($waited -lt $maxWait) {
    $testResult = Invoke-DbCommand -Query "SELECT 1" -Database "master"
    if ($testResult.Success) {
        Write-ColorOutput "✅ Database is ready" "Green"
        break
    }
    Start-Sleep -Seconds 2
    $waited += 2
    if ($Verbose) {
        Write-ColorOutput "   Waiting... $waited / $maxWait seconds" "Gray"
    }
}

if ($waited -ge $maxWait) {
    Write-ColorOutput "❌ Database timeout" "Red"
    exit 1
}

# 3. Check if database exists
Write-ColorOutput "`n3. Checking KatanaDB..." "Yellow"
$dbCheckResult = Invoke-DbCommand -Query "SELECT DB_ID('$DB_NAME')" -Database "master"

if (-not $dbCheckResult.Success -or $dbCheckResult.Output -notmatch "\d+") {
    Write-ColorOutput "⚠️  KatanaDB does not exist (will be created)" "Yellow"
}

# 4. Backup (if database exists and not skipped)
if (-not $SkipBackup -and $dbCheckResult.Success) {
    Write-ColorOutput "`n4. Creating backup..." "Yellow"
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "/var/opt/mssql/backup/KatanaDB_" + $timestamp + ".bak"
    
    $backupSql = "BACKUP DATABASE [$DB_NAME] TO DISK = N'$backupPath' WITH FORMAT, INIT"
    $backupResult = Invoke-DbCommand -Query $backupSql
    
    if ($backupResult.Success) {
        Write-ColorOutput "✅ Backup created: $backupPath" "Green"
    } else {
        Write-ColorOutput "⚠️  Backup failed, continuing..." "Yellow"
    }
}

# 5. Create migration tracking table
Write-ColorOutput "`n5. Creating migration tracking table..." "Yellow"

$createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '$MIGRATION_TABLE')
BEGIN
    CREATE TABLE [$MIGRATION_TABLE] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ScriptName NVARCHAR(500) NOT NULL UNIQUE,
        AppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        Success BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL
    );
END
"@

$createResult = Invoke-DbCommand -Query $createTableSql

if ($createResult.Success) {
    Write-ColorOutput "✅ Migration tracking table ready" "Green"
} else {
    Write-ColorOutput "⚠️  Could not create tracking table" "Yellow"
}

# 6. Apply EF Core Migrations
Write-ColorOutput "`n6. Applying EF Core migrations..." "Yellow"

docker-compose stop backend 2>&1 | Out-Null

$efCmd = "dotnet ef database update --project /app/src/Katana.Data/Katana.Data.csproj --startup-project /app/src/Katana.API/Katana.API.csproj"
$efResult = docker-compose run --rm backend $efCmd 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-ColorOutput "✅ EF migrations applied" "Green"
} else {
    Write-ColorOutput "⚠️  EF migrations may have issues" "Yellow"
    if ($Verbose) {
        Write-ColorOutput $efResult "Gray"
    }
}

# 7. Apply SQL Scripts
Write-ColorOutput "`n7. Applying SQL scripts..." "Yellow"

$successCount = 0
$failCount = 0
$skipCount = 0

foreach ($scriptFile in $SQL_SCRIPTS) {
    $scriptPath = Join-Path $SQL_SCRIPTS_DIR $scriptFile
    
    if (-not (Test-Path $scriptPath)) {
        Write-ColorOutput "⚠️  Not found: $scriptFile" "Yellow"
        $skipCount++
        continue
    }
    
    # Check if already applied
    if (-not $Force) {
        $checkSql = "SELECT COUNT(*) FROM [$MIGRATION_TABLE] WHERE ScriptName = '$scriptFile' AND Success = 1"
        $checkResult = Invoke-DbCommand -Query $checkSql
        
        if ($checkResult.Success -and $checkResult.Output -match "1") {
            Write-ColorOutput "ℹ️  Already applied: $scriptFile" "Blue"
            $skipCount++
            continue
        }
    }
    
    # Apply script
    Write-ColorOutput "Applying: $scriptFile" "Cyan"
    
    try {
        $sqlContent = Get-Content $scriptPath -Raw
        $applyResult = Invoke-DbCommand -Query $sqlContent
        
        if ($applyResult.Success) {
            Write-ColorOutput "✅ Applied: $scriptFile" "Green"
            
            # Record success
            $recordSql = "IF EXISTS (SELECT 1 FROM [$MIGRATION_TABLE] WHERE ScriptName = '$scriptFile') UPDATE [$MIGRATION_TABLE] SET AppliedAt = GETDATE(), Success = 1, ErrorMessage = NULL WHERE ScriptName = '$scriptFile' ELSE INSERT INTO [$MIGRATION_TABLE] (ScriptName, Success) VALUES ('$scriptFile', 1)"
            Invoke-DbCommand -Query $recordSql | Out-Null
            
            $successCount++
        } else {
            Write-ColorOutput "❌ Failed: $scriptFile" "Red"
            
            if ($Verbose -and $applyResult.Output) {
                Write-ColorOutput "   Error: $($applyResult.Output)" "Red"
            }
            
            # Record failure
            $errorMsg = $applyResult.Output -replace "'", "''"
            $recordSql = "IF EXISTS (SELECT 1 FROM [$MIGRATION_TABLE] WHERE ScriptName = '$scriptFile') UPDATE [$MIGRATION_TABLE] SET AppliedAt = GETDATE(), Success = 0, ErrorMessage = '$errorMsg' WHERE ScriptName = '$scriptFile' ELSE INSERT INTO [$MIGRATION_TABLE] (ScriptName, Success, ErrorMessage) VALUES ('$scriptFile', 0, '$errorMsg')"
            Invoke-DbCommand -Query $recordSql | Out-Null
            
            $failCount++
        }
    } catch {
        Write-ColorOutput "❌ Exception: $scriptFile - $_" "Red"
        $failCount++
    }
}

# 8. Show migration status
Write-ColorOutput "`n8. Migration Status:" "Yellow"

$statusSql = "SELECT ScriptName, FORMAT(AppliedAt, 'yyyy-MM-dd HH:mm:ss') AS AppliedAt, CASE WHEN Success = 1 THEN 'Success' ELSE 'Failed' END AS Status FROM [$MIGRATION_TABLE] ORDER BY AppliedAt DESC"
$statusResult = Invoke-DbCommand -Query $statusSql

if ($statusResult.Success) {
    Write-Host $statusResult.Output
}

# 9. Restart backend
Write-ColorOutput "`n9. Restarting backend..." "Yellow"
docker-compose restart backend 2>&1 | Out-Null
Start-Sleep -Seconds 5
Write-ColorOutput "✅ Backend restarted" "Green"

# 10. Summary
Write-ColorOutput "`n=== MIGRATION SUMMARY ===" "Cyan"
Write-Host ""
Write-ColorOutput "  Total Scripts: $($SQL_SCRIPTS.Count)" "Cyan"
Write-ColorOutput "  ✅ Success: $successCount" "Green"
Write-ColorOutput "  ❌ Failed: $failCount" "Red"
Write-ColorOutput "  ⏭️  Skipped: $skipCount" "Yellow"
Write-Host ""

if ($failCount -eq 0) {
    Write-ColorOutput "✅ All migrations completed successfully!" "Green"
    Write-Host ""
    Write-ColorOutput "Next: Run .\quick-fix-check.ps1" "Yellow"
    Write-Host ""
    exit 0
} else {
    Write-ColorOutput "⚠️  Some migrations failed" "Yellow"
    Write-Host ""
    Write-ColorOutput "Run with -Verbose for details" "Gray"
    Write-Host ""
    exit 1
}
