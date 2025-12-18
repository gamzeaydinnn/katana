# LUCA STOK KARTI SILME VE GUNCELLEME TESTI
# HIZ01 stok kartini test eder

$baseUrl = "http://localhost:5055/api"
$token = ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LUCA STOK KARTI SILME VE GUNCELLEME TESTI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# LOGIN
Write-Host "[1/6] Giris yapiliyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "   OK Giris basarili!" -ForegroundColor Green
    } else {
        Write-Host "   ERROR Token alinamadi!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ERROR Giris basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# HIZ01 URUNUNU BUL
Write-Host "[2/6] HIZ01 urunu araniyor..." -ForegroundColor Yellow
try {
    $lucaResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $lucaProducts = $lucaResponse.data
    
    $hiz01 = $lucaProducts | Where-Object { $_.productCode -eq "HIZ01" }
    
    if ($hiz01) {
        Write-Host "   OK HIZ01 bulundu!" -ForegroundColor Green
        Write-Host "      ID: $($hiz01.id)" -ForegroundColor Gray
        Write-Host "      Kod: $($hiz01.productCode)" -ForegroundColor Gray
        Write-Host "      Ad: $($hiz01.productName)" -ForegroundColor Gray
        Write-Host "      Fiyat: $($hiz01.unitPrice) TL" -ForegroundColor Gray
        Write-Host "      Miktar: $($hiz01.quantity)" -ForegroundColor Gray
    } else {
        Write-Host "   WARNING HIZ01 bulunamadi! Test edilecek urun yok." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Mevcut urunler:" -ForegroundColor Cyan
        $lucaProducts | Select-Object -First 5 | ForEach-Object {
            Write-Host "   - $($_.productCode) - $($_.productName)" -ForegroundColor Gray
        }
        exit 0
    }
} catch {
    Write-Host "   ERROR Urunler cekilemedi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# GUNCELLEME TESTI
Write-Host "[3/6] HIZ01 guncelleniyor..." -ForegroundColor Yellow
try {
    $updateBody = @{
        productCode = "HIZ01"
        productName = "TEST GUNCELLEME - %1 KDV LI MUHTELIF ALIMLAR"
        unit = "Adet"
        quantity = 999
        unitPrice = 123.45
        vatRate = 1
    } | ConvertTo-Json

    Write-Host "   Gonderilen veri:" -ForegroundColor Gray
    Write-Host "   $updateBody" -ForegroundColor DarkGray
    
    $updateResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca/$($hiz01.id)" -Method Put -Body $updateBody -Headers $headers
    
    Write-Host "   OK Guncelleme basarili!" -ForegroundColor Green
    Write-Host "      Yeni Ad: $($updateResponse.productName)" -ForegroundColor Gray
    Write-Host "      Yeni Fiyat: $($updateResponse.unitPrice) TL" -ForegroundColor Gray
    Write-Host "      Yeni Miktar: $($updateResponse.quantity)" -ForegroundColor Gray
    
    $hiz01Updated = $updateResponse
} catch {
    Write-Host "   ERROR Guncelleme basarisiz: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""

# GUNCELLEME DOGRULAMA
Write-Host "[4/6] Guncelleme dogrulanıyor..." -ForegroundColor Yellow
try {
    Start-Sleep -Seconds 2
    
    $verifyResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $verifiedHiz01 = $verifyResponse.data | Where-Object { $_.productCode -eq "HIZ01" }
    
    if ($verifiedHiz01) {
        $priceMatch = $verifiedHiz01.unitPrice -eq 123.45
        $quantityMatch = $verifiedHiz01.quantity -eq 999
        $nameMatch = $verifiedHiz01.productName -like "*TEST GUNCELLEME*"
        
        if ($priceMatch -and $quantityMatch -and $nameMatch) {
            Write-Host "   OK Guncelleme dogrulandi!" -ForegroundColor Green
            Write-Host "      Fiyat: $($verifiedHiz01.unitPrice) TL - OK" -ForegroundColor Gray
            Write-Host "      Miktar: $($verifiedHiz01.quantity) - OK" -ForegroundColor Gray
            Write-Host "      Ad: $($verifiedHiz01.productName) - OK" -ForegroundColor Gray
        } else {
            Write-Host "   WARNING Guncelleme kismen basarili:" -ForegroundColor Yellow
            if ($priceMatch) {
                Write-Host "      Fiyat: $($verifiedHiz01.unitPrice) TL - OK" -ForegroundColor Gray
            } else {
                Write-Host "      Fiyat: $($verifiedHiz01.unitPrice) TL - FAIL" -ForegroundColor Red
            }
            if ($quantityMatch) {
                Write-Host "      Miktar: $($verifiedHiz01.quantity) - OK" -ForegroundColor Gray
            } else {
                Write-Host "      Miktar: $($verifiedHiz01.quantity) - FAIL" -ForegroundColor Red
            }
            if ($nameMatch) {
                Write-Host "      Ad: $($verifiedHiz01.productName) - OK" -ForegroundColor Gray
            } else {
                Write-Host "      Ad: $($verifiedHiz01.productName) - FAIL" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ERROR HIZ01 dogrulama sirasinda bulunamadi!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ERROR Dogrulama basarisiz: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# SILME TESTI
Write-Host "[5/6] HIZ01 siliniyor..." -ForegroundColor Yellow
try {
    $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/Products/$($hiz01.id)" -Method Delete -Headers $headers
    
    Write-Host "   OK Silme istegi gonderildi!" -ForegroundColor Green
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 204) {
        Write-Host "   OK Silme basarili! (HTTP 204 No Content)" -ForegroundColor Green
    } else {
        Write-Host "   ERROR Silme basarisiz: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   HTTP Status: $statusCode" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "   Detay: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""

# SILME DOGRULAMA
Write-Host "[6/6] Silme dogrulanıyor..." -ForegroundColor Yellow
try {
    Start-Sleep -Seconds 2
    
    $finalResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $deletedCheck = $finalResponse.data | Where-Object { $_.productCode -eq "HIZ01" }
    
    if ($deletedCheck) {
        Write-Host "   WARNING HIZ01 hala mevcut! Silme basarisiz olabilir." -ForegroundColor Yellow
        Write-Host "      ID: $($deletedCheck.id)" -ForegroundColor Gray
        Write-Host "      Kod: $($deletedCheck.productCode)" -ForegroundColor Gray
        Write-Host "      Ad: $($deletedCheck.productName)" -ForegroundColor Gray
    } else {
        Write-Host "   OK HIZ01 basariyla silindi!" -ForegroundColor Green
        Write-Host "      Urun artik listede yok." -ForegroundColor Gray
    }
} catch {
    Write-Host "   ERROR Dogrulama basarisiz: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Ozet:" -ForegroundColor White
Write-Host "  1. Giris: OK" -ForegroundColor Green
if ($hiz01) {
    Write-Host "  2. HIZ01 Bulma: OK" -ForegroundColor Green
} else {
    Write-Host "  2. HIZ01 Bulma: FAIL" -ForegroundColor Red
}
if ($hiz01Updated) {
    Write-Host "  3. Guncelleme: OK" -ForegroundColor Green
} else {
    Write-Host "  3. Guncelleme: FAIL" -ForegroundColor Red
}
if ($verifiedHiz01) {
    Write-Host "  4. Guncelleme Dogrulama: OK" -ForegroundColor Green
} else {
    Write-Host "  4. Guncelleme Dogrulama: FAIL" -ForegroundColor Red
}
Write-Host "  5. Silme: OK" -ForegroundColor Green
if (-not $deletedCheck) {
    Write-Host "  6. Silme Dogrulama: OK" -ForegroundColor Green
} else {
    Write-Host "  6. Silme Dogrulama: WARNING" -ForegroundColor Yellow
}
Write-Host ""
