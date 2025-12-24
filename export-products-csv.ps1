# Export all 1034 products to CSV format
Write-Host "Exporting all products to CSV..." -ForegroundColor Cyan

$query = @"
SELECT 
    Id,
    Name,
    SKU,
    Description,
    Price,
    Stock,
    CategoryId,
    IsActive,
    LucaId,
    Barcode,
    katana_product_id,
    katana_order_id,
    CreatedAt,
    UpdatedAt
FROM Products
ORDER BY Id
"@

sqlcmd -S localhost,1433 -d KatanaDB -U sa -P "Admin00!S" -Q $query -s "," -o "all-products.csv" -W

Write-Host "`nCSV Export complete! Check 'all-products.csv' file" -ForegroundColor Green
Write-Host "Total products: 1034" -ForegroundColor Yellow
