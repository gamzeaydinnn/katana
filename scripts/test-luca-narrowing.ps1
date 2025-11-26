<#
test-luca-narrowing.ps1

Systematik olarak Koza stok kartı oluşturma hatasını daraltmak için 4 fazda 15 test çalıştırır:
- Faz 1: Tarih formatları
- Faz 2: Minimal alan kombinasyonları
- Faz 3: Encoding / content-type matrisi
- Faz 4: Boolean flag varyasyonları

Her testin istek/yanıt/başlıklarını scripts/logs/narrowing-test altına yazar, özet üretir.

Kullanım (repo kökünden):
  .\scripts\test-luca-narrowing.ps1 -Username "Admin" -Password "2009Bfm" -OrgCode "1422649" -BranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$Username = 'Admin',
    [string]$Password = '2009Bfm',
    [string]$OrgCode = '',
    [int]$BranchId = 854,
    [int]$DefaultOlcumBirimiId = 5,
    [double]$DefaultKdvOran = 0.20,
    [int]$DefaultKartTipi = 4,
    [string]$DefaultKategoriAgacKod = '001',
    [string]$LogDir = './scripts/logs/narrowing-test'
)

if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

$timestamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$testResults = @()

function Write-JsonFile($path, $obj) {
    $json = if ($obj -is [string]) { $obj } else { $obj | ConvertTo-Json -Depth 10 -Compress }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

function Send-LucaTest($endpoint, $bodyObj, $contentType, $encoding, $testId, $testName) {
    $url = "$BaseUrl$endpoint"

    Write-Host "`n[$testId] $testName" -ForegroundColor Cyan
    Write-Host "   ContentType: $contentType | Encoding: $encoding"

    try {
        # Body string
        if ($contentType -like '*json*') {
            $bodyString = $bodyObj | ConvertTo-Json -Depth 10 -Compress
        }
        elseif ($contentType -like '*form*') {
            $formPairs = @()
            foreach ($key in $bodyObj.Keys) {
                $value = $bodyObj[$key]
                if ($value -is [bool]) { $value = if ($value) { 'true' } else { 'false' } }
                $formPairs += "$key=$([System.Uri]::EscapeDataString($value))"
            }
            $bodyString = $formPairs -join '&'
        }
        else {
            $bodyString = $bodyObj | ConvertTo-Json -Depth 10 -Compress
        }

        # Encoding
        $enc = if ($encoding -eq 'windows-1254') {
            try { [System.Text.Encoding]::GetEncoding(1254) } catch { [System.Text.Encoding]::UTF8 }
        } else { [System.Text.Encoding]::UTF8 }

        $bytes = $enc.GetBytes($bodyString)

        # Save request
        $reqFile = Join-Path $LogDir "$testId-request.txt"
        Set-Content -Path $reqFile -Value $bodyString -Encoding UTF8

        # Build request
        $req = [System.Net.HttpWebRequest]::Create($url)
        $req.Method = 'POST'
        $req.ContentType = $contentType
        $req.Accept = 'application/json'
        $req.CookieContainer = $global:cookieJar
        $req.ContentLength = $bytes.Length

        $reqStream = $req.GetRequestStream()
        $reqStream.Write($bytes, 0, $bytes.Length)
        $reqStream.Close()

        # Response
        $resp = $req.GetResponse()
        $respStream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($respStream, $enc)
        $responseText = $reader.ReadToEnd()
        $reader.Close()

        $respFile = Join-Path $LogDir "$testId-response.txt"
        Set-Content -Path $respFile -Value $responseText -Encoding UTF8

        $headerFile = Join-Path $LogDir "$testId-headers.txt"
        Set-Content -Path $headerFile -Value $resp.Headers.ToString() -Encoding UTF8

        # Analyze
        $isJson = $responseText.Trim() -match '^\s*[{\[]'
        $isHtml = $responseText -match '<html|<HTML|<!DOCTYPE'
        $hasSuccess = $responseText -match '"error"\s*:\s*false' -or $responseText -match '"success"\s*:\s*true' -or $responseText -match '"code"\s*:\s*(0|1000)'
        $hasJsonError = $responseText -match '"error"\s*:\s*true' -or $responseText -match '"code"\s*:\s*[1-9]'
        $errorMessage = if ($responseText -match '"message"\s*:\s*"([^"]+)"') { $Matches[1] } else { '' }

        $status = if ($isHtml) {
            "HTML_ERROR"
        } elseif ($hasSuccess) {
            "SUCCESS"
        } elseif ($hasJsonError) {
            "JSON_ERROR: $errorMessage"
        } elseif ($isJson) {
            "JSON_UNKNOWN"
        } else {
            "UNEXPECTED"
        }

        Write-Host "   Result: $status" -ForegroundColor $(
            if ($status -match "SUCCESS") { "Green" }
            elseif ($status -match "JSON_ERROR") { "Yellow" }
            elseif ($status -match "HTML_ERROR") { "Red" }
            else { "Gray" }
        )

        return @{
            TestId = $testId
            TestName = $testName
            Status = $status
            IsJson = $isJson
            IsHtml = $isHtml
            ContentType = $contentType
            Encoding = $encoding
            ErrorMessage = $errorMessage
            ResponseLength = $responseText.Length
        }
    }
    catch [System.Net.WebException] {
        $errResp = $_.Exception.Response
        if ($errResp) {
            $errStream = $errResp.GetResponseStream()
            $errReader = New-Object System.IO.StreamReader($errStream)
            $errText = $errReader.ReadToEnd()
            $errReader.Close()

            $errFile = Join-Path $LogDir "$testId-error.txt"
            Set-Content -Path $errFile -Value $errText -Encoding UTF8

            Write-Host "   Result: ❌ HTTP_ERROR ($($errResp.StatusCode))" -ForegroundColor Red

            return @{
                TestId = $testId
                TestName = $testName
                Status = "HTTP_ERROR"
                ErrorMessage = $errResp.StatusCode
            }
        }

        Write-Host "   Result: ❌ REQUEST_FAILED - $_" -ForegroundColor Red
        return @{
            TestId = $testId
            TestName = $testName
            Status = "REQUEST_FAILED"
            ErrorMessage = $_.Exception.Message
        }
    }
    catch {
        Write-Host "   Result: ❌ EXCEPTION - $_" -ForegroundColor Red
        Set-Content (Join-Path $LogDir "$testId-exception.txt") $_.Exception.ToString()

        return @{
            TestId = $testId
            TestName = $testName
            Status = "EXCEPTION"
            ErrorMessage = $_.Exception.Message
        }
    }
}

# --- PHASE 0: Session ---
Write-Host "=== PHASE 0: Session Initialization ===" -ForegroundColor Yellow
$global:cookieJar = New-Object System.Net.CookieContainer
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Write-Host "Logging in as $Username..."
$loginPayload = @{
    orgCode = $OrgCode
    userName = $Username
    userPassword = $Password
}

try {
    $loginResp = Invoke-WebRequest -Uri "$($BaseUrl)Giris.do" -Method Post `
        -Body ($loginPayload | ConvertTo-Json) -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing

    Write-Host "✅ Login successful" -ForegroundColor Green

    foreach ($cookie in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($cookie.Name, $cookie.Value, $cookie.Path, $cookie.Domain)
        $global:cookieJar.Add($netCookie)
    }
}
catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Selecting branch $BranchId..."
$branchPayload = @{ orgSirketSubeId = $BranchId }

try {
    $branchResp = Invoke-WebRequest -Uri "$($BaseUrl)GuncelleYtkSirketSubeDegistir.do" -Method Post `
        -Body ($branchPayload | ConvertTo-Json) -WebSession $session `
        -Headers @{ 'Content-Type' = 'application/json' } -UseBasicParsing

    Write-Host "✅ Branch selected" -ForegroundColor Green

    foreach ($cookie in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
        $netCookie = New-Object System.Net.Cookie($cookie.Name, $cookie.Value, $cookie.Path, $cookie.Domain)
        $global:cookieJar.Add($netCookie)
    }
}
catch {
    Write-Host "⚠️  Branch selection warning: $_" -ForegroundColor Yellow
}

# Common defaults for all payloads
$defaultPayloadFields = @{
    kartTuru = 1
    olcumBirimiId = $DefaultOlcumBirimiId
    kartTipi = $DefaultKartTipi
    kartAlisKdvOran = $DefaultKdvOran
    kartSatisKdvOran = $DefaultKdvOran
    kategoriAgacKod = $DefaultKategoriAgacKod
    perakendeSatisBirimFiyat = 100.0
    perakendeAlisBirimFiyat = 80.0
}

$testSKU = "NARROW-$timestamp"
$now = Get-Date

# PHASE 1: Date formats
Write-Host "`n=== PHASE 1: Date Format Variations (JSON/UTF-8) ===" -ForegroundColor Yellow
$dateFormats = @(
    @{ Format = 'yyyy-MM-dd'; Date = $now.ToString('yyyy-MM-dd'); Name = 'ISO Date (yyyy-MM-dd)' },
    @{ Format = 'dd.MM.yyyy'; Date = $now.ToString('dd.MM.yyyy'); Name = 'Turkish Date (dd.MM.yyyy)' },
    @{ Format = 'yyyy-MM-ddTHH:mm:ss'; Date = $now.ToString('yyyy-MM-ddTHH:mm:ss'); Name = 'ISO DateTime' },
    @{ Format = 'dd/MM/yyyy'; Date = $now.ToString('dd/MM/yyyy'); Name = 'Slash Date (dd/MM/yyyy)' }
)

foreach ($df in $dateFormats) {
    $payload = $defaultPayloadFields.Clone()
    $payload.kartAdi = "Test Date Format"
    $payload.kartKodu = "$testSKU-D$($dateFormats.IndexOf($df) + 1)"
    $payload.baslangicTarihi = $df.Date

    $testId = "P1-$($dateFormats.IndexOf($df) + 1)"
    $result = Send-LucaTest 'EkleStkWsSkart.do' $payload 'application/json; charset=utf-8' 'utf-8' $testId $df.Name
    $testResults += $result
}

# PHASE 2: Minimal fields
Write-Host "`n=== PHASE 2: Minimal Required Fields ===" -ForegroundColor Yellow
$minimalTests = @(
    @{
        Name = 'Absolute Minimal'
        Fields = @{
            kartAdi = "Minimal Test"
            kartKodu = "$testSKU-M1"
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            kartTuru = 1
            kartTipi = $DefaultKartTipi
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    },
    @{
        Name = 'Minimal + Prices'
        Fields = @{
            kartAdi = "Minimal Prices"
            kartKodu = "$testSKU-M2"
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            perakendeSatisBirimFiyat = 100.0
            perakendeAlisBirimFiyat = 80.0
            kartTuru = 1
            kartTipi = $DefaultKartTipi
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    },
    @{
        Name = 'Minimal + KDV'
        Fields = @{
            kartAdi = "Minimal KDV"
            kartKodu = "$testSKU-M3"
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            kartTuru = 1
            kartTipi = $DefaultKartTipi
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    },
    @{
        Name = 'Minimal Complete'
        Fields = @{
            kartAdi = "Minimal Complete"
            kartKodu = "$testSKU-M4"
            kartTuru = 1
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            perakendeSatisBirimFiyat = 100.0
            perakendeAlisBirimFiyat = 80.0
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            kartTipi = $DefaultKartTipi
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    }
)

foreach ($mt in $minimalTests) {
    $testId = "P2-$($minimalTests.IndexOf($mt) + 1)"
    $result = Send-LucaTest 'EkleStkWsSkart.do' $mt.Fields 'application/json; charset=utf-8' 'utf-8' $testId $mt.Name
    $testResults += $result
}

# PHASE 3: Encoding / content-type matrix
Write-Host "`n=== PHASE 3: Encoding/Content-Type Matrix ===" -ForegroundColor Yellow
$basePayload = $defaultPayloadFields.Clone()
$basePayload.kartAdi = "Encoding Test"
$basePayload.kartKodu = "$testSKU-E"
$basePayload.baslangicTarihi = $now.ToString('yyyy-MM-dd')

$encodingTests = @(
    @{ ContentType = 'application/json; charset=utf-8'; Encoding = 'utf-8'; Name = 'JSON UTF-8' },
    @{ ContentType = 'application/json; charset=windows-1254'; Encoding = 'windows-1254'; Name = 'JSON CP1254' },
    @{ ContentType = 'application/x-www-form-urlencoded; charset=utf-8'; Encoding = 'utf-8'; Name = 'Form UTF-8' },
    @{ ContentType = 'application/x-www-form-urlencoded; charset=windows-1254'; Encoding = 'windows-1254'; Name = 'Form CP1254' }
)

foreach ($et in $encodingTests) {
    $testId = "P3-$($encodingTests.IndexOf($et) + 1)"
    $payload = $basePayload.Clone()
    $payload.kartKodu = "$testSKU-E$($encodingTests.IndexOf($et) + 1)"

    $result = Send-LucaTest 'EkleStkWsSkart.do' $payload $et.ContentType $et.Encoding $testId $et.Name
    $testResults += $result
}

# PHASE 4: Boolean flags
Write-Host "`n=== PHASE 4: Boolean Flag Variations ===" -ForegroundColor Yellow
$flagTests = @(
    @{
        Name = 'No Flags'
        Fields = @{
            kartAdi = "No Flags"
            kartKodu = "$testSKU-F1"
            kartTuru = 1
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            perakendeSatisBirimFiyat = 100.0
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            kartTipi = $DefaultKartTipi
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    },
    @{
        Name = 'Flags as Boolean'
        Fields = @{
            kartAdi = "Flags Boolean"
            kartKodu = "$testSKU-F2"
            kartTuru = 1
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            perakendeSatisBirimFiyat = 100.0
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            satilabilirFlag = $true
            satinAlinabilirFlag = $true
            maliyetHesaplanacakFlag = $true
            kartTipi = $DefaultKartTipi
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    },
    @{
        Name = 'Flags as 0/1'
        Fields = @{
            kartAdi = "Flags Numeric"
            kartKodu = "$testSKU-F3"
            kartTuru = 1
            olcumBirimiId = $DefaultOlcumBirimiId
            baslangicTarihi = $now.ToString('yyyy-MM-dd')
            perakendeSatisBirimFiyat = 100.0
            kartAlisKdvOran = $DefaultKdvOran
            kartSatisKdvOran = $DefaultKdvOran
            satilabilirFlag = 1
            satinAlinabilirFlag = 1
            maliyetHesaplanacakFlag = 1
            kartTipi = $DefaultKartTipi
            kategoriAgacKod = $DefaultKategoriAgacKod
        }
    }
)

foreach ($ft in $flagTests) {
    $testId = "P4-$($flagTests.IndexOf($ft) + 1)"
    $result = Send-LucaTest 'EkleStkWsSkart.do' $ft.Fields 'application/json; charset=utf-8' 'utf-8' $testId $ft.Name
    $testResults += $result
}

# Summary
Write-Host "`n`n============================================" -ForegroundColor Yellow
Write-Host "           TEST RESULTS SUMMARY" -ForegroundColor Yellow
Write-Host "============================================`n" -ForegroundColor Yellow

$successCount = ($testResults | Where-Object { $_.Status -match 'SUCCESS' }).Count
$jsonErrorCount = ($testResults | Where-Object { $_.Status -match 'JSON_ERROR' }).Count
$htmlErrorCount = ($testResults | Where-Object { $_.Status -match 'HTML_ERROR' }).Count
$failCount = ($testResults | Where-Object { $_.Status -match 'FAILED|EXCEPTION|HTTP_ERROR|REQUEST_FAILED' }).Count

Write-Host "Total Tests:    $($testResults.Count)"
Write-Host "Successful:  $successCount" -ForegroundColor Green
Write-Host "JSON Error:  $jsonErrorCount" -ForegroundColor Yellow
Write-Host "HTML Error:  $htmlErrorCount" -ForegroundColor Red
Write-Host "Failed:      $failCount" -ForegroundColor Red

if ($successCount -gt 0) {
    Write-Host "`nSUCCESS! Working configuration(s) found:" -ForegroundColor Green
    $testResults | Where-Object { $_.Status -match 'SUCCESS' } | ForEach-Object {
        Write-Host "   [$($_.TestId)] $($_.TestName) - $($_.ContentType) / $($_.Encoding)"
    }
}
elseif ($jsonErrorCount -gt 0) {
    Write-Host "`nJSON errors detected. Common error messages:" -ForegroundColor Yellow
    $testResults | Where-Object { $_.Status -match 'JSON_ERROR' } |
        Group-Object ErrorMessage |
        ForEach-Object {
            Write-Host "   - $($_.Name) ($($_.Count) occurrences)"
        }
}
else {
    Write-Host "`n❌ All tests failed with HTML errors or exceptions." -ForegroundColor Red
    Write-Host "   This indicates a server-side issue in Koza's EkleStkWsSkart.do endpoint."
    Write-Host "   Recommend escalation to Koza support with test artifacts."
}

$summaryFile = Join-Path $LogDir "test-summary-$timestamp.json"
Write-JsonFile $summaryFile $testResults

Write-Host "`nAll test artifacts saved to: $LogDir" -ForegroundColor Cyan
Write-Host "Summary JSON: $summaryFile`n"
