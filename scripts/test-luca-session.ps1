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

Usage examples:
  .\test-luca-session.ps1
  .\test-luca-session.ps1 -BaseUrl 'http://85.111.1.49:57005/Yetki/' -OrgCode 1422649 -UserName Admin -Password WebServis -ForcedBranchId 854
#>
param(
    [string]$BaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$UserName = 'Admin',
    [string]$Password = 'WebServis',
    [int]$ForcedBranchId = 85,
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
}
else {
    Write-Warning "Skipping change-branch because no branch id selected. Stock creation might fail with code 1003."
}

# 4) Create stock card
Write-Host "[4/4] Create stock card -> $($BaseUrl)$($endpoints.StockCreate)"
$effectiveName = [string]::IsNullOrWhiteSpace($ProductName) ? $ProductSKU : $ProductName
$stockPayload = @{
    KartAdi = $effectiveName;
    KartTuru = 1;
    OlcumBirimiId = $OlcumBirimiId;
    KartKodu = $ProductSKU;
    PerakendeSatisBirimFiyat = 100.0;
    PerakendeAlisBirimFiyat = 80.0
} | ConvertTo-Json
try {
    $stockResp = Invoke-WebRequest -Uri (Url $endpoints.StockCreate) -Method Post -Body $stockPayload -WebSession $session -Headers $headers -UseBasicParsing -ErrorAction Stop
    $stockContent = $stockResp.Content
    Write-JsonFile "$LogDir\stock-create-response.json" $stockContent
    Set-Content "$LogDir\http-stock-headers.txt" ($stockResp.Headers | Out-String)
    Write-Host "Stock create response saved to $LogDir/stock-create-response.json"
}
catch {
    Write-Warning "Stock create request failed: $_"
    if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        try { $body = (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd(); Write-JsonFile "$LogDir\stock-create-response.json" $body } catch {}
    }
    throw
}

Write-Host "Done. Artifacts written to: $LogDir"

# Print last cookies and responses summary
Write-Host "--- Summary ---"
Get-Content "$LogDir\cookies-after-change-branch.json" -ErrorAction SilentlyContinue | Write-Host
Get-Content "$LogDir\change-branch-response.json" -ErrorAction SilentlyContinue | Write-Host
Get-Content "$LogDir\stock-create-response.json" -ErrorAction SilentlyContinue | Write-Host

# End
