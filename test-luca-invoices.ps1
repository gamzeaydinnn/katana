# Luca Fatura API Test Script
# Bu script tum fatura endpoint'lerini test eder

$baseUrl = "http://localhost:5055/api/luca-invoices"

Write-Host "Luca Fatura API Test Scripti" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. Session Durumu Kontrol
Write-Host "1. Session durumu kontrol ediliyor..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/session-status" -Method Get
    Write-Host "OK - Session Status: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "HATA - Session kontrol hatasi: $_" -ForegroundColor Red
}
Write-Host ""

# 2. Session Yenileme (Gerekirse)
Write-Host "2. Session yenileniyor..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/refresh-session" -Method Post
    Write-Host "OK - Session yenilendi" -ForegroundColor Green
} catch {
    Write-Host "HATA - Session yenileme hatasi: $_" -ForegroundColor Red
}
Write-Host ""

# 3. Fatura Listesi
Write-Host "3. Fatura listesi aliniyor..." -ForegroundColor Yellow
$listRequest = @{
    parUstHareketTuru = "16"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/list" -Method Post -Body $listRequest -ContentType "application/json"
    Write-Host "OK - Fatura sayisi: $($response.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "HATA - Fatura listesi hatasi: $_" -ForegroundColor Red
}
Write-Host ""

# 4. Dovizli Fatura Listesi
Write-Host "4. Dovizli fatura listesi aliniyor (USD)..." -ForegroundColor Yellow
$currencyRequest = @{
    ftrSsFaturaBaslik = @{}
    gnlParaBirimRapor = @{
        paraBirimId = 4
    }
    parUstHareketTuru = "16"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/list-currency" -Method Post -Body $currencyRequest -ContentType "application/json"
    Write-Host "OK - Dovizli fatura sayisi: $($response.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "HATA - Dovizli fatura listesi hatasi: $_" -ForegroundColor Red
}
Write-Host ""

# 5. Fatura Olusturma (Test)
Write-Host "5. Test faturasi olusturuluyor..." -ForegroundColor Yellow
$createRequest = @{
    belgeTarihi = "12/12/2025"
    duzenlemeSaati = "14:30"
    vadeTarihi = "12/12/2025"
    belgeAciklama = "Test Faturasi - Katana Integration"
    belgeTurDetayId = "76"
    faturaTur = "1"
    paraBirimKod = "TRY"
    kdvFlag = $true
    musteriTedarikci = "1"
    kurBedeli = 1.0
    detayList = @(
        @{
            kartTuru = 1
            kartKodu = "00003"
            birimFiyat = 100.0
            miktar = 1
            tutar = 100.0
            kdvOran = 0.20
            depoKodu = "000.003.001"
        }
    )
    cariKodu = "00000017"
    cariTip = 1
    cariTanim = "VOLKAN UNAL"
    cariKisaAd = "VOLKAN UNAL"
    cariYasalUnvan = "VOLKAN UNAL"
    vergiNo = "12"
    il = "ANKARA"
    ilce = "CANKAYA"
    odemeTipi = "KREDIKARTI_BANKAKARTI"
    gonderimTipi = "ELEKTRONIK"
    efaturaTuru = 1
} | ConvertTo-Json -Depth 5

Write-Host "DEBUG - Gonderilen JSON:" -ForegroundColor Cyan
Write-Host $createRequest -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/create" -Method Post -Body $createRequest -ContentType "application/json"
    
    Write-Host "DEBUG - Response:" -ForegroundColor Cyan
    Write-Host ($response | ConvertTo-Json -Depth 5) -ForegroundColor Cyan
    
    if ($response.success) {
        $invoiceId = $response.data.ssFaturaBaslikId
        Write-Host "OK - Fatura olusturuldu! ID: $invoiceId" -ForegroundColor Green
        
        # 6. Fatura PDF Linki
        Write-Host ""
        Write-Host "6. Fatura PDF linki aliniyor..." -ForegroundColor Yellow
        $pdfRequest = @{
            ssFaturaBaslikId = $invoiceId
        } | ConvertTo-Json
        
        try {
            $pdfResponse = Invoke-RestMethod -Uri "$baseUrl/pdf-link" -Method Post -Body $pdfRequest -ContentType "application/json"
            Write-Host "OK - PDF Link: $($pdfResponse.data.pdfLink)" -ForegroundColor Green
        } catch {
            Write-Host "HATA - PDF link hatasi: $_" -ForegroundColor Red
        }
        
        # 7. Fatura Silme (Test faturasini temizle)
        Write-Host ""
        Write-Host "7. Test faturasi siliniyor..." -ForegroundColor Yellow
        try {
            $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/$invoiceId" -Method Delete
            Write-Host "OK - Test faturasi silindi" -ForegroundColor Green
        } catch {
            Write-Host "UYARI - Fatura silinirken hata: $_" -ForegroundColor Yellow
        }
    } else {
        Write-Host "HATA - Fatura olusturulamadi: $($response.error)" -ForegroundColor Red
    }
} catch {
    Write-Host "HATA - Fatura olusturma hatasi: $_" -ForegroundColor Red
    Write-Host "Detay: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test tamamlandi!" -ForegroundColor Green
Write-Host ""
Write-Host "Notlar:" -ForegroundColor Cyan
Write-Host "- Eger 'HTML response' hatasi aliyorsaniz, session yenileyin" -ForegroundColor White
Write-Host "- Cari kodlari ve stok kodlari sisteminize gore guncelleyin" -ForegroundColor White
Write-Host "- Detayli loglar icin logs/ klasorunu kontrol edin" -ForegroundColor White
