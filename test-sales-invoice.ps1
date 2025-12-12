# LUCA SATIS FATURASI TEST SCRIPTI
$baseUrl = "http://localhost:5055"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "LUCA SATIS FATURASI TESTI" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Luca stok kartlari
$lucaStockCodes = @(
    @{ KartKodu = "0200B501-0003"; KartAdi = "KAYNAKLI BORU UCU O R"; BirimFiyat = 150.00; Miktar = 2 },
    @{ KartKodu = "0200B501-A"; KartAdi = "0200B501 BUKUMLU BORU"; BirimFiyat = 200.00; Miktar = 1 },
    @{ KartKodu = "020555/CBK"; KartAdi = "VIT-112 RENAULT KANGO"; BirimFiyat = 350.00; Miktar = 3 },
    @{ KartKodu = "0205B301-A"; KartAdi = "BUKUMLU BORU 0205B30"; BirimFiyat = 180.00; Miktar = 2 }
)

$belgeTurDetayId = 76

$detayList = @()
foreach ($item in $lucaStockCodes) {
    $detayList += @{
        kartTuru = 1
        kartKodu = $item.KartKodu
        kartAdi = $item.KartAdi
        birimFiyat = $item.BirimFiyat
        miktar = $item.Miktar
        kdvOran = 0.20
        tutar = ($item.BirimFiyat * $item.Miktar)
        depoKodu = "01"
    }
}

$invoiceRequest = @{
    belgeSeri = "A"
    belgeTarihi = (Get-Date).ToString("dd/MM/yyyy")
    vadeTarihi = (Get-Date).AddDays(30).ToString("dd/MM/yyyy")
    belgeTakipNo = "SF-" + (Get-Date).ToString("yyyyMMdd-HHmmss")
    belgeAciklama = "Test satis faturasi - Katana Integration"
    belgeTurDetayId = $belgeTurDetayId
    faturaTur = 1
    paraBirimKod = "TRY"
    kurBedeli = 1
    babsFlag = $false
    kdvFlag = $true
    musteriTedarikci = 1
    cariKodu = "MUSTERI-001"
    cariTanim = "Test Musteri A.S."
    cariTip = 1
    cariKisaAd = "Test Musteri"
    cariYasalUnvan = "Test Musteri Anonim Sirketi"
    vergiNo = "1234567890"
    vergiDairesi = "Kadikoy"
    adresSerbest = "Test Adres, Istanbul"
    iletisimTanim = "0212 555 1234"
    detayList = $detayList
}

Write-Host ""
Write-Host "Fatura Bilgileri:" -ForegroundColor Yellow
Write-Host "   Belge Seri: $($invoiceRequest.belgeSeri)"
Write-Host "   Belge Tarihi: $($invoiceRequest.belgeTarihi)"
Write-Host "   Belge Turu Detay ID: $belgeTurDetayId (Mal Satis Faturasi)"
Write-Host "   Musteri: $($invoiceRequest.cariTanim)"
Write-Host "   Kalem Sayisi: $($detayList.Count)"
Write-Host ""

Write-Host "Fatura Kalemleri:" -ForegroundColor Yellow
foreach ($item in $detayList) {
    $toplam = $item.birimFiyat * $item.miktar
    Write-Host "   - $($item.kartKodu): $($item.kartAdi) x $($item.miktar) = $toplam TL"
}

$toplamTutar = 0
foreach ($item in $detayList) {
    $toplamTutar += ($item.birimFiyat * $item.miktar)
}
Write-Host ""
Write-Host "   TOPLAM: $toplamTutar TL" -ForegroundColor Green
Write-Host ""

$jsonBody = $invoiceRequest | ConvertTo-Json -Depth 10

Write-Host "Fatura Luca'ya gonderiliyor..." -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sync/to-luca/sales-invoice" -Method POST -Body $jsonBody -ContentType "application/json; charset=utf-8" -ErrorAction Stop
    
    Write-Host "BASARILI!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5
}
catch {
    Write-Host "HATA!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host "   Details: $($_.ErrorDetails.Message)"
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
