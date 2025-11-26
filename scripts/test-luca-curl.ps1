<#
test-luca-curl.ps1

PowerShell’ın HttpWebRequest/encoding katmanını baypas edip curl ile ham HTTP gönderir.
Adımlar:
1) Giris.do (login)
2) GuncelleYtkSirketSubeDegistir.do (branch)
3) EkleStkWsSkart.do form-encoded
4) EkleStkWsSkart.do JSON

Loglar: scripts/logs/curl-test (cookie dosyası, curl verbose output, yanıtlar)

Kullanım (repo kökünden):
    .\scripts\test-luca-curl.ps1 -OrgCode 1422649 -Username Admin -Password WebServis -BranchId 11746
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
$LogDir = './scripts/logs/curl-test'
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$cookieFile = Join-Path $LogDir "cookies-$timestamp.txt"

Write-Host "=== CURL RAW HTTP TEST ===" -ForegroundColor Yellow
Write-Host "Using cookie file: $cookieFile`n"

# Step 1: Login with curl
Write-Host "[STEP 1] Login with curl..." -ForegroundColor Cyan
$loginJson = @"
{"orgCode":"$OrgCode","userName":"$Username","userPassword":"$Password"}
"@
Set-Content -Path (Join-Path $LogDir "login-request.json") -Value $loginJson -Encoding UTF8

$loginReqFile = Join-Path $LogDir "login-request.json"
Set-Content -Path $loginReqFile -Value $loginJson -Encoding UTF8

try {
    $loginOutput = & curl.exe -X POST "$($BaseUrl)Giris.do" -H 'Content-Type: application/json' -H 'Accept: application/json' --data "@$loginReqFile" -c "$cookieFile" -v 2>&1
    $loginOutput | Out-File (Join-Path $LogDir "login-output.txt") -Encoding UTF8

    if ($loginOutput -match "200 OK" -or $loginOutput -match "JSESSIONID") {
        Write-Host "Login successful" -ForegroundColor Green
    } else {
        Write-Host "Login response unclear" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Change branch
Write-Host "`n[STEP 2] Change branch with curl..." -ForegroundColor Cyan
$branchJson = "{""orgSirketSubeId"":$BranchId}"
Set-Content -Path (Join-Path $LogDir "branch-request.json") -Value $branchJson -Encoding UTF8

$branchReqFile = Join-Path $LogDir "branch-request.json"
Set-Content -Path $branchReqFile -Value $branchJson -Encoding UTF8

try {
    $branchOutput = & curl.exe -X POST "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -H 'Content-Type: application/json' -H 'Accept: application/json' --data "@$branchReqFile" -b "$cookieFile" -c "$cookieFile" -v 2>&1
    $branchOutput | Out-File (Join-Path $LogDir "branch-output.txt") -Encoding UTF8
    Write-Host "Branch command sent" -ForegroundColor Green
}
catch {
    Write-Host "Branch warning: $_" -ForegroundColor Yellow
}

# Step 3: Form-encoded stock create
Write-Host "`n[STEP 3] Create stock with curl (form-encoded)..." -ForegroundColor Cyan
$testDate = (Get-Date).ToString('yyyy-MM-dd')
$testSKU = "CURL-$timestamp"

$formData = "baslangicTarihi=$testDate&kartAdi=Curl+Test&kartKodu=$testSKU&kartTuru=1&olcumBirimiId=945&perakendeSatisBirimFiyat=100.0&perakendeAlisBirimFiyat=80.0&kartAlisKdvOran=0.20&kartSatisKdvOran=0.20"
$stockReqFile = Join-Path $LogDir "stock-request.txt"
Set-Content -Path $stockReqFile -Value $formData -Encoding UTF8

try {
    $stockOutput = & curl.exe -X POST "$($BaseUrl)EkleStkWsSkart.do" -H 'Content-Type: application/x-www-form-urlencoded; charset=utf-8' -H 'Accept: application/json' --data "@$stockReqFile" -b "$cookieFile" -v 2>&1
    $stockOutput | Out-File (Join-Path $LogDir "stock-output.txt") -Encoding UTF8

    $responseBody = ($stockOutput | Select-String -Pattern '\{.*\}' -AllMatches).Matches.Value
    if ($responseBody) {
        Set-Content -Path (Join-Path $LogDir "stock-response.json") -Value $responseBody -Encoding UTF8
        Write-Host "Response body: $responseBody" -ForegroundColor White

        try {
            $json = $responseBody | ConvertFrom-Json
            if ($json.error -eq $false -or $json.id) {
                Write-Host "`nSUCCESS! Stock created with ID: $($json.id)" -ForegroundColor Green
            } else {
                Write-Host "`nError: $($json.message)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "Could not parse JSON response" -ForegroundColor Gray
        }
    } else {
        Write-Host "No JSON response found in output" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Stock create failed: $_" -ForegroundColor Red
}

# Step 4: JSON stock create
Write-Host "`n[STEP 4] Try JSON format with curl..." -ForegroundColor Cyan
$jsonData = @"
{
  "baslangicTarihi": "$testDate",
  "kartAdi": "Curl JSON Test",
  "kartKodu": "$testSKU-JSON",
  "kartTuru": 1,
  "olcumBirimiId": 945,
  "perakendeSatisBirimFiyat": 100.0,
  "perakendeAlisBirimFiyat": 80.0,
  "kartAlisKdvOran": 0.20,
  "kartSatisKdvOran": 0.20
}
"@
$jsonReqFile = Join-Path $LogDir "stock-json-request.json"
Set-Content -Path $jsonReqFile -Value $jsonData -Encoding UTF8

try {
    $jsonOutput = & curl.exe -X POST "$($BaseUrl)EkleStkWsSkart.do" -H 'Content-Type: application/json; charset=utf-8' -H 'Accept: application/json' --data "@$jsonReqFile" -b "$cookieFile" -v 2>&1
    $jsonOutput | Out-File (Join-Path $LogDir "stock-json-output.txt") -Encoding UTF8

    $responseBody = ($jsonOutput | Select-String -Pattern '\{.*\}' -AllMatches).Matches.Value
    if ($responseBody) {
        Set-Content -Path (Join-Path $LogDir "stock-json-response.json") -Value $responseBody -Encoding UTF8
        Write-Host "Response: $responseBody" -ForegroundColor White
    }
}
catch {
    Write-Host "JSON request failed: $_" -ForegroundColor Red
}

Write-Host "`n=== CURL TEST COMPLETE ===" -ForegroundColor Yellow
Write-Host "All artifacts saved to: $LogDir"
Write-Host "Review verbose output files to see raw HTTP headers/body`n"
