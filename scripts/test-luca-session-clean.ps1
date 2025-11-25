<#
PowerShell E2E test script for Luca (Koza) session consistency.
Performs in same session:
  1) Login (Giris.do)
  2) Get branches (YdlUserResponsibilityOrgSs.do)
  3) Change branch (GuncelleYtkSirketSubeDegistir.do)
  4) Create stock card (EkleStkWsSkart.do)

Saves artifacts to `scripts/logs/`:
  - login-response.json
  - branches.json
  - change-branch-response.json
  - stock-create-response.json
  - cookies.txt
  - http-<step>-headers.txt
#>
param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$UserName = 'Admin',
    [string]$Password = 'WebServis',
    [int]$ForcedBranchId = 854,
    [string]$ProductSKU = 'TEST-SKU-001',
    [string]$ProductName = 'Test Product From Script',
    [int]$OlcumBirimiId = 5,
    [string]$LogDir = "$(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)\logs"
)

# Ensure base url ends with '/'
if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }

# Endpoints (relative)
$endpoints = @{ 
    Auth = 'Giris.do';
    Branches = 'YdlUserResponsibilityOrgSs.do';
    ChangeBranch = 'GuncelleYtkSirketSubeDegistir.do';
    StockCreate = 'EkleStkWsSkart.do'
}

# Create log dir
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

