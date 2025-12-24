# Veritabanından Katana ürünlerini kontrol et
$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Admin00!S;TrustServerCertificate=True;"

Write-Host "Veritabanından Katana ürünleri kontrol ediliyor..." -ForegroundColor Cyan

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    # Tüm ürünleri say
    $countQuery = "SELECT COUNT(*) as TotalCount FROM Products WHERE katana_product_id IS NOT NULL"
    $countCommand = New-Object System.Data.SqlClient.SqlCommand($countQuery, $connection)
    $totalCount = $countCommand.ExecuteScalar()
    
    Write-Host "`nKatana'dan senkronize edilmiş toplam ürün: $totalCount" -ForegroundColor Green
    
    # 12 Aralık tarihli ürünleri kontrol et
    $dec12Query = @"
SELECT 
    Id,
    katana_product_id as KatanaProductId,
    SKU,
    Name,
    Stock,
    CreatedAt,
    UpdatedAt
FROM Products 
WHERE katana_product_id IS NOT NULL
  AND (
    CAST(CreatedAt AS DATE) = '2024-12-12' OR 
    CAST(UpdatedAt AS DATE) = '2024-12-12'
  )
ORDER BY UpdatedAt DESC
"@
    
    $dec12Command = New-Object System.Data.SqlClient.SqlCommand($dec12Query, $connection)
    $dec12Reader = $dec12Command.ExecuteReader()
    
    $dec12Products = @()
    while ($dec12Reader.Read()) {
        $dec12Products += [PSCustomObject]@{
            Id = $dec12Reader["Id"]
            KatanaProductId = $dec12Reader["KatanaProductId"]
            SKU = $dec12Reader["SKU"]
            Name = $dec12Reader["Name"]
            Stock = $dec12Reader["Stock"]
            CreatedAt = $dec12Reader["CreatedAt"]
            UpdatedAt = $dec12Reader["UpdatedAt"]
        }
    }
    $dec12Reader.Close()
    
    Write-Host "`n12 Aralık tarihli ürün sayısı: $($dec12Products.Count)" -ForegroundColor Yellow
    
    if ($dec12Products.Count -gt 0) {
        Write-Host "`n12 Aralık Tarihli Ürünler:" -ForegroundColor Cyan
        Write-Host "=" * 100
        
        foreach ($product in $dec12Products) {
            Write-Host "`nDB ID: $($product.Id) | Katana ID: $($product.KatanaProductId)" -ForegroundColor White
            Write-Host "SKU: $($product.SKU)" -ForegroundColor White
            Write-Host "İsim: $($product.Name)" -ForegroundColor White
            Write-Host "Stok: $($product.Stock)" -ForegroundColor Green
            Write-Host "Oluşturulma: $($product.CreatedAt)" -ForegroundColor Gray
            Write-Host "Güncellenme: $($product.UpdatedAt)" -ForegroundColor Gray
            Write-Host "-" * 100
        }
        
        # JSON olarak kaydet
        $dec12Products | ConvertTo-Json -Depth 10 | Out-File "db-katana-products-dec12.json" -Encoding UTF8
        Write-Host "`nÜrünler 'db-katana-products-dec12.json' dosyasına kaydedildi." -ForegroundColor Green
    }
    
    # Son güncellenen 20 ürünü göster
    $recentQuery = @"
SELECT TOP 20
    Id,
    katana_product_id as KatanaProductId,
    SKU,
    Name,
    Stock,
    UpdatedAt
FROM Products 
WHERE katana_product_id IS NOT NULL
ORDER BY UpdatedAt DESC
"@
    
    $recentCommand = New-Object System.Data.SqlClient.SqlCommand($recentQuery, $connection)
    $recentReader = $recentCommand.ExecuteReader()
    
    Write-Host "`n`nSon Güncellenen 20 Ürün:" -ForegroundColor Cyan
    Write-Host "=" * 100
    
    while ($recentReader.Read()) {
        $updatedAt = $recentReader["UpdatedAt"]
        Write-Host "ID: $($recentReader['Id']) | Katana: $($recentReader['KatanaProductId']) | SKU: $($recentReader['SKU']) | Stok: $($recentReader['Stock']) | Güncelleme: $updatedAt" -ForegroundColor White
    }
    $recentReader.Close()
    
    $connection.Close()
    
} catch {
    Write-Host "`nHata oluştu: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
