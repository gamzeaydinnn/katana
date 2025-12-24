# Check SO-79 Order Details
Write-Host "=== SO-79 Order Details ===" -ForegroundColor Cyan

$server = "localhost,1433"
$database = "KatanaDB"
$user = "sa"
$password = "Admin00!S"

$connectionString = "Server=$server;Database=$database;User Id=$user;Password=$password;TrustServerCertificate=True;"

$sql = @"
-- Order header
SELECT 
    so.Id, so.OrderNo, so.BelgeSeri, so.BelgeNo, so.Status, 
    so.CustomerId, so.CustomerRef, so.Currency, so.ConversionRate,
    so.LastSyncError
FROM SalesOrders so
WHERE so.OrderNo = 'SO-79';

-- Order lines
SELECT 
    sol.Id as LineId, sol.SalesOrderId, sol.VariantId,
    sol.SKU, sol.ProductName, sol.Quantity, sol.PricePerUnit, sol.TaxRate,
    sol.LucaStokId, sol.LucaDepoId
FROM SalesOrderLines sol
WHERE sol.SalesOrderId IN (SELECT Id FROM SalesOrders WHERE OrderNo = 'SO-79');

-- Check variant mappings for these variants
SELECT 
    vm.Id, vm.KatanaVariantId, vm.ProductId, vm.Sku,
    p.Name as ProductName, p.LucaId as ProductLucaId
FROM VariantMappings vm
LEFT JOIN Products p ON vm.ProductId = p.Id
WHERE vm.KatanaVariantId IN (
    SELECT VariantId FROM SalesOrderLines 
    WHERE SalesOrderId IN (SELECT Id FROM SalesOrders WHERE OrderNo = 'SO-79')
);

-- Check Products table
SELECT TOP 5 Id, Name, LucaId, katana_product_id FROM Products WHERE LucaId IS NOT NULL;
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
    
    Write-Host "`n=== Order Header ===" -ForegroundColor Yellow
    $dataset.Tables[0] | Format-Table -AutoSize
    
    Write-Host "`n=== Order Lines ===" -ForegroundColor Yellow
    $dataset.Tables[1] | Format-Table -AutoSize
    
    Write-Host "`n=== Variant Mappings ===" -ForegroundColor Yellow
    if ($dataset.Tables[2].Rows.Count -gt 0) {
        $dataset.Tables[2] | Format-Table -AutoSize
    } else {
        Write-Host "No variant mappings found" -ForegroundColor Gray
    }
    
    Write-Host "`n=== Sample Products with LucaId ===" -ForegroundColor Yellow
    $dataset.Tables[3] | Format-Table -AutoSize
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
