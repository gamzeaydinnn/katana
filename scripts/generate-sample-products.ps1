param(
    [int]$Count = 50,
    [string]$SampleFile = '.\scripts\logs\sample-products.json',
    [string]$OutDir = '.\scripts\logs'
)

if (-not (Test-Path $SampleFile)) {
    Write-Error "Sample file not found: $SampleFile"
    exit 1
}
$raw = Get-Content $SampleFile -Raw | ConvertFrom-Json
$items = @()
for ($i=1; $i -le $Count; $i++) {
    $clone = $raw[0] | ConvertTo-Json | ConvertFrom-Json
    # Uniqueize kartKodu and kartAdi
    $clone.kartKodu = "SYNC-TEST-" + ([string]::Format("{0:D4}", $i))
    $clone.kartAdi = $clone.kartAdi + " #" + $i
    # Ensure dates are present
    $clone.baslangicTarihi = (Get-Date).AddDays(-1).ToString('dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture)
    $items += $clone
}
$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outFile = Join-Path $OutDir "generated-products-$ts.json"
$items | ConvertTo-Json -Depth 5 | Out-File $outFile -Encoding utf8
Write-Output "Generated $Count products -> $outFile"
return $outFile
