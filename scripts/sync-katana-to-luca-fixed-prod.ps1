param(
    [string]$KatanaApiBaseUrl = 'http://localhost:5000',
    [string]$KozaBaseUrl = 'https://akozas.luca.com.tr/Yetki/',
    [string]$OrgCode = '7374953',
    [string]$Username = 'Admin',
    [string]$Password = '2009Bfm',
    [int]$BranchId = 11746,
    [int]$Limit = 0,
    [string]$ProductsFile = '',
    [string]$KatanaApiToken = '',
    [int]$PageSize = 100,
    [ValidateSet('skip','fail')] [string]$OnDuplicate = 'skip',
    [switch]$DryRun,
    [switch]$ForceSendDuplicates,
    [switch]$PreferLocalDedupe,
    [switch]$Live,
    [switch]$PreviewOnly
)

if (-not $KozaBaseUrl.EndsWith('/')) { $KozaBaseUrl += '/' }
if (-not $KatanaApiBaseUrl.EndsWith('/')) { $KatanaApiBaseUrl += '/' }

function Log([string]$m) { Write-Output $m }

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

function FixMojibake([string]$s) {
    if ($null -eq $s) { return $s }
    $orig = [string]$s
    try {
        # If percent-encoded bytes are present, try unescaping first
        if ($orig -match '%[0-9A-Fa-f]{2}') {
            try { $un = [System.Uri]::UnescapeDataString($orig); if ($un -and $un -ne $orig) { $orig = $un } } catch {}
            try {
                $orig = [regex]::Replace($orig, '%([0-9A-Fa-f]{2})', { param($m) $b = [Convert]::ToByte($m.Groups[1].Value,16); return [System.Text.Encoding]::GetEncoding(1254).GetString([byte[]]@($b)) })
            } catch {}
        }

        $enc1254 = [System.Text.Encoding]::GetEncoding(1254)
        $enc1252 = [System.Text.Encoding]::GetEncoding(1252)
        $utf8 = [System.Text.Encoding]::UTF8

        # Heuristic: if string contains typical mojibake markers, try reinterpretation
        if ($orig -match '[\u0000-\u007F]*[ÃÄÅ].*') {
            try { $candidate = $utf8.GetString($enc1254.GetBytes($orig)); if ($candidate -and ($candidate -notmatch '�')) { return $candidate } } catch {}
            try { $candidate2 = $utf8.GetString($enc1252.GetBytes($orig)); if ($candidate2 -and ($candidate2 -notmatch '�')) { return $candidate2 } } catch {}
        }

        return $orig
    } catch {
        return $orig
    }
}

# Load products (from file or Katana API). Try multiple auth header variants and endpoint shapes
if ($ProductsFile -and (Test-Path $ProductsFile)) {
    $prods = Get-Content -Raw -Path $ProductsFile | ConvertFrom-Json
    if ($prods -and $prods.psobject.properties.match('products')) {
        $prods = $prods.products
    }
} else {
    $prods = @()
    # Build authentication header attempts. Some Katana installations expect X-Api-Key, others Authorization: Bearer.
    $authVariants = @()
    if ($KatanaApiToken -and $KatanaApiToken.Trim().Length -gt 0) {
        $authVariants += @{ Name = 'Authorization'; Value = "Bearer $KatanaApiToken" }
        $authVariants += @{ Name = 'X-Api-Key'; Value = $KatanaApiToken }
    } else {
        $authVariants += @{ Name = ''; Value = '' }
    }

    $fetched = $false
    foreach ($auth in $authVariants) {
        $page = 1
        $headers = @{}
        if ($auth.Name -ne '') { $headers[$auth.Name] = $auth.Value; Log "Trying Katana API auth header: $($auth.Name)" }
        Log "Fetching products from Katana API (paginated)"
        try {
            while ($true) {
                $altUrl = ("{0}products?page={1}`&pageSize={2}" -f $KatanaApiBaseUrl, $page, $PageSize)
                $primaryUrl = ("{0}api/Luca/products?page={1}`&pageSize={2}" -f $KatanaApiBaseUrl, $page, $PageSize)
                Log "Fetching page $page -> products: $altUrl"
                try {
                    if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri $altUrl -Method Get -Headers $headers -ErrorAction Stop } else { $pageData = Invoke-RestMethod -Uri $altUrl -Method Get -ErrorAction Stop }
                } catch {
                    Log "Products path failed for page $page, trying Luca-shaped path: $primaryUrl"
                    if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri $primaryUrl -Method Get -Headers $headers -ErrorAction Stop } else { $pageData = Invoke-RestMethod -Uri $primaryUrl -Method Get -ErrorAction Stop }
                }
                if (-not $pageData) { break }
                if ($pageData -and $pageData.PSObject.Properties.Match('data')) { $items = $pageData.data } elseif ($pageData -is [System.Collections.IEnumerable]) { $items = $pageData } else { $items = ,$pageData }
                $count = 0
                if ($items) { $count = ($items | Measure-Object).Count }
                if ($count -eq 0) { break }
                $prods += $items
                if ($count -lt $PageSize) { break }
                $page += 1
            }
            $fetched = $true
            break
        } catch {
            Log (("WARNING: paginated fetch failed using header '$($auth.Name)': " + $_.Exception.Message))
            Log "Attempting fallback single fetch to ${KatanaApiBaseUrl}api/Luca/products"
            try {
                if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -Headers $headers -ErrorAction Stop } else { $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -ErrorAction Stop }
                if ($pageData -is [System.Collections.IEnumerable]) { $prods += $pageData } else { $prods += ,$pageData }
                $fetched = $true
                break
            } catch {
                Log (("ERROR fetching products (fallback) using header '$($auth.Name)': " + $_.Exception.Message))
            }
        }
    }

    if (-not $fetched) {
        Log "ERROR: Unable to fetch products from Katana API with any auth variant. Supply correct -KatanaApiToken or check Katana API access."
    }
}

