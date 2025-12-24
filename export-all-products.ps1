# Export all 1034 products from KatanaDB
Write-Host "Exporting all products from KatanaDB..." -ForegroundColor Cyan

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

sqlcmd -S localhost,1433 -d KatanaDB -U sa -P "Admin00!S" -Q $query -o "all-products-export.txt" -W

Write-Host "`nExport complete! Check 'all-products-export.txt' file" -ForegroundColor Green
Write-Host "Total products: 1034" -ForegroundColor Yellow
