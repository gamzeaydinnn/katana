param(
    [string]$KozaBaseUrl = 'https://akozas.luca.com.tr/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path (Get-Location) "scripts\logs\koza-verify-kod-$ts"
New-Item -Path $outDir -ItemType Directory -Force | Out-Null

Write-Output "Locating latest sync-katana-to-luca success.json under scripts/logs..."
$syncDirs = Get-ChildItem -Path (Join-Path (Get-Location) 'scripts\logs') -Directory -Filter 'sync-katana-to-luca-*' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
if (-not $syncDirs -or $syncDirs.Count -eq 0) { Write-Error "No sync-katana-to-luca logs found."; exit 1 }
$latest = $syncDirs[0].FullName
$successFile = Join-Path $latest 'success.json'
if (-not (Test-Path $successFile)) { Write-Error "No success.json in $latest"; exit 1 }

Write-Output "Reading success file: $successFile"
$entries = Get-Content $successFile -Raw | ConvertFrom-Json -ErrorAction Stop

$skus = $entries | ForEach-Object { $_.SKU } | Select-Object -Unique
Write-Output "Found $($skus.Count) unique SKUs to check."

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
foreach ($sku in $skus) {
    Write-Output "Checking SKU: $sku"
    $body = @{ stkSkart = @{ kod = $sku } } | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        $rawFile = Join-Path $outDir ("raw-kod-$($sku).json")
        $resp | ConvertTo-Json -Depth 10 | Out-File $rawFile -Encoding utf8
        $found = $false
        $record = $null
        if ($resp -and $resp.PSObject.Properties.Match('stkSkart') -and $resp.stkSkart.Count -gt 0) {
            foreach ($r in $resp.stkSkart) { if ($r.kod -eq $sku -or $r.kod -eq [string]$sku) { $found = $true; $record = $r; break } }
        } elseif ($resp -and $resp.PSObject.Properties.Match('data') -and $resp.data.Count -gt 0) {
            foreach ($r in $resp.data) { if ($r.kod -eq $sku -or $r.kod -eq [string]$sku) { $found = $true; $record = $r; break } }
        } else {
            if ($resp -is [System.Collections.IEnumerable]) {
                foreach ($r in $resp) { if ($r.kod -eq $sku) { $found = $true; $record = $r; break } }
            }
        }
        if ($found) { Write-Output "FOUND SKU: $sku -> skartId=$($record.skartId)" } else { Write-Output "NOT FOUND SKU: $sku" }
        $results += [pscustomobject]@{ SKU=$sku; found=$found; record=$record; raw=$rawFile }
    } catch {
        Write-Warning ("Lookup failed for SKU {0}: {1}" -f $sku, $_.Exception.Message)
        $results += [pscustomobject]@{ SKU=$sku; found=$false; record=$null; error=$_.Exception.Message }
    }
}

$out = Join-Path $outDir 'verify-kod-results.json'
$results | ConvertTo-Json -Depth 10 | Out-File $out -Encoding utf8
Write-Output "Kod-verify run complete. Results: $out"
