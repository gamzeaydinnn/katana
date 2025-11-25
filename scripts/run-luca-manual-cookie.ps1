<#
run-luca-manual-cookie.ps1

Ad-hoc PowerShell helper to run Luca (Koza) requests using a manual JSESSIONID cookie.
Saves artifacts under `scripts/logs` relative to the script file. Intended for interactive use
from the repository root. Designed to avoid writing to C:\ when LogDir is missing.

Usage:
  # from repo root (PowerShell prompt)
  .\scripts\run-luca-manual-cookie.ps1

  # or pass custom cookie and branch id
  .\scripts\run-luca-manual-cookie.ps1 -JSessionValue 'JSESSIONID=...' -ForcedBranchId 854
#>

param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$CookieDomain = '85.111.1.49',
    # Optional manual JSESSIONID (if you prefer manual injection instead of login)
    [string]$JSessionValue = '',
    # Preferred: use server login to obtain session cookies
    [string]$OrgCode = '',
    [string]$Username = '',
    [string]$Password = '',
    [int]$ForcedBranchId = 854,
    [string]$ProductSKU = 'MY-SKU-1',
    [string]$ProductName = 'Test Product',
    [int]$OlcumBirimiId = 5,
    [string]$LogDir = $null
)

# Ensure BaseUrl ends with '/'
if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }

# Resolve script directory and default LogDir to scripts/logs next to this file
if ($PSScriptRoot) {
    $scriptRoot = $PSScriptRoot
} else {
    $scriptRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
}
if (-not $LogDir -or [string]::IsNullOrWhiteSpace($LogDir)) {
    $LogDir = Join-Path $scriptRoot 'logs'
}

# Create log dir if missing
if (-not (Test-Path $LogDir)) { New-Item -Path $LogDir -ItemType Directory -Force | Out-Null }

