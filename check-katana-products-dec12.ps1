# Katana API'den 12 Aralık tarihindeki ürünleri kontrol et
$katanaApiKey = "ed8c38d1-4015-45e5-9c28-381d3fe148b6"
$katanaBaseUrl = "https://api.katanamrp.com/v1"

$headers = @{
    "X-Api-Key" = $katanaApiKey
    "Accept" = "application/json"
}

Write-Host "Katana API'den ürünler çekiliyor..." -ForegroundColor Cyan

try {
    # İlk 100 ürünü çek
    $response = Invoke-RestMethod -Uri "$katanaBaseUrl/products?limit=100" -Headers $headers -Method Get
    
    Write-Host "`nToplam ürün sayısı: $($response.count)" -ForegroundColor Green
    
    # 12 Aralık tarihinde oluşturulan veya güncellenen ürünleri filtrele
    $dec12Products = $response.data | Where-Object {
        $createdDate = [DateTime]::Parse($_.created_at)
        $updatedDate = [DateTime]::Parse($_.updated_at)
        ($createdDate.Date -eq [DateTime]::Parse("2024-12-12").Date) -or 
        ($updatedDate.Date -eq [DateTime]::Parse("2024-12-12").Date)
    }
    
    Write-Host "`n12 Aralık tarihli ürün sayısı: $($dec12Products.Count)" -ForegroundColor Yellow
    
    if ($dec12Products.Count -gt 0) {
        Write-Host "`n12 Aralık Tarihli Ürünler:" -ForegroundColor Cyan
        Write-Host "=" * 80
        
        foreach ($product in $dec12Products) {
            Write-Host "`nÜrün ID: $($product.id)" -ForegroundColor White
            Write-Host "SKU: $($product.sku)" -ForegroundColor White
            Write-Host "İsim: $($product.name)" -ForegroundColor White
            Write-Host "Oluşturulma: $($product.created_at)" -ForegroundColor Gray
            Write-Host "Güncellenme: $($product.updated_at)" -ForegroundColor Gray
            Write-Host "Stok: $($product.in_stock)" -ForegroundColor Green
            Write-Host "-" * 80
        }
        
        # JSON olarak kaydet
        $dec12Products | ConvertTo-Json -Depth 10 | Out-File "katana-products-dec12.json" -Encoding UTF8
        Write-Host "`nÜrünler 'katana-products-dec12.json' dosyasına kaydedildi." -ForegroundColor Green
    }
    
    # Tüm ürünleri de göster (özet)
    Write-Host "`n`nTüm Ürünler (İlk 20):" -ForegroundColor Cyan
    Write-Host "=" * 80
    
    $response.data | Select-Object -First 20 | ForEach-Object {
        Write-Host "ID: $($_.id) | SKU: $($_.sku) | İsim: $($_.name) | Stok: $($_.in_stock)" -ForegroundColor White
    }
    
    # Tüm ürünleri JSON olarak kaydet
    $response.data | ConvertTo-Json -Depth 10 | Out-File "katana-all-products.json" -Encoding UTF8
    Write-Host "`nTüm ürünler 'katana-all-products.json' dosyasına kaydedildi." -ForegroundColor Green
    
} catch {
    Write-Host "`nHata oluştu: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Detay: $($_.Exception)" -ForegroundColor Red
}
