param(
    [string]$ProductsFile = '..\logs\generated-products-20251126-131927.json',
    [int]$Limit = 50
)

$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$logDir = Join-Path (Join-Path $PSScriptRoot '..\logs') "preview-from-generated-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -Path $logDir -ItemType Directory -Force | Out-Null

function UrlEncodeCp1254([string]$s) {
    if ($null -eq $s) { return [string]'' }
    $enc = [System.Text.Encoding]::GetEncoding(1254)
    $bytes = $enc.GetBytes([string]$s)
    $sb = New-Object System.Text.StringBuilder
    foreach ($b in $bytes) {
        if ($b -eq 0x20) { [void]$sb.Append('+'); continue }
        if ((($b -ge 0x30) -and ($b -le 0x39)) -or (($b -ge 0x41) -and ($b -le 0x5A)) -or (($b -ge 0x61) -and ($b -le 0x7A)) -or ($b -in 45,46,95,126)) {
            [void]$sb.Append([char]$b)
        } else {
            [void]$sb.Append('%' + $b.ToString('X2'))
        }
    }
    return ([string]$sb.ToString())
}

# Load products
$fullPath = Join-Path (Join-Path $PSScriptRoot '..\logs') (Split-Path $ProductsFile -Leaf)
if (-not (Test-Path $fullPath)) { Write-Error "Products file not found: $fullPath"; exit 1 }
$prods = Get-Content -Raw -Path $fullPath | ConvertFrom-Json
if ($Limit -gt 0) { $prods = $prods | Select-Object -First $Limit }

$preview = @()
foreach ($p in $prods) {
    # Ensure scalar strings (handle arrays like ["Test Ürün"]) to avoid System.Object[] output
    $sku = (@($p.kartKodu) -join '')
    $name = (@($p.kartAdi) -join '')
    if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
    if (-not $name) { $name = $sku }

    $startDate = (@($p.baslangicTarihi) -join '')
    if ($startDate) {
        try { $d = [datetime]::Parse($startDate); $startDate = $d.ToString('dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture) } catch { }
    } else { $startDate = (Get-Date).ToString('dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture) }

    $kartTuru = if ($p.kartTuru) { [int]$p.kartTuru } else { 1 }
    $olcumBirimiId = if ($p.olcumBirimiId) { [int]$p.olcumBirimiId } else { 1 }
    $kartTipi = if ($p.kartTipi) { [int]$p.kartTipi } else { 4 }
    $kategoriAgacKod = (@($p.kategoriAgacKod) -join '')

    $toFlag = {
        param($v)
        if ($null -eq $v) { return 1 }
        if ($v -is [bool]) { if ($v) { return 1 } else { return 0 } }
        try { return [int]$v } catch { return 1 }
    }
    $satilabilirFlag = & $toFlag $p.satilabilirFlag
    $satinAlinabilirFlag = & $toFlag $p.satinAlinabilirFlag
    $lotNoFlag = & $toFlag $p.lotNoFlag
    $maliyetHesaplanacakFlag = & $toFlag $p.maliyetHesaplanacakFlag

    $form = (
        'baslangicTarihi=' + ([string](UrlEncodeCp1254 $startDate)) +
        '&kartKodu=' + ([string](UrlEncodeCp1254 $sku)) +
        '&kartAdi=' + ([string](UrlEncodeCp1254 $name)) +
        '&kartTuru=' + $kartTuru.ToString() +
        '&olcumBirimiId=' + $olcumBirimiId.ToString() +
        '&kartAlisKdvOran=' + ([string]($p.kartAlisKdvOran)) +
        '&kartSatisKdvOran=' + ([string]($p.kartSatisKdvOran)) +
        '&kartTipi=' + $kartTipi.ToString() +
        '&kategoriAgacKod=' + ([string](UrlEncodeCp1254 $kategoriAgacKod)) +
        '&satilabilirFlag=' + $satilabilirFlag.ToString() +
        '&satinAlinabilirFlag=' + $satinAlinabilirFlag.ToString() +
        '&lotNoFlag=' + $lotNoFlag.ToString() +
        '&maliyetHesaplanacakFlag=' + $maliyetHesaplanacakFlag.ToString()
    )

    $safeSku = ($sku -replace '[^a-zA-Z0-9_-]', '_')
    $formFile = Join-Path $logDir ("preview-form-$safeSku.txt")
    $formBytes = [System.Text.Encoding]::GetEncoding(1254).GetBytes($form)
    [System.IO.File]::WriteAllBytes($formFile, $formBytes)

    $preview += [pscustomobject]@{ SKU=$sku; Name=$name; FormFile=$formFile }
}

$preview | ConvertTo-Json -Depth 3 | Out-File (Join-Path $logDir 'preview.json') -Encoding utf8
Write-Host "Preview saved: $($preview.Count) forms -> $logDir\preview.json"
