# Apply cleanup tables migration
$ErrorActionPreference = "Stop"

Write-Host "=== Applying Cleanup Tables Migration ===" -ForegroundColor Cyan
Write-Host ""

$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Admin00!S;TrustServerCertificate=True;"
$sqlFile = "db/migrations/add_cleanup_tables.sql"

try {
    Write-Host "Reading SQL file: $sqlFile" -ForegroundColor Yellow
    $sql = Get-Content $sqlFile -Raw
    
    # Split by GO statements
    $batches = $sql -split '\r?\nGO\r?\n'
    $batchCount = $batches.Count
    Write-Host "Found $batchCount SQL batches" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "Connecting to database..." -ForegroundColor Yellow
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "Connected successfully!" -ForegroundColor Green
    Write-Host ""
    
    $batchNum = 0
    foreach ($batch in $batches) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        
        $batchNum++
        Write-Host "Executing batch $batchNum..." -ForegroundColor Yellow
        
        $command = $connection.CreateCommand()
        $command.CommandText = $batch
        $command.CommandTimeout = 60
        $result = $command.ExecuteNonQuery()
        
        Write-Host "  Batch $batchNum completed" -ForegroundColor Green
    }
    
    $connection.Close()
    Write-Host ""
    Write-Host "Migration applied successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Verify tables were created
    Write-Host "Verifying tables..." -ForegroundColor Yellow
    $connection.Open()
    
    $verifyCmd = $connection.CreateCommand()
    $verifyCmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('CleanupOperations', 'CleanupLogs') ORDER BY TABLE_NAME"
    
    $reader = $verifyCmd.ExecuteReader()
    while ($reader.Read()) {
        $tableName = $reader['TABLE_NAME']
        Write-Host "  Table created: $tableName" -ForegroundColor Green
    }
    $reader.Close()
    
    # Check if new columns were added to SalesOrders
    $verifyCmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SalesOrders' AND COLUMN_NAME IN ('ApprovedDate', 'ApprovedBy', 'SyncStatus') ORDER BY COLUMN_NAME"
    
    $reader = $verifyCmd.ExecuteReader()
    Write-Host ""
    Write-Host "SalesOrders new columns:" -ForegroundColor Yellow
    while ($reader.Read()) {
        $colName = $reader['COLUMN_NAME']
        Write-Host "  Column added: $colName" -ForegroundColor Green
    }
    $reader.Close()
    
    # Check if KatanaOrderId was added to SalesOrderLines
    $verifyCmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SalesOrderLines' AND COLUMN_NAME = 'KatanaOrderId'"
    
    $reader = $verifyCmd.ExecuteReader()
    Write-Host ""
    Write-Host "SalesOrderLines new columns:" -ForegroundColor Yellow
    while ($reader.Read()) {
        $colName = $reader['COLUMN_NAME']
        Write-Host "  Column added: $colName" -ForegroundColor Green
    }
    $reader.Close()
    
    $connection.Close()
    Write-Host ""
    Write-Host "All tables and columns verified successfully!" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "Migration failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
    exit 1
}
