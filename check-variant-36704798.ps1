# Check Variant 36704798 Details
Write-Host "=== Checking Variant 36704798 ===" -ForegroundColor Cyan

$server = "localhost,1433"
$database = "KatanaDB"
$user = "sa"
$password = "Admin00!S"

$connectionString = "Server=$server;Database=$database;User Id=$user;Password=$password;TrustServerCertificate=True;"

$sql = @"
-- Check if this variant exists in ProductVariants
SELECT * FROM ProductVariants WHERE Id = 36704798;

-- Check VariantMappings for this variant
SELECT * FROM VariantMappings WHERE KatanaVariantId = 36704798;

-- Check all VariantMappings
SELECT TOP 10 * FROM VariantMappings ORDER BY Id DESC;

-- Check Products that might match
SELECT TOP 10 Id, Name, SKU, LucaId, katana_product_id FROM Products ORDER BY Id DESC;

-- Check if there's a product with katana_product_id matching
SELECT * FROM Products WHERE katana_product_id = 36704798;
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
    
    Write-Host "`n=== ProductVariants (Id=36704798) ===" -ForegroundColor Yellow
    if ($dataset.Tables[0].Rows.Count -gt 0) {
        $dataset.Tables[0] | Format-Table -AutoSize
    } else {
        Write-Host "No ProductVariants found for Id=36704798" -ForegroundColor Gray
    }
    
    Write-Host "`n=== VariantMappings for KatanaVariantId=36704798 ===" -ForegroundColor Yellow
    if ($dataset.Tables[1].Rows.Count -gt 0) {
        $dataset.Tables[1] | Format-Table -AutoSize
    } else {
        Write-Host "No VariantMappings found for 36704798" -ForegroundColor Gray
    }
    
    Write-Host "`n=== Recent VariantMappings ===" -ForegroundColor Yellow
    $dataset.Tables[2] | Format-Table -AutoSize
    
    Write-Host "`n=== Recent Products ===" -ForegroundColor Yellow
    $dataset.Tables[3] | Format-Table -AutoSize
    
    Write-Host "`n=== Products with katana_product_id=36704798 ===" -ForegroundColor Yellow
    if ($dataset.Tables[4].Rows.Count -gt 0) {
        $dataset.Tables[4] | Format-Table -AutoSize
    } else {
        Write-Host "No Products found with katana_product_id=36704798" -ForegroundColor Gray
    }
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
