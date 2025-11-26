<#
test-luca-stock-create-variants.ps1

Koza stok kartı oluşturma için farklı payload + encoding kombinasyonlarını dener.
Her deneme için istek/yanıt/headers dosyalarını scripts/logs/stock-create-variants altına yazar.

Kullanım (repo kökünden):
  .\scripts\test-luca-stock-create-variants.ps1 -Username "Admin" -Password "2009Bfm" -OrgCode "1422649" -BranchId 854

Alternatif: Mevcut JSESSIONID vermek için -JSessionId parametresi kullanılabilir (login atlanır).
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = '',
    [string]$Password = '',
    [string]$JSessionId = '',
    [int]$BranchId = 854,
    [int]$DefaultOlcumBirimiId = 5,
    [double]$DefaultKdvOran = 0.20,
    [int]$DefaultKartTipi = 4,
    [string]$DefaultKategoriAgacKod = '001',
    [string]$LogDir = '.\scripts\logs\stock-create-variants'
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

function Write-JsonFile($path, $obj) {
    $json = $null
    if ($obj -is [string]) { $json = $obj } else { $json = $obj | ConvertTo-Json -Depth 10 }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

function New-Session() {
    return New-Object Microsoft.PowerShell.Commands.WebRequestSession
}

function Login-IfNeeded([Microsoft.PowerShell.Commands.WebRequestSession]$session) {
    if (-not [string]::IsNullOrWhiteSpace($JSessionId)) {
        try {
            $cookie = New-Object System.Net.Cookie('JSESSIONID', $JSessionId, '/', ([Uri]$BaseUrl).Host)
            $session.Cookies.Add($cookie)
            Write-Host "[i] Using provided JSESSIONID (skipping Giris.do)" -ForegroundColor Cyan
            return $true
        } catch {
            Write-Warning "Provided JSESSIONID could not be added: $_"
            return $false
        }
    }

    if ([string]::IsNullOrWhiteSpace($Username) -or [string]::IsNullOrWhiteSpace($Password)) {
        Write-Warning "Username/Password missing and no JSESSIONID supplied."
        return $false
    }

    $loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Depth 5
    $loginPath = Join-Path $LogDir '01-login-request.json'
    Write-JsonFile $loginPath $loginPayload

    try {
        $resp = Invoke-WebRequest -Uri ($BaseUrl + 'Giris.do') -Method Post -Body $loginPayload -WebSession $session -Headers @{ 'Content-Type' = 'application/json'; 'Accept' = 'application/json' } -UseBasicParsing -ErrorAction Stop
        Write-JsonFile (Join-Path $LogDir '01-login-response.json') $resp.Content
        $cookieDump = @()
        foreach ($c in $session.Cookies.GetCookies([Uri]$BaseUrl)) {
            $cookieDump += [PSCustomObject]@{ Name=$c.Name; Value=$c.Value; Domain=$c.Domain; Path=$c.Path; Expires=$c.Expires; HttpOnly=$c.HttpOnly }
            if ($c.Name -eq 'JSESSIONID') { $script:JSessionId = $c.Value }
        }
        Write-JsonFile (Join-Path $LogDir '01-cookies-after-login.json') $cookieDump
        Write-Host "[OK] Login tamam. JSESSIONID=$JSessionId" -ForegroundColor Green
        return $true
    } catch {
        Write-Warning "Login failed: $_"
        return $false
    }
}

function Select-Branch([Microsoft.PowerShell.Commands.WebRequestSession]$session, [int]$branchId) {
    try {
        $payloads = @(
            @{ orgSirketSubeId = $branchId },
            @{ vtOrgYtkSirketSubeId = $branchId },
            @{ orgSirketSube = $branchId }
        )
        foreach ($p in $payloads) {
            $json = $p | ConvertTo-Json -Depth 5
            $resp = Invoke-WebRequest -Uri ($BaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $json -WebSession $session -Headers @{ 'Content-Type'='application/json'; 'Accept'='application/json' } -UseBasicParsing -ErrorAction Stop
            Write-JsonFile (Join-Path $LogDir '02-branch-change-response.json') $resp.Content
            return $true
        }
    } catch {
        Write-Warning "Branch selection failed: $_"
    }
    return $false
}

function Verify-Branch([Microsoft.PowerShell.Commands.WebRequestSession]$session) {
    try {
        $resp = Invoke-WebRequest -Uri ($BaseUrl + 'YdlUserResponsibilityOrgSs.do') -Method Post -Body '{}' -WebSession $session -Headers @{ 'Content-Type'='application/json'; 'Accept'='application/json' } -UseBasicParsing -ErrorAction Stop
        Write-JsonFile (Join-Path $LogDir '03-branches-after-change.json') $resp.Content
    } catch {
        Write-Warning "Branch verify failed: $_"
    }
}

function Send-Variant([Microsoft.PowerShell.Commands.WebRequestSession]$session, [string]$name, $payloadObj, [string]$encodingName) {
    $payloadJson = $payloadObj | ConvertTo-Json -Depth 10
    $reqFile = Join-Path $LogDir ("$name-request.json")
    Write-JsonFile $reqFile $payloadJson

    $encoding = [System.Text.Encoding]::UTF8
    if ($encodingName -eq 'cp1254') {
        try { $encoding = [System.Text.Encoding]::GetEncoding(1254) } catch { $encoding = [System.Text.Encoding]::UTF8 }
    }

    $bytes = $encoding.GetBytes($payloadJson)
    $req = [System.Net.HttpWebRequest]::Create($BaseUrl + 'EkleStkWsSkart.do')
    $req.Method = 'POST'
    $req.ContentType = "application/json; charset=$encodingName"
    $req.Accept = 'application/json'
    $req.CookieContainer = New-Object System.Net.CookieContainer
    $baseUri = [Uri]$BaseUrl
    foreach ($c in $session.Cookies.GetCookies($baseUri)) {
        $netCookie = New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)
        $req.CookieContainer.Add($netCookie)
    }

    $req.ContentLength = $bytes.Length
    $stream = $req.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()

    $resp = $null
    $respText = ""
    $statusCode = 0
    try {
        $resp = $req.GetResponse()
        $statusCode = [int]$resp.StatusCode
        $respStream = $resp.GetResponseStream()
        $ms = New-Object System.IO.MemoryStream
        $buf = New-Object byte[] 8192
        while (($read = $respStream.Read($buf,0,$buf.Length)) -gt 0) { $ms.Write($buf,0,$read) }
        $respBytes = $ms.ToArray()
        $respStream.Close()

        $respCharset = $resp.Headers['Content-Type'] -replace '.*charset=',''
        if ($respCharset -match '1254') {
            try { $respText = [System.Text.Encoding]::GetEncoding(1254).GetString($respBytes) } catch { $respText = [System.Text.Encoding]::UTF8.GetString($respBytes) }
        } else {
            $respText = [System.Text.Encoding]::UTF8.GetString($respBytes)
        }
    } catch [System.Net.WebException] {
        $statusCode = [int]$_.Exception.Response.StatusCode
        $resp = $_.Exception.Response
        $respText = New-Object System.IO.StreamReader($resp.GetResponseStream()).ReadToEnd()
    }

    $respPath = Join-Path $LogDir ("$name-response.txt")
    Set-Content -Path $respPath -Value $respText -Encoding UTF8
    if ($resp -ne $null) {
        $hdrPath = Join-Path $LogDir ("$name-headers.txt")
        Set-Content -Path $hdrPath -Value $resp.Headers.ToString() -Encoding UTF8
    }

    $jsonOk = $false
    $code = $null
    try {
        $parsed = $respText | ConvertFrom-Json
        if ($parsed -and $parsed.code -ne $null) {
            $code = $parsed.code
            if ($code -eq 0 -or $code -eq 1000) { $jsonOk = $true }
        }
    } catch { }

    $looksHtml = $respText -match '<html' -or $respText -match '<HTML'

    return [PSCustomObject]@{
        Name = $name
        StatusCode = $statusCode
        Code = $code
        IsJsonSuccess = $jsonOk
        IsHtml = $looksHtml
        ResponseFile = $respPath
    }
}

# --- Başlangıç ---
Write-Host "== KOZA STOCK CREATE VARIANT TESTER ==" -ForegroundColor Cyan
$session = New-Session

$loggedIn = Login-IfNeeded -session $session
if (-not $loggedIn) { Write-Host "Aborting (login missing/failed)." -ForegroundColor Red; exit 1 }

$branchOk = Select-Branch -session $session -branchId $BranchId
if (-not $branchOk) { Write-Host "Aborting (branch selection failed)." -ForegroundColor Red; exit 1 }
Verify-Branch -session $session

$nowSku = "TEST-" + (Get-Date).ToString("yyyyMMddHHmmss")

$baseMinimal = @{
    kartAdi = "Variant Minimal"
    kartKodu = $nowSku
    kartTuru = 1
    olcumBirimiId = $DefaultOlcumBirimiId
    kartAlisKdvOran = $DefaultKdvOran
    kartSatisKdvOran = $DefaultKdvOran
    kartTipi = $DefaultKartTipi
    kategoriAgacKod = $DefaultKategoriAgacKod
}

$withFlags = $baseMinimal.Clone()
$withFlags.SatilabilirFlag = $true
$withFlags.SatinAlinabilirFlag = $true
$withFlags.MaliyetHesaplanacakFlag = $true

$extended = $withFlags.Clone()
$extended.kartAlisKdvOran = 0.20
$extended.kartSatisKdvOran = 0.20
$extended.kartTipi = 4
$extended.kategoriAgacKod = "001"
$extended.perakendeSatisBirimFiyat = 100
$extended.perakendeAlisBirimFiyat = 80

$variants = @(
    @{ Name="01-minimal-utf8"; Payload=$baseMinimal; Encoding="utf-8" },
    @{ Name="02-minimal-cp1254"; Payload=$baseMinimal; Encoding="cp1254" },
    @{ Name="03-flags-utf8"; Payload=$withFlags; Encoding="utf-8" },
    @{ Name="04-flags-cp1254"; Payload=$withFlags; Encoding="cp1254" },
    @{ Name="05-extended-utf8"; Payload=$extended; Encoding="utf-8" },
    @{ Name="06-extended-cp1254"; Payload=$extended; Encoding="cp1254" }
)

$results = @()
foreach ($v in $variants) {
    Write-Host ("[TEST] {0} ({1})" -f $v.Name, $v.Encoding) -ForegroundColor Yellow
    $res = Send-Variant -session $session -name $v.Name -payloadObj $v.Payload -encodingName $v.Encoding
    $results += $res
    Write-Host (" -> Status={0} Code={1} Html={2}" -f $res.StatusCode, $res.Code, $res.IsHtml)
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
foreach ($r in $results) {
    $status = if ($r.IsJsonSuccess -and -not $r.IsHtml) { "✅" } elseif ($r.IsHtml) { "❌ HTML" } else { "⚠️" }
    Write-Host ("{0} {1} (HTTP {2}, code={3}) -> {4}" -f $status, $r.Name, $r.StatusCode, $r.Code, $r.ResponseFile)
}

Write-Host "`nLogs under: $LogDir" -ForegroundColor Green
