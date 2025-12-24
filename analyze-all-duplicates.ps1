# Tüm Duplicate Ürünleri Analiz Et (SKU bazında)
Write-Host "=== Katana Duplicate Ürün Analizi (Detaylı) ===" -ForegroundColor Cyan
Write-Host ""

# JSON'dan ürünleri oku
$allProducts = Get-Content "katana-all-products.json" -Raw | ConvertFrom-Json

Write-Host "Toplam ürün: $($allProducts.Count)" -ForegroundColor White
Write-Host ""

# SKU'ya göre grupla
$skuGroups = $allProducts | Group-Object -Property sku

# Duplicate SKU'ları bul (2'den fazla olan)
$duplicates = $skuGroups | Where-Object { $_.Count -gt 1 }

Write-Host "=== DUPLICATE ANALİZİ ===" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Benzersiz SKU: $($skuGroups.Count)" -ForegroundColor White
Write-Host "Duplicate SKU: $($duplicates.Count)" -ForegroundColor Yellow
Write-Host "Toplam Duplicate Ürün: $(($duplicates | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)" -ForegroundColor Yellow
Write-Host "Silinecek Ürün: $(($duplicates | ForEach-Object { $_.Count - 1 } | Measure-Object -Sum).Sum)" -ForegroundColor Red
Write-Host ""

if ($duplicates.Count -eq 0) {
    Write-Host "✓ Duplicate ürün bulunamadı!" -ForegroundColor Green
    exit 0
}

# En çok tekrar edenleri göster
Write-Host "EN ÇOK TEKRAR EDEN SKU'LAR (Top 20)" -ForegroundColor Yellow
Write-Host "-----------------------------------"
$topDuplicates = $duplicates | Sort-Object Count -Descending | Select-Object -First 20
foreach ($dup in $topDuplicates) {
    Write-Host "  $($dup.Name): $($dup.Count) kez" -ForegroundColor White
}
Write-Host ""

# Detaylı duplicate listesi
Write-Host "DUPLICATE DETAYLARI (İlk 10 SKU)" -ForegroundColor Yellow
Write-Host "--------------------------------"
foreach ($group in ($duplicates | Select-Object -First 10)) {
    $sku = $group.Name
    $products = $group.Group | Sort-Object created_at
    
    Write-Host ""
    Write-Host "SKU: $sku ($($products.Count) adet)" -ForegroundColor Cyan
    
    foreach ($product in $products) {
        $date = if ($product.created_at) { 
            [DateTime]::Parse($product.created_at).ToString("yyyy-MM-dd HH:mm") 
        } else { 
            "Bilinmiyor" 
        }
        $keep = if ($product -eq $products[0]) { "[KORUNACAK]" } else { "[SİLİNECEK]" }
        $color = if ($product -eq $products[0]) { "Green" } else { "Red" }
        Write-Host "  ID: $($product.id) | Tarih: $date $keep" -ForegroundColor $color
    }
}

Write-Host ""
Write-Host "=== STRATEJİ ===" -ForegroundColor Cyan
Write-Host "================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Her duplicate SKU için:" -ForegroundColor Yellow
Write-Host "  - EN ESKİ ürün korunacak (ilk oluşturulan)" -ForegroundColor Green
Write-Host "  - Diğer tüm duplicate'ler silinecek" -ForegroundColor Red
Write-Host ""

# Silinecek ürünleri hesapla
$toDelete = @()
$toKeep = @()

foreach ($group in $duplicates) {
    $products = $group.Group | Sort-Object created_at
    $toKeep += $products[0]
    for ($i = 1; $i -lt $products.Count; $i++) {
        $toDelete += $products[$i]
    }
}

Write-Host "Korunacak: $($toKeep.Count) ürün" -ForegroundColor Green
Write-Host "Silinecek: $($toDelete.Count) ürün" -ForegroundColor Red
Write-Host ""

# Kategorilere göre analiz
Write-Host "SİLİNECEK ÜRÜNLER - KATEGORİ BAZINDA" -ForegroundColor Yellow
Write-Host "------------------------------------"

$variantCount = ($toDelete | Where-Object { $_.sku -like "VARIANT-*" }).Count
$otherCount = $toDelete.Count - $variantCount

Write-Host "  VARIANT ürünleri: $variantCount" -ForegroundColor White
Write-Host "  Diğer ürünler: $otherCount" -ForegroundColor White
Write-Host ""

# SKU prefix analizi
$prefixes = $toDelete | ForEach-Object {
    $sku = $_.sku
    if ($sku -like "VARIANT-*") { 
        "VARIANT" 
    }
    elseif ($sku -match '^[A-Z]+') { 
        $sku.Substring(0, [Math]::Min(3, $sku.Length))
    }
    else { 
        "OTHER" 
    }
} | Group-Object | Sort-Object Count -Descending | Select-Object -First 10

Write-Host "  Prefix bazında:" -ForegroundColor Gray
foreach ($prefix in $prefixes) {
    Write-Host "    $($prefix.Name): $($prefix.Count)" -ForegroundColor Gray
}

# Rapor kaydet
$report = @{
    Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    TotalProducts = $allProducts.Count
    UniqueSKUs = $skuGroups.Count
    DuplicateSKUs = $duplicates.Count
    ToKeep = $toKeep.Count
    ToDelete = $toDelete.Count
    VariantToDelete = $variantCount
    OtherToDelete = $otherCount
    TopDuplicates = $topDuplicates | ForEach-Object {
        [PSCustomObject]@{
            SKU = $_.Name
            Count = $_.Count
        }
    }
    ProductsToDelete = $toDelete | ForEach-Object {
        [PSCustomObject]@{
            Id = $_.id
            SKU = $_.sku
            Name = $_.name
            CreatedAt = $_.created_at
        }
    }
}

$report | ConvertTo-Json -Depth 10 | Out-File -FilePath "all-duplicates-analysis.json" -Encoding UTF8

Write-Host ""
Write-Host "Rapor kaydedildi: all-duplicates-analysis.json" -ForegroundColor Green
Write-Host ""
Write-Host "Sonraki Adım:" -ForegroundColor Yellow
Write-Host "  Tüm duplicate'leri silmek için: .\delete-all-duplicates.ps1 -DryRun:`$false" -ForegroundColor White
