<#
test-luca-field-names.ps1

Amaç: baslangicTarihi alanı sunucuya hangi isimle/formatla gitmeli?
8 farklı alan adı varyasyonu ve iki encoding ile test eder:
- baslangicTarihi (utf8 / cp1254)
- BaslangicTarihi (utf8 / cp1254)
- startDate (utf8 / cp1254)
- stkSkart.baslangicTarihi (utf8 / cp1254)

Her test için request/response/headers logs: scripts/logs/field-name-test

Kullanım (repo kökünden):
  .\scripts\test-luca-field-names.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854,
    [int]$OlcumBirimiId = 5,
    [double]$KdvOran = 0.20,
    [int]$KartTipi = 4,
    [string]$KategoriKod = '001',
    [string]$LogDir = './scripts/logs/field-name-test'
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$testDate = (Get-Date).ToString('yyyy-MM-dd')
$testSKU = "FIELD-$timestamp"

$cookieJar = New-Object System.Net.CookieContainer
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Write-Text($path, $text) { Set-Content -Path $path -Value $text -Encoding UTF8 }

Write-Host "=== LOGIN ===" -ForegroundColor Yellow
$loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)Giris.do" -Method Post -Body $loginPayload -WebSession $session -Headers @{ 'Content-Type'='application/json' } -UseBasicParsing
    foreach ($c in $session.Cookies.GetCookies([Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)
        $cookieJar.Add($netCookie)
    }
    Write-Host "Login ok" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "=== BRANCH ===" -ForegroundColor Yellow
$branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json
try {
    Invoke-WebRequest -Uri "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -Method Post -Body $branchPayload -WebSession $session -Headers @{ 'Content-Type'='application/json' } -UseBasicParsing | Out-Null
    foreach ($c in $session.Cookies.GetCookies([Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)
        $cookieJar.Add($netCookie)
    }
    Write-Host "Branch ok" -ForegroundColor Green
} catch {
    Write-Host "Branch warning: $_" -ForegroundColor Yellow
}

function Send-Test($fieldName, $encodingName, $idx) {
    $payload = @{
        kartAdi = "Field Name Test $idx"
        kartKodu = "$testSKU-$idx"
        kartTuru = 1
        olcumBirimiId = $OlcumBirimiId
        perakendeSatisBirimFiyat = 100.0
        perakendeAlisBirimFiyat = 80.0
        kartAlisKdvOran = $KdvOran
        kartSatisKdvOran = $KdvOran
        kartTipi = $KartTipi
        kategoriAgacKod = $KategoriKod
    }
    $payload[$fieldName] = $testDate

    # form-encode
    $pairs = @()
    foreach ($k in $payload.Keys) {
        $v = $payload[$k]
        $pairs += "$k=$([System.Uri]::EscapeDataString($v))"
    }
    $bodyString = $pairs -join '&'

    $enc = [System.Text.Encoding]::UTF8
    if ($encodingName -eq 'cp1254') {
        try { $enc = [System.Text.Encoding]::GetEncoding(1254) } catch { $enc = [System.Text.Encoding]::UTF8 }
    }
    $bytes = $enc.GetBytes($bodyString)

    $req = [System.Net.HttpWebRequest]::Create("$($BaseUrl)EkleStkWsSkart.do")
    $req.Method = 'POST'
    $req.ContentType = "application/x-www-form-urlencoded; charset=$encodingName"
    $req.Accept = 'application/json'
    $req.CookieContainer = $cookieJar
    $req.ContentLength = $bytes.Length

    $reqStream = $req.GetRequestStream()
    $reqStream.Write($bytes,0,$bytes.Length)
    $reqStream.Close()

    $respText = ""
    $statusCode = 0
    try {
        $resp = $req.GetResponse()
        $statusCode = [int]$resp.StatusCode
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream(), $enc)
        $respText = $reader.ReadToEnd()
        $reader.Close()
    } catch [System.Net.WebException] {
        $statusCode = [int]$_.Exception.Response.StatusCode
        $respText = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()).ReadToEnd()
    }

    $prefix = ("{0:D2}-{1}-{2}" -f $idx, $fieldName, $encodingName)
    Write-Text (Join-Path $LogDir "$prefix-request.txt") $bodyString
    Write-Text (Join-Path $LogDir "$prefix-response.txt") $respText

    Write-Host ("[{0}] {1} / {2} -> HTTP {3}" -f $idx, $fieldName, $encodingName, $statusCode)
    try {
        $parsed = $respText | ConvertFrom-Json
        if ($parsed.code -eq 0 -or $parsed.code -eq 1000 -or $parsed.error -eq $false) {
            Write-Host "   SUCCESS" -ForegroundColor Green
        } else {
            Write-Host "   code=$($parsed.code) msg=$($parsed.message)" -ForegroundColor Yellow
        }
    } catch {
        if ($respText -match 'baslangic') {
            Write-Host "   baslangicTarihi error" -ForegroundColor Red
        } else {
            Write-Host "   Non-JSON/HTML" -ForegroundColor Gray
        }
    }
}

Write-Host "`n=== FIELD NAME MATRIX ===" -ForegroundColor Yellow
$tests = @(
    @{ Field='baslangicTarihi'; Encoding='utf-8' },
    @{ Field='baslangicTarihi'; Encoding='cp1254' },
    @{ Field='BaslangicTarihi'; Encoding='utf-8' },
    @{ Field='BaslangicTarihi'; Encoding='cp1254' },
    @{ Field='startDate'; Encoding='utf-8' },
    @{ Field='startDate'; Encoding='cp1254' },
    @{ Field='stkSkart.baslangicTarihi'; Encoding='utf-8' },
    @{ Field='stkSkart.baslangicTarihi'; Encoding='cp1254' }
)

$i = 1
foreach ($t in $tests) {
    Send-Test $t.Field $t.Encoding $i
    $i++
}

Write-Host "`nBitti. Loglar: $LogDir" -ForegroundColor Green
