param(
    [string]$KatanaApiBaseUrl = 'http://localhost:5000',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854,
    [int]$Limit = 0,
    [string]$ProductsFile = '',
    [string]$KatanaApiToken = '',
    [int]$PageSize = 100,
    [ValidateSet('skip','fail')] [string]$OnDuplicate = 'skip',
    [switch]$DryRun,
    [switch]$ForceSendDuplicates,
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

# Load products (from file or Katana API). Try multiple auth header variants and endpoint shapes
if ($ProductsFile -and (Test-Path $ProductsFile)) {
    $prods = Get-Content -Raw -Path $ProductsFile | ConvertFrom-Json
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
            Log ("WARNING: paginated fetch failed using header '$($auth.Name)': " + $_.Exception.Message)
            Log "Attempting fallback single fetch to ${KatanaApiBaseUrl}api/Luca/products"
            try {
                if ($headers.Count -gt 0) { $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -Headers $headers -ErrorAction Stop } else { $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -ErrorAction Stop }
                if ($pageData -is [System.Collections.IEnumerable]) { $prods += $pageData } else { $prods += ,$pageData }
                $fetched = $true
                break
            } catch {
                Log ("ERROR fetching products (fallback) using header '$($auth.Name)': " + $_.Exception.Message)
            }
        }
    }

    if (-not $fetched) {
        Log "ERROR: Unable to fetch products from Katana API with any auth variant. Supply correct -KatanaApiToken or check Katana API access."
        # leave $prods empty so preview will show 0 but do not exit here (caller may expect to proceed)
    }
}

if ($Limit -gt 0) { $prods = $prods | Select-Object -First $Limit }
Log "Products fetched: $($prods.Count)"

# Normalize SKU/Name fields and deduplicate products by SKU to avoid repeated columns-parsing duplicates
function Get-SkuKey([psobject]$p) {
    $sku = @($p.kartKodu, $p.kod, $p.sku, $p.SKU) -join ''
    $sku = ([string]$sku).Trim()
    if (-not $sku -or $sku -eq '') {
        # fallback to name-based key
        $name = @($p.kartAdi, $p.name, $p.adi, $p.Name) -join ''
        $name = ([string]$name).Trim()
        if (-not $name -or $name -eq '') { return "AUTO-" + [guid]::NewGuid().ToString().Substring(0,8) }
        return "NAME-" + ($name -replace '\s+','_').Substring(0,[Math]::Min(32,$name.Length))
    }
    return $sku
}

# Attach __skuKey property and then keep first occurrence per key (unless ForceSendDuplicates)
if (-not $ForceSendDuplicates.IsPresent) {
    $prods = $prods | ForEach-Object {
        $p = $_
        try { $k = Get-SkuKey $p } catch { $k = "AUTO-" + [guid]::NewGuid().ToString().Substring(0,8) }
        if ($p -is [pscustomobject]) { $p | Add-Member -NotePropertyName '__skuKey' -NotePropertyValue $k -Force } else { $p | Add-Member -NotePropertyName '__skuKey' -NotePropertyValue $k -Force }
        $p
    }
    $prods = $prods | Group-Object -Property '__skuKey' | ForEach-Object { $_.Group[0] }
    Log "Products after dedupe: $($prods.Count)"
} else {
    Log "ForceSendDuplicates set: skipping deduplication, will attempt to send all fetched items ($($prods.Count))."
}