function Write-JsonFile($path, $obj) {
    $json = $null
    if ($obj -is [string]) { $json = $obj } else { $json = $obj | ConvertTo-Json -Depth 10 }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

function Url([string]$relative) { return ([string]::Format("{0}{1}",$BaseUrl,$relative)) }

# Avoid assigning to the automatic $Host variable; use $CookieDomain instead
# Build session
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# If OrgCode/Username/Password provided, try full login (Giris.do) to let server create session state
if (-not [string]::IsNullOrWhiteSpace($Username) -and -not [string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "Performing Giris.do login to obtain session cookie..."
    try {
        $loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Depth 5
        Write-JsonFile (Join-Path $LogDir 'manual-login-request.json') $loginPayload
        $loginResp = Invoke-WebRequest -Uri (Url 'Giris.do') -Method Post -Body $loginPayload -WebSession $session -Headers @{ 'Content-Type' = 'application/json'; 'Accept' = 'application/json' } -UseBasicParsing -ErrorAction Stop
        Write-JsonFile (Join-Path $LogDir 'manual-login-response.json') $loginResp.Content
        Write-Host "Login response saved to $LogDir\manual-login-response.json"

        # Dump cookies after login
        $cookieDump = @()
        foreach ($c in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
            $cookieDump += ([PSCustomObject]@{ Name=$c.Name; Value=$c.Value; Domain=$c.Domain; Path=$c.Path; Expires=$c.Expires; HttpOnly=$c.HttpOnly })
        }
        Write-JsonFile (Join-Path $LogDir 'cookies-after-login.json') $cookieDump
        Set-Content (Join-Path $LogDir 'cookies.txt') ($cookieDump | ConvertTo-Json -Depth 3)
    }
    catch {
        Write-Warning "Login failed: $_"
    }
}
else {
    # Fall back to manual cookie injection if JSessionValue provided
    if (-not [string]::IsNullOrWhiteSpace($JSessionValue)) {
        try {
            $cookie = New-Object System.Net.Cookie('JSESSIONID', $JSessionValue, '/', $CookieDomain)
            $session.Cookies.Add($cookie)
            Write-Host "Added manual JSESSIONID to session cookie jar."
        }
        catch {
            Write-Warning "Could not add manual cookie: $_"
        }
    }
    else {
        Write-Host "No login credentials and no manual JSESSIONID provided; continuing with empty cookie jar (may fail)."
    }
}

$headers = @{ 'Accept' = 'application/json'; 'Content-Type' = 'application/json' }

# Send helper that supports cp1254 encoding when needed
function Send-HttpWithEncoding([string]$relative, [string]$bodyString, [string]$encodingName, [string]$contentType, [string]$outPrefix) {
    try {
        $url = (Url $relative)

        if ($encodingName -eq 'windows-1254') {
            try { $enc = [System.Text.Encoding]::GetEncoding(1254) } catch { Write-Warning "cp1254 not available; falling back to UTF8"; $enc = [System.Text.Encoding]::UTF8 }
        }
        elseif ($encodingName -eq 'utf-8') { $enc = [System.Text.Encoding]::UTF8 }
        else { $enc = [System.Text.Encoding]::UTF8 }

        $bytes = $enc.GetBytes($bodyString)

        $req = [System.Net.HttpWebRequest]::Create($url)
        $req.Method = 'POST'
        $req.ContentType = $contentType
        $req.Accept = 'application/json'
        $req.CookieContainer = New-Object System.Net.CookieContainer

        $baseUri = [System.Uri]$BaseUrl
        foreach ($c in $session.Cookies.GetCookies($baseUri)) {
            $netCookie = New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain)
            $req.CookieContainer.Add($netCookie)
        }

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

        $respContentType = $resp.Headers['Content-Type']
        $respCharset = $null
        if ($respContentType -match 'charset=([^;\r\n]+)') { $respCharset = $Matches[1] }

        if ($respCharset -and $respCharset -match '1254') {
            try { $respText = [System.Text.Encoding]::GetEncoding(1254).GetString($respBytes) } catch { $respText = [System.Text.Encoding]::UTF8.GetString($respBytes) }
        }
        else { $respText = [System.Text.Encoding]::UTF8.GetString($respBytes) }

        $timeStamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
        $respPath = Join-Path $LogDir "$outPrefix-response-$timeStamp.txt"
        $hdrPath = Join-Path $LogDir "$outPrefix-headers-$timeStamp.txt"
        Set-Content -Path $respPath -Value $respText -Encoding UTF8
        Set-Content -Path $hdrPath -Value ($resp.Headers.ToString()) -Encoding UTF8

        # If server sent Set-Cookie headers (e.g. new JSESSIONID), update our $session cookie jar
        try {
            $setCookie = $resp.Headers['Set-Cookie']
            if ($setCookie) {
                $baseUri = [System.Uri]$BaseUrl
                # split possible multiple cookies
                $cookieParts = $setCookie -split ','
                foreach ($part in $cookieParts) {
                    if ($part -match 'JSESSIONID=([^;\s]+)') {
                        $newVal = $Matches[1]
                        try {
                            $nc = New-Object System.Net.Cookie('JSESSIONID', $newVal, '/', $baseUri.Host)
                            $session.Cookies.Add($baseUri, $nc)
                            Write-Host "Updated session cookie from Set-Cookie: JSESSIONID=$newVal"
                        } catch {
                            Write-Warning "Failed to add cookie from Set-Cookie: $_"
                        }
                    }
                }
            }
        }
        catch { }
        return @{ StatusCode = $resp.StatusCode; Content = $respText; Response = $resp }
    }
    catch {
        Write-Warning "Send-HttpWithEncoding failed: $_"
        return @{ StatusCode = 0; Content = ''; Response = $null }
    }
}

Write-Host "Using LogDir = $LogDir"

# 1) Fetch branches
Write-Host "[1] Fetch branches..."
try {
    $branchesResp = Invoke-WebRequest -Uri (Url 'YdlUserResponsibilityOrgSs.do') -Method Post -Body '{}' -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
    Write-JsonFile (Join-Path $LogDir 'manual-branches.json') $branchesResp.Content
    Write-Host "Branches saved to $LogDir\manual-branches.json"
}
catch {
    Write-Warning "Branches fetch failed: $_"
}

# 2) Change branch
$branchId = $ForcedBranchId
Write-Host "[2] Change branch -> $branchId"
$changeCandidates = @(
    @{ orgSirketSubeId = $branchId },
    @{ vtOrgYtkSirketSubeId = $branchId },
    @{ orgSirketSube = $branchId }
)
$changed = $false
foreach ($p in $changeCandidates) {
    $json = $p | ConvertTo-Json -Depth 5
    try {
        $resp = Invoke-WebRequest -Uri (Url 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $json -WebSession $session -Headers $headers -ContentType 'application/json' -UseBasicParsing -ErrorAction Stop
        Write-JsonFile (Join-Path $LogDir 'manual-change-branch.json') $resp.Content
        Write-Host "Change-branch attempt saved to $LogDir\manual-change-branch.json"
        $changed = $true
        break
    }
    catch {
        Write-Warning ("Change attempt failed for payload {0}: {1}" -f $json, $_)
    }
}
if (-not $changed) { Write-Warning "All change-branch attempts failed." }

# 3) Verify branch
Write-Host "[3] Verify branch selection..."
try {
    $verifyResp = Invoke-WebRequest -Uri (Url 'YdlUserResponsibilityOrgSs.do') -Method Post -Body '{}' -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
    Write-JsonFile (Join-Path $LogDir 'manual-branches-after-change.json') $verifyResp.Content
    Write-Host "Verify response saved to $LogDir\manual-branches-after-change.json"
}
catch {
    Write-Warning "Branch verify failed: $_"
}

# 4) Send stock-create as cp1254 JSON
Write-Host "[4] Sending stock-create (cp1254 JSON)..."
$today = (Get-Date).ToString('yyyy-MM-dd')
$stockObj = @{
  kartAdi = $ProductName
  kartTuru = 1
  baslangicTarihi = $today
  olcumBirimiId = $OlcumBirimiId
  kartKodu = $ProductSKU
  perakendeSatisBirimFiyat = 100.0
  perakendeAlisBirimFiyat = 80.0
}
$stockBody = $stockObj | ConvertTo-Json -Depth 10
Write-JsonFile (Join-Path $LogDir 'manual-stock-create-request.json') $stockBody
$res = Send-HttpWithEncoding 'EkleStkWsSkart.do' $stockBody 'windows-1254' 'application/json; charset=windows-1254' 'manual-stock-create'
Write-JsonFile (Join-Path $LogDir 'manual-stock-create-response.json') $res.Content
if ($res.Response -ne $null) { Set-Content (Join-Path $LogDir 'manual-stock-create-headers.txt') $res.Response.Headers.ToString() }
Write-Host "Stock-create response saved to $LogDir\manual-stock-create-response.json"

# If server says login/branch required, try once more after applying any Set-Cookie updates
try {
    $needRetry = $false
    $contentLower = $res.Content.ToString().ToLower()
    if ($contentLower -like '*login olunmalı*' -or $contentLower -like '*şirket şube seçimi yap*' -or $contentLower -match '"code"\s*:\s*1001' -or $contentLower -match '"code"\s*:\s*1003') { $needRetry = $true }
    if ($needRetry) {
        Write-Host "Server responded with login/branch requirement. Re-running change-branch and retrying stock-create..."

        # attempt change-branch again using updated session cookie
        $changed = $false
        foreach ($p in $changeCandidates) {
            $json = $p | ConvertTo-Json -Depth 5
            try {
                $resp = Invoke-WebRequest -Uri (Url 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $json -WebSession $session -Headers $headers -ContentType 'application/json' -UseBasicParsing -ErrorAction Stop
                Write-JsonFile (Join-Path $LogDir 'manual-change-branch-retry.json') $resp.Content
                Write-Host "Retry change-branch attempt saved to $LogDir\manual-change-branch-retry.json"
                $changed = $true
                break
            }
            catch {
                Write-Warning ("Retry change attempt failed for payload {0}: {1}" -f $json, $_)
            }
        }

        if ($changed) {
            # update cookies saved after retry
            $cookieDump2 = @()
            foreach ($c in $session.Cookies.GetCookies([System.Uri]$BaseUrl)) {
                $cookieDump2 += ([PSCustomObject]@{ Name=$c.Name; Value=$c.Value; Domain=$c.Domain; Path=$c.Path; Expires=$c.Expires; HttpOnly=$c.HttpOnly })
            }
            Write-JsonFile (Join-Path $LogDir 'cookies-after-change-branch-retry.json') $cookieDump2
            Set-Content (Join-Path $LogDir 'cookies.txt') ($cookieDump2 | ConvertTo-Json -Depth 3)

            # retry stock-create using utf-8 JSON first
            $res2 = Send-HttpWithEncoding 'EkleStkWsSkart.do' $stockBody 'utf-8' 'application/json; charset=utf-8' 'manual-stock-create-retry'
            Write-JsonFile (Join-Path $LogDir 'manual-stock-create-response-retry.json') $res2.Content
            if ($res2.Response -ne $null) { Set-Content (Join-Path $LogDir 'manual-stock-create-headers-retry.txt') $res2.Response.Headers.ToString() }
            Write-Host "Retry stock-create response saved to $LogDir\manual-stock-create-response-retry.json"
        }
    }
}
catch { Write-Warning "Retry logic failed: $_" }

Write-Host "Done. Check $LogDir for artifacts (manual-*-*.json / .txt)."
