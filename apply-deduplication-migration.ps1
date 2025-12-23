# Apply Product Deduplication Migration
Write-Host "Applying product deduplication migration..." -ForegroundColor Cyan

# Check if Docker is running
$dockerRunning = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker is not running. Please start Docker first." -ForegroundColor Red
    exit 1
}

# Copy SQL file to container and execute
docker cp db/migrations/add_product_deduplication_tables.sql katana-db-1:/tmp/migration.sql

# Apply migration via Docker (SQL Server) - Using correct database name and password from appsettings
$result = docker exec -i katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -i /tmp/migration.sql -C 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration applied successfully!" -ForegroundColor Green
    
    # Verify tables were created
    Write-Host "`nVerifying tables..." -ForegroundColor Yellow
    $verifyQuery = "SELECT name FROM sys.tables WHERE name IN ('merge_history', 'keep_separate_groups', 'merge_rollback_data') ORDER BY name;"
    
    $tables = docker exec -i katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q $verifyQuery -h -1 -C
    Write-Host "Created tables:" -ForegroundColor Green
    Write-Host $tables
    
    # Cleanup
    docker exec -i katana-db-1 rm /tmp/migration.sql
} else {
    Write-Host "Migration failed!" -ForegroundColor Red
    Write-Host $result
    exit 1
}
