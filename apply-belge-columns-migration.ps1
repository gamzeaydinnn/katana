# Apply BelgeSeri, BelgeNo, BelgeTakipNo columns migration to OrderMappings table
# This fixes the "Invalid column name 'BelgeNo'" error

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Applying OrderMappings Belge Columns Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Connection string - adjust as needed
$connectionString = "Server=localhost;Database=KatanaIntegration;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

# Read the migration SQL
$migrationPath = "db/migrations/add_belge_columns_to_ordermappings.sql"
if (-not (Test-Path $migrationPath)) {
    Write-Host "ERROR: Migration file not found at $migrationPath" -ForegroundColor Red
    exit 1
}

$sqlScript = Get-Content $migrationPath -Raw
Write-Host "Migration SQL loaded from: $migrationPath" -ForegroundColor Green

# Try to execute via Docker first
Write-Host "`nAttempting to apply migration via Docker..." -ForegroundColor Yellow

try {
    # Check if docker container is running
    $containerRunning = docker ps --filter "name=katana-db" --format "{{.Names}}" 2>$null
    
    if ($containerRunning) {
        Write-Host "Found running container: $containerRunning" -ForegroundColor Green
        
        # Execute SQL via docker exec
        $result = docker exec -i katana-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -d KatanaIntegration -C -Q "$sqlScript" 2>&1
        
        Write-Host "`nMigration Result:" -ForegroundColor Cyan
        Write-Host $result
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n========================================" -ForegroundColor Green
            Write-Host "Migration applied successfully!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "`nPlease restart the backend to pick up the changes:" -ForegroundColor Yellow
            Write-Host "  docker-compose restart backend" -ForegroundColor White
        } else {
            Write-Host "`nMigration may have failed. Check the output above." -ForegroundColor Red
        }
    } else {
        Write-Host "Docker container 'katana-db' not found. Trying direct connection..." -ForegroundColor Yellow
        
        # Try direct SQL connection
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $connectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $sqlScript
        $command.ExecuteNonQuery()
        
        $connection.Close()
        
        Write-Host "`n========================================" -ForegroundColor Green
        Write-Host "Migration applied successfully via direct connection!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    }
} catch {
    Write-Host "`nError applying migration: $_" -ForegroundColor Red
    Write-Host "`nManual steps:" -ForegroundColor Yellow
    Write-Host "1. Connect to your SQL Server database" -ForegroundColor White
    Write-Host "2. Run the SQL script at: $migrationPath" -ForegroundColor White
    Write-Host "3. Restart the backend service" -ForegroundColor White
}

Write-Host "`nDone." -ForegroundColor Cyan
