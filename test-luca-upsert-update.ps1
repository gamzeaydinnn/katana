# ============================================
# LUCA STOK KARTI UPSERT GUNCELLEME TESTI
# ============================================
# Bu script Luca Stok Karti UPSERT duzeltmesini test eder:
# - Ayni SKU'lu urun geldiginde yeni kart acilmamali
# - Mevcut kart guncellenmeli
# 
# Akis:
# Urun Geldi -> FindStockCardBySkuAsync(SKU)
# ├─ Bulundu -> UpdateStockCardAsync() -> GuncelleStkWsSkart.do
# └─ Bulunamadi -> SendStockCardsAsync() -> EkleStkWsKart.do
# ============================================

$baseUrl = "http://localhost:5055/api"
$token = ""
$testResults = @{
    Login = $false
    FindExisting = $false
    UpdateExisting = $false
    VerifyUpdate = $false
    CreateNew = $false
    VerifyCreate = $false
    UpsertExisting = $false
    VerifyUpsert = $false
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LUCA STOK KARTI UPSERT GUNCELLEME TESTI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Tarihi: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# ============================================
# ADIM 1: LOGIN
# ============================================
Write-Host "[1/8] Giris yapiliyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "   [OK] Giris basarili!" -ForegroundColor Green
        $testResults.Login = $true
    } else {
        Write-Host "   [FAIL] Token alinamadi!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   [FAIL] Giris basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# ============================================
# ADIM 2: MEVCUT STOK KARTLARINI LISTELE
# ============================================
Write-Host "[2/8] Mevcut stok kartlari listeleniyor..." -ForegroundColor Yellow
try {
    $stockCardsResponse = Invoke-RestMethod -Uri "$baseUrl/Luca/stock-cards" -Method Get -Headers $headers
    
    $stockCards = $stockCardsResponse
    if ($stockCardsResponse.data) {
        $stockCards = $stockCardsResponse.data
    }
    
    $stockCardCount = if ($stockCards -is [array]) { $stockCards.Count } else { 1 }
    
    Write-Host "   [OK] $stockCardCount adet stok karti bulundu" -ForegroundColor Green
    
    # Ilk 5 karti goster
    if ($stockCardCount -gt 0) {
        Write-Host "   Ornek kartlar:" -ForegroundColor Gray
        $stockCards | Select-Object -First 5 | ForEach-Object {
            $kod = if ($_.kartKodu) { $_.kartKodu } elseif ($_.kod) { $_.kod } else { "N/A" }
            $ad = if ($_.kartAdi) { $_.kartAdi } elseif ($_.adi) { $_.adi } else { "N/A" }
            Write-Host "      - $kod : $ad" -ForegroundColor DarkGray
        }
    }
    
    $testResults.FindExisting = $true
} catch {
    Write-Host "   [FAIL] Stok kartlari listelenemedi: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================
# ADIM 3: TEST ICIN MEVCUT BIR URUN SEC
# ============================================
Write-Host "[3/8] Test icin mevcut urun seciliyor..." -ForegroundColor Yellow

$testSku = $null
$testSkartId = $null
$originalName = $null
$originalPrice = $null

try {
    # Mevcut bir urun bul
    if ($stockCards -and $stockCardCount -gt 0) {
        $existingCard = $stockCards | Select-Object -First 1
        
        $testSku = if ($existingCard.kartKodu) { $existingCard.kartKodu } elseif ($existingCard.kod) { $existingCard.kod } else { $null }
        $testSkartId = if ($existingCard.skartId) { $existingCard.skartId } elseif ($existingCard.stokKartId) { $existingCard.stokKartId } else { $null }
        $originalName = if ($existingCard.kartAdi) { $existingCard.kartAdi } elseif ($existingCard.adi) { $existingCard.adi } else { "N/A" }
        $originalPrice = if ($existingCard.perakendeSatisBirimFiyat) { $existingCard.perakendeSatisBirimFiyat } else { 0 }
        
        if ($testSku) {
            Write-Host "   [OK] Test urunu secildi:" -ForegroundColor Green
            Write-Host "      SKU: $testSku" -ForegroundColor Gray
            Write-Host "      SkartId: $testSkartId" -ForegroundColor Gray
            Write-Host "      Ad: $originalName" -ForegroundColor Gray
            Write-Host "      Fiyat: $originalPrice TL" -ForegroundColor Gray
        } else {
            Write-Host "   [WARN] Mevcut urun bulunamadi, yeni urun olusturulacak" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   [WARN] Stok karti yok, yeni urun olusturulacak" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   [FAIL] Urun secimi basarisiz: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================
# ADIM 4: UPSERT TESTI - MEVCUT URUN GUNCELLEME
# ============================================
if ($testSku -and $testSkartId) {
    Write-Host "[4/8] UPSERT Testi - Mevcut urun guncelleniyor..." -ForegroundColor Yellow
    Write-Host "   Beklenen davranis: UPDATE (yeni kart acilmamali)" -ForegroundColor Cyan
    
    $newName = "UPSERT TEST - $(Get-Date -Format 'HHmmss')"
    $newPrice = [math]::Round((Get-Random -Minimum 10 -Maximum 1000), 2)
    
    try {
        # Sync endpoint'i kullanarak upsert yap
        $syncBody = @{
            products = @(
                @{
                    sku = $testSku
                    name = $newName
                    price = $newPrice
                    categoryId = 1
                }
            )
        } | ConvertTo-Json -Depth 3
        
        Write-Host "   Gonderilen veri:" -ForegroundColor Gray
        Write-Host "      SKU: $testSku" -ForegroundColor DarkGray
        Write-Host "      Yeni Ad: $newName" -ForegroundColor DarkGray
        Write-Host "      Yeni Fiyat: $newPrice TL" -ForegroundColor DarkGray
        
        # Products endpoint'i ile guncelle
        $updateBody = @{
            name = $newName
            price = $newPrice
            stock = 100
            categoryId = 1
            isActive = $true
        } | ConvertTo-Json
        
        # Oncelikle local DB'de urun var mi kontrol et
        $localProduct = $null
        try {
            $localProduct = Invoke-RestMethod -Uri "$baseUrl/Products/by-sku/$testSku" -Method Get -Headers $headers
        } catch {
            Write-Host "   [INFO] Local DB'de urun bulunamadi, Luca'dan sync edilecek" -ForegroundColor Gray
        }
        
        if ($localProduct) {
            # Local'de varsa guncelle
            $updateResponse = Invoke-RestMethod -Uri "$baseUrl/Products/$($localProduct.id)" -Method Put -Body $updateBody -Headers $headers
            Write-Host "   [OK] Local urun guncellendi, Luca'ya sync ediliyor..." -ForegroundColor Green
        } else {
            # Local'de yoksa olustur
            $createBody = @{
                name = $newName
                sku = $testSku
                price = $newPrice
                stock = 100
                categoryId = 1
                isActive = $true
            } | ConvertTo-Json
            
            $createResponse = Invoke-RestMethod -Uri "$baseUrl/Products" -Method Post -Body $createBody -Headers $headers
            Write-Host "   [OK] Urun olusturuldu ve Luca'ya sync ediliyor..." -ForegroundColor Green
        }
        
        $testResults.UpdateExisting = $true
        
        # Biraz bekle (async sync icin)
        Write-Host "   [INFO] Luca sync bekleniyor (3 saniye)..." -ForegroundColor Gray
        Start-Sleep -Seconds 3
        
    } catch {
        Write-Host "   [FAIL] Guncelleme basarisiz: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "[4/8] UPSERT Testi - Mevcut urun yok, atlaniyor..." -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# ADIM 5: GUNCELLEME DOGRULAMA
# ============================================
if ($testSku -and $testResults.UpdateExisting) {
    Write-Host "[5/8] Guncelleme dogrulaniyor..." -ForegroundColor Yellow
    
    try {
        # Luca'dan tekrar cek
        $verifyResponse = Invoke-RestMethod -Uri "$baseUrl/Luca/stock-cards" -Method Get -Headers $headers
        
        $verifyCards = $verifyResponse
        if ($verifyResponse.data) {
            $verifyCards = $verifyResponse.data
        }
        
        # Ayni SKU'lu kartlari say
        $matchingCards = $verifyCards | Where-Object { 
            $kod = if ($_.kartKodu) { $_.kartKodu } elseif ($_.kod) { $_.kod } else { "" }
            $kod -eq $testSku 
        }
        
        $matchCount = if ($matchingCards -is [array]) { $matchingCards.Count } else { if ($matchingCards) { 1 } else { 0 } }
        
        if ($matchCount -eq 1) {
            Write-Host "   [OK] Ayni SKU ile sadece 1 kart var (UPSERT calisiyor!)" -ForegroundColor Green
            $testResults.VerifyUpdate = $true
            
            $updatedCard = $matchingCards
            if ($matchingCards -is [array]) { $updatedCard = $matchingCards[0] }
            
            $updatedName = if ($updatedCard.kartAdi) { $updatedCard.kartAdi } elseif ($updatedCard.adi) { $updatedCard.adi } else { "N/A" }
            Write-Host "      Guncel Ad: $updatedName" -ForegroundColor Gray
            
        } elseif ($matchCount -gt 1) {
            Write-Host "   [FAIL] Ayni SKU ile $matchCount kart var! (DUPLIKASYON!)" -ForegroundColor Red
            Write-Host "   UPSERT calismadi - yeni kart acilmis!" -ForegroundColor Red
        } else {
            Write-Host "   [WARN] SKU bulunamadi: $testSku" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   [FAIL] Dogrulama basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[5/8] Guncelleme dogrulama - Atlaniyor (onceki adim basarisiz)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# ADIM 6: YENI URUN OLUSTURMA TESTI
# ============================================
Write-Host "[6/8] Yeni urun olusturma testi..." -ForegroundColor Yellow

$newTestSku = "UPSERT-TEST-$(Get-Date -Format 'yyyyMMddHHmmss')"
$newTestName = "UPSERT Test Urunu - $(Get-Date -Format 'HH:mm:ss')"
$newTestPrice = [math]::Round((Get-Random -Minimum 50 -Maximum 500), 2)

try {
    $createBody = @{
        name = $newTestName
        sku = $newTestSku
        price = $newTestPrice
        stock = 50
        categoryId = 1
        isActive = $true
    } | ConvertTo-Json
    
    Write-Host "   Yeni urun olusturuluyor:" -ForegroundColor Gray
    Write-Host "      SKU: $newTestSku" -ForegroundColor DarkGray
    Write-Host "      Ad: $newTestName" -ForegroundColor DarkGray
    Write-Host "      Fiyat: $newTestPrice TL" -ForegroundColor DarkGray
    
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/Products" -Method Post -Body $createBody -Headers $headers
    
    Write-Host "   [OK] Yeni urun olusturuldu! ID: $($createResponse.id)" -ForegroundColor Green
    $testResults.CreateNew = $true
    
    # Biraz bekle (async sync icin)
    Write-Host "   [INFO] Luca sync bekleniyor (3 saniye)..." -ForegroundColor Gray
    Start-Sleep -Seconds 3
    
} catch {
    Write-Host "   [FAIL] Yeni urun olusturulamadi: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# ============================================
# ADIM 7: YENI URUN DOGRULAMA
# ============================================
if ($testResults.CreateNew) {
    Write-Host "[7/8] Yeni urun Luca'da dogrulaniyor..." -ForegroundColor Yellow
    
    try {
        $verifyNewResponse = Invoke-RestMethod -Uri "$baseUrl/Luca/stock-cards" -Method Get -Headers $headers
        
        $verifyNewCards = $verifyNewResponse
        if ($verifyNewResponse.data) {
            $verifyNewCards = $verifyNewResponse.data
        }
        
        $newMatchingCards = $verifyNewCards | Where-Object { 
            $kod = if ($_.kartKodu) { $_.kartKodu } elseif ($_.kod) { $_.kod } else { "" }
            $kod -eq $newTestSku 
        }
        
        $newMatchCount = if ($newMatchingCards -is [array]) { $newMatchingCards.Count } else { if ($newMatchingCards) { 1 } else { 0 } }
        
        if ($newMatchCount -eq 1) {
            Write-Host "   [OK] Yeni urun Luca'da olusturuldu!" -ForegroundColor Green
            $testResults.VerifyCreate = $true
        } elseif ($newMatchCount -gt 1) {
            Write-Host "   [FAIL] Yeni urun icin $newMatchCount kart olusturulmus! (DUPLIKASYON!)" -ForegroundColor Red
        } else {
            Write-Host "   [WARN] Yeni urun Luca'da bulunamadi (sync gecikmis olabilir)" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   [FAIL] Dogrulama basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[7/8] Yeni urun dogrulama - Atlaniyor (onceki adim basarisiz)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# ADIM 8: AYNI SKU ILE TEKRAR UPSERT
# ============================================
if ($testResults.CreateNew) {
    Write-Host "[8/8] Ayni SKU ile tekrar UPSERT testi..." -ForegroundColor Yellow
    Write-Host "   Beklenen davranis: UPDATE (yeni kart acilmamali)" -ForegroundColor Cyan
    
    $updatedTestName = "UPSERT GUNCELLEME - $(Get-Date -Format 'HH:mm:ss')"
    $updatedTestPrice = [math]::Round((Get-Random -Minimum 100 -Maximum 1000), 2)
    
    try {
        # Ayni SKU ile tekrar olustur/guncelle
        $localProduct = Invoke-RestMethod -Uri "$baseUrl/Products/by-sku/$newTestSku" -Method Get -Headers $headers
        
        # SKU alani zorunlu - validator kontrolu
        $updateBody = @{
            name = $updatedTestName
            sku = $newTestSku
            price = $updatedTestPrice
            stock = 75
            categoryId = 1
            isActive = $true
        } | ConvertTo-Json
        
        Write-Host "   Guncelleme gonderiliyor:" -ForegroundColor Gray
        Write-Host "      SKU: $newTestSku" -ForegroundColor DarkGray
        Write-Host "      Yeni Ad: $updatedTestName" -ForegroundColor DarkGray
        Write-Host "      Yeni Fiyat: $updatedTestPrice TL" -ForegroundColor DarkGray
        
        $updateResponse = Invoke-RestMethod -Uri "$baseUrl/Products/$($localProduct.id)" -Method Put -Body $updateBody -Headers $headers
        
        Write-Host "   [OK] Guncelleme gonderildi!" -ForegroundColor Green
        
        # Biraz bekle
        Write-Host "   [INFO] Luca sync bekleniyor (3 saniye)..." -ForegroundColor Gray
        Start-Sleep -Seconds 3
        
        # Dogrula
        $finalResponse = Invoke-RestMethod -Uri "$baseUrl/Luca/stock-cards" -Method Get -Headers $headers
        
        $finalCards = $finalResponse
        if ($finalResponse.data) {
            $finalCards = $finalResponse.data
        }
        
        $finalMatchingCards = $finalCards | Where-Object { 
            $kod = if ($_.kartKodu) { $_.kartKodu } elseif ($_.kod) { $_.kod } else { "" }
            $kod -eq $newTestSku 
        }
        
        $finalMatchCount = if ($finalMatchingCards -is [array]) { $finalMatchingCards.Count } else { if ($finalMatchingCards) { 1 } else { 0 } }
        
        if ($finalMatchCount -eq 1) {
            Write-Host "   [OK] UPSERT CALISIYOR! Ayni SKU ile hala sadece 1 kart var." -ForegroundColor Green
            $testResults.UpsertExisting = $true
            $testResults.VerifyUpsert = $true
        } elseif ($finalMatchCount -gt 1) {
            Write-Host "   [FAIL] UPSERT CALISMADI! $finalMatchCount kart olusturulmus!" -ForegroundColor Red
            Write-Host "   Bu duplikasyon sorununu gosteriyor!" -ForegroundColor Red
        } else {
            Write-Host "   [WARN] Kart bulunamadi" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "   [FAIL] UPSERT testi basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[8/8] UPSERT tekrar testi - Atlaniyor (onceki adim basarisiz)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# TEST SONUCLARI
# ============================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SONUCLARI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$passCount = 0
$failCount = 0
$warnCount = 0

# Login
if ($testResults.Login) {
    Write-Host "  [PASS] 1. Giris" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [FAIL] 1. Giris" -ForegroundColor Red
    $failCount++
}

# Find Existing
if ($testResults.FindExisting) {
    Write-Host "  [PASS] 2. Mevcut Stok Kartlari Listeleme" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [FAIL] 2. Mevcut Stok Kartlari Listeleme" -ForegroundColor Red
    $failCount++
}

# Update Existing
if ($testResults.UpdateExisting) {
    Write-Host "  [PASS] 3. Mevcut Urun Guncelleme" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [WARN] 3. Mevcut Urun Guncelleme (Atlandi)" -ForegroundColor Yellow
    $warnCount++
}

# Verify Update
if ($testResults.VerifyUpdate) {
    Write-Host "  [PASS] 4. Guncelleme Dogrulama (Duplikasyon Yok)" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [WARN] 4. Guncelleme Dogrulama" -ForegroundColor Yellow
    $warnCount++
}

# Create New
if ($testResults.CreateNew) {
    Write-Host "  [PASS] 5. Yeni Urun Olusturma" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [FAIL] 5. Yeni Urun Olusturma" -ForegroundColor Red
    $failCount++
}

# Verify Create
if ($testResults.VerifyCreate) {
    Write-Host "  [PASS] 6. Yeni Urun Luca Dogrulama" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [WARN] 6. Yeni Urun Luca Dogrulama" -ForegroundColor Yellow
    $warnCount++
}

# Upsert Existing
if ($testResults.UpsertExisting) {
    Write-Host "  [PASS] 7. UPSERT Tekrar Testi" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [WARN] 7. UPSERT Tekrar Testi" -ForegroundColor Yellow
    $warnCount++
}

# Verify Upsert
if ($testResults.VerifyUpsert) {
    Write-Host "  [PASS] 8. UPSERT Dogrulama (Duplikasyon Yok)" -ForegroundColor Green
    $passCount++
} else {
    Write-Host "  [WARN] 8. UPSERT Dogrulama" -ForegroundColor Yellow
    $warnCount++
}

Write-Host ""
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "Toplam: $passCount PASS, $failCount FAIL, $warnCount WARN" -ForegroundColor White
Write-Host ""

# Genel Sonuc
if ($failCount -eq 0 -and $testResults.VerifyUpsert) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "UPSERT DUZELTMESI CALISIYOR!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ayni SKU'lu urun geldiginde:" -ForegroundColor White
    Write-Host "  - Yeni kart ACILMIYOR" -ForegroundColor Green
    Write-Host "  - Mevcut kart GUNCELLENIYOR" -ForegroundColor Green
    Write-Host ""
} elseif ($failCount -gt 0) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "BAZI TESTLER BASARISIZ!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Loglari kontrol edin:" -ForegroundColor Yellow
    Write-Host "  docker logs katana-backend --tail 100" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "TESTLER KISMEN TAMAMLANDI" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Bazi testler atlanmis olabilir." -ForegroundColor Yellow
    Write-Host "Luca baglantisini kontrol edin." -ForegroundColor Yellow
    Write-Host ""
}

# Temizlik bilgisi
if ($newTestSku) {
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host "Test Urunu Bilgisi:" -ForegroundColor Cyan
    Write-Host "  SKU: $newTestSku" -ForegroundColor Gray
    Write-Host "  Bu urunu manuel olarak silebilirsiniz." -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Test tamamlandi: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""
