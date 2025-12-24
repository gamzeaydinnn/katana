# Katana API'den gelen siparişlerdeki ürünleri analiz et
$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Admin00!S;TrustServerCertificate=True;"

Write-Host "Katana API siparişlerinden gelen ürünler analiz ediliyor..." -ForegroundColor Cyan

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    # Toplam sipariş sayısı
    $orderCountQuery = "SELECT COUNT(*) FROM SalesOrders WHERE KatanaOrderId IS NOT NULL"
    $orderCountCmd = New-Object System.Data.SqlClient.SqlCommand($orderCountQuery, $connection)
    $orderCount = $orderCountCmd.ExecuteScalar()
    
    Write-Host "`nKatana'dan gelen toplam sipariş: $orderCount" -ForegroundColor Green
    
    # Siparişlerdeki ürünleri analiz et
    $analysisQuery = @"
SELECT 
    so.Id as OrderId,
    so.OrderNo,
    so.KatanaOrderId,
    so.Status,
    so.OrderCreatedDate,
    sol.Id as LineId,
    sol.SKU,
    sol.ProductName,
    sol.Quantity,
    sol.PricePerUnit,
    sol.Total,
    sol.KatanaRowId,
    sol.VariantId,
    p.Id as ProductDbId,
    p.katana_product_id as KatanaProductId,
    p.katana_order_id as KatanaOrderIdInProduct
