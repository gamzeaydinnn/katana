# Direkt Luca'ya stok karti olusturma testi
$ErrorActionPreference = "Continue"

# Oncelikle session al
Write-Host "1. Session aliniyor..." -ForegroundColor Cyan
$loginBody = @{
    orgCode = "akozas"
    userName = "ENTEGRASYON"
    userPassword = "Ent2025!"
} | ConvertTo-Json

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginResp = Invoke-WebRequest -Uri "https://akozas.luca.com.tr/luca-rs/rest/login" `
    -Method POST `
    -Body $loginBody `
    -ContentType "application/json; charset=utf-8" `
    -WebSession $session `
    -UseBasicParsing

Write-Host "Login Response: $($loginResp.StatusCode)" -ForegroundColor Green
Write-Host "Cookies: $($session.Cookies.GetCookies('https://akozas.luca.com.tr') | ForEach-Object { $_.Name + '=' + $_.Value.Substring(0, [Math]::Min(20, $_.Value.Length)) + '...' })" -ForegroundColor Yellow

# Branch sec
Write-Host "`n2. Branch seciliyor..." -ForegroundColor Cyan
$branchBody = '{"orgSirketSubeId":11746}'
$branchResp = Invoke-WebRequest -Uri "https://akozas.luca.com.tr/luca-rs/rest/changeBranch" `
    -Method POST `
    -Body $branchBody `
    -ContentType "application/json; charset=utf-8" `
    -WebSession $session `
    -UseBasicParsing

Write-Host "Branch Response: $($branchResp.StatusCode)" -ForegroundColor Green

# Stok karti olustur - POSTMAN ORNEGI
Write-Host "`n3. Stok karti olusturuluyor..." -ForegroundColor Cyan

$stockCard = @{
    kartAdi = "TEST-KIRO-001"
    kartKodu = "TEST-KIRO-001"
    kartTipi = 4
    kartAlisKdvOran = 1
    kartSatisKdvOran = 1
    olcumBirimiId = 5
    baslangicTarihi = (Get-Date).ToString("dd/MM/yyyy")
    kartTuru = 1
    stokKategoriId = 1
    barkod = "TEST-KIRO-001"
    satilabilirFlag = 1
    satinAlinabilirFlag = 1
    lotNoFlag = 0
    minStokKontrol = 0
    maliyetHesaplanacakFlag = 1
    gtipKodu = ""
    ihracatKategoriNo = ""
    detayAciklama = ""
    stopajOran = 0
    alisIskontoOran1 = 0
    satisIskontoOran1 = 0
    perakendeAlisBirimFiyat = 0
    perakendeSatisBirimFiyat = 0
    rafOmru = 0
    garantiSuresi = 0
    uzunAdi = "TEST-KIRO-001"
}

$stockJson = $stockCard | ConvertTo-Json -Depth 10
Write-Host "Request JSON:" -ForegroundColor Yellow
Write-Host $stockJson

$stockResp = Invoke-WebRequest -Uri "https://akozas.luca.com.tr/luca-rs/rest/stkwsStokKarti/kaydet" `
    -Method POST `
    -Body $stockJson `
    -ContentType "application/json; charset=utf-8" `
    -WebSession $session `
    -UseBasicParsing

Write-Host "`n=== RESPONSE ===" -ForegroundColor Magenta
Write-Host "Status: $($stockResp.StatusCode)" -ForegroundColor Green
Write-Host "Headers:" -ForegroundColor Yellow
$stockResp.Headers | Format-Table -AutoSize
Write-Host "Body:" -ForegroundColor Yellow
Write-Host $stockResp.Content
