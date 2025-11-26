$outDir = Join-Path '.' ("scripts/logs/sent-summary-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$files = Get-ChildItem -Path .\scripts\logs -Recurse -Filter success.json -ErrorAction SilentlyContinue
$entries = @()
foreach ($f in $files) {
    try {
        $text = Get-Content $f.FullName -Raw -ErrorAction Stop
        if (-not $text) { continue }
        $j = $text | ConvertFrom-Json -ErrorAction Stop
    } catch { continue }
    if ($j -is [System.Array]) { $arr = $j } else { $arr = @($j) }
    foreach ($it in $arr) {
        $sku = $null
        if ($it.sku) { $sku = $it.sku }
        elseif ($it.kod) { $sku = $it.kod }
        elseif ($it.kartKodu) { $sku = $it.kartKodu }
        elseif ($it.kodu) { $sku = $it.kodu }
        elseif ($it.STOK_KODU) { $sku = $it.STOK_KODU }
        else { $sku = $null }
        $skartId = $null
        if ($it.skartId) { $skartId = $it.skartId }
        elseif ($it.skartID) { $skartId = $it.skartID }
        $entries += [PSCustomObject]@{ sku = $sku; skartId = $skartId; source = $f.FullName }
    }
}

if (-not $entries -or $entries.Count -eq 0) {
    Write-Output "No success.json entries found under ./scripts/logs. Exiting."
    exit 1
}

$totalEntries = $entries.Count
$uniqueSkus = ($entries | Where-Object { $_.sku } | Select-Object -Property sku -Unique).Count
$entriesWithSkartId = ($entries | Where-Object { $_.skartId -ne $null -and ($_.skartId -ne '') }).Count
$uniqueSkartIds = ($entries | Where-Object { $_.skartId -ne $null -and ($_.skartId -ne '') } | Select-Object -Property skartId -Unique).Count

$detailFile = Join-Path $outDir 'sent-entries.json'
$summaryFile = Join-Path $outDir 'sent-summary.json'
$entries | ConvertTo-Json -Depth 5 | Out-File $detailFile -Encoding utf8

$summary = [PSCustomObject]@{
    timestamp = (Get-Date).ToString('o')
    totalEntries = $totalEntries
    uniqueSkus = $uniqueSkus
    entriesWithSkartId = $entriesWithSkartId
    uniqueSkartIds = $uniqueSkartIds
    detailFile = $detailFile
}
$summary | ConvertTo-Json -Depth 5 | Out-File $summaryFile -Encoding utf8

Write-Output "Summary written to: $summaryFile"
Write-Output "Total entries: $totalEntries; Unique SKUs: $uniqueSkus; Entries with skartId: $entriesWithSkartId; Unique skartIds: $uniqueSkartIds"