FROM SalesOrders so
INNER JOIN SalesOrderLines sol ON so.Id = sol.SalesOrderId
LEFT JOIN Products p ON sol.SKU = p.SKU
WHERE so.KatanaOrderId IS NOT NULL
ORDER BY so.OrderCreatedDate DESC, so.Id DESC
"@
    
    $analysisCmd = New-Object System.Data.SqlClient.SqlCommand($analysisQuery, $connection)
    $reader = $analysisCmd.ExecuteReader()
    
    $orderProducts = @{}
    $allProducts = @()
    $uniqueSkus = @{}
    
    while ($reader.Read()) {
        $orderId = $reader["OrderId"]
        $sku = $reader["SKU"]
        
        $product = [PSCustomObject]@{
            OrderId = $orderId
            OrderNo = $reader["OrderNo"]
            KatanaOrderId = $reader["KatanaOrderId"]
            OrderStatus = $reader["Status"]
            OrderDate = if ($reader["OrderCreatedDate"] -is [DBNull]) { $null } else { $reader["OrderCreatedDate"] }
            LineId = $reader["LineId"]
            SKU = $sku
            ProductName = $reader["ProductName"]
            Quantity = $reader["Quantity"]
            PricePerUnit = $reader["PricePerUnit"]
            Total = $reader["Total"]
            KatanaRowId = if ($reader["KatanaRowId"] -is [DBNull]) { $null } else { $reader["KatanaRowId"] }
            VariantId = if ($reader["VariantId"] -is [DBNull]) { $null } else { $reader["VariantId"] }
            ProductDbId = if ($reader["ProductDbId"] -is [DBNull]) { $null } else { $reader["ProductDbId"] }
            KatanaProductId = if ($reader["KatanaProductId"] -is [DBNull]) { $null } else { $reader["KatanaProductId"] }
            KatanaOrderIdInProduct = if ($reader["KatanaOrderIdInProduct"] -is [DBNull]) { $null } else { $reader["KatanaOrderIdInProduct"] }
        }
        
        $allProducts += $product
        
        if (-not $orderProducts.ContainsKey($orderId)) {
            $orderProducts[$orderId] = @()
        }
        $orderProducts[$orderId] += $product
        
        if (-not $uniqueSkus.ContainsKey($sku)) {
            $uniqueSkus[$sku] = @{
                SKU = $sku
                ProductName = $reader["ProductName"]
                OrderCount = 0
                TotalQuantity = 0
                InProductsTable = $reader["ProductDbId"] -isnot [DBNull]
                ProductDbId = if ($reader["ProductDbId"] -is [DBNull]) { $null } else { $reader["ProductDbId"] }
            }
        }
        $uniqueSkus[$sku].OrderCount++
        $uniqueSkus[$sku].TotalQuantity += $reader["Quantity"]
    }
    $reader.Close()
    
    Write-Host "`n" + "=" * 120
    Write-Host "KATANA SİPARİŞLERİNDEN GELEN ÜRÜNLER ANALİZİ" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    Write-Host "`nToplam sipariş satırı: $($allProducts.Count)" -ForegroundColor Yellow
    Write-Host "Benzersiz SKU sayısı: $($uniqueSkus.Count)" -ForegroundColor Yellow
    Write-Host "Sipariş sayısı: $($orderProducts.Count)" -ForegroundColor Yellow
    
    # Benzersiz SKU'ları göster
    Write-Host "`n`nBENZERSİZ ÜRÜNLER (SKU Bazında):" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    $skuList = $uniqueSkus.Values | Sort-Object -Property TotalQuantity -Descending
    $counter = 0
    foreach ($sku in $skuList) {
        $counter++
        $inDb = if ($sku.InProductsTable) { "[VAR] Products tablosunda" } else { "[YOK] Products tablosunda" }
        Write-Host "`n$counter. SKU: $($sku.SKU)" -ForegroundColor White
        Write-Host "   Isim: $($sku.ProductName)" -ForegroundColor Gray
        Write-Host "   Siparis Sayisi: $($sku.OrderCount) | Toplam Miktar: $($sku.TotalQuantity)" -ForegroundColor Green
        Write-Host "   Durum: $inDb" -ForegroundColor $(if ($sku.InProductsTable) { "Green" } else { "Red" })
        if ($sku.ProductDbId) {
            Write-Host "   Product DB ID: $($sku.ProductDbId)" -ForegroundColor DarkGray
        }
    }
    
    # Siparişleri göster
    Write-Host "`n`n" + "=" * 120
    Write-Host "SİPARİŞLER VE ÜRÜN DETAYLARI:" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    $orderCounter = 0
    foreach ($orderId in ($orderProducts.Keys | Sort-Object -Descending)) {
        $orderCounter++
        $orderLines = $orderProducts[$orderId]
        $firstLine = $orderLines[0]
        
        Write-Host "`n$orderCounter. SİPARİŞ" -ForegroundColor Yellow
        Write-Host "   Sipariş No: $($firstLine.OrderNo)" -ForegroundColor White
        Write-Host "   Katana Order ID: $($firstLine.KatanaOrderId)" -ForegroundColor Magenta
        Write-Host "   Durum: $($firstLine.OrderStatus)" -ForegroundColor Green
        Write-Host "   Tarih: $($firstLine.OrderDate)" -ForegroundColor Gray
        Write-Host "   Ürün Sayısı: $($orderLines.Count)" -ForegroundColor Cyan
        
        foreach ($line in $orderLines) {
            Write-Host "      - SKU: $($line.SKU) | $($line.ProductName)" -ForegroundColor White
            Write-Host "        Miktar: $($line.Quantity) | Birim Fiyat: $($line.PricePerUnit) | Toplam: $($line.Total)" -ForegroundColor Gray
            if ($line.VariantId) {
                Write-Host "        Variant ID: $($line.VariantId)" -ForegroundColor DarkGray
            }
            if ($line.ProductDbId) {
                Write-Host "        [VAR] Products tablosunda mevcut (ID: $($line.ProductDbId))" -ForegroundColor Green
            } else {
                Write-Host "        [YOK] Products tablosunda YOK" -ForegroundColor Red
            }
        }
    }
    
    # İstatistikler
    Write-Host "`n`n" + "=" * 120
    Write-Host "İSTATİSTİKLER:" -ForegroundColor Cyan
    Write-Host "=" * 120
    
    $productsInDb = ($uniqueSkus.Values | Where-Object { $_.InProductsTable }).Count
    $productsNotInDb = ($uniqueSkus.Values | Where-Object { -not $_.InProductsTable }).Count
    
    Write-Host "`nProducts tablosunda OLAN ürünler: $productsInDb" -ForegroundColor Green
    Write-Host "Products tablosunda OLMAYAN ürünler: $productsNotInDb" -ForegroundColor Red
    Write-Host "Toplam benzersiz SKU: $($uniqueSkus.Count)" -ForegroundColor Yellow
    
    # JSON olarak kaydet
    $analysisResult = @{
        Summary = @{
            TotalOrders = $orderProducts.Count
            TotalOrderLines = $allProducts.Count
            UniqueSkus = $uniqueSkus.Count
            ProductsInDb = $productsInDb
            ProductsNotInDb = $productsNotInDb
        }
        UniqueProducts = $skuList
        Orders = $orderProducts
        AllOrderLines = $allProducts
    }
    
    $analysisResult | ConvertTo-Json -Depth 10 | Out-File "katana-order-products-analysis.json" -Encoding UTF8
    Write-Host "`nDetaylı analiz 'katana-order-products-analysis.json' dosyasına kaydedildi." -ForegroundColor Green
    
    # TXT dosyası oluştur
    $txtContent = @"
