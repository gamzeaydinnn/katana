<#
test-luca-hybrid-structure.ps1

Hybrid yapıları dener: bazı alanlar root, bazıları wrapper içinde. Amaç hangi kombinasyonun Koza tarafından kabul edildiğini görmek.
Denemeler:
1) baslangicTarihi root, diğer alanlar vtStkSkart içinde
2) kartAdi root, diğer alanlar vtStkSkart içinde
3) kartAdi + baslangicTarihi root, diğer alanlar vtStkSkart içinde
4) Tüm alanlar root
5) Form-encoded: bazı alanlar prefix'li, bazıları değil

Loglar: scripts/logs/hybrid-test

Kullanım:
  .\scripts\test-luca-hybrid-structure.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'https://akozas.luca.com.tr/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
$LogDir = './scripts/logs/hybrid-test'
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$testDate = (Get-Date).ToString('yyyy-MM-dd')
$testSKU = "HYBRID-$timestamp"

Write-Host "=== HYBRID STRUCTURE TEST ===" -ForegroundColor Yellow
Write-Host "Testing: Some fields in wrapper, some at root level`n"

# Session init
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Write-Host "[INIT] Login + Branch..." -ForegroundColor Cyan
try {
    $loginResp = Invoke-WebRequest -Uri "$($BaseUrl)Giris.do" -Method Post `
        -Body (@{orgCode=$OrgCode;userName=$Username;userPassword=$Password} | ConvertTo-Json) `
        -WebSession $session -Headers @{'Content-Type'='application/json'} -UseBasicParsing

    $branchResp = Invoke-WebRequest -Uri "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -Method Post `
        -Body (@{orgSirketSubeId=$BranchId} | ConvertTo-Json) `
        -WebSession $session -Headers @{'Content-Type'='application/json'} -UseBasicParsing

    Write-Host "Session ready`n" -ForegroundColor Green
}
catch {
    Write-Host "Init failed: $_" -ForegroundColor Red
    exit 1
}

