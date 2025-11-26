# Parse all success.json files and extract skartId from Response fields
$outDir = Join-Path -Path '.' -ChildPath ("scripts/logs/created-by-koza-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$files = Get-ChildItem -Path .\scripts\logs -Recurse -Filter success.json -ErrorAction SilentlyContinue
$results = @()
foreach ($f in $files) {
    try {
        $text = Get-Content $f.FullName -Raw -ErrorAction Stop
        if (-not $text) { continue }
        $arr = $text | ConvertFrom-Json -ErrorAction Stop
    } catch {
        continue
    }
    if ($arr -isnot [System.Array]) { $arr = @($arr) }
    foreach ($it in $arr) {
        $sku = $null
        if ($it.SKU) { $sku = $it.SKU }
        elseif ($it.sku) { $sku = $it.sku }
        elseif ($it.Kod) { $sku = $it.Kod }
        elseif ($it.kod) { $sku = $it.kod }

        $respRaw = $null
        if ($it.Response) { $respRaw = $it.Response }
        elseif ($it.response) { $respRaw = $it.response }

        $parsed = $null
        $skartId = $null
        $message = $null
        if ($respRaw) {
            # If already an object, take it
            if ($respRaw -is [System.Management.Automation.PSCustomObject] -or $respRaw -is [System.Collections.Hashtable]) {
                $parsed = $respRaw
            } else {
                # Try to parse JSON string (may be escaped)
                $s = $respRaw -as [string]
                # Trim surrounding quotes if present
                $s = $s.Trim()
                if ($s.StartsWith('"') -and $s.EndsWith('"')) {
                    $s = $s.Substring(1, $s.Length-2)
                }
                # Unescape common escapes
                try { $candidate = $s -replace '\\u([0-9A-Fa-f]{4})', { [char]([Convert]::ToInt32($args[0].Groups[1].Value,16)) } ; $candidate = $candidate -replace '\\"','"' -replace '\\\\','\\' ; $parsed = $candidate | ConvertFrom-Json -ErrorAction Stop } catch {
                    # fallback: try to extract skartId by regex
                    $parsed = $null
                }
            }
        }

        if ($parsed) {
            if ($parsed.skartId) { $skartId = $parsed.skartId }
            if ($parsed.message) { $message = $parsed.message }
        } else {
            # regex extraction from raw string (if present)
            if ($respRaw) {
                $m = [regex]::Match($respRaw, '"?skartId"?\s*[:=]\s*([0-9]+)')
                if ($m.Success) { $skartId = [int]$m.Groups[1].Value }
                $m2 = [regex]::Match($respRaw, '"?message"?\s*[:=]\s*"([^"]+)"')
                if ($m2.Success) { $message = $m2.Groups[1].Value }
            }
        }

        $results += [PSCustomObject]@{
            sku = $sku
            skartId = $skartId
            message = $message
            responseRaw = $respRaw
            source = $f.FullName
        }
    }
}

if (-not $results -or $results.Count -eq 0) {
    Write-Output "No parsed responses found."
    exit 1
}

$created = $results | Where-Object { $_.skartId -ne $null }
$createdUniqueSkus = ($created | Select-Object -Property sku -Unique).Count
$createdCount = $created.Count
$uniqueSkartIds = ($created | Select-Object -Property skartId -Unique).Count

$detailFile = Join-Path $outDir 'created.json'
$summaryFile = Join-Path $outDir 'created-summary.json'
$results | ConvertTo-Json -Depth 6 | Out-File $detailFile -Encoding utf8

$summary = [PSCustomObject]@{
    timestamp = (Get-Date).ToString('o')
    totalEntriesScanned = $results.Count
    createdCount = $createdCount
    createdUniqueSkus = $createdUniqueSkus
    uniqueSkartIds = $uniqueSkartIds
    detailFile = $detailFile
}
$summary | ConvertTo-Json -Depth 4 | Out-File $summaryFile -Encoding utf8

Write-Output "Parsed $($results.Count) entries. Created (with skartId): $createdCount (unique SKUs: $createdUniqueSkus, unique skartIds: $uniqueSkartIds)."
Write-Output "Details: $detailFile"
Write-Output "Summary: $summaryFile"