# PreviewOnly: create forms
if ($PreviewOnly) {
    $logDir = Join-Path $PSScriptRoot '..\logs'
    $outDir = Join-Path $logDir ("preview-fixed-$(Get-Date -Format 'yyyyMMdd-HHmmss')")
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
    $preview = @()
    foreach ($p in $prods) {
        # determine sku/name with fallbacks
        $sku = @($p.kartKodu, $p.kod, $p.sku, $p.SKU) -join ''
        if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
        $name = @($p.kartAdi, $p.name, $p.adi, $p.Name) -join ''
        if (-not $name) { $name = $sku }
        $name = $name

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

        $form = "baslangicTarihi=$([string](UrlEncodeCp1254 $startDate))&kartKodu=$([string](UrlEncodeCp1254 $sku))&kartAdi=$([string](UrlEncodeCp1254 $name))&kartTuru=$kartTuru&olcumBirimiId=$olcumBirimiId&kartAlisKdvOran=$($p.kartAlisKdvOran)&kartSatisKdvOran=$($p.kartSatisKdvOran)&kartTipi=$kartTipi&kategoriAgacKod=$([string](UrlEncodeCp1254 $kategoriAgacKod))&satilabilirFlag=$satilabilirFlag&satinAlinabilirFlag=$satinAlinabilirFlag&lotNoFlag=$lotNoFlag&maliyetHesaplanacakFlag=$maliyetHesaplanacakFlag"

        $safeSku = ($sku -replace '[^a-zA-Z0-9_-]', '_')
        $outFile = Join-Path $outDir ("preview-form-$safeSku.txt")
        [System.IO.File]::WriteAllBytes($outFile, [System.Text.Encoding]::GetEncoding(1254).GetBytes($form))
        $preview += [pscustomobject]@{ SKU=$sku; Name=$name; FormFile=$outFile }
    }
    $preview | ConvertTo-Json -Depth 3 | Out-File (Join-Path $outDir 'preview.json') -Encoding utf8
    Log "Preview saved: $($preview.Count) forms -> $outDir\preview.json"
    exit 0
}

# For live run: login/change branch then post each product
$sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try { Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body (@{ orgCode=$OrgCode; userName=$Username; userPassword=$Password } | ConvertTo-Json) -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop; Log 'Koza login OK' } catch { Log "Koza login failed: $($_.Exception.Message)"; exit 1 }
try { Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body (@{ orgSirketSubeId = $BranchId } | ConvertTo-Json) -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop; Log 'Branch change OK' } catch { Log "Change branch failed: $($_.Exception.Message)"; exit 1 }

