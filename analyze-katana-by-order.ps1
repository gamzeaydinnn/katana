# Katana ürünlerini sipariş bazında analiz et
# Veritabanından mapping bilgilerini kullanır

$baseUrl = "http://localhost:5055"
$username = "admin"
$password = "Katana2025!"

Write-Host "=== Katana Ürün - Sipariş Analizi ===" -ForegroundColor Cyan
Write-Host ""

# Giriş yap
Write-Host "Giriş yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Giriş başarılı" -ForegroundColor Green
Write-Host ""

# Veritabanından sipariş-ürün mapping'lerini çek
Write-Host "Sipariş-ürün mapping'leri çekiliyor..." -ForegroundColor Yellow

try {
    # Önce cleanup analysis endpoint'ini deneyelim
    $analysisData = Invoke-RestMethod -Uri "$baseUrl/api/katana-cleanup/analysis" -Method Get -Headers $headers
    
    Write-Host "Analiz verileri alındı" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "=== ANALİZ RAPORU ===" -ForegroundColor Cyan
    Write-Host "=====================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "GENEL İSTATİSTİKLER" -ForegroundColor Yellow
    Write-Host "-------------------"
    Write-Host "Onaylı Sipariş Sayısı     : $($analysisData.approvedOrdersCount)"
    Write-Host "Katana'ya Gönderilen Ürün : $($analysisData.totalProductsSent)"
    Write-Host "Benzersiz SKU Sayısı      : $($analysisData.uniqueSkuCount)"
    Write-Host ""
    
    if ($analysisData.orderProducts) {
        Write-Host "SİPARİŞ BAZINDA ÜRÜNLER" -ForegroundColor Yellow
        Write-Host "-----------------------"
        
        $orderGroups = $analysisData.orderProducts | Group-Object -Property OrderNumber
        
        foreach ($orderGroup in $orderGroups) {
            $orderNum = $orderGroup.Name
            $products = $orderGroup.Group
            
            Write-Host ""
            Write-Host "Sipariş: $orderNum ($($products.Count) ürün)" -ForegroundColor White
            
            foreach ($product in $products) {
                Write-Host "  - SKU: $($product.Sku) | Katana ID: $($product.KatanaProductId) | Ürün: $($product.ProductName)" -ForegroundColor Gray
            }
        }
    }
    
    if ($analysisData.uniqueSkus) {
        Write-Host ""
        Write-Host "BENZERSIZ SKU'LAR" -ForegroundColor Yellow
        Write-Host "-----------------"
        foreach ($sku in $analysisData.uniqueSkus) {
            Write-Host "  - $sku" -ForegroundColor Gray
        }
    }
    
    # JSON'a kaydet
    $analysisData | ConvertTo-Json -Depth 10 | Out-File -FilePath "katana-order-analysis.json" -Encoding UTF8
    Write-Host ""
    Write-Host "Rapor kaydedildi: katana-order-analysis.json" -ForegroundColor Green
}
catch {
    Write-Host "Endpoint bulunamadı, alternatif yöntem deneniyor..." -ForegroundColor Yellow
    Write-Host ""
    
    # Katana JSON'dan ve veritabanından manuel analiz
    if (-not (Test-Path "katana-all-products.json")) {
        Write-Host "HATA: katana-all-products.json bulunamadı!" -ForegroundColor Red
        Write-Host "Önce şunu çalıştırın: .\test-katana-products-direct.ps1" -ForegroundColor Yellow
        exit 1
    }
    
    $katanaProducts = Get-Content "katana-all-products.json" -Raw | ConvertFrom-Json
    
    Write-Host "Katana'dan $($katanaProducts.Count) ürün bulundu" -ForegroundColor Green
    Write-Host ""
    
    # VARIANT- ile başlayan SKU'ları bul (bunlar sipariş ürünleri)
    $variantProducts = $katanaProducts | Where-Object { $_.sku -like "VARIANT-*" }
    
    Write-Host "=== VARIANT ÜRÜN ANALİZİ ===" -ForegroundColor Cyan
    Write-Host "============================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Toplam Ürün              : $($katanaProducts.Count)"
    Write-Host "VARIANT Ürünleri         : $($variantProducts.Count)"
    Write-Host "Diğer Ürünler            : $($katanaProducts.Count - $variantProducts.Count)"
    Write-Host ""
    
    if ($variantProducts.Count -gt 0) {
        Write-Host "VARIANT ÜRÜN DETAYLARI (İlk 50)" -ForegroundColor Yellow
        Write-Host "-------------------------------"
        
        foreach ($product in ($variantProducts | Select-Object -First 50)) {
            $createdDate = if ($product.created_at) { 
                [DateTime]::Parse($product.created_at).ToString("yyyy-MM-dd HH:mm") 
            } else { 
                "Bilinmiyor" 
            }
            
            Write-Host "SKU: $($product.sku) | ID: $($product.id) | Oluşturma: $createdDate" -ForegroundColor Gray
        }
        
        if ($variantProducts.Count -gt 50) {
            Write-Host "... ve $($variantProducts.Count - 50) ürün daha" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "DİĞER ÜRÜN TİPLERİ" -ForegroundColor Yellow
    Write-Host "------------------"
    
    $otherProducts = $katanaProducts | Where-Object { $_.sku -notlike "VARIANT-*" }
    $skuPrefixes = $otherProducts | ForEach-Object { 
        if ($_.sku -match "^([A-Z]+)") { $matches[1] } else { "OTHER" }
    } | Group-Object | Sort-Object Count -Descending | Select-Object -First 10
    
    foreach ($prefix in $skuPrefixes) {
        Write-Host "  $($prefix.Name): $($prefix.Count) ürün" -ForegroundColor Gray
    }
    
    # Rapor kaydet
    $report = @{
        Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        TotalProducts = $katanaProducts.Count
        VariantProducts = $variantProducts.Count
        OtherProducts = $otherProducts.Count
        VariantProductList = $variantProducts | ForEach-Object {
            [PSCustomObject]@{
                Id = $_.id
                SKU = $_.sku
                Name = $_.name
                CreatedAt = $_.created_at
            }
        }
    }
    
    $report | ConvertTo-Json -Depth 10 | Out-File -FilePath "katana-order-analysis.json" -Encoding UTF8
    Write-Host ""
    Write-Host "Rapor kaydedildi: katana-order-analysis.json" -ForegroundColor Green
}

Write-Host ""
