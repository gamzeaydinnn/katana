param(
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path (Get-Location) "scripts\logs\koza-verify-skartids-$ts"
New-Item -Path $outDir -ItemType Directory -Force | Out-Null

Write-Output "Locating latest sync-katana-to-luca success.json under scripts/logs..."
$syncDirs = Get-ChildItem -Path (Join-Path (Get-Location) 'scripts\logs') -Directory -Filter 'sync-katana-to-luca-*' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
if (-not $syncDirs -or $syncDirs.Count -eq 0) { Write-Error "No sync-katana-to-luca logs found."; exit 1 }
$latest = $syncDirs[0].FullName
$successFile = Join-Path $latest 'success.json'
if (-not (Test-Path $successFile)) { Write-Error "No success.json in $latest"; exit 1 }

Write-Output "Reading success file: $successFile"
$entries = Get-Content $successFile -Raw | ConvertFrom-Json -ErrorAction Stop

$targets = @()
foreach ($e in $entries) {
    $sku = $e.SKU
    $resp = $e.Response
    $skartId = $null
    if ($resp -and ($resp -isnot [string])) {
        # sometimes already parsed
        if ($resp.PSObject.Properties.Match('skartId')) { $skartId = $resp.skartId }
    } elseif ($resp -and ($resp -is [string])) {
        $txt = $resp.Trim()
        if ($txt -eq '(form-saved)') { continue }
        # try direct JSON parse
        try {
            $p = $txt | ConvertFrom-Json -ErrorAction Stop
            if ($p.PSObject.Properties.Match('skartId')) { $skartId = $p.skartId }
        } catch {
            # try simple regex
            $m = [regex]::Match($txt, '"?skartId"?\s*[:=]\s*(\d+)')
            if ($m.Success) { $skartId = [int]$m.Groups[1].Value }
        }
    }
    if ($skartId) { $targets += [pscustomobject]@{ SKU=$sku; skartId=$skartId } }
}

if ($targets.Count -eq 0) { Write-Output "No skartIds parsed from success.json"; exit 0 }

Write-Output "Parsed $($targets.Count) skartIds. Logging in to Koza to verify presence..."

$sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginJson = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Compress
try {
    $login = Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body $loginJson -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Write-Output "Koza login: $($login.StatusCode)"
} catch { Write-Error ("Koza login failed: {0}" -f $_.Exception.Message); exit 1 }

try {
    $branchResp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body (@{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress) -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Write-Output "Branch change: $($branchResp.StatusCode)"
} catch { Write-Warning ("Branch change failed: {0}" -f $_.Exception.Message) }

$results = @()
foreach ($t in $targets) {
    $id = $t.skartId
    $sku = $t.SKU
    Write-Output "Checking skartId: $id (SKU: $sku)"
    $body = @{ stkSkart = @{ skartId = $id } } | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        $rawFile = Join-Path $outDir ("raw-$id.json")
        $resp | ConvertTo-Json -Depth 10 | Out-File $rawFile -Encoding utf8
        $found = $false
        # common places: resp.stkSkart (array) or resp.data
        if ($resp -and $resp.PSObject.Properties.Match('stkSkart') -and $resp.stkSkart.Count -gt 0) {
            foreach ($r in $resp.stkSkart) { if ($r.skartId -eq $id) { $found = $true; $record = $r; break } }
        } elseif ($resp -and $resp.PSObject.Properties.Match('data') -and $resp.data.Count -gt 0) {
            foreach ($r in $resp.data) { if ($r.skartId -eq $id -or $r.skartId -eq [string]$id) { $found = $true; $record = $r; break } }
        } else {
            # try scanning top-level arrays
            if ($resp -is [System.Collections.IEnumerable]) {
                foreach ($r in $resp) { if ($r.skartId -eq $id) { $found = $true; $record = $r; break } }
            }
        }
        if ($found) { Write-Output "FOUND: $id" } else { Write-Output "NOT FOUND: $id" }
        $results += [pscustomobject]@{ SKU=$sku; skartId=$id; found=$found; record=$record; raw=$rawFile }
    } catch {
        Write-Warning ("Lookup failed for {0}: {1}" -f $id, $_.Exception.Message)
        $results += [pscustomobject]@{ SKU=$sku; skartId=$id; found=$false; record=$null; error=$_.Exception.Message }
    }
}

$out = Join-Path $outDir 'verify-results.json'
$results | ConvertTo-Json -Depth 10 | Out-File $out -Encoding utf8
Write-Output "Verify run complete. Results: $out"