$success=@(); $failed=@(); $skipped=@()
foreach ($p in $prods) {
    $sku = @($p.kartKodu, $p.kod, $p.sku, $p.SKU) -join ''
    if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
    $name = @($p.kartAdi, $p.name, $p.adi, $p.Name) -join ''
    if (-not $name) { $name = $sku }
    $kategoriAgacKod = @($p.kategoriAgacKod) -join ''
    $startDate = @($p.baslangicTarihi) -join ''
    if ($startDate) { try { $d=[datetime]::Parse($startDate); $startDate = $d.ToString("dd'/'MM'/'yyyy") } catch {} } else { $startDate = (Get-Date).ToString("dd'/'MM'/'yyyy") }

    $kartTuru = if ($p.kartTuru) { [int]$p.kartTuru } else { 1 }
    $olcumBirimiId = if ($p.olcumBirimiId) { [int]$p.olcumBirimiId } else { 1 }
    $kartTipi = if ($p.kartTipi) { [int]$p.kartTipi } else { 4 }
    $toFlag = { param($v) if ($null -eq $v) { 1 } elseif ($v -is [bool]) { if ($v) {1} else {0} } else { try { [int]$v } catch { 1 } } }
    $satilabilirFlag = & $toFlag $p.satilabilirFlag
    $satinAlinabilirFlag = & $toFlag $p.satinAlinabilirFlag
    $lotNoFlag = & $toFlag $p.lotNoFlag
    $maliyetHesaplanacakFlag = & $toFlag $p.maliyetHesaplanacakFlag

    # check exists (skip lookup if forcing duplicates)
    $exists = $false
    if (-not $ForceSendDuplicates.IsPresent) {
        try {
            $check = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body (@{ stkSkart = @{ kodBas=$sku; kodBit=$sku; kodOp='between' } } | ConvertTo-Json) -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
            if ($check.stkSkart -and $check.stkSkart.Count -gt 0) { $exists = $true } elseif ($check.data -and $check.data.Count -gt 0) { $exists = $true }
        } catch {
            Log ("WARN: lookup failed for " + $sku + ": " + $_.Exception.Message)
        }
        if ($exists) { if ($OnDuplicate -eq 'skip') { Log "SKIP: $sku exists"; $skipped += @{ SKU=$sku; Name=$name }; continue } else { Log "DUP: $sku exists"; $failed += @{ SKU=$sku; Name=$name; Error='Duplicate' }; continue } }
    } else {
        Log "ForceSendDuplicates: skipping Koza existence lookup for $sku and proceeding to create."
    }

    $form = "baslangicTarihi=$([string](UrlEncodeCp1254 $startDate))&kartKodu=$([string](UrlEncodeCp1254 $sku))&kartAdi=$([string](UrlEncodeCp1254 $name))&kartTuru=$kartTuru&olcumBirimiId=$olcumBirimiId&kartAlisKdvOran=$($p.kartAlisKdvOran)&kartSatisKdvOran=$($p.kartSatisKdvOran)&kartTipi=$kartTipi&kategoriAgacKod=$([string](UrlEncodeCp1254 $kategoriAgacKod))&satilabilirFlag=$satilabilirFlag&satinAlinabilirFlag=$satinAlinabilirFlag&lotNoFlag=$lotNoFlag&maliyetHesaplanacakFlag=$maliyetHesaplanacakFlag"

    if ($DryRun) { Log "DRYRUN: form for $sku -> length $($form.Length)"; $success += @{ SKU=$sku; Name=$name; Status='DRYRUN' }; continue }

    try {
        $bytes = [System.Text.Encoding]::GetEncoding(1254).GetBytes($form)
        $resp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'EkleStkWsSkart.do') -Method Post -Body $bytes -ContentType 'application/x-www-form-urlencoded; charset=windows-1254' -WebSession $sess -UseBasicParsing -ErrorAction Stop
        # Try to read raw response bytes and decode with CP1254 to avoid mojibake when server returns CP1254-encoded content
        $body = $null
        try {
            if ($resp -and $resp.RawContentStream -ne $null) {
                $ms = New-Object System.IO.MemoryStream
                $resp.RawContentStream.CopyTo($ms)
                $rawBytes = $ms.ToArray()
                $body = [System.Text.Encoding]::GetEncoding(1254).GetString($rawBytes)
            } else {
                $body = $resp.Content
            }
        } catch {
            $body = $resp.Content
        }
        # Apply mojibake fixer to body and name so logs store readable Turkish chars
        $bodyFixed = FixMojibake $body
        $nameFixed = FixMojibake $name
        $parsed = $null
        try { $parsed = $bodyFixed | ConvertFrom-Json -ErrorAction Stop } catch {}
        if ($parsed -and $parsed.error) {
            Log "KOZA-ERROR: $sku -> $($parsed.message)"
            $failed += @{ SKU=$sku; Name=$nameFixed; Response=$bodyFixed }
        } else {
            Log "OK: $sku"
            $success += @{ SKU=$sku; Name=$nameFixed; Response=$bodyFixed }
        }
    } catch { Log "FAILED: $sku -> $($_.Exception.Message)"; $failed += @{ SKU=$sku; Name=$name; Error=$_.Exception.Message } }
}

Log "Done. Success: $($success.Count), Failed: $($failed.Count), Skipped: $($skipped.Count)"
$success | ConvertTo-Json -Depth 3 | Set-Content (Join-Path (Get-Location) 'scripts\logs\sync-fixed-success.json') -Encoding utf8
$failed | ConvertTo-Json -Depth 3 | Set-Content (Join-Path (Get-Location) 'scripts\logs\sync-fixed-failed.json') -Encoding utf8