================================================================================
KATANA SİPARİŞLERİNDEN GELEN ÜRÜNLER - DETAYLI RAPOR
================================================================================
Oluşturulma Tarihi: $(Get-Date -Format "dd.MM.yyyy HH:mm:ss")

ÖZET İSTATİSTİKLER:
- Toplam Sipariş Sayısı: $($orderProducts.Count)
- Toplam Sipariş Satırı: $($allProducts.Count)
- Benzersiz SKU Sayısı: $($uniqueSkus.Count)
- Products Tablosunda Olan: $productsInDb
- Products Tablosunda Olmayan: $productsNotInDb

================================================================================
BENZERSIZ ÜRÜNLER (SKU BAZINDA)
================================================================================

"@
    
    $counter = 0
    foreach ($sku in $skuList) {
        $counter++
        $inDb = if ($sku.InProductsTable) { "VAR" } else { "YOK" }
        $txtContent += @"
$counter. SKU: $($sku.SKU)
   İsim: $($sku.ProductName)
   Sipariş Sayısı: $($sku.OrderCount)
   Toplam Miktar: $($sku.TotalQuantity)
   Products Tablosunda: $inDb
   $(if ($sku.ProductDbId) { "Product DB ID: $($sku.ProductDbId)" } else { "" })

"@
    }
    
    $txtContent += @"

================================================================================
SİPARİŞLER VE ÜRÜN DETAYLARI
================================================================================

"@
    
    $orderCounter = 0
    foreach ($orderId in ($orderProducts.Keys | Sort-Object -Descending)) {
        $orderCounter++
        $orderLines = $orderProducts[$orderId]
        $firstLine = $orderLines[0]
        
        $txtContent += @"
$orderCounter. SİPARİŞ
   Sipariş No: $($firstLine.OrderNo)
   Katana Order ID: $($firstLine.KatanaOrderId)
   Durum: $($firstLine.OrderStatus)
   Tarih: $($firstLine.OrderDate)
   Ürün Sayısı: $($orderLines.Count)

   Ürünler:
"@
        
        foreach ($line in $orderLines) {
            $inDbText = if ($line.ProductDbId) { "Products tablosunda VAR (ID: $($line.ProductDbId))" } else { "Products tablosunda YOK" }
            $txtContent += @"
      • SKU: $($line.SKU)
        İsim: $($line.ProductName)
        Miktar: $($line.Quantity) | Birim Fiyat: $($line.PricePerUnit) | Toplam: $($line.Total)
        $(if ($line.VariantId) { "Variant ID: $($line.VariantId)" } else { "" })
        Durum: $inDbText

"@
        }
        
        $txtContent += "`n"
    }
    
    $txtContent += @"
================================================================================
RAPOR SONU
================================================================================
"@
    
    $txtContent | Out-File "katana-order-products-report.txt" -Encoding UTF8
    Write-Host "TXT rapor 'katana-order-products-report.txt' dosyasına kaydedildi." -ForegroundColor Green
    
    $connection.Close()
    
} catch {
    Write-Host "`nHata oluştu: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