# Test 1: baslangicTarihi root, diğerleri wrapper
Write-Host "[TEST 1] baslangicTarihi at root + vtStkSkart wrapper" -ForegroundColor Cyan
$json1 = @"
{
  "baslangicTarihi": "$testDate",
  "vtStkSkart": {
    "kartAdi": "Hybrid Test 1",
    "kartKodu": "$testSKU-T1",
    "kartTuru": 1,
    "olcumBirimiId": 945,
    "perakendeSatisBirimFiyat": 100.0,
    "perakendeAlisBirimFiyat": 80.0,
    "kartAlisKdvOran": 0.20,
    "kartSatisKdvOran": 0.20
  }
}
"@
Set-Content (Join-Path $LogDir "test1-request.json") $json1 -Encoding UTF8
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json1 -WebSession $session `
        -Headers @{'Content-Type'='application/json; charset=utf-8'} -UseBasicParsing
    $content = $resp.Content
    Set-Content (Join-Path $LogDir "test1-response.txt") $content -Encoding UTF8
    try {
        $json = $content | ConvertFrom-Json
        if ($json.error -eq $false -or $json.id) {
            Write-Host "   SUCCESS ID=$($json.id)" -ForegroundColor Green
        } else {
            Write-Host "   Error: $($json.message)" -ForegroundColor Yellow
        }
    } catch { Write-Host "   Non-JSON" -ForegroundColor Gray }
} catch { Write-Host "   Failed: $_" -ForegroundColor Red }

# Test 2: kartAdi root, diğerleri wrapper
Write-Host "`n[TEST 2] kartAdi at root + vtStkSkart wrapper" -ForegroundColor Cyan
$json2 = @"
{
  "kartAdi": "Hybrid Test 2",
  "vtStkSkart": {
    "baslangicTarihi": "$testDate",
    "kartKodu": "$testSKU-T2",
    "kartTuru": 1,
    "olcumBirimiId": 945,
    "perakendeSatisBirimFiyat": 100.0,
    "kartAlisKdvOran": 0.20
  }
}
"@
Set-Content (Join-Path $LogDir "test2-request.json") $json2 -Encoding UTF8
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json2 -WebSession $session `
        -Headers @{'Content-Type'='application/json; charset=utf-8'} -UseBasicParsing
    $content = $resp.Content
    Set-Content (Join-Path $LogDir "test2-response.txt") $content -Encoding UTF8
    try {
        $json = $content | ConvertFrom-Json
        if ($json.error -eq $false -or $json.id) {
            Write-Host "   SUCCESS ID=$($json.id)" -ForegroundColor Green
        } else {
            Write-Host "   Error: $($json.message)" -ForegroundColor Yellow
        }
    } catch { Write-Host "   Non-JSON" -ForegroundColor Gray }
} catch { Write-Host "   Failed: $_" -ForegroundColor Red }

# Test 3: kartAdi + baslangicTarihi root
Write-Host "`n[TEST 3] kartAdi + baslangicTarihi at root + vtStkSkart wrapper" -ForegroundColor Cyan
$json3 = @"
{
  "kartAdi": "Hybrid Test 3",
  "baslangicTarihi": "$testDate",
  "vtStkSkart": {
    "kartKodu": "$testSKU-T3",
    "kartTuru": 1,
    "olcumBirimiId": 945,
    "perakendeSatisBirimFiyat": 100.0,
    "kartAlisKdvOran": 0.20
  }
}
"@
Set-Content (Join-Path $LogDir "test3-request.json") $json3 -Encoding UTF8
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json3 -WebSession $session `
        -Headers @{'Content-Type'='application/json; charset=utf-8'} -UseBasicParsing
    $content = $resp.Content
    Set-Content (Join-Path $LogDir "test3-response.txt") $content -Encoding UTF8
    try {
        $json = $content | ConvertFrom-Json
        if ($json.error -eq $false -or $json.id) {
            Write-Host "   SUCCESS ID=$($json.id)" -ForegroundColor Green
        } else {
            Write-Host "   Error: $($json.message)" -ForegroundColor Yellow
        }
    } catch { Write-Host "   Non-JSON" -ForegroundColor Gray }
} catch { Write-Host "   Failed: $_" -ForegroundColor Red }

# Test 4: Tüm alanlar root
Write-Host "`n[TEST 4] All fields at root (no wrapper)" -ForegroundColor Cyan
$json4 = @"
{
  "kartAdi": "Hybrid Test 4",
  "baslangicTarihi": "$testDate",
  "kartKodu": "$testSKU-T4",
  "kartTuru": 1,
  "olcumBirimiId": 945,
  "perakendeSatisBirimFiyat": 100.0,
  "perakendeAlisBirimFiyat": 80.0,
  "kartAlisKdvOran": 0.20,
  "kartSatisKdvOran": 0.20
}
"@
Set-Content (Join-Path $LogDir "test4-request.json") $json4 -Encoding UTF8
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $json4 -WebSession $session `
        -Headers @{'Content-Type'='application/json; charset=utf-8'} -UseBasicParsing
    $content = $resp.Content
    Set-Content (Join-Path $LogDir "test4-response.txt") $content -Encoding UTF8
    try {
        $json = $content | ConvertFrom-Json
        if ($json.error -eq $false -or $json.id) {
            Write-Host "   SUCCESS ID=$($json.id)" -ForegroundColor Green
        } else {
            Write-Host "   Error: $($json.message)" -ForegroundColor Yellow
        }
    } catch { Write-Host "   Non-JSON" -ForegroundColor Gray }
} catch { Write-Host "   Failed: $_" -ForegroundColor Red }

# Test 5: Form-encoded karışık
Write-Host "`n[TEST 5] Mixed form (some prefixed, some not)" -ForegroundColor Cyan
$form5 = "kartAdi=Hybrid+Form&baslangicTarihi=$testDate&vtStkSkart.kartKodu=$testSKU-T5&vtStkSkart.kartTuru=1&vtStkSkart.olcumBirimiId=945&vtStkSkart.perakendeSatisBirimFiyat=100&vtStkSkart.kartAlisKdvOran=0.20"
Set-Content (Join-Path $LogDir "test5-request.txt") $form5 -Encoding UTF8
try {
    $resp = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $form5 -WebSession $session `
        -Headers @{'Content-Type'='application/x-www-form-urlencoded; charset=utf-8'} -UseBasicParsing
    $content = $resp.Content
    Set-Content (Join-Path $LogDir "test5-response.txt") $content -Encoding UTF8
    try {
        $json = $content | ConvertFrom-Json
        if ($json.error -eq $false -or $json.id) {
            Write-Host "   SUCCESS ID=$($json.id)" -ForegroundColor Green
        } else {
            Write-Host "   Error: $($json.message)" -ForegroundColor Yellow
        }
    } catch { Write-Host "   Non-JSON" -ForegroundColor Gray }
} catch { Write-Host "   Failed: $_" -ForegroundColor Red }

Write-Host "`n=== COMPLETE ===" -ForegroundColor Yellow
Write-Host "Logs: $LogDir"
Write-Host "If all tests still fail, this is strong evidence of a server-side parsing issue.`n"
