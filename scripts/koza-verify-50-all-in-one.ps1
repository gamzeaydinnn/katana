# One-shot verifier: collect up to 50 SKUs from success.json and verify in Koza
$outDir = Join-Path -Path '.' -ChildPath ("scripts/logs/koza-check-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Gather success.json files
$files = Get-ChildItem -Path .\scripts\logs -Recurse -Filter success.json -ErrorAction SilentlyContinue
$items = @()
foreach ($f in $files) {
    try {
        $text = Get-Content $f.FullName -Raw -ErrorAction Stop
        if (-not $text) { continue }
        $j = $text | ConvertFrom-Json -ErrorAction Stop
    } catch {
        continue
    }
    if ($j -is [System.Array]) { $arr = $j } else { $arr = @($j) }
    foreach ($it in $arr) {
        $sku = $null
        if ($it.sku) { $sku = $it.sku }
        elseif ($it.kod) { $sku = $it.kod }
        elseif ($it.kartKodu) { $sku = $it.kartKodu }
        elseif ($it.kodu) { $sku = $it.kodu }
        elseif ($it.STOK_KODU) { $sku = $it.STOK_KODU }
        else { continue }
        $skartId = $null
        if ($it.skartId) { $skartId = $it.skartId }
        $items += [PSCustomObject]@{ sku = $sku; skartId = $skartId; source = $f.FullName }
    }
}

if (-not $items -or $items.Count -eq 0) {
    Write-Output "No success.json entries found under ./scripts/logs. Exiting."
    exit 1
}

# Unique by sku preserving first occurrence, then take first 50
$uniq = $items | Group-Object -Property sku | ForEach-Object { $_.Group[0] }
$selection = $uniq | Select-Object -Unique sku,skartId,source | Select-Object -First 50
$skusFile = Join-Path $outDir 'skus.json'
$selection | ConvertTo-Json -Depth 4 | Out-File $skusFile -Encoding utf8
Write-Output "Collected $($selection.Count) unique SKUs -> $skusFile"

# Koza connection settings (adjust if needed)
$baseUrl = 'http://85.111.1.49:57005'
$orgCode = 1422649
$username = 'Admin'
$password = 'WebServis'
$branchId = 854

# Login and set branch
$sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{ orgCode = $orgCode; userName = $username; userPassword = $password } | ConvertTo-Json -Compress
try {
    Invoke-RestMethod -Uri "$baseUrl/Yetki/Giris.do" -Method Post -Body $loginBody -ContentType 'application/json' -WebSession $sess -ErrorAction Stop | Out-Null
    Write-Output 'Login OK'
} catch {
    Write-Output "Login failed: $($_.Exception.Message)"
    exit 1
}
$branchBody = @{ orgSirketSubeId = $branchId } | ConvertTo-Json -Compress
try { Invoke-RestMethod -Uri "$baseUrl/Yetki/GuncelleYtkSirketSubeDegistir.do" -Method Post -Body $branchBody -ContentType 'application/json' -WebSession $sess -ErrorAction Stop | Out-Null; Write-Output 'Branch set OK' } catch { Write-Warning "Branch set failed: $($_.Exception.Message)" }

$results = @()
foreach ($entry in $selection) {
    $sku = $entry.sku
    $skartId = $entry.skartId
    $found = $false
    $raws = @()

    if ($skartId) {
        $body = @{ stkSkart = @{ skartId = [int]$skartId } } | ConvertTo-Json -Compress
        try { $resp = Invoke-RestMethod -Uri "$baseUrl/Yetki/ListeleStkSkart.do" -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop } catch { $resp = $null }
        $raws += @{ attempt = 'bySkartId'; body = $body; response = $resp }
        if ($resp -and $resp.list) {
            foreach ($r in $resp.list) {
                if (($r.skartId -eq [int]$skartId) -or ($r.kod -eq $sku) -or ($r.adi -like "*$sku*") -or ($r.malAdiII -like "*$sku*")) { $found = $true; break }
            }
        }
    }

    if (-not $found) {
        $body = @{ stkSkart = @{ kod = $sku }; kodOp = 'equals' } | ConvertTo-Json -Compress
        try { $resp = Invoke-RestMethod -Uri "$baseUrl/Yetki/ListeleStkSkart.do" -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop } catch { $resp = $null }
        $raws += @{ attempt = 'byKod_equals'; body = $body; response = $resp }
        if ($resp -and $resp.list) {
            foreach ($r in $resp.list) {
                if (($r.kod -eq $sku) -or ($r.kod -like "*$sku*") -or ($r.adi -like "*$sku*")) { $found = $true; break }
            }
        }
    }

    if (-not $found) {
        $body = @{ stkSkart = @{ kodBas = $sku; kodBit = $sku }; kodOp = 'between' } | ConvertTo-Json -Compress
        try { $resp = Invoke-RestMethod -Uri "$baseUrl/Yetki/ListeleStkSkart.do" -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop } catch { $resp = $null }
        $raws += @{ attempt = 'byKod_between'; body = $body; response = $resp }
        if ($resp -and $resp.list) {
            foreach ($r in $resp.list) {
                if (($r.kod -eq $sku) -or ($r.adi -like "*$sku*")) { $found = $true; break }
            }
        }
    }

    if (-not $found) {
        $page = 0
        while (-not $found -and $page -lt 3) {
            $body = @{ page = $page; size = 50 } | ConvertTo-Json -Compress
            try { $resp = Invoke-RestMethod -Uri "$baseUrl/Yetki/ListeleStkSkart.do" -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop } catch { $resp = $null }
            $raws += @{ attempt = "scan_page_$page"; body = $body; response = $resp }
            if ($resp -and $resp.list) {
                foreach ($r in $resp.list) {
                    if (($r.kod -eq $sku) -or ($r.adi -like "*$sku*") -or ($r.malAdiII -like "*$sku*") -or ($r.kod -like "*$sku*")) { $found = $true; break }
                }
            }
            $page++
        }
    }

    $results += [PSCustomObject]@{ sku = $sku; skartId = $skartId; found = $found }
    $safe = ($sku -replace '[\\/:*?"<>|]','_')
    # Save raw attempts for this SKU
    $raws | ConvertTo-Json -Depth 8 | Out-File (Join-Path $outDir ("raw-{0}.json" -f $safe)) -Encoding utf8
}

# Save summary and raw results
$results | ConvertTo-Json -Depth 8 | Out-File (Join-Path $outDir 'koza-check-results.json') -Encoding utf8

$foundCount = ($results | Where-Object { $_.found } | Measure-Object).Count
$missingCount = ($results | Where-Object { -not $_.found } | Measure-Object).Count
Write-Output "Summary - Found: $foundCount Missing: $missingCount"
Write-Output "Logs written to: $outDir"
