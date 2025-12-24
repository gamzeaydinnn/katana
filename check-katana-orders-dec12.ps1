# 12 Aralık 2025 tarihli Katana siparişlerini ve ürünlerini kontrol et
$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Admin00!S;TrustServerCertificate=True;"

Write-Host "12 Aralık 2025 tarihli Katana siparişleri kontrol ediliyor..." -ForegroundColor Cyan

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    # 12 Aralık 2025 tarihli siparişleri kontrol et
    $ordersQuery = @"
SELECT 
    Id,
    OrderNo,
    KatanaOrderId,
    CustomerId,
    Status,
    TotalAmount,
    Currency,
    OrderDate,
    CreatedAt,
    UpdatedAt
FROM SalesOrders 
WHERE CAST(OrderDate AS DATE) = '2025-12-12' 
   OR CAST(CreatedAt AS DATE) = '2025-12-12'
   OR CAST(UpdatedAt AS DATE) = '2025-12-12'
ORDER BY OrderDate DESC
"@
    
    $ordersCommand = New-Object System.Data.SqlClient.SqlCommand($ordersQuery, $connection)
    $ordersReader = $ordersCommand.ExecuteReader()
    
    $orders = @()
    while ($ordersReader.Read()) {
        $orders += [PSCustomObject]@{
            Id = $ordersReader["Id"]
            OrderNo = $ordersReader["OrderNo"]
            KatanaOrderId = if ($ordersReader["KatanaOrderId"] -is [DBNull]) { $null } else { $ordersReader["KatanaOrderId"] }
            CustomerId = if ($ordersReader["CustomerId"] -is [DBNull]) { $null } else { $ordersReader["CustomerId"] }
            Status = $ordersReader["Status"]
            TotalAmount = $ordersReader["TotalAmount"]
            Currency = $ordersReader["Currency"]
            OrderDate = $ordersReader["OrderDate"]
            CreatedAt = $ordersReader["CreatedAt"]
            UpdatedAt = $ordersReader["UpdatedAt"]
        }
    }
    $ordersReader.Close()
    
    Write-Host "`n12 Aralık 2025 tarihli sipariş sayısı: $($orders.Count)" -ForegroundColor Yellow
    
    if ($orders.Count -gt 0) {
        Write-Host "`n12 Aralık 2025 Tarihli Siparişler:" -ForegroundColor Cyan
        Write-Host "=" * 120
        
        foreach ($order in $orders) {
            Write-Host "`nSipariş ID: $($order.Id)" -ForegroundColor White
            Write-Host "Sipariş No: $($order.OrderNo)" -ForegroundColor White
            if ($order.KatanaOrderId) {
                Write-Host "Katana Order ID: $($order.KatanaOrderId)" -ForegroundColor Magenta
            }
            Write-Host "Durum: $($order.Status)" -ForegroundColor Green
            Write-Host "Tutar: $($order.TotalAmount) $($order.Currency)" -ForegroundColor Green
            Write-Host "Sipariş Tarihi: $($order.OrderDate)" -ForegroundColor Gray
            Write-Host "Oluşturulma: $($order.CreatedAt)" -ForegroundColor Gray
            Write-Host "-" * 120
            
            # Bu siparişe ait ürünleri getir
            $linesQuery = @"
SELECT 
    Id,
    SalesOrderId,
    SKU,
    ProductName,
    Quantity,
    PricePerUnit,
    Total,
    TaxRate,
    KatanaRowId,
    VariantId
FROM SalesOrderLines 
WHERE SalesOrderId = @OrderId
"@
            
            $linesCommand = New-Object System.Data.SqlClient.SqlCommand($linesQuery, $connection)
            $linesCommand.Parameters.AddWithValue("@OrderId", $order.Id) | Out-Null
            $linesReader = $linesCommand.ExecuteReader()
            
            $lineCount = 0
            Write-Host "  Sipariş Kalemleri:" -ForegroundColor Cyan
            while ($linesReader.Read()) {
                $lineCount++
                $variantId = if ($linesReader["VariantId"] -is [DBNull]) { "N/A" } else { $linesReader["VariantId"] }
                $katanaRowId = if ($linesReader["KatanaRowId"] -is [DBNull]) { "N/A" } else { $linesReader["KatanaRowId"] }
                
                Write-Host "    $lineCount. SKU: $($linesReader['SKU']) | Ürün: $($linesReader['ProductName'])" -ForegroundColor White
                Write-Host "       Miktar: $($linesReader['Quantity']) | Birim Fiyat: $($linesReader['PricePerUnit']) | Toplam: $($linesReader['Total'])" -ForegroundColor Gray
                Write-Host "       Variant ID: $variantId | Katana Row ID: $katanaRowId" -ForegroundColor DarkGray
            }
            $linesReader.Close()
            
            if ($lineCount -eq 0) {
                Write-Host "    (Sipariş kalemi bulunamadı)" -ForegroundColor DarkGray
            }
        }
        
        # JSON olarak kaydet
        $orders | ConvertTo-Json -Depth 10 | Out-File "katana-orders-dec12-2025.json" -Encoding UTF8
        Write-Host "`nSiparişler 'katana-orders-dec12-2025.json' dosyasına kaydedildi." -ForegroundColor Green
    } else {
        Write-Host "`n12 Aralık 2025 tarihli sipariş bulunamadı." -ForegroundColor Yellow
        
        # En son siparişleri göster
        Write-Host "`nEn Son 10 Sipariş:" -ForegroundColor Cyan
        Write-Host "=" * 120
        
        $recentOrdersQuery = @"
SELECT TOP 10
    Id,
    OrderNo,
    KatanaOrderId,
    Status,
    TotalAmount,
    Currency,
    OrderDate,
    CreatedAt
FROM SalesOrders 
ORDER BY OrderDate DESC, CreatedAt DESC
"@
        
        $recentCommand = New-Object System.Data.SqlClient.SqlCommand($recentOrdersQuery, $connection)
        $recentReader = $recentCommand.ExecuteReader()
        
        while ($recentReader.Read()) {
            $katanaId = if ($recentReader["KatanaOrderId"] -is [DBNull]) { "N/A" } else { $recentReader["KatanaOrderId"] }
            Write-Host "ID: $($recentReader['Id']) | No: $($recentReader['OrderNo']) | Katana: $katanaId | Durum: $($recentReader['Status']) | Tutar: $($recentReader['TotalAmount']) | Tarih: $($recentReader['OrderDate'])" -ForegroundColor White
        }
        $recentReader.Close()
    }
    
    # Katana Order ID'si olan ürünleri kontrol et
    Write-Host "`n`nKatana Order ID'si Olan Ürünler (12 Aralık 2025):" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    $productsQuery = @"
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
WHERE katana_order_id IS NOT NULL
  AND (
    CAST(CreatedAt AS DATE) = '2025-12-12' OR 
    CAST(UpdatedAt AS DATE) = '2025-12-12'
  )
