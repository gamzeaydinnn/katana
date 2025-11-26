<#
test-luca-manual-payload.ps1

PowerShell hashtable → JSON dönüşümü veya encoding kaynaklı alan kaybını dışlamak için
stok kartı isteğini manuel JSON/string olarak gönderir. 4 farklı yaklaşımı dener:
1) UTF-8 JSON (baslangicTarihi)
2) UTF-8 JSON (BaslangicTarihi)
3) cp1254 JSON
4) Form-encoded

Loglar: scripts/logs/manual-payload-test

Kullanım (repo kökünden):
  .\scripts\test-luca-manual-payload.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
$LogDir = './scripts/logs/manual-payload-test'
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')

# --- Login ---
Write-Host "=== Login ===" -ForegroundColor Yellow
$global:cookieJar = New-Object System.Net.CookieContainer
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$loginPayload = @{
    orgCode = $OrgCode
    userName = $Username
    userPassword = $Password
} | ConvertTo-Json

try {
    $loginResp = Invoke-WebRequest -Uri "$($BaseUrl)Giris.do" -Method Post `
        -Body $loginPayload -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing
    Write-Host "Login successful" -ForegroundColor Green

    foreach ($cookie in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($cookie.Name, $cookie.Value, $cookie.Path, $cookie.Domain)
        $global:cookieJar.Add($netCookie)
    }
}
catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

# --- Branch selection ---
Write-Host "=== Branch Selection ===" -ForegroundColor Yellow
$branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json

try {
    $branchResp = Invoke-WebRequest -Uri "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -Method Post `
        -Body $branchPayload -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing
    Write-Host "Branch selected" -ForegroundColor Green

    foreach ($cookie in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($cookie.Name, $cookie.Value, $cookie.Path, $cookie.Domain)
        $global:cookieJar.Add($netCookie)
    }
}
catch {
    Write-Host "Branch warning: $_" -ForegroundColor Yellow
}

# --- Tests ---
$testDate = (Get-Date).ToString('yyyy-MM-dd')
$testSKU = "MANUAL-$timestamp"

Write-Host "`n=== Testing Different Field Names / Encodings ===" -ForegroundColor Yellow

function Save-And-Report($name, $statusCode, $respText) {
    $file = Join-Path $LogDir "$name-response.txt"
    Set-Content -Path $file -Value $respText -Encoding UTF8
    Write-Host "   Status: $statusCode"
    Write-Host "   Response: $($respText.Substring(0, [Math]::Min(200, $respText.Length)))"
    try {
        $jsonResp = $respText | ConvertFrom-Json
        if ($jsonResp.error -eq $false -or $jsonResp.id -or $jsonResp.code -eq 0 -or $jsonResp.code -eq 1000) {
            Write-Host "   SUCCESS" -ForegroundColor Green
        } else {
            $msg = $jsonResp.message
            Write-Host "   Error: $msg" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   Non-JSON response" -ForegroundColor Gray
    }
}

# Test 1: UTF-8 JSON, baslangicTarihi
Write-Host "`n[TEST 1] baslangicTarihi (utf-8 json)" -ForegroundColor Cyan
$json1 = @"
{
    "kartAdi": "Test Manual 1",
    "kartKodu": "$testSKU-T1",
    "kartTuru": 1,
    "olcumBirimiId": 5,
    "baslangicTarihi": "$testDate",
    "perakendeSatisBirimFiyat": 100.0,
    "perakendeAlisBirimFiyat": 80.0,
    "kartAlisKdvOran": 0.20,
    "kartSatisKdvOran": 0.20,
    "kategoriAgacKod": "001",
    "kartTipi": 4
}
"@
    Set-Content -Path (Join-Path $LogDir "test1-request.json") -Value $json1 -Encoding UTF8
try {
    $resp1 = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json1 -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json; charset=utf-8' } -UseBasicParsing
    Save-And-Report "test1" $resp1.StatusCode $resp1.Content
} catch {
    Write-Host "   ❌ Request failed: $_" -ForegroundColor Red
}

# Test 2: UTF-8 JSON, BaslangicTarihi
Write-Host "`n[TEST 2] BaslangicTarihi (utf-8 json, PascalCase)" -ForegroundColor Cyan
$json2 = @"
{
    "kartAdi": "Test Manual 2",
    "kartKodu": "$testSKU-T2",
    "kartTuru": 1,
    "olcumBirimiId": 5,
    "BaslangicTarihi": "$testDate",
    "perakendeSatisBirimFiyat": 100.0,
    "perakendeAlisBirimFiyat": 80.0,
    "kartAlisKdvOran": 0.20,
    "kartSatisKdvOran": 0.20,
    "kategoriAgacKod": "001",
    "kartTipi": 4
}
"@
    Set-Content -Path (Join-Path $LogDir "test2-request.json") -Value $json2 -Encoding UTF8
try {
    $resp2 = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json2 -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json; charset=utf-8' } -UseBasicParsing
    Save-And-Report "test2" $resp2.StatusCode $resp2.Content
} catch {
    Write-Host "   ❌ Request failed: $_" -ForegroundColor Red
}

# Test 3: Windows-1254 JSON
Write-Host "`n[TEST 3] baslangicTarihi (json cp1254)" -ForegroundColor Cyan
try {
    $enc1254 = [System.Text.Encoding]::GetEncoding(1254)
    $bytes = $enc1254.GetBytes($json1)

    $req = [System.Net.HttpWebRequest]::Create("$($BaseUrl)EkleStkWsSkart.do")
    $req.Method = 'POST'
    $req.ContentType = 'application/json; charset=windows-1254'
    $req.Accept = 'application/json'
    $req.CookieContainer = $global:cookieJar
    $req.ContentLength = $bytes.Length

    $reqStream = $req.GetRequestStream()
    $reqStream.Write($bytes, 0, $bytes.Length)
    $reqStream.Close()

    $resp = $req.GetResponse()
    $respStream = $resp.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($respStream, $enc1254)
    $responseText = $reader.ReadToEnd()
    $reader.Close()

    Save-And-Report "test3" $resp.StatusCode $responseText
} catch {
    Write-Host "   ❌ Request failed: $_" -ForegroundColor Red
}

# Test 4: Form-encoded
Write-Host "`n[TEST 4] Form-encoded" -ForegroundColor Cyan
$pairs = @()
$pairs += "kartAdi=Test+Manual+4"
$pairs += "kartKodu=$testSKU-T4"
$pairs += "kartTuru=1"
$pairs += "olcumBirimiId=5"
$pairs += "baslangicTarihi=$testDate"
$pairs += "perakendeSatisBirimFiyat=100.0"
$pairs += "perakendeAlisBirimFiyat=80.0"
$pairs += "kartAlisKdvOran=0.20"
$pairs += "kartSatisKdvOran=0.20"
$pairs += "kategoriAgacKod=001"
$pairs += "kartTipi=4"
$formData = $pairs -join '&'
Set-Content -Path (Join-Path $LogDir "test4-request.txt") -Value $formData -Encoding UTF8
try {
    $resp4 = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $formData -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/x-www-form-urlencoded; charset=utf-8' } -UseBasicParsing
    Save-And-Report "test4" $resp4.StatusCode $resp4.Content
} catch {
    Write-Host "   ❌ Request failed: $_" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Yellow
Write-Host "Logs saved to: $LogDir`n"
