# Veritabanındaki tüm ürünleri kontrol et
$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Admin00!S;TrustServerCertificate=True;"

Write-Host "Veritabanındaki tüm ürünler kontrol ediliyor..." -ForegroundColor Cyan

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    # Tüm ürünleri say
    $countQuery = "SELECT COUNT(*) as TotalCount FROM Products"
    $countCommand = New-Object System.Data.SqlClient.SqlCommand($countQuery, $connection)
    $totalCount = $countCommand.ExecuteScalar()
    
    Write-Host "`nToplam ürün sayısı: $totalCount" -ForegroundColor Green
    
    # 12 Aralık tarihli ürünleri kontrol et
    $dec12Query = @"
SELECT 
    Id,
    katana_product_id as KatanaProductId,
    katana_order_id as KatanaOrderId,
    SKU,
    Name,
    Stock,
    Price,
    CreatedAt,
    UpdatedAt
FROM Products 
WHERE CAST(CreatedAt AS DATE) = '2024-12-12' OR CAST(UpdatedAt AS DATE) = '2024-12-12'
ORDER BY UpdatedAt DESC
"@
    
    $dec12Command = New-Object System.Data.SqlClient.SqlCommand($dec12Query, $connection)
    $dec12Reader = $dec12Command.ExecuteReader()
    
    $dec12Products = @()
    while ($dec12Reader.Read()) {
        $dec12Products += [PSCustomObject]@{
            Id = $dec12Reader["Id"]
            KatanaProductId = if ($dec12Reader["KatanaProductId"] -is [DBNull]) { $null } else { $dec12Reader["KatanaProductId"] }
            KatanaOrderId = if ($dec12Reader["KatanaOrderId"] -is [DBNull]) { $null } else { $dec12Reader["KatanaOrderId"] }
            SKU = $dec12Reader["SKU"]
            Name = $dec12Reader["Name"]
            Stock = $dec12Reader["Stock"]
            Price = $dec12Reader["Price"]
            CreatedAt = $dec12Reader["CreatedAt"]
            UpdatedAt = $dec12Reader["UpdatedAt"]
        }
    }
    $dec12Reader.Close()
    
    Write-Host "`n12 Aralık tarihli ürün sayısı: $($dec12Products.Count)" -ForegroundColor Yellow
    
    if ($dec12Products.Count -gt 0) {
        Write-Host "`n12 Aralık Tarihli Ürünler:" -ForegroundColor Cyan
        Write-Host "=" * 120
        
        foreach ($product in $dec12Products) {
            Write-Host "`nDB ID: $($product.Id)" -ForegroundColor White
            if ($product.KatanaProductId) {
                Write-Host "Katana Product ID: $($product.KatanaProductId)" -ForegroundColor Magenta
            }
            if ($product.KatanaOrderId) {
                Write-Host "Katana Order ID: $($product.KatanaOrderId)" -ForegroundColor Magenta
            }
            Write-Host "SKU: $($product.SKU)" -ForegroundColor White
            Write-Host "İsim: $($product.Name)" -ForegroundColor White
            Write-Host "Stok: $($product.Stock) | Fiyat: $($product.Price) TL" -ForegroundColor Green
            Write-Host "Oluşturulma: $($product.CreatedAt)" -ForegroundColor Gray
            Write-Host "Güncellenme: $($product.UpdatedAt)" -ForegroundColor Gray
            Write-Host "-" * 120
        }
        
        # JSON olarak kaydet
        $dec12Products | ConvertTo-Json -Depth 10 | Out-File "db-all-products-dec12.json" -Encoding UTF8
        Write-Host "`nÜrünler 'db-all-products-dec12.json' dosyasına kaydedildi." -ForegroundColor Green
    }
    
    # Son güncellenen 30 ürünü göster
    $recentQuery = @"
SELECT TOP 30
    Id,
    katana_product_id as KatanaProductId,
    katana_order_id as KatanaOrderId,
    SKU,
    Name,
    Stock,
    Price,
    CreatedAt,
    UpdatedAt
FROM Products 
ORDER BY UpdatedAt DESC
"@
    
    $recentCommand = New-Object System.Data.SqlClient.SqlCommand($recentQuery, $connection)
    $recentReader = $recentCommand.ExecuteReader()
    
    Write-Host "`n`nSon Güncellenen 30 Ürün:" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    $recentProducts = @()
    while ($recentReader.Read()) {
        $katanaId = if ($recentReader["KatanaProductId"] -is [DBNull]) { "N/A" } else { $recentReader["KatanaProductId"] }
        $katanaOrderId = if ($recentReader["KatanaOrderId"] -is [DBNull]) { "N/A" } else { $recentReader["KatanaOrderId"] }
        $updatedAt = $recentReader["UpdatedAt"]
        $createdAt = $recentReader["CreatedAt"]
        
        Write-Host "ID: $($recentReader['Id']) | Katana: $katanaId | Order: $katanaOrderId | SKU: $($recentReader['SKU']) | Stok: $($recentReader['Stock']) | Güncelleme: $updatedAt" -ForegroundColor White
        
        $recentProducts += [PSCustomObject]@{
            Id = $recentReader["Id"]
            KatanaProductId = $katanaId
            KatanaOrderId = $katanaOrderId
            SKU = $recentReader["SKU"]
            Name = $recentReader["Name"]
            Stock = $recentReader["Stock"]
            Price = $recentReader["Price"]
            CreatedAt = $createdAt
            UpdatedAt = $updatedAt
        }
    }
    $recentReader.Close()
    
    # Son ürünleri de kaydet
    $recentProducts | ConvertTo-Json -Depth 10 | Out-File "db-recent-products.json" -Encoding UTF8
    Write-Host "`nSon ürünler 'db-recent-products.json' dosyasına kaydedildi." -ForegroundColor Green
    
    $connection.Close()
    
} catch {
    Write-Host "`nHata oluştu: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
