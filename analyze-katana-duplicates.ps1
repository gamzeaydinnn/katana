# Katana JSON dosyasından duplicate analizi
# katana-all-products.json dosyasını kullanır

Write-Host "=== Katana Duplicate Analizi (JSON'dan) ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "katana-all-products.json")) {
    Write-Host "HATA: katana-all-products.json bulunamadı!" -ForegroundColor Red
    Write-Host "Önce şunu çalıştırın: .\test-katana-products-direct.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "JSON dosyası okunuyor..." -ForegroundColor Yellow
$allProducts = Get-Content "katana-all-products.json" -Raw | ConvertFrom-Json

Write-Host "Toplam $($allProducts.Count) ürün bulundu" -ForegroundColor Green
Write-Host ""

# Duplicate analizi
Write-Host "Duplicate analizi yapılıyor..." -ForegroundColor Yellow

# SKU'ya göre grupla
$skuGroups = $allProducts | Group-Object -Property sku

# Duplicate SKU'ları bul (2'den fazla olan)
$duplicateSkus = $skuGroups | Where-Object { $_.Count -gt 1 }

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
    Write-Host "DUPLICATE SKU'LAR (İlk 30)" -ForegroundColor Yellow
    Write-Host "-------------------------"
    
    $duplicateDetails = @()
    
    foreach ($group in ($duplicateSkus | Select-Object -First 30)) {
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
    
    if ($duplicateSkus.Count -gt 30) {
        Write-Host ""
        Write-Host "... ve $($duplicateSkus.Count - 30) SKU daha" -ForegroundColor Gray
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

# En çok tekrar eden SKU'ları göster
Write-Host "EN ÇOK TEKRAR EDEN SKU'LAR (Top 10)" -ForegroundColor Yellow
Write-Host "-----------------------------------"
$topDuplicates = $duplicateSkus | Sort-Object Count -Descending | Select-Object -First 10
foreach ($dup in $topDuplicates) {
    Write-Host "  $($dup.Name): $($dup.Count) kez" -ForegroundColor White
}
Write-Host ""

# JSON raporu kaydet
$report = @{
    Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    TotalProducts = $allProducts.Count
    UniqueSKUs = $skuGroups.Count
    DuplicateSKUs = $duplicateSkus.Count
    ProductsToKeep = $productsToKeep.Count
    ProductsToDelete = $productsToDelete.Count
    TopDuplicates = $topDuplicates | ForEach-Object {
        [PSCustomObject]@{
            SKU = $_.Name
            Count = $_.Count
        }
    }
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
$reportJson | Out-File -FilePath "katana-duplicate-analysis-report.json" -Encoding UTF8

Write-Host "Detaylı rapor kaydedildi: katana-duplicate-analysis-report.json" -ForegroundColor Green
Write-Host ""
Write-Host "Sonraki Adım:" -ForegroundColor Yellow
Write-Host "  1. Raporu inceleyin: notepad katana-duplicate-analysis-report.json" -ForegroundColor White
Write-Host "  2. Duplicate'leri silmek için backend endpoint'i ekleyelim" -ForegroundColor White
Write-Host ""
