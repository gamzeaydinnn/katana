param(
    [int]$BatchSize = 50,
    [string]$KatanaApiBaseUrl = 'https://api.katanamrp.com/v1/',
    [string]$KatanaApiToken = 'use-appsettings',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746,
    [int]$DelaySeconds = 2
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$workDir = Join-Path (Get-Location) "scripts\logs\full-sync-$ts"
New-Item -Path $workDir -ItemType Directory -Force | Out-Null
$allFile = Join-Path $workDir 'all-products.json'

Write-Output "Fetching all products from Katana API to $allFile"
# Reuse sync script's ability to read appsettings by invoking in PreviewOnly and capture products file
# But sync script doesn't write raw products; we'll fetch directly here.

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
$pagesize = 100
while ($true) {
    $url = "$KatanaApiBaseUrl/products?page=$page&pageSize=$pagesize"
    try {
        if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri $url -Method Get -Headers $headers -ErrorAction Stop }
        else { $pageData = Invoke-RestMethod -Uri $url -Method Get -ErrorAction Stop }
    } catch {
        Write-Warning "Page fetch failed: $($_.Exception.Message)"; break
    }
    if ($null -eq $pageData) { break }
    $items = $null
    if ($pageData -and $pageData.PSObject.Properties.Match('data')) { $items = $pageData.data }
    elseif ($pageData -is [System.Collections.IEnumerable]) { $items = $pageData }
    else { $items = ,$pageData }
    $count = 0
    if ($items) { $count = ($items | Measure-Object).Count }
    if ($count -eq 0) { break }
    $all += $items
    if ($count -lt $pagesize) { break }
    $page += 1
}

$all | ConvertTo-Json -Depth 10 | Out-File $allFile -Encoding utf8
Write-Output "Fetched $($all.Count) products. Splitting into batches of $BatchSize"

$index = 0
$batchNo = 0
while ($index -lt $all.Count) {
    $batchNo++
    $chunk = $all[$index..([Math]::Min($index+$BatchSize-1, $all.Count-1))]
    $chunkFile = Join-Path $workDir ("products-chunk-$batchNo.json")
    $chunk | ConvertTo-Json -Depth 10 | Out-File $chunkFile -Encoding utf8
    Write-Output "Starting batch $batchNo (items: $($chunk.Count)) -> $chunkFile"
    & .\scripts\sync-katana-to-luca.ps1 -ProductsFile $chunkFile -KozaBaseUrl $KozaBaseUrl -OrgCode $OrgCode -Username $Username -Password $Password -BranchId $BranchId -OnDuplicate skip
    Write-Output "Batch $batchNo complete. Sleeping $DelaySeconds seconds"
    Start-Sleep -Seconds $DelaySeconds
    $index += $chunk.Count
}

Write-Output "All batches complete. Logs are in $workDir"
