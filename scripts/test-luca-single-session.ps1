<#
test-luca-single-session.ps1

Amaç: Aynı WebSession içinde login + branch + stok oluşturma yaparak cookie/session kaybını dışlamak.
3 format deniyor:
 A) Form-encoded
 B) vtStkSkart JSON wrapper
 C) Form vtStkSkart. prefix

Loglar: scripts/logs/single-session-test

Kullanım:
  .\scripts\test-luca-single-session.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
$LogDir = './scripts/logs/single-session-test'
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')

Write-Host "=== SINGLE SESSION TEST ===" -ForegroundColor Yellow
Write-Host "Goal: Keep session alive and test immediately after branch selection`n"

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Login
Write-Host "[1/4] Login..." -ForegroundColor Cyan
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
    $cookieCount = $session.Cookies.GetCookies([System.Uri]$BaseUrl).Count
    Write-Host "   Cookies stored: $cookieCount" -ForegroundColor Gray
}
catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

# Branch
Write-Host "`n[2/4] Branch selection..." -ForegroundColor Cyan
$branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json

try {
    $branchResp = Invoke-WebRequest -Uri "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -Method Post `
        -Body $branchPayload -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing

    Write-Host "Branch selected" -ForegroundColor Green
    $cookieCount = $session.Cookies.GetCookies([System.Uri]$BaseUrl).Count
    Write-Host "   Cookies stored: $cookieCount" -ForegroundColor Gray
}
catch {
    Write-Host "Branch warning: $_" -ForegroundColor Yellow
}

