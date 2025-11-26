<#
test-luca-correct-format.ps1

Hedef: Luca/Koza stok kartı endpointine kullanıcının paylaştığı doğru formatta tek bir payload gönderip doğrudan root seviyeden çalışır hale gelmesini test eder.
Format kuralları:
 1. Tarih dd/MM/yyyy (örn. 06/04/2022)
 2. Boolean alanlar integer (1 veya 0)
 3. Wrapper yok, alanlar root seviyeden gönderiliyor
 4. satilabilirFlag, satinAlinabilirFlag, lotNoFlag ve maliyetHesaplanacakFlag kesinlikle mevcut olmalı

Kullanım (repo kökünden):
  .\scripts\test-luca-correct-format.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854,
    [int]$OlcumBirimiId = 5,
    [int]$KartTuru = 1,
    [int]$KartTipi = 4,
    [string]$KategoriKod = '001',
    [double]$KartAlisKdvOran = 0.20,
    [double]$KartSatisKdvOran = 0.20,
    [double]$PerakendeAlisBirimFiyat = 80.0,
    [double]$PerakendeSatisBirimFiyat = 100.0,
    [string]$LogDir = './scripts/logs/correct-format'
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$testDate = (Get-Date).ToString('dd/MM/yyyy')
$testSKU = "CORRECT-$timestamp"

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Save-Log($name, $content) {
    Set-Content -Path (Join-Path $LogDir $name) -Value $content -Encoding UTF8
}

Write-Host "=== LOGIN ===" -ForegroundColor Yellow
$loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json
try {
    $loginResp = Invoke-WebRequest -Uri "$BaseUrl`Giris.do" -Method Post `
        -Body $loginPayload -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing
    Write-Host "Login succeeded" -ForegroundColor Green
}
catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "=== BRANCH ===" -ForegroundColor Yellow
$branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json
try {
    Invoke-WebRequest -Uri "$BaseUrl`GuncelleYtkSirketSubeDegistir.do" -Method Post `
        -Body $branchPayload -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing | Out-Null
    Write-Host "Branch selected" -ForegroundColor Green
}
catch {
    Write-Host "Branch selection warning: $_" -ForegroundColor Yellow
}

Write-Host "`n=== CORRECT PAYLOAD ===" -ForegroundColor Cyan
$payload = @{
    kartAdi = "Correct Format $testSKU"
    kartKodu = $testSKU
    kartTuru = $KartTuru
    baslangicTarihi = $testDate
    olcumBirimiId = $OlcumBirimiId
    kategoriAgacKod = $KategoriKod
    kartTipi = $KartTipi
    kartAlisKdvOran = $KartAlisKdvOran
    kartSatisKdvOran = $KartSatisKdvOran
    perakendeAlisBirimFiyat = $PerakendeAlisBirimFiyat
    perakendeSatisBirimFiyat = $PerakendeSatisBirimFiyat
    maliyetHesaplanacakFlag = 1
    satilabilirFlag = 1
    satinAlinabilirFlag = 1
    lotNoFlag = 1
}

$jsonPayload = $payload | ConvertTo-Json -Depth 3
Save-Log 'correct-format-request.json' $jsonPayload

$endpoint = "$BaseUrl`EkleStkWsSkart.do"
try {
    $resp = Invoke-WebRequest -Uri $endpoint -Method Post -Body $jsonPayload `
        -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json; charset=utf-8' } -UseBasicParsing
    Save-Log 'correct-format-response.json' $resp.Content
    Write-Host "HTTP $($resp.StatusCode)" -ForegroundColor Green
    try {
        $parsed = $resp.Content | ConvertFrom-Json
        Write-Host ("code: {0} message: {1}" -f $parsed.code, $parsed.message) -ForegroundColor Cyan
    } catch {
        Write-Host "Response is not JSON" -ForegroundColor Gray
    }
}
catch {
    $body = ''
    if ($_.Exception.Response -ne $null) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        $reader.Close()
    }
    Save-Log 'correct-format-response.json' $body
    Write-Host "Request failed: $_" -ForegroundColor Red
}

Write-Host "`nLog directory: $LogDir" -ForegroundColor Yellow
