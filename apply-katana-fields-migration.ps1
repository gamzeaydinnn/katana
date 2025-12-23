#!/usr/bin/env pwsh
# Apply Katana fields migration to Products table (SQL Server)

Write-Host "Applying Katana fields migration..." -ForegroundColor Cyan

$migrationFile = "db/migrations/add_katana_fields_to_products.sql"

if (-not (Test-Path $migrationFile)) {
    Write-Host "Migration file not found: $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reading migration file..." -ForegroundColor Yellow
$sql = Get-Content $migrationFile -Raw

Write-Host "Getting database connection info from appsettings..." -ForegroundColor Yellow

# Get connection string from appsettings
$appsettingsPath = "src/Katana.API/appsettings.Development.json"
if (-not (Test-Path $appsettingsPath)) {
    $appsettingsPath = "src/Katana.API/appsettings.json"
}

$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.SqlServerConnection

if (-not $connectionString) {
    Write-Host "Connection string not found in appsettings" -ForegroundColor Red
    exit 1
}

Write-Host "Connection string found" -ForegroundColor Green

# Parse SQL Server connection string
$dbServer = if ($connectionString -match "Server=([^;,]+)") { $matches[1] } else { "localhost" }
$dbPort = if ($connectionString -match ",(\d+)") { $matches[1] } else { "1433" }
$dbName = if ($connectionString -match "Database=([^;]+)") { $matches[1] } else { "KatanaDB" }
$dbUser = if ($connectionString -match "User Id=([^;]+)") { $matches[1] } else { "sa" }
$dbPassword = if ($connectionString -match "Password=([^;]+)") { $matches[1] } else { "" }

Write-Host "Database: $dbName on $dbServer`:$dbPort" -ForegroundColor Cyan

# Convert PostgreSQL syntax to SQL Server syntax
$sqlServerSql = $sql -replace "ALTER TABLE products", "ALTER TABLE dbo.products"
$sqlServerSql = $sqlServerSql -replace "ADD COLUMN", "ADD"
$sqlServerSql = $sqlServerSql -replace "INTEGER NULL", "INT NULL"
$sqlServerSql = $sqlServerSql -replace "BIGINT NULL", "BIGINT NULL"
$sqlServerSql = $sqlServerSql -replace "CREATE INDEX", "CREATE NONCLUSTERED INDEX"
$sqlServerSql = $sqlServerSql -replace "WHERE katana_order_id IS NOT NULL", ""
$sqlServerSql = $sqlServerSql -replace "WHERE katana_product_id IS NOT NULL", ""
$sqlServerSql = $sqlServerSql -replace "COMMENT ON COLUMN.*", ""

Write-Host "Executing migration via sqlcmd..." -ForegroundColor Yellow

# Add SET QUOTED_IDENTIFIER ON at the beginning
$sqlServerSql = "SET QUOTED_IDENTIFIER ON;`nGO`n" + $sqlServerSql

# Execute using sqlcmd
$sqlServerSql | sqlcmd -S "$dbServer,$dbPort" -d $dbName -U $dbUser -P $dbPassword -b

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration applied successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verifying columns..." -ForegroundColor Yellow
    
    # Verify columns
    $verifyQuery = @"
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'products' 
  AND COLUMN_NAME IN ('katana_product_id', 'katana_order_id')
ORDER BY COLUMN_NAME;
"@
    
    $verifyQuery | sqlcmd -S "$dbServer,$dbPort" -d $dbName -U $dbUser -P $dbPassword
    
    Write-Host ""
    Write-Host "Katana fields migration completed!" -ForegroundColor Green
} else {
    Write-Host "Migration failed" -ForegroundColor Red
    exit 1
}