# Verify session with a list call
Write-Host "`n[3/4] Verify session..." -ForegroundColor Cyan
try {
    $verifyResp = Invoke-WebRequest -Uri "$($BaseUrl)ListeleGnlOlcumBirimi.do" -Method Post `
        -Body '{"gnlOlcumBirimi":{"tanim":""}}' -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing
    $verifyJson = $verifyResp.Content | ConvertFrom-Json
    Write-Host "Session valid - measurement units fetched" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  Session verification failed: $_" -ForegroundColor Yellow
}

# Immediate stock create variants
Write-Host "`n[4/4] Stock create (IMMEDIATE)..." -ForegroundColor Cyan
$testDate = (Get-Date).ToString('yyyy-MM-dd')
$testSKU = "SESSION-$timestamp"

# A) Form-encoded
Write-Host "`n   [A] Form-encoded..." -ForegroundColor Magenta
$pairsA = @()
$pairsA += "baslangicTarihi=$testDate"
$pairsA += "kartAdi=Session+Test+Form"
$pairsA += "kartKodu=$testSKU-A"
$pairsA += "kartTuru=1"
$pairsA += "olcumBirimiId=945"
$pairsA += "perakendeSatisBirimFiyat=100.0"
$pairsA += "perakendeAlisBirimFiyat=80.0"
$pairsA += "kartAlisKdvOran=0.20"
$pairsA += "kartSatisKdvOran=0.20"
$formData = $pairsA -join '&'
Set-Content (Join-Path $LogDir "testA-form-request.txt") $formData -Encoding UTF8

try {
    $respA = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $formData -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/x-www-form-urlencoded; charset=utf-8' } `
        -UseBasicParsing
    $contentA = $respA.Content
    Set-Content (Join-Path $LogDir "testA-form-response.txt") $contentA -Encoding UTF8
    Write-Host "      Status: $($respA.StatusCode)" -ForegroundColor White
    try {
        $jsonA = $contentA | ConvertFrom-Json
        if ($jsonA.error -eq $false -or $jsonA.id) {
            Write-Host "      SUCCESS! Stock ID: $($jsonA.id)" -ForegroundColor Green
        } elseif ($jsonA.code -eq 1002 -or $jsonA.message -match 'login|Login') {
            Write-Host "      Session lost: $($jsonA.message)" -ForegroundColor Red
        } else {
            Write-Host "      Error: $($jsonA.message)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "      Non-JSON response" -ForegroundColor Gray
        Write-Host "      First 200 chars: $($contentA.Substring(0, [Math]::Min(200, $contentA.Length)))"
    }
} catch {
    Write-Host "      Request failed: $_" -ForegroundColor Red
}

# B) vtStkSkart JSON
Write-Host "`n   [B] vtStkSkart JSON wrapper..." -ForegroundColor Magenta
$jsonB = @"
{
    "vtStkSkart": {
        "baslangicTarihi": "$testDate",
        "kartAdi": "Session Test JSON",
        "kartKodu": "$testSKU-B",
        "kartTuru": 1,
        "olcumBirimiId": 945,
        "perakendeSatisBirimFiyat": 100.0,
        "perakendeAlisBirimFiyat": 80.0,
        "kartAlisKdvOran": 0.20,
        "kartSatisKdvOran": 0.20
    }
}
"@
Set-Content (Join-Path $LogDir "testB-json-request.json") $jsonB -Encoding UTF8

try {
    $respB = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $jsonB -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json; charset=utf-8' } `
        -UseBasicParsing
    $contentB = $respB.Content
    Set-Content (Join-Path $LogDir "testB-json-response.txt") $contentB -Encoding UTF8
    Write-Host "      Status: $($respB.StatusCode)" -ForegroundColor White
    try {
        $jsonResp = $contentB | ConvertFrom-Json
        if ($jsonResp.error -eq $false -or $jsonResp.id) {
            Write-Host "      SUCCESS! Stock ID: $($jsonResp.id)" -ForegroundColor Green
                } elseif ($jsonResp.code -eq 1002) {
                    Write-Host "      Session lost: $($jsonResp.message)" -ForegroundColor Red
                } else {
                    Write-Host "      Error: $($jsonResp.message)" -ForegroundColor Yellow
        }
    } catch {
            Write-Host "      Non-JSON response" -ForegroundColor Gray
        Write-Host "      First 200 chars: $($contentB.Substring(0, [Math]::Min(200, $contentB.Length)))"
    }
} catch {
    Write-Host "      ❌ Request failed: $_" -ForegroundColor Red
}

# C) Form with vtStkSkart prefix
Write-Host "`n   [C] Form with vtStkSkart. prefix..." -ForegroundColor Magenta
$pairsC = @()
$pairsC += "vtStkSkart.baslangicTarihi=$testDate"
$pairsC += "vtStkSkart.kartAdi=Session+Test+Prefix"
$pairsC += "vtStkSkart.kartKodu=$testSKU-C"
$pairsC += "vtStkSkart.kartTuru=1"
$pairsC += "vtStkSkart.olcumBirimiId=945"
$pairsC += "vtStkSkart.perakendeSatisBirimFiyat=100.0"
$pairsC += "vtStkSkart.kartAlisKdvOran=0.20"
$formC = $pairsC -join '&'
Set-Content (Join-Path $LogDir "testC-form-prefix-request.txt") $formC -Encoding UTF8

try {
    $respC = Invoke-WebRequest -Uri "$($BaseUrl)EkleStkWsSkart.do" -Method Post `
        -Body $formC -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/x-www-form-urlencoded; charset=utf-8' } `
        -UseBasicParsing
    $contentC = $respC.Content
    Set-Content (Join-Path $LogDir "testC-form-prefix-response.txt") $contentC -Encoding UTF8
    Write-Host "      Status: $($respC.StatusCode)" -ForegroundColor White
    try {
        $jsonC = $contentC | ConvertFrom-Json
        if ($jsonC.error -eq $false -or $jsonC.id) {
            Write-Host "      SUCCESS! Stock ID: $($jsonC.id)" -ForegroundColor Green
                } elseif ($jsonC.code -eq 1002) {
                    Write-Host "      Session lost: $($jsonC.message)" -ForegroundColor Red
                } else {
                    Write-Host "      Error: $($jsonC.message)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "      Non-JSON response" -ForegroundColor Gray
        Write-Host "      First 200 chars: $($contentC.Substring(0, [Math]::Min(200, $contentC.Length)))"
    }
} catch {
    Write-Host "      ❌ Request failed: $_" -ForegroundColor Red
}

Write-Host "`n=== TEST COMPLETE ===" -ForegroundColor Yellow
Write-Host "All artifacts saved to: $LogDir"
Write-Host "`nKey insight: Testing with persistent WebSession to avoid cookie issues`n"