if ($Limit -gt 0) { $prods = $prods | Select-Object -First $Limit }
Log "Products fetched: $($prods.Count)"

# PreviewOnly: create forms (safe, no sends)
if ($PreviewOnly) {
    $logDir = Join-Path $PSScriptRoot '..\logs'
    $outDir = Join-Path $logDir ("preview-fixed-$(Get-Date -Format 'yyyyMMdd-HHmmss')")
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
    $preview = @()
    foreach ($p in $prods) {
        $sku = @($p.kartKodu, $p.kod, $p.sku, $p.SKU) -join ''
        if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
        $name = @($p.kartAdi, $p.name, $p.adi, $p.Name) -join ''
        if (-not $name) { $name = $sku }
        $name = FixMojibake $name

        $kategoriAgacKod = @($p.kategoriAgacKod) -join ''
        $startDate = @($p.baslangicTarihi) -join ''
        if ($startDate) { try { $d = [datetime]::Parse($startDate); $startDate = $d.ToString("dd'/'MM'/'yyyy") } catch {} } else { $startDate = (Get-Date).ToString("dd'/'MM'/'yyyy") }

        $kartTuru = if ($p.kartTuru) { [int]$p.kartTuru } else { 1 }
        $olcumBirimiId = if ($p.olcumBirimiId) { [int]$p.olcumBirimiId } else { 1 }
        $kartTipi = if ($p.kartTipi) { [int]$p.kartTipi } else { 4 }
        $toFlag = { param($v) if ($null -eq $v) { 1 } elseif ($v -is [bool]) { if ($v) {1} else {0} } else { try { [int]$v } catch { 1 } } }
        $satilabilirFlag = & $toFlag $p.satilabilirFlag
        $satinAlinabilirFlag = & $toFlag $p.satinAlinabilirFlag
        $lotNoFlag = & $toFlag $p.lotNoFlag
        $maliyetHesaplanacakFlag = & $toFlag $p.maliyetHesaplanacakFlag

        $form = ("baslangicTarihi={0}&kartKodu={1}&kartAdi={2}&kartTuru={3}&olcumBirimiId={4}&kartAlisKdvOran={5}&kartSatisKdvOran={6}&kartTipi={7}&kategoriAgacKod={8}&satilabilirFlag={9}&satinAlinabilirFlag={10}&lotNoFlag={11}&maliyetHesaplanacakFlag={12}" -f
            ([string](UrlEncodeCp1254 $startDate)), ([string](UrlEncodeCp1254 $sku)), ([string](UrlEncodeCp1254 $name)), $kartTuru, $olcumBirimiId, $p.kartAlisKdvOran, $p.kartSatisKdvOran, $kartTipi, ([string](UrlEncodeCp1254 $kategoriAgacKod)), $satilabilirFlag, $satinAlinabilirFlag, $lotNoFlag, $maliyetHesaplanacakFlag)

        $safeSku = ($sku -replace '[^a-zA-Z0-9_-]', '_')
        $outFile = Join-Path $outDir ("preview-form-$safeSku.txt")
        [System.IO.File]::WriteAllBytes($outFile, [System.Text.Encoding]::GetEncoding(1254).GetBytes($form))
        $preview += [pscustomobject]@{ SKU=$sku; Name=$name; FormFile=$outFile }
    }
    $preview | ConvertTo-Json -Depth 3 | Out-File (Join-Path $outDir 'preview.json') -Encoding utf8
    Log "Preview saved: $($preview.Count) forms -> $outDir\preview.json"
    exit 0
}

Log "No preview requested and prod script now mirrors non-prod preview flow. Exiting."
exit 0