ORDER BY UpdatedAt DESC
"@
    
    $productsCommand = New-Object System.Data.SqlClient.SqlCommand($productsQuery, $connection)
    $productsReader = $productsCommand.ExecuteReader()
    
    $productCount = 0
    $products = @()
    while ($productsReader.Read()) {
        $productCount++
        $product = [PSCustomObject]@{
            Id = $productsReader["Id"]
            KatanaProductId = if ($productsReader["KatanaProductId"] -is [DBNull]) { $null } else { $productsReader["KatanaProductId"] }
            KatanaOrderId = $productsReader["KatanaOrderId"]
            SKU = $productsReader["SKU"]
            Name = $productsReader["Name"]
            Stock = $productsReader["Stock"]
            Price = $productsReader["Price"]
            CreatedAt = $productsReader["CreatedAt"]
            UpdatedAt = $productsReader["UpdatedAt"]
        }
        $products += $product
        
        Write-Host "`n$productCount. DB ID: $($product.Id)" -ForegroundColor White
        Write-Host "   Katana Order ID: $($product.KatanaOrderId)" -ForegroundColor Magenta
        if ($product.KatanaProductId) {
            Write-Host "   Katana Product ID: $($product.KatanaProductId)" -ForegroundColor Magenta
        }
        Write-Host "   SKU: $($product.SKU)" -ForegroundColor White
        Write-Host "   İsim: $($product.Name)" -ForegroundColor White
        Write-Host "   Stok: $($product.Stock) | Fiyat: $($product.Price)" -ForegroundColor Green
        Write-Host "   Oluşturulma: $($product.CreatedAt)" -ForegroundColor Gray
        Write-Host "   Güncellenme: $($product.UpdatedAt)" -ForegroundColor Gray
    }
    $productsReader.Close()
    
    if ($productCount -eq 0) {
        Write-Host "12 Aralık 2025 tarihli Katana Order ID'li ürün bulunamadı." -ForegroundColor Yellow
    } else {
        $products | ConvertTo-Json -Depth 10 | Out-File "katana-order-products-dec12-2025.json" -Encoding UTF8
        Write-Host "`nÜrünler 'katana-order-products-dec12-2025.json' dosyasına kaydedildi." -ForegroundColor Green
    }
    
    $connection.Close()
    
} catch {
    Write-Host "`nHata oluştu: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
