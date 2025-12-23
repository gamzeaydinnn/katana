# Katana Duplicate Ürün Analizi
# Bu script Katana'daki duplicate ürünleri bulur

$baseUrl = "http://localhost:5055"
$username = "admin"
$password = "Katana2025!"

Write-Host "=== Katana Duplicate Ürün Analizi ===" -ForegroundColor Cyan
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

# Katana'dan tüm ürünleri çek
Write-Host "Katana'dan ürünler çekiliyor..." -ForegroundColor Yellow
$allProducts = Invoke-RestMethod -Uri "$baseUrl/api/katana-cleanup/all-products" -Method Get -Headers $headers

Write-Host "Toplam $($allProducts.Count) ürün bulundu" -ForegroundColor Green
Write-Host ""

# Duplicate analizi
Write-Host "Duplicate analizi yapılıyor..." -ForegroundColor Yellow

# SKU'ya göre grupla
$skuGroups = $allProducts | Group-Object -Property sku

# Duplicate SKU'ları bul (2'den fazla olan)
$duplicateSkus = $skuGroups | Where-Object { $_.Count -gt 1 }

# Order ID'ye göre grupla (sales_order_id veya katana_order_id varsa)
$orderGroups = @{}
foreach ($product in $allProducts) {
    # Katana API'den gelen ürünlerde order bilgisi olabilir
    # Şimdilik SKU bazlı duplicate'lere odaklanalım
}

Write-Host ""
Write-Host "=== DUPLICATE ANALİZ RAPORU ===" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "GENEL İSTATİSTİKLER" -ForegroundColor Yellow
Write-Host "-------------------"
Write-Host "Toplam Ürün Sayısı        : $($allProducts.Count)"
Write-Host "Benzersiz SKU Sayısı      : $($skuGroups.Count)"
Write-Host "Duplicate SKU Sayısı      : $($duplicateSkus.Count)"
Write-Host "Toplam Duplicate Ürün     : $(($duplicateSkus | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)"
Write-Host "Silinecek Ürün Sayısı     : $(($duplicateSkus | ForEach-Object { $_.Count - 1 } | Measure-Object -Sum).Sum)"
Write-Host ""

if ($duplicateSkus.Count -gt 0) {
    Write-Host "DUPLICATE SKU'LAR (İlk 20)" -ForegroundColor Yellow
    Write-Host "-------------------------"
    
    $duplicateDetails = @()
    
    foreach ($group in ($duplicateSkus | Select-Object -First 20)) {
        $sku = $group.Name
        $count = $group.Count
        $products = $group.Group
        
        Write-Host ""
        Write-Host "SKU: $sku (Tekrar: $count kez)" -ForegroundColor White
        
        foreach ($product in $products) {
            $createdDate = if ($product.created_at) { 
                [DateTime]::Parse($product.created_at).ToString("yyyy-MM-dd HH:mm") 
            } else { 
                "Bilinmiyor" 
            }
            
            Write-Host "  - ID: $($product.id) | Oluşturma: $createdDate | İsim: $($product.name)" -ForegroundColor Gray
            
            $duplicateDetails += [PSCustomObject]@{
                SKU = $sku
                KatanaProductId = $product.id
                Name = $product.name
                CreatedAt = $createdDate
                DuplicateCount = $count
            }
        }
    }
    
    if ($duplicateSkus.Count -gt 20) {
        Write-Host ""
        Write-Host "... ve $($duplicateSkus.Count - 20) SKU daha" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== SİLME STRATEJİSİ ===" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Her duplicate SKU için:" -ForegroundColor Yellow
Write-Host "  1. EN ESKİ ürün KORUNACAK (ilk oluşturulan)"
Write-Host "  2. Diğer tüm duplicate'ler SİLİNECEK"
Write-Host ""

# Silinecek ürünleri belirle
$productsToDelete = @()
$productsToKeep = @()

foreach ($group in $duplicateSkus) {
    $products = $group.Group | Sort-Object created_at
    
    # İlk ürünü koru (en eski)
    $keepProduct = $products[0]
    $productsToKeep += $keepProduct
    
    # Geri kalanları sil
    for ($i = 1; $i -lt $products.Count; $i++) {
        $productsToDelete += $products[$i]
    }
}

Write-Host "KORUNACAK ÜRÜNLER: $($productsToKeep.Count)" -ForegroundColor Green
Write-Host "SİLİNECEK ÜRÜNLER: $($productsToDelete.Count)" -ForegroundColor Red
Write-Host ""

# JSON raporu kaydet
$report = @{
    Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    TotalProducts = $allProducts.Count
    UniqueSKUs = $skuGroups.Count
    DuplicateSKUs = $duplicateSkus.Count
    ProductsToKeep = $productsToKeep.Count
    ProductsToDelete = $productsToDelete.Count
    DuplicateDetails = $duplicateDetails
    ProductsToDeleteList = $productsToDelete | ForEach-Object {
        [PSCustomObject]@{
            Id = $_.id
            SKU = $_.sku
            Name = $_.name
            CreatedAt = $_.created_at
        }
    }
    ProductsToKeepList = $productsToKeep | ForEach-Object {
        [PSCustomObject]@{
            Id = $_.id
            SKU = $_.sku
            Name = $_.name
            CreatedAt = $_.created_at
        }
    }
}

$reportJson = $report | ConvertTo-Json -Depth 10
$reportJson | Out-File -FilePath "katana-duplicate-analysis.json" -Encoding UTF8

Write-Host "Detaylı rapor kaydedildi: katana-duplicate-analysis.json" -ForegroundColor Green
Write-Host ""
Write-Host "Sonraki Adım:" -ForegroundColor Yellow
Write-Host "  Duplicate'leri silmek için: .\test-katana-duplicate-delete.ps1 -DryRun:`$false" -ForegroundColor White
Write-Host ""
