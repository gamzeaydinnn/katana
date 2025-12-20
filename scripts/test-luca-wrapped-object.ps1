<#
test-luca-wrapped-object.ps1

Koza stok kartı endpointinin sarılmış (wrapped) JSON bekleyip beklemediğini test eder.
5 deneme:
1) {"stkSkart": {...}}
2) {"stkWsSkart": {...}}
3) Root PascalCase alanlar
4) gnl pattern wrapper (stkSkart)
5) Form-encoded + stkSkart. prefix

Loglar: scripts/logs/wrapped-test

Kullanım:
  .\scripts\test-luca-wrapped-object.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746,
    [int]$OlcumBirimiId = 5,
    [double]$KdvOran = 0.20,
    [int]$KartTipi = 4,
    [string]$KategoriKod = '001'
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
$LogDir = './scripts/logs/wrapped-test'
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
# Force dd/MM/yyyy to match Koza expectation
$testDate = (Get-Date).ToString('dd/MM/yyyy')
$testSKU = "WRAP-$timestamp"

$cookieJar = New-Object System.Net.CookieContainer
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Save-Text($path, $text) { Set-Content -Path $path -Value $text -Encoding UTF8 }

Write-Host "=== Session Init ===" -ForegroundColor Yellow
$loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json
try {
    $loginResp = Invoke-WebRequest -Uri "$BaseUrl`Giris.do" -Method Post -Body $loginPayload -WebSession $session -Headers @{ 'Content-Type'='application/json' } -UseBasicParsing
    foreach ($c in $session.Cookies.GetCookies([Uri]$BaseUrl)) {
        $cookieJar.Add((New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)))
    }
    Write-Host "✅ Login" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "=== Branch Selection ===" -ForegroundColor Yellow
$branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json
try {
    Invoke-WebRequest -Uri "$BaseUrl`GuncelleYtkSirketSubeDegistir.do" -Method Post -Body $branchPayload -WebSession $session -Headers @{ 'Content-Type'='application/json' } -UseBasicParsing | Out-Null
    foreach ($c in $session.Cookies.GetCookies([Uri]$BaseUrl)) {
        $cookieJar.Add((New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)))
    }
    Write-Host "✅ Branch" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Branch warning: $_" -ForegroundColor Yellow
}

function Send-JsonTest($name, $jsonBody) {
    Save-Text (Join-Path $LogDir "$name-request.json") $jsonBody
    try {
        # Send as windows-1254 encoded JSON
        $headersLocal = @{ 'Content-Type' = 'application/json; charset=windows-1254' }
        $encBody = [System.Text.Encoding]::GetEncoding(1254).GetBytes($jsonBody)
        $resp = Invoke-WebRequest -Uri "$BaseUrl`EkleStkWsSkart.do" -Method Post -Body $encBody -WebSession $session -ContentType $headersLocal['Content-Type'] -Headers $headersLocal -UseBasicParsing
        $content = $resp.Content
        Save-Text (Join-Path $LogDir "$name-response.txt") $content
        Write-Host "[$name] HTTP $($resp.StatusCode)"
        try {
            $parsed = $content | ConvertFrom-Json
            if ($parsed.error -eq $false -or $parsed.id -or $parsed.code -eq 0 -or $parsed.code -eq 1000) {
                Write-Host "   ✅ SUCCESS" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  $($parsed.message)" -ForegroundColor Yellow
            }
        } catch { Write-Host "   ❓ Non-JSON (HTML?)" -ForegroundColor Gray }
    } catch {
        Write-Host "[$name] ❌ Failed: $_" -ForegroundColor Red
    }
}

function Send-FormTest($name, $bodyString) {
    Save-Text (Join-Path $LogDir "$name-request.txt") $bodyString
    try {
        # Send form as windows-1254
        $headersLocal = @{ 'Content-Type' = 'application/x-www-form-urlencoded; charset=windows-1254' }
        $encBody = [System.Text.Encoding]::GetEncoding(1254).GetBytes($bodyString)
        $resp = Invoke-WebRequest -Uri "$BaseUrl`EkleStkWsSkart.do" -Method Post -Body $encBody -WebSession $session -ContentType $headersLocal['Content-Type'] -Headers $headersLocal -UseBasicParsing
        $content = $resp.Content
        Save-Text (Join-Path $LogDir "$name-response.txt") $content
        Write-Host "[$name] HTTP $($resp.StatusCode)"
        try {
            $parsed = $content | ConvertFrom-Json
            if ($parsed.error -eq $false -or $parsed.id -or $parsed.code -eq 0 -or $parsed.code -eq 1000) {
                Write-Host "   ✅ SUCCESS" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  $($parsed.message)" -ForegroundColor Yellow
            }
        } catch { Write-Host "   ❓ Non-JSON (HTML?)" -ForegroundColor Gray }
    } catch {
        Write-Host "[$name] ❌ Failed: $_" -ForegroundColor Red
    }
}

