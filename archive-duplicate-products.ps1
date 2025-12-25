# Mukerrer/Duplicate Urunleri Katana'da Arsivle (Pasife Al)
# Bu script belirli SKU'lari veya urun ID'lerini Katana'da arsivler (is_archived = true)

param(
    [switch]$DryRun = $true,
    [switch]$Execute,
    [string]$BaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MUKERRER URUN ARŞIVLEME SCRIPTI          " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# DryRun veya Execute modu kontrolu
if ($Execute) {
    $DryRun = $false
    Write-Host "EXECUTE MODU - Urunler gercekten arsivlenecek!" -ForegroundColor Red
    Write-Host "    5 saniye icinde iptal etmek icin Ctrl+C basin..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
} else {
    Write-Host "DRY RUN MODU - Sadece preview, hicbir degisiklik yapilmayacak" -ForegroundColor Green
    Write-Host "    Gercek arşivleme icin: .\archive-duplicate-products.ps1 -Execute" -ForegroundColor Yellow
}

Write-Host ""

$duplicateSkus = @(
    # =============================================================================
    # GRUP 1: Ayni SKU'ya Sahip Urunler - IKINCI KAYITLAR
    # =============================================================================
    # NOT: Asagidaki SKU'lar icin ISMI FARKLI olan kayitlar arsivlenecek
    # Ornek: "81.06301-8212" isimli kayit arsivlenecek, "COOLING WATER PIPE" kalacak
    
    # "81.06301-8212",  # Isim: "81.06301-8212" arsivlenecek (COOLING WATER PIPE kalacak)
    # "81.06301-8211",  # Isim: "81.06301-8211" arsivlenecek (COOLING WATER PIPE kalacak)
    # "9855411580",     # Isim: "Pipe-2 304L" arsivlenecek (Pipe-1 304L kalacak)
    
    # =============================================================================
    # GRUP 2: Tam Kopya - CL-29 02 00347 01
    # =============================================================================
    # Bu urun tamamen ayni bilgilerle 2 kez kayitli - birini arsivle
    
    # "CL-29 02 00347 01"  # Ikinci kaydi arsivle
    
    # =============================================================================
    # GRUP 3: Ayni Isme Sahip Urunler - O10 BAKIR BORU (6 farkli SKU)
    # =============================================================================
    # NOT: Bu grupta 6 farkli SKU var, sadece BIRINI tutmalisiniz!
    # Ornek: "29 02 00045 05-02" kalacak, digerleri arsivlenecek
    
    # "32 11 00070 03-02",  # O10 BAKIR BORU
    # "32 11 00059 04-01",  # O10 BAKIR BORU
    # "32 11 00059 04-02",  # O10 BAKIR BORU
    # "29 02 00355 00-01",  # O10 BAKIR BORU
    # "29 02 00329 02-01"   # O10 BAKIR BORU
    # NOT: "29 02 00045 05-02" TUTULACAK (yukarida yok)
)

$duplicateProductIds = @(
    # =============================================================================
    # O10 BAKIR BORU DUPLICATE'LARI
    # =============================================================================
    # TUTULACAK: ID 15042 | SKU: 29 02 00045 05-02 | Name: O10 BAKIR BORU
    # ARSIVLENECEKLER (5 kayit):
    
    15039,  # SKU: 29 02 00329 02-01 | Name: O10 BAKIR BORU
    15038,  # SKU: 29 02 00355 00-01 | Name: O10 BAKIR BORU
    15040,  # SKU: 32 11 00059 04-01 | Name: O10 BAKIR BORU
    15041,  # SKU: 32 11 00059 04-02 | Name: O10 BAKIR BORU
    197     # SKU: 32 11 00070 03-02 | Name: O10 BAKIR BORU
)

Write-Host "ARSIVLENECEK URUNLER:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "SKU sayisi    : $($duplicateSkus.Count)" -ForegroundColor White
Write-Host "Product ID    : $($duplicateProductIds.Count)" -ForegroundColor White
Write-Host ""

if ($duplicateSkus.Count -eq 0 -and $duplicateProductIds.Count -eq 0) {
    Write-Host "Arsivlenecek urun listesi bos!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Script'i duzenleyip arsivlenecek SKU'lari ekleyin" -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Login ve token al
Write-Host "[*] Login yapiliyor..." -ForegroundColor Yellow

try {
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if (-not $token) {
        Write-Host "[X] Token alinamadi!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[OK] Login basarili" -ForegroundColor Green
} catch {
    Write-Host "[X] Login hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Katana urunlerini cek
Write-Host ""
Write-Host "[*] Katana'dan urun listesi aliniyor..." -ForegroundColor Yellow

try {
    $productsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/products" -Method Get -Headers $headers
    $allProducts = $productsResponse
    Write-Host "[OK] $($allProducts.Count) urun alindi" -ForegroundColor Green
} catch {
    Write-Host "[X] Urun listesi alinamadi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Arsivlenecek urunleri bul
$productsToArchive = @()

# SKU bazli arama
foreach ($sku in $duplicateSkus) {
    $match = $allProducts | Where-Object { $_.sku -eq $sku }
    if ($match) {
        $productsToArchive += $match
        Write-Host "  SKU bulundu: $sku (ID: $($match.id))" -ForegroundColor Green
    } else {
        Write-Host "  SKU bulunamadi: $sku" -ForegroundColor Yellow
    }
}

# Product ID bazli arama
foreach ($productId in $duplicateProductIds) {
    $match = $allProducts | Where-Object { $_.id -eq $productId }
    if ($match) {
        $productsToArchive += $match
        Write-Host "  ID bulundu: $productId (SKU: $($match.sku))" -ForegroundColor Green
    } else {
        Write-Host "  ID bulunamadi: $productId" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ARSIVLENECEK URUNLER                      " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($productsToArchive.Count -eq 0) {
    Write-Host "Eslesen urun bulunamadi!" -ForegroundColor Yellow
    exit 0
}

Write-Host ("{0,-12} {1,-30} {2,-40}" -f "ID", "SKU", "Urun Adi") -ForegroundColor White
Write-Host "--------------------------------------------------------------------------------" -ForegroundColor Gray

foreach ($product in $productsToArchive) {
    $name = if ($product.name.Length -gt 38) { $product.name.Substring(0, 35) + "..." } else { $product.name }
    $sku = if ($product.sku.Length -gt 28) { $product.sku.Substring(0, 25) + "..." } else { $product.sku }
    Write-Host ("{0,-12} {1,-30} {2,-40}" -f $product.id, $sku, $name)
}

Write-Host "--------------------------------------------------------------------------------" -ForegroundColor Gray
Write-Host "Toplam: $($productsToArchive.Count) urun" -ForegroundColor White
Write-Host ""

# DryRun modunda cik
if ($DryRun) {
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "  DRY RUN TAMAMLANDI                        " -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Bu sadece bir preview'di." -ForegroundColor Cyan
    Write-Host "    Gercek arşivleme icin: .\archive-duplicate-products.ps1 -Execute" -ForegroundColor White
    Write-Host ""
    exit 0
}

# Execute modu - Gercek arşivleme
Write-Host "============================================" -ForegroundColor Red
Write-Host "  ARŞIVLEME BASLIYOR                        " -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

$successCount = 0
$failCount = 0
$errors = @()

foreach ($product in $productsToArchive) {
    Write-Host "Arsivleniyor: $($product.sku) (ID: $($product.id))..." -NoNewline
    
    try {
        # Katana API'de urunu arsivle (is_archived = true)
        $archiveUrl = "$BaseUrl/api/products/$($product.id)/deactivate"
        $response = Invoke-RestMethod -Uri $archiveUrl -Method Put -Headers $headers -ErrorAction Stop
        
        Write-Host " [Arsivlendi]" -ForegroundColor Green
        $successCount++
        
        # Rate limiting icin bekle
        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host " [HATA]" -ForegroundColor Red
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
Write-Host "  ARŞIVLEME SONUCLARI                       " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Basarili : $successCount" -ForegroundColor Green
Write-Host "Basarisiz: $failCount" -ForegroundColor Red
Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "HATALAR:" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  SKU: $($err.SKU) | ID: $($err.ProductId)" -ForegroundColor Gray
        Write-Host "  Hata: $($err.Error)" -ForegroundColor Gray
        Write-Host ""
    }
}

# Sonuc raporunu kaydet
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
Write-Host "[OK] Sonuclar kaydedildi: $resultFile" -ForegroundColor Green
Write-Host ""
Write-Host "Script tamamlandi." -ForegroundColor Gray
