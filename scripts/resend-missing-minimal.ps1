param(
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$logDir = Join-Path (Get-Location) "scripts\logs\resend-minimal-$ts"
New-Item -Path $logDir -ItemType Directory -Force | Out-Null

Write-Output "Scanning for created.json files under scripts\logs to find missing skartId entries..."
$createdFiles = Get-ChildItem -Path (Join-Path (Get-Location) 'scripts\logs') -Recurse -Filter created.json -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
if (-not $createdFiles -or $createdFiles.Count -eq 0) {
    Write-Error "No created.json files found under scripts/logs. Run parse-create-responses.ps1 first."; exit 1
}

$all = @()
foreach ($f in $createdFiles) {
    try { $j = Get-Content $f -Raw | ConvertFrom-Json -ErrorAction Stop; if ($j -is [System.Array]) { $all += $j } else { $all += ,$j } } catch { Write-Warning "Failed reading $f" }
}

$missing = $all | Where-Object { -not $_.skartId -or $_.skartId -eq $null }
if ($missing.Count -eq 0) { Write-Output "No missing entries found; nothing to resend."; exit 0 }

Write-Output "Found $($missing.Count) entries without skartId. Building minimal product list..."

$today = (Get-Date).ToString('dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture)
$products = @()
foreach ($m in $missing) {
    $sku = $m.sku
    if (-not $sku) { continue }
    $prod = [pscustomobject]@{
        kod = $sku
        name = $sku
        kartKodu = $sku
        kartAdi = $sku
        baslangicTarihi = $today
        satilabilirFlag = 1
        satinAlinabilirFlag = 1
        lotNoFlag = 0
        maliyetHesaplanacakFlag = 1
        kartTuru = 1
        olcumBirimiId = 1
        kartTipi = 4
        kartAlisKdvOran = 0.0
        kartSatisKdvOran = 0.0
    }
    $products += $prod
}

$outFile = Join-Path $logDir 'products-minimal.json'
$products | ConvertTo-Json -Depth 5 | Out-File $outFile -Encoding utf8
Write-Output "Wrote minimal products file: $outFile (count: $($products.Count))"

Write-Output "Calling sync-katana-to-luca.ps1 with the minimal products file (OnDuplicate=skip). Logs will be under scripts/logs/sync-katana-to-luca-<ts>."

# Call sync script
& .\scripts\sync-katana-to-luca.ps1 -ProductsFile $outFile -KozaBaseUrl $KozaBaseUrl -OrgCode $OrgCode -Username $Username -Password $Password -BranchId $BranchId -OnDuplicate skip

Write-Output "Resend-complete. Check $logDir and scripts/logs for sync results." 
