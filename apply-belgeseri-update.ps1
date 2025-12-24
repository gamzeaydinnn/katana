# BelgeSeri Update Script
# SalesOrders ve OrderMappings tablolarında BelgeSeri = 'A' olan kayıtları 'EFA2025' olarak günceller

Write-Host "=== BelgeSeri Update Script ===" -ForegroundColor Cyan
Write-Host "Updating BelgeSeri from 'A' to 'EFA2025'..." -ForegroundColor Yellow

$server = "localhost,1433"
$database = "KatanaDB"
$user = "sa"
$password = "Admin00!S"

$connectionString = "Server=$server;Database=$database;User Id=$user;Password=$password;TrustServerCertificate=True;"

$sql = @"
-- Önce mevcut durumu kontrol et
SELECT 'SalesOrders - Before' as TableName, BelgeSeri, COUNT(*) as Count 
FROM SalesOrders 
WHERE BelgeSeri IN ('A', 'EFA2025')
GROUP BY BelgeSeri;

SELECT 'OrderMappings - Before' as TableName, BelgeSeri, COUNT(*) as Count 
FROM OrderMappings 
WHERE BelgeSeri IN ('A', 'EFA2025')
GROUP BY BelgeSeri;

-- SalesOrders tablosunu güncelle
UPDATE SalesOrders SET BelgeSeri = 'EFA2025' WHERE BelgeSeri = 'A';
PRINT 'SalesOrders updated: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

-- OrderMappings tablosunu güncelle
UPDATE OrderMappings SET BelgeSeri = 'EFA2025' WHERE BelgeSeri = 'A';
PRINT 'OrderMappings updated: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

-- Sonucu kontrol et
SELECT 'SalesOrders - After' as TableName, BelgeSeri, COUNT(*) as Count 
FROM SalesOrders 
WHERE BelgeSeri IN ('A', 'EFA2025')
GROUP BY BelgeSeri;

SELECT 'OrderMappings - After' as TableName, BelgeSeri, COUNT(*) as Count 
FROM OrderMappings 
WHERE BelgeSeri IN ('A', 'EFA2025')
GROUP BY BelgeSeri;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 60
    
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset) | Out-Null
    
    foreach ($table in $dataset.Tables) {
        if ($table.Rows.Count -gt 0) {
            $table | Format-Table -AutoSize
        }
    }
    
    Write-Host "`nBelgeSeri update completed successfully!" -ForegroundColor Green
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
