<#
test-luca-correct-format.ps1

Hedef: Luca/Koza stok kartı endpointine kullanıcıdan gelen örnek formatı 3 farklı varyasyonla test ederek
- Tarih dd/MM/yyyy
- Boolean olmayan flag'ler 0/1 olarak
- Wrapper yok, root seviyede alanlar
- satilabilirFlag, satinAlinabilirFlag, lotNoFlag, maliyetHesaplanacakFlag (+ minStokKontrol) kesinlikle gönderiliyor

Üç test:
  1) Koza örneğine göre tam payload (tüm alanlar)
  2) Sadece zorunlu alanlar
  3) Zorunlu + fiyat alanları

Kullanım (repo kökünden):
    .\scripts\test-luca-correct-format.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 11746
#>

param(
    [string]$BaseUrl = 'https://akozas.luca.com.tr/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746,
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
$bitisDate = (Get-Date).AddMonths(6).ToString('dd/MM/yyyy')
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

Write-Host "`n=== CORRECT FORMAT TESTS ===" -ForegroundColor Cyan

$endpoint = "$BaseUrl`EkleStkWsSkart.do"

$flags = @{
    maliyetHesaplanacakFlag = 1
    satilabilirFlag = 1
    satinAlinabilirFlag = 1
    lotNoFlag = 0
    minStokKontrol = 0
}

$basePayload = @{
    kartAdi = "Correct Format $testSKU"
    kartKodu = $testSKU
    kartTuru = $KartTuru
    baslangicTarihi = $testDate
    olcumBirimiId = $OlcumBirimiId
    kategoriAgacKod = $KategoriKod
    kartTipi = $KartTipi
}

function Send-JsonTest($name, $payload, $description) {
    Write-Host "`n[$name] $description" -ForegroundColor Yellow
    $jsonPayload = $payload | ConvertTo-Json -Depth 4
    Save-Log "$name-request.json" $jsonPayload

    try {
        # send as windows-1254 to match Koza expected charset
        $headers = @{ 'Content-Type' = 'application/json; charset=windows-1254' }
        # convert to cp1254 bytes then send
        $encJson = [System.Text.Encoding]::GetEncoding(1254).GetBytes($jsonPayload)
        $resp = Invoke-WebRequest -Uri $endpoint -Method Post -Body $encJson `
            -WebSession $session `
            -ContentType $headers['Content-Type'] -Headers $headers -UseBasicParsing
        Save-Log "$name-response.json" $resp.Content
        Write-Host "   HTTP $($resp.StatusCode)" -ForegroundColor Green
        try {
            $parsed = $resp.Content | ConvertFrom-Json
            Write-Host ("   code: {0} message: {1}" -f $parsed.code, $parsed.message) -ForegroundColor Cyan
        } catch {
            Write-Host "   Response is not JSON" -ForegroundColor Gray
        }
    } catch {
        $body = ''
        if ($_.Exception.Response -ne $null) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
        }
        Save-Log "$name-response.json" $body
        Write-Host "   Request failed: $_" -ForegroundColor Red
    }
}

$test1 = $basePayload + @{
    kartAlisKdvOran = $KartAlisKdvOran
    kartSatisKdvOran = $KartSatisKdvOran
    perakendeAlisBirimFiyat = $PerakendeAlisBirimFiyat
    perakendeSatisBirimFiyat = $PerakendeSatisBirimFiyat
    barkod = "$testSKU-B"
    rafOmru = 365
    bitisTarihi = $bitisDate
    gtipKodu = '01010101'
    garantiSuresi = 12
} + $flags

$test2 = $basePayload + $flags

$test3 = $basePayload + @{
    kartAlisKdvOran = $KartAlisKdvOran
    kartSatisKdvOran = $KartSatisKdvOran
    perakendeAlisBirimFiyat = $PerakendeAlisBirimFiyat
    perakendeSatisBirimFiyat = $PerakendeSatisBirimFiyat
} + $flags

Send-JsonTest 'test1-full' $test1 'EXACT format from Koza example (full field set)'
Send-JsonTest 'test2-minimal' $test2 'Minimal required fields only'
Send-JsonTest 'test3-prices' $test3 'Required fields plus price-related values'

Write-Host "`nLog directory: $LogDir" -ForegroundColor Yellow