# Helper to write JSON to file
function Write-JsonFile($path, $obj) {
    $json = $null
    if ($obj -is [string]) { $json = $obj } else { $json = $obj | ConvertTo-Json -Depth 10 }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

# Create a single persistent session (keeps cookies)
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$headers = @{ 'Accept' = 'application/json'; 'Content-Type' = 'application/json' }

# Helper: full URL
function Url([string]$relative) { return ([string]::Format("{0}{1}",$BaseUrl,$relative)) }

# 1) Login
$loginPayload = @{ orgCode = $OrgCode; userName = $UserName; userPassword = $Password } | ConvertTo-Json
Write-Host "[1/4] POST Login -> $($BaseUrl)$($endpoints.Auth)"
try {
    $loginResp = Invoke-WebRequest -Uri (Url $endpoints.Auth) -Method Post -Body $loginPayload -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
    $loginContent = $loginResp.Content
    Write-JsonFile "$LogDir\login-response.json" $loginContent
    Set-Content "$LogDir\http-login-headers.txt" ($loginResp.Headers | Out-String)
    Write-Host "Login response saved to $LogDir/login-response.json"
}
catch {
    Write-Warning "Login request failed: $_"
    if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        try { $body = (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd(); Write-JsonFile "$LogDir\login-response.json" $body } catch {}
    }
    throw
}

# Save cookies after login
$uri = [System.Uri]$BaseUrl
$cookieDump = @()
foreach ($c in $session.Cookies.GetCookies($uri)) {
    $cookieDump += ([PSCustomObject]@{ Name=$c.Name; Value=$c.Value; Domain=$c.Domain; Path=$c.Path; Expires=$c.Expires; HttpOnly=$c.HttpOnly })
}
Write-JsonFile "$LogDir\cookies-after-login.json" $cookieDump
Set-Content "$LogDir\cookies.txt" ($cookieDump | ConvertTo-Json -Depth 3)
Write-Host "Cookies after login saved to $LogDir/cookies-after-login.json"
Write-Host "Login cookies:"
($cookieDump | ConvertTo-Json -Depth 3) | Write-Host

# 2) Get branches
Write-Host "[2/4] Fetch branches -> $($BaseUrl)$($endpoints.Branches)"
try {
    $branchesResp = Invoke-WebRequest -Uri (Url $endpoints.Branches) -Method Post -Body '{}' -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
    $branchesContent = $branchesResp.Content
    Write-JsonFile "$LogDir\branches.json" $branchesContent
    Set-Content "$LogDir\http-branches-headers.txt" ($branchesResp.Headers | Out-String)
    Write-Host "Branches response saved to $LogDir/branches.json"
}
catch {
    Write-Warning "Branches request failed: $_"
    if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        try { $body = (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd(); Write-JsonFile "$LogDir\branches.json" $body } catch {}
    }
    throw
}

# Parse branches JSON to pick an id
$branchId = $null
try {
    $branchesJson = $branchesContent | ConvertFrom-Json -ErrorAction Stop
    # Branch list shape may vary. Try common shapes
    if ($branchesJson -is [array]) {
        $first = $branchesJson | Select-Object -First 1
        if ($first -ne $null -and ($first.orgSirketSubeId -ne $null -or $first.orgSirketSubeId -ne $null)) {
            $branchId = $first.orgSirketSubeId
        }
    }
    elseif ($branchesJson.data -ne $null) {
        $first = $branchesJson.data | Select-Object -First 1
        if ($first -ne $null) {
            if ($null -ne $first.orgSirketSubeId) { $branchId = $first.orgSirketSubeId }
            elseif ($null -ne $first.id) { $branchId = $first.id }
            elseif ($null -ne $first.OrgSirketSubeId) { $branchId = $first.OrgSirketSubeId }
        }
    }
    # try flatten
    if (-not $branchId) {
        $all = ($branchesJson | ConvertTo-Json -Depth 5) | ConvertFrom-Json
        foreach ($item in $all) {
            if ($item.orgSirketSubeId) { $branchId = $item.orgSirketSubeId; break }
            if ($item.id) { $branchId = $item.id; break }
        }
    }
}
catch {
    Write-Warning "Could not parse branches JSON: $_"
}

if ($ForcedBranchId -gt 0) { $branchId = $ForcedBranchId }
if (-not $branchId) { Write-Warning "No branch id found. Provide -ForcedBranchId to the script to force selection." }
else { Write-Host "Selected branch id: $branchId" }

# 3) Change branch
if ($branchId) {
    Write-Host "[3/4] Change branch -> $($BaseUrl)$($endpoints.ChangeBranch)"
    $changePayloadCandidates = @(
        @{ orgSirketSubeId = $branchId },
        @{ vtOrgYtkSirketSubeId = $branchId },
        @{ orgSirketSube = $branchId }
    )

    $changeResp = $null
    foreach ($p in $changePayloadCandidates) {
        $json = $p | ConvertTo-Json
        try {
            $changeResp = Invoke-WebRequest -Uri (Url $endpoints.ChangeBranch) -Method Post -Body $json -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
            $changeContent = $changeResp.Content
            Write-JsonFile "$LogDir\change-branch-response.json" $changeContent
            Set-Content "$LogDir\http-change-branch-headers.txt" ($changeResp.Headers | Out-String)
            Write-Host "Change-branch attempt with payload $($json) -> saved to change-branch-response.json"
            break
        }
        catch {
            Write-Warning "Change branch attempt failed for payload $($json): $_"
            if ($_.Exception.Response -ne $null) {
                $resp = $_.Exception.Response
                try { $body = (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd(); Write-JsonFile "$LogDir\change-branch-response.json" $body } catch {}
            }
        }
    }

    # Save cookies after change-branch
    $cookieDump2 = @()
    foreach ($c in $session.Cookies.GetCookies($uri)) {
        $cookieDump2 += ([PSCustomObject]@{ Name=$c.Name; Value=$c.Value; Domain=$c.Domain; Path=$c.Path; Expires=$c.Expires; HttpOnly=$c.HttpOnly })
    }
    Write-JsonFile "$LogDir\cookies-after-change-branch.json" $cookieDump2
    Set-Content "$LogDir\cookies.txt" ($cookieDump2 | ConvertTo-Json -Depth 3)
    Write-Host "Cookies after change-branch saved to $LogDir/cookies-after-change-branch.json"
    Write-Host "Change-branch cookies:"
    ($cookieDump2 | ConvertTo-Json -Depth 3) | Write-Host

    # Verify branch selection state
    Write-Host "[3b] Verify branch selection -> $($BaseUrl)$($endpoints.Branches)"
    try {
        $verifyResp = Invoke-WebRequest -Uri (Url $endpoints.Branches) -Method Post -Body '{}' -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
        $verifyContent = $verifyResp.Content
        Write-JsonFile "$LogDir\\branches-verify.json" $verifyContent
        Write-Host "Branch verification response saved to $LogDir\\branches-verify.json"
    }
    catch {
        Write-Warning "Branch verification request failed: $_"
    }
}
else {
    Write-Warning "Skipping change-branch because no branch id selected. Stock creation might fail with code 1003."
}

# 4) Create stock card
Write-Host "[4/4] Create stock card -> $($BaseUrl)$($endpoints.StockCreate)"
function Send-HttpWithEncoding([string]$relative, [string]$bodyString, [string]$encodingName, [string]$contentType, [string]$outPrefix) {
    # Uses HttpWebRequest to send raw bytes with requested encoding and reuse the session cookie container
    try {
        $url = (Url $relative)

        if ($encodingName -eq 'windows-1254') {
            try { $enc = [System.Text.Encoding]::GetEncoding(1254) } catch { Write-Warning "cp1254 encoding provider not available on this runtime; falling back to UTF8"; $enc = [System.Text.Encoding]::UTF8 }
        }
        elseif ($encodingName -eq 'utf-8') { $enc = [System.Text.Encoding]::UTF8 }
        else { $enc = [System.Text.Encoding]::UTF8 }

        $bytes = $enc.GetBytes($bodyString)

        $req = [System.Net.WebRequest]::Create($url)
        $req.Method = 'POST'
        $req.ContentType = $contentType
        $req.Accept = 'application/json'
        # Attach existing cookies
        $req.CookieContainer = $session.Cookies
        $req.ContentLength = $bytes.Length

        $reqStream = $req.GetRequestStream()
        $reqStream.Write($bytes, 0, $bytes.Length)
        $reqStream.Close()

        $resp = $req.GetResponse()
        $respStream = $resp.GetResponseStream()
        $ms = New-Object System.IO.MemoryStream
        $buf = New-Object byte[] 8192
        while (($read = $respStream.Read($buf,0,$buf.Length)) -gt 0) { $ms.Write($buf,0,$read) }
        $respBytes = $ms.ToArray()
        $respStream.Close()

        # try decode by response charset then fallback
        $respContentType = $resp.Headers['Content-Type']
        $respCharset = $null
        if ($respContentType -match 'charset=([^;\r\n]+)') { $respCharset = $Matches[1] }

        if ($respCharset -and $respCharset -match '1254') {
            try { $respText = [System.Text.Encoding]::GetEncoding(1254).GetString($respBytes) } catch { Write-Warning "cp1254 decode not available; decoding as UTF8"; $respText = [System.Text.Encoding]::UTF8.GetString($respBytes) }
        }
        else { $respText = [System.Text.Encoding]::UTF8.GetString($respBytes) }

        # write artifacts
        $timeStamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
        Set-Content -Path "$LogDir\${outPrefix}-response-${timeStamp}.txt" -Value $respText -Encoding UTF8
        Set-Content -Path "$LogDir\${outPrefix}-headers-${timeStamp}.txt" -Value ($resp.Headers.ToString()) -Encoding UTF8
        return @{ StatusCode = $resp.StatusCode; Content = $respText; Response = $resp }
    }
    catch {
        Write-Warning "Send-HttpWithEncoding failed: $_"
        return @{ StatusCode = 0; Content = ''; Response = $null }
    }
}

# Option 1: Full payload with date-only (yyyy-MM-dd) and windows-1254 encoding
$stockPayloadObj = @{
    kartAdi = $ProductName;
    kartTuru = 1;
    baslangicTarihi = (Get-Date).ToString('yyyy-MM-dd');
    olcumBirimiId = $OlcumBirimiId;
    kartKodu = $ProductSKU;
    maliyetHesaplanacakFlag = $true;
    kartTipi = 1;
    kategoriAgacKod = "";
    kartAlisKdvOran = 0.20;
    kartSatisKdvOran = 0.20;
    bitisTarihi = (Get-Date).AddYears(10).ToString('yyyy-MM-dd');
    barkod = $ProductSKU;
    perakendeAlisBirimFiyat = 80.0;
    perakendeSatisBirimFiyat = 100.0;
    satilabilirFlag = $true;
    satinAlinabilirFlag = $true;
    detayAciklama = "Created by test script - cp1254 date-only"
}
$stockPayload = $stockPayloadObj | ConvertTo-Json -Depth 10
Write-JsonFile "$LogDir\stock-create-request.json" $stockPayload
Write-Host "Sending full payload as windows-1254 JSON with date-only fields..."
$res = Send-HttpWithEncoding $endpoints.StockCreate $stockPayload 'windows-1254' 'application/json; charset=windows-1254' 'stock-create-cp1254'
Write-JsonFile "$LogDir\stock-create-response-cp1254.json" $res.Content
if ($res.Response -ne $null) { Set-Content "$LogDir\http-stock-headers-cp1254.txt" $res.Response.Headers.ToString() } else { Set-Content "$LogDir\http-stock-headers-cp1254.txt" "<no-response>" }
Write-Host "Full payload (cp1254) response status: $($res.StatusCode)"

# If server returned HTML (unexpected error), run Option 2: minimal payload tests
if ($res.Content -and $res.Content.TrimStart().StartsWith('<')) {
    Write-Host "Detected HTML response for full payload - running minimal payload tests (UTF-8, cp1254, form-encoded)"

    $minimal = @{
        kartAdi = $ProductName;
        kartKodu = $ProductSKU;
        kartTuru = 1;
        baslangicTarihi = (Get-Date).ToString('yyyy-MM-dd');
        olcumBirimiId = $OlcumBirimiId
    }
    $minimalJson = $minimal | ConvertTo-Json -Depth 5
    # UTF-8 JSON
    $r1 = Send-HttpWithEncoding $endpoints.StockCreate $minimalJson 'utf-8' 'application/json; charset=utf-8' 'minimal-json-utf8'
    Write-JsonFile "$LogDir\minimal-json-utf8-response.json" $r1.Content
    if ($r1.Response -ne $null) { Set-Content "$LogDir\http-minimal-json-utf8-headers.txt" $r1.Response.Headers.ToString() } else { Set-Content "$LogDir\http-minimal-json-utf8-headers.txt" "<no-response>" }

    # cp1254 JSON
    $r2 = Send-HttpWithEncoding $endpoints.StockCreate $minimalJson 'windows-1254' 'application/json; charset=windows-1254' 'minimal-json-cp1254'
    Write-JsonFile "$LogDir\minimal-json-cp1254-response.json" $r2.Content
    if ($r2.Response -ne $null) { Set-Content "$LogDir\http-minimal-json-cp1254-headers.txt" $r2.Response.Headers.ToString() } else { Set-Content "$LogDir\http-minimal-json-cp1254-headers.txt" "<no-response>" }

    # form-encoded (cp1254)
    $formPairs = $minimal.GetEnumerator() | ForEach-Object { "$($_.Key)=$( [System.Uri]::EscapeDataString(($_.Value -as [string]) ))" }
    $formBody = [string]::Join('&', $formPairs)
    $r3 = Send-HttpWithEncoding $endpoints.StockCreate $formBody 'windows-1254' 'application/x-www-form-urlencoded; charset=windows-1254' 'minimal-form-cp1254'
    Write-JsonFile "$LogDir\minimal-form-cp1254-response.json" $r3.Content
    if ($r3.Response -ne $null) { Set-Content "$LogDir\http-minimal-form-cp1254-headers.txt" $r3.Response.Headers.ToString() } else { Set-Content "$LogDir\http-minimal-form-cp1254-headers.txt" "<no-response>" }

    # form-encoded (utf8)
    $r4 = Send-HttpWithEncoding $endpoints.StockCreate $formBody 'utf-8' 'application/x-www-form-urlencoded; charset=utf-8' 'minimal-form-utf8'
    Write-JsonFile "$LogDir\minimal-form-utf8-response.json" $r4.Content
    if ($r4.Response -ne $null) { Set-Content "$LogDir\http-minimal-form-utf8-headers.txt" $r4.Response.Headers.ToString() } else { Set-Content "$LogDir\http-minimal-form-utf8-headers.txt" "<no-response>" }

    Write-Host "Minimal tests complete. Files written to $LogDir"
}
else {
    # response was not HTML; save original response
    Write-JsonFile "$LogDir\stock-create-response.json" $res.Content
    if ($res.Response -ne $null) { Set-Content "$LogDir\http-stock-headers.txt" $res.Response.Headers.ToString() } else { Set-Content "$LogDir\http-stock-headers.txt" "<no-response>" }
    Write-Host "Stock create completed (non-HTML response)."    
}

Write-Host "Done. Artifacts written to: $LogDir"

# Print last cookies and responses summary
Write-Host "--- Summary ---"
Get-Content "$LogDir\cookies-after-change-branch.json" -ErrorAction SilentlyContinue | Write-Host
Get-Content "$LogDir\change-branch-response.json" -ErrorAction SilentlyContinue | Write-Host
Get-Content "$LogDir\stock-create-response.json" -ErrorAction SilentlyContinue | Write-Host

# End