Write-Host "`n=== Wrapped Object Tests ===" -ForegroundColor Yellow

$commonJsonFields = @"
    "baslangicTarihi": "$testDate",
    "kartAdi": "Test Wrapped",
    "kartKodu": "$testSKU",
    "kartTuru": 1,
    "olcumBirimiId": $OlcumBirimiId,
    "perakendeSatisBirimFiyat": 100.0,
    "perakendeAlisBirimFiyat": 80.0,
    "kartAlisKdvOran": $KdvOran,
    "kartSatisKdvOran": $KdvOran,
    "kartTipi": $KartTipi,
    "kategoriAgacKod": "$KategoriKod"
"@

# 1) stkSkart wrapper
$json1 = @"
{
  "stkSkart": {
$commonJsonFields
  }
}
"@
Send-JsonTest "test1-stkSkart" $json1

# 2) stkWsSkart wrapper
$json2 = @"
{
  "stkWsSkart": {
$commonJsonFields
  }
}
"@
Send-JsonTest "test2-stkWsSkart" $json2

# 3) Root PascalCase
$json3 = @"
{
  "BaslangicTarihi": "$testDate",
  "KartAdi": "Test Mixed Case",
  "KartKodu": "$testSKU-T3",
  "KartTuru": 1,
  "OlcumBirimiId": $OlcumBirimiId,
  "PerakendeSatisBirimFiyat": 100.0,
  "PerakendeAlisBirimFiyat": 80.0,
  "KartAlisKdvOran": $KdvOran,
  "KartSatisKdvOran": $KdvOran,
  "KartTipi": $KartTipi
}
"@
Send-JsonTest "test3-root-pascal" $json3

# 4) gnl pattern wrapper (same as test1)
$json4 = $json1
Send-JsonTest "test4-gnl-wrapper" $json4

# 5) Form with prefix
$formBody = "stkSkart.baslangicTarihi=$testDate&stkSkart.kartAdi=Test+Form+Prefix&stkSkart.kartKodu=$testSKU-T5&stkSkart.kartTuru=1&stkSkart.olcumBirimiId=$OlcumBirimiId&stkSkart.perakendeSatisBirimFiyat=100.0&stkSkart.kartAlisKdvOran=$KdvOran&stkSkart.kartSatisKdvOran=$KdvOran&stkSkart.kartTipi=$KartTipi&stkSkart.kategoriAgacKod=$KategoriKod"
Send-FormTest "test5-form-prefix" $formBody

# 6) vtStkSkart wrapper (entity pattern)
$json6 = @"
{
  "vtStkSkart": {
    "baslangicTarihi": "$testDate",
    "kartAdi": "Test VT Entity",
    "kartKodu": "$testSKU-T6",
    "kartTuru": 1,
    "olcumBirimiId": $OlcumBirimiId,
    "perakendeSatisBirimFiyat": 100.0,
    "perakendeAlisBirimFiyat": 80.0,
    "kartAlisKdvOran": $KdvOran,
    "kartSatisKdvOran": $KdvOran,
    "kartTipi": $KartTipi,
    "kategoriAgacKod": "$KategoriKod"
  }
}
"@
Send-JsonTest "test6-vtStkSkart" $json6

# 7) Double wrapped: stkWsSkart -> vtStkSkart
$json7 = @"
{
  "stkWsSkart": {
    "vtStkSkart": {
      "baslangicTarihi": "$testDate",
      "kartAdi": "Test Double Wrap",
      "kartKodu": "$testSKU-T7",
      "kartTuru": 1,
      "olcumBirimiId": $OlcumBirimiId,
      "perakendeSatisBirimFiyat": 100.0,
      "perakendeAlisBirimFiyat": 80.0,
      "kartAlisKdvOran": $KdvOran,
      "kartSatisKdvOran": $KdvOran,
      "kartTipi": $KartTipi,
      "kategoriAgacKod": "$KategoriKod"
    }
  }
}
"@
Send-JsonTest "test7-double-wrap" $json7

# 8) Form with vtStkSkart prefix
$formBody2 = "vtStkSkart.baslangicTarihi=$testDate&vtStkSkart.kartAdi=Test+Form+VT&vtStkSkart.kartKodu=$testSKU-T8&vtStkSkart.kartTuru=1&vtStkSkart.olcumBirimiId=$OlcumBirimiId&vtStkSkart.perakendeSatisBirimFiyat=100.0&vtStkSkart.kartAlisKdvOran=$KdvOran&vtStkSkart.kartSatisKdvOran=$KdvOran&vtStkSkart.kartTipi=$KartTipi&vtStkSkart.kategoriAgacKod=$KategoriKod"
Send-FormTest "test8-form-vt-prefix" $formBody2

Write-Host "`nComplete. Logs: $LogDir" -ForegroundColor Yellow
