# Check Migration Status
# Shows which migrations have been applied and which are pending

param(
    [switch]$Detailed
)

$ErrorActionPreference = "Continue"

function Write-Header($text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }
function Write-Success($text) { Write-Host "✅ $text" -ForegroundColor Green }
function Write-Warning($text) { Write-Host "⚠️  $text" -ForegroundColor Yellow }
function Write-Info($text) { Write-Host "ℹ️  $text" -ForegroundColor Blue }

$DB_NAME = "KatanaDB"
$DB_USER = "sa"
$DB_PASS = "Admin00!S"
$MIGRATION_TABLE = "__MigrationHistory"

Write-Header "MIGRATION STATUS CHECK"

# Check if database is running
Write-Host "`nChecking database connection..." -ForegroundColor Yellow
$testQuery = "SELECT 1"
$result = $testQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Cannot connect to database"
    Write-Host "Make sure database is running: docker-compose up -d db" -ForegroundColor Gray
    exit 1
}

Write-Success "Database connection OK"

# Check if migration tracking table exists
Write-Host "`nChecking migration tracking table..." -ForegroundColor Yellow
$tableCheckQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '$MIGRATION_TABLE'"
$tableExists = $tableCheckQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1

if ($LASTEXITCODE -ne 0 -or $tableExists -notmatch "1") {
    Write-Warning "Migration tracking table does not exist"
    Write-Info "Run: .\auto-apply-all-migrations.ps1 to create it"
    exit 0
}

Write-Success "Migration tracking table exists"

# Get applied migrations
Write-Header "APPLIED MIGRATIONS"

$appliedQuery = @"
SELECT 
    ScriptName,
    FORMAT(AppliedAt, 'yyyy-MM-dd HH:mm:ss') AS AppliedAt,
    AppliedBy,
    CASE WHEN Success = 1 THEN 'Success' ELSE 'Failed' END AS Status,
    CASE WHEN ErrorMessage IS NULL THEN '' ELSE LEFT(ErrorMessage, 100) END AS Error
FROM [$MIGRATION_TABLE]
ORDER BY AppliedAt DESC
"@

$applied = $appliedQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -W -s "|" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host $applied
    Write-Host ""
} else {
    Write-Warning "Could not retrieve applied migrations"
}

# Count migrations
$countQuery = @"
SELECT 
    COUNT(*) AS Total,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) AS Successful,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS Failed
FROM [$MIGRATION_TABLE]
"@

$counts = $countQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1

if ($LASTEXITCODE -eq 0 -and $counts -match "(\d+)\s+(\d+)\s+(\d+)") {
    $total = $matches[1]
    $successful = $matches[2]
    $failed = $matches[3]
    
    Write-Header "SUMMARY"
    Write-Host ""
    Write-Host "  Total Migrations: $total" -ForegroundColor Cyan
    Write-Host "  ✅ Successful: $successful" -ForegroundColor Green
    Write-Host "  ❌ Failed: $failed" -ForegroundColor Red
    Write-Host ""
}

# Check for pending migrations
Write-Header "PENDING MIGRATIONS"

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

$pending = @()

foreach ($script in $SQL_SCRIPTS) {
    $scriptPath = Join-Path "db" $script
    
    if (-not (Test-Path $scriptPath)) {
        continue
    }
    
    $checkQuery = "SELECT COUNT(*) FROM [$MIGRATION_TABLE] WHERE ScriptName = '$script' AND Success = 1"
    $isApplied = $checkQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $isApplied -match "0") {
        $pending += $script
    }
}

if ($pending.Count -gt 0) {
    Write-Host ""
    Write-Warning "Found $($pending.Count) pending migration(s):"
    Write-Host ""
    foreach ($script in $pending) {
        Write-Host "  - $script" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Info "Run: .\auto-apply-all-migrations.ps1 to apply pending migrations"
} else {
    Write-Host ""
    Write-Success "No pending migrations - all up to date!"
}

# Show failed migrations if any
if ($failed -gt 0) {
    Write-Header "FAILED MIGRATIONS"
    
    $failedQuery = @"
SELECT 
    ScriptName,
    FORMAT(AppliedAt, 'yyyy-MM-dd HH:mm:ss') AS AppliedAt,
    ErrorMessage
FROM [$MIGRATION_TABLE]
WHERE Success = 0
ORDER BY AppliedAt DESC
"@
    
    $failedList = $failedQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -W -s "|" 2>&1
    
    Write-Host ""
    Write-Host $failedList
    Write-Host ""
    Write-Info "To retry failed migrations, run: .\auto-apply-all-migrations.ps1 -Force"
}

# Detailed mode - show table info
if ($Detailed) {
    Write-Header "DATABASE TABLES"
    
    $tablesQuery = @"
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME
"@
    
    $tables = $tablesQuery | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -W -s "|" 2>&1
    
    Write-Host ""
    Write-Host $tables
    Write-Host ""
    
    # Check specific important tables
    Write-Header "KEY TABLES STATUS"
    
    $keyTables = @("Products", "Customers", "MappingTables", "ProductLucaMappings")
    
    Write-Host ""
    foreach ($table in $keyTables) {
        $checkTable = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '$table'"
        $exists = $checkTable | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1
        
        if ($LASTEXITCODE -eq 0 -and $exists -match "1") {
            # Get row count
            $rowCount = "SELECT COUNT(*) FROM [$table]" | docker-compose exec -T db sqlcmd -S localhost -U $DB_USER -P $DB_PASS -d $DB_NAME -h -1 -W 2>&1
            
            if ($LASTEXITCODE -eq 0 -and $rowCount -match "(\d+)") {
                Write-Host "  ✅ $table - $($matches[1]) rows" -ForegroundColor Green
            } else {
                Write-Host "  ✅ $table - exists" -ForegroundColor Green
            }
        } else {
            Write-Host "  ❌ $table - missing" -ForegroundColor Red
        }
    }
    Write-Host ""
}

Write-Host ""
Write-Host "For more details, run with -Detailed flag" -ForegroundColor Gray
Write-Host ""
