param(
    [int]$TargetTotal = 50,
    [string]$KatanaApiBaseUrl = 'https://api.katanamrp.com/v1/',
    [string]$KatanaApiToken = 'use-appsettings',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

$logRoot = Join-Path (Get-Location) 'scripts\logs'
Write-Output "Collecting existing success.json files under $logRoot"
$successFiles = Get-ChildItem -Path $logRoot -Recurse -Filter success.json -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
$sent = @()
foreach ($f in $successFiles) {
    try { $j = Get-Content $f -Raw | ConvertFrom-Json -ErrorAction Stop; if ($j -is [System.Array]) { $sent += $j } else { $sent += ,$j } } catch { }
}
$uniqueSent = @()
if ($sent.Count -gt 0) { $uniqueSent = $sent | Select-Object -Property SKU,Name -Unique }
$sentCount = $uniqueSent.Count
Write-Output "Already sent (unique) count: $sentCount"

if ($sentCount -ge $TargetTotal) {
    Write-Output "Target of $TargetTotal already reached. No action needed."; exit 0
}
$needed = $TargetTotal - $sentCount
Write-Output "Need to send $needed more products. Fetching Katana products..."

# Read appsettings if requested
$cfgPath = Join-Path (Get-Location) 'src\Katana.API\appsettings.Development.json'
$token = $null
if ($KatanaApiToken -and ($KatanaApiToken -ieq 'use-appsettings' -or $KatanaApiToken -ieq '<use-appsettings>')) {
    if (Test-Path $cfgPath) {
        try { $cfg = Get-Content -Raw $cfgPath | ConvertFrom-Json -ErrorAction Stop; $token = $cfg.KatanaApi.ApiKey } catch { }
    }
}
if (-not $token -and $KatanaApiToken -and $KatanaApiToken -ne 'use-appsettings') { $token = $KatanaApiToken }

$headers = @{}
if ($token) { $headers['Authorization'] = "Bearer $token" }

$all = @()
$page = 1
$pageSize = 100
while ($true) {
    $url = "$KatanaApiBaseUrl/products?page=$page&pageSize=$pageSize"
    try {
        if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri $url -Method Get -Headers $headers -ErrorAction Stop } else { $pageData = Invoke-RestMethod -Uri $url -Method Get -ErrorAction Stop }
    } catch {
        Write-Warning ("Failed to fetch page {0}: {1}" -f $page, $_.Exception.Message)
        break
    }
    if (-not $pageData) { break }
    $items = $null
    if ($pageData -and $pageData.PSObject.Properties.Match('data')) { $items = $pageData.data }
    elseif ($pageData -is [System.Collections.IEnumerable]) { $items = $pageData }
    else { $items = ,$pageData }
    $count = 0; if ($items) { $count = ($items | Measure-Object).Count }
    if ($count -eq 0) { break }
    $all += $items
    if ($count -lt $pageSize) { break }
    $page += 1
}
Write-Output "Fetched total products from Katana: $($all.Count)"

# Build a simple identifier for Katana products: prefer variant.sku, else product.name
$sentNames = @($uniqueSent | ForEach-Object { $_.Name })
$sentSKUs = @($uniqueSent | ForEach-Object { $_.SKU })

$candidates = @()
foreach ($p in $all) {
    $prodName = $null
    if ($p.PSObject.Properties.Match('name')) { $prodName = $p.name }
    if (-not $prodName -and $p.PSObject.Properties.Match('kartAdi')) { $prodName = $p.kartAdi }
    $variantSku = $null
    if ($p.PSObject.Properties.Match('variants') -and $p.variants -and $p.variants.Count -gt 0) {
        $variantSku = $p.variants[0].sku
    }
    # Consider candidate if its name or variant SKU not already in sent lists
    $already = $false
    if ($variantSku -and ($sentSKUs -contains $variantSku)) { $already = $true }
    if ($prodName -and ($sentNames -contains $prodName)) { $already = $true }
    if (-not $already) { $candidates += $p }
}
Write-Output "Candidates not yet sent: $($candidates.Count)"
if ($candidates.Count -eq 0) { Write-Output "No new candidates found."; exit 0 }

$toSend = $candidates | Select-Object -First $needed
$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path (Get-Location) "scripts\logs\send-missing-$ts"
New-Item -Path $outDir -ItemType Directory -Force | Out-Null
$outFile = Join-Path $outDir 'products-to-send.json'
$toSend | ConvertTo-Json -Depth 10 | Out-File $outFile -Encoding utf8
Write-Output "Wrote $($toSend.Count) products to $outFile. Now invoking sync script to send them."

# Call sync script for this ProductsFile
& .\scripts\sync-katana-to-luca.ps1 -ProductsFile $outFile -KozaBaseUrl $KozaBaseUrl -OrgCode $OrgCode -Username $Username -Password $Password -BranchId $BranchId -OnDuplicate skip

Write-Output "Done. Check logs in $outDir and scripts/logs for results."