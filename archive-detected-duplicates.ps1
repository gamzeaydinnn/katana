# Tespit Edilen Mükerrer Ürünleri Katana'da Arşivle
# Bu script, kullanıcının tespit ettiği mükerrer ürünleri Katana'da pasife alır

param(
    [switch]$Execute,
    [string]$BaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TESPİT EDİLEN MÜKERRER ÜRÜNLERİ ARŞİVLE  " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# =============================================================================
# TESPİT EDİLEN MÜKERRER ÜRÜNLER
# =============================================================================
# 
# 1. AYNI SKU'YA SAHİP ÜRÜNLER (İsimleri farklı yazılmış)
#    - SKU: 81.06301-8212 → İsim: "81.06301-8212" ve "COOLING WATER PİPE"
#    - SKU: 81.06301-8211 → İsim: "81.06301-8211" ve "COOLING WATER PIPE"  
#    - SKU: 9855411580    → İsim: "Pipe-1 304L" ve "Pipe-2 304L"
#    - SKU: CL-29 02 00347 01 → İsim: "32 20 00126..." (TAM KOPYA - 2 kayıt)
#
# 2. AYNI İSME SAHİP ÜRÜNLER (Farklı SKU'larla)
#    - İsim: "Ø10 BAKIR BORU" → 6 farklı SKU ile kayıt
#      - 32 11 00070 03-02
#      - 32 11 00059 04-01
#      - 32 11 00059 04-02
#      - 29 02 00355 00-01
#      - 29 02 00045 05-02
#      - 29 02 00329 02-01
#
# =============================================================================

# Mode kontrolü
$DryRun = -not $Execute
if ($Execute) {
    Write-Host "⚠️  EXECUTE MODU - Ürünler gerçekten arşivlenecek!" -ForegroundColor Red
    Write-Host "    5 saniye içinde iptal etmek için Ctrl+C basın..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
} else {
    Write-Host "ℹ️  DRY RUN MODU - Sadece preview" -ForegroundColor Green
    Write-Host "    Gerçek arşivleme için: .\archive-detected-duplicates.ps1 -Execute" -ForegroundColor Yellow
}
Write-Host ""

# Login ve token al
Write-Host "[*] Login yapılıyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if (-not $token) {
        Write-Host "[X] Token alınamadı!" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] Login başarılı" -ForegroundColor Green
} catch {
    Write-Host "[X] Login hatası: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Katana ürünlerini çek
Write-Host ""
Write-Host "[*] Katana'dan ürün listesi alınıyor..." -ForegroundColor Yellow

try {
    $productsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/products/katana-products" -Method Get -Headers $headers
    $allProducts = $productsResponse
    Write-Host "[OK] $($allProducts.Count) ürün alındı" -ForegroundColor Green
} catch {
    Write-Host "[X] Ürün listesi alınamadı: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MÜKERRER ÜRÜN ANALİZİ                     " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. AYNI SKU'YA SAHİP ÜRÜNLER ANALİZİ
Write-Host "📋 AYNI SKU'YA SAHİP ÜRÜNLER:" -ForegroundColor Yellow
Write-Host "-----------------------------" -ForegroundColor Gray

$skuGroups = $allProducts | Group-Object -Property sku | Where-Object { $_.Count -gt 1 }

if ($skuGroups.Count -gt 0) {
    foreach ($group in $skuGroups) {
        Write-Host ""
        Write-Host "  SKU: $($group.Name) - $($group.Count) kayıt bulundu!" -ForegroundColor Red
        foreach ($product in $group.Group) {
            $archived = if ($product.is_archived) { "[ARŞİV]" } else { "[AKTİF]" }
            Write-Host "    ID: $($product.id) | İsim: $($product.name) $archived" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  ✓ Aynı SKU'ya sahip mükerrer ürün bulunamadı" -ForegroundColor Green
}

Write-Host ""

# 2. AYNI İSME SAHİP ÜRÜNLER ANALİZİ
Write-Host "📋 AYNI İSME SAHİP ÜRÜNLER:" -ForegroundColor Yellow
Write-Host "----------------------------" -ForegroundColor Gray

$nameGroups = $allProducts | Group-Object -Property name | Where-Object { $_.Count -gt 1 } | Sort-Object -Property Count -Descending | Select-Object -First 20

if ($nameGroups.Count -gt 0) {
    foreach ($group in $nameGroups) {
        Write-Host ""
        Write-Host "  İsim: $($group.Name) - $($group.Count) kayıt" -ForegroundColor Yellow
        foreach ($product in $group.Group) {
            $archived = if ($product.is_archived) { "[ARŞİV]" } else { "[AKTİF]" }
            Write-Host "    ID: $($product.id) | SKU: $($product.sku) $archived" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  ✓ Aynı isme sahip mükerrer ürün bulunamadı" -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ARŞİVLEME ÖNERİLERİ                       " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Arşivlenecek ürünleri belirle
$productsToArchive = @()

# AYNI SKU - İlk kaydı tut, diğerlerini arşivle
foreach ($group in $skuGroups) {
    $sorted = $group.Group | Sort-Object -Property id
    # İlk kaydı tut (en düşük ID)
    $keep = $sorted | Select-Object -First 1
    $archive = $sorted | Select-Object -Skip 1
    
    Write-Host "SKU: $($group.Name)" -ForegroundColor Yellow
    Write-Host "  ✓ TUTULACAK: ID=$($keep.id), İsim=$($keep.name)" -ForegroundColor Green
    foreach ($p in $archive) {
        Write-Host "  ✗ ARŞİVLENECEK: ID=$($p.id), İsim=$($p.name)" -ForegroundColor Red
        if (-not $p.is_archived) {
            $productsToArchive += $p
        }
    }
    Write-Host ""
}

# AYNI İSİM - "Ø10 BAKIR BORU" örneği için (6 kayıt)
$bakirBoruProducts = $allProducts | Where-Object { $_.name -like "*Ø10 BAKIR BORU*" -or $_.name -like "*O10 BAKIR BORU*" -or $_.name -like "*10 BAKIR BORU*" }

if ($bakirBoruProducts.Count -gt 1) {
    Write-Host "İsim: Ø10 BAKIR BORU (ve benzeri)" -ForegroundColor Yellow
    $sorted = $bakirBoruProducts | Sort-Object -Property id
    $keep = $sorted | Select-Object -First 1
    $archive = $sorted | Select-Object -Skip 1
    
    Write-Host "  ✓ TUTULACAK: ID=$($keep.id), SKU=$($keep.sku)" -ForegroundColor Green
    foreach ($p in $archive) {
        Write-Host "  ✗ ARŞİVLENECEK: ID=$($p.id), SKU=$($p.sku)" -ForegroundColor Red
        if (-not $p.is_archived) {
            $productsToArchive += $p
        }
    }
    Write-Host ""
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ÖZET                                      " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Arşivlenecek ürün sayısı: $($productsToArchive.Count)" -ForegroundColor White
Write-Host ""

if ($productsToArchive.Count -eq 0) {
    Write-Host "✓ Arşivlenecek mükerrer ürün bulunamadı!" -ForegroundColor Green
    exit 0
}

# Arşivlenecek ürünleri listele
Write-Host "ARŞİVLENECEK ÜRÜNLER:" -ForegroundColor Red
Write-Host ("{0,-10} {1,-30} {2,-40}" -f "ID", "SKU", "İsim") -ForegroundColor White
Write-Host "--------------------------------------------------------------------------------" -ForegroundColor Gray

foreach ($product in $productsToArchive) {
    $name = if ($product.name.Length -gt 38) { $product.name.Substring(0, 35) + "..." } else { $product.name }
    $sku = if ($product.sku.Length -gt 28) { $product.sku.Substring(0, 25) + "..." } else { $product.sku }
    Write-Host ("{0,-10} {1,-30} {2,-40}" -f $product.id, $sku, $name)
}
Write-Host "--------------------------------------------------------------------------------" -ForegroundColor Gray
Write-Host ""

# DryRun modunda çık
if ($DryRun) {
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "  DRY RUN TAMAMLANDI                        " -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "ℹ️  Gerçek arşivleme için: .\archive-detected-duplicates.ps1 -Execute" -ForegroundColor Cyan
    Write-Host ""
    
    # Preview sonuçlarını kaydet
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $preview = @{
        Timestamp = $timestamp
        DuplicateSkuGroups = $skuGroups | ForEach-Object { 
            @{
                SKU = $_.Name
                Count = $_.Count
                Products = $_.Group | Select-Object id, sku, name, is_archived
            }
        }
        DuplicateNameGroups = $nameGroups | ForEach-Object {
            @{
                Name = $_.Name
                Count = $_.Count
                Products = $_.Group | Select-Object id, sku, name, is_archived
            }
        }
        ProductsToArchive = $productsToArchive | Select-Object id, sku, name
        TotalToArchive = $productsToArchive.Count
    }
    
    $previewFile = "duplicate-analysis-$timestamp.json"
    $preview | ConvertTo-Json -Depth 10 | Out-File -FilePath $previewFile -Encoding UTF8
    Write-Host "[OK] Analiz sonuçları kaydedildi: $previewFile" -ForegroundColor Green
    exit 0
}

# Execute modu - Gerçek arşivleme
Write-Host "============================================" -ForegroundColor Red
Write-Host "  ARŞİVLEME BAŞLIYOR                        " -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

$successCount = 0
$failCount = 0
$errors = @()

foreach ($product in $productsToArchive) {
    Write-Host "Arşivleniyor: $($product.sku) (ID: $($product.id))..." -NoNewline
    
    try {
        # Katana API'de ürünü arşivle
        $archiveUrl = "$BaseUrl/api/products/$($product.id)/deactivate"
        
        $response = Invoke-RestMethod -Uri $archiveUrl -Method Put -Headers $headers -ErrorAction Stop
        
        Write-Host " [✓ Arşivlendi]" -ForegroundColor Green
        $successCount++
        
        # Rate limiting için bekle
        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host " [✗ HATA]" -ForegroundColor Red
        $failCount++
        $errors += [PSCustomObject]@{
            ProductId = $product.id
            SKU = $product.sku
            Error = $_.Exception.Message
        }
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ARŞİVLEME SONUÇLARI                       " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Başarılı : $successCount" -ForegroundColor Green
Write-Host "Başarısız: $failCount" -ForegroundColor Red
Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "HATALAR:" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  SKU: $($err.SKU) | ID: $($err.ProductId)" -ForegroundColor Gray
        Write-Host "  Hata: $($err.Error)" -ForegroundColor Gray
        Write-Host ""
    }
}

# Sonuç raporunu kaydet
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$result = @{
    Timestamp = $timestamp
    TotalProducts = $productsToArchive.Count
    SuccessCount = $successCount
    FailCount = $failCount
    ArchivedProducts = $productsToArchive | Select-Object id, sku, name
    Errors = $errors
}

$resultFile = "archive-duplicates-result-$timestamp.json"
$result | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultFile -Encoding UTF8
Write-Host ""
Write-Host "[OK] Sonuçlar kaydedildi: $resultFile" -ForegroundColor Green
Write-Host ""
Write-Host "Script tamamlandı." -ForegroundColor Gray
