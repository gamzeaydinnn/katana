param(
    [string]$KatanaApiBaseUrl = 'http://localhost:5000',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854,
    [int]$Limit = 0,
    [string]$ExcelFile = '',
    [string]$ProductsFile = '',
    [string]$KatanaApiToken = '',
    [int]$PageSize = 100,
    [ValidateSet('skip','fail')] [string]$OnDuplicate = 'skip',
    [switch]$DryRun,
    [switch]$PreviewOnly
)

if (-not $KozaBaseUrl.EndsWith('/')) { $KozaBaseUrl += '/' }
if (-not $KatanaApiBaseUrl.EndsWith('/')) { $KatanaApiBaseUrl += '/' }

# Convenience: allow passing 'use-appsettings' to read KatanaApi.ApiKey from appsettings.Development.json
function Log([string]$m) {
    if ($null -ne $logFile -and $logFile -ne '') {
        try { $m | Tee-Object -FilePath $logFile -Append -ErrorAction SilentlyContinue } catch { }
    }
    Write-Output $m
}

# URL-encode using codepage 1254 bytes (windows-1254) so percent-escapes represent CP1254 bytes
function UrlEncodeCp1254([string]$s) {
    if ($null -eq $s) { return [string]'' }
    $enc = [System.Text.Encoding]::GetEncoding(1254)
    $bytes = $enc.GetBytes([string]$s)
    $sb = New-Object System.Text.StringBuilder
    foreach ($b in $bytes) {
        # space -> + for form encoding
        if ($b -eq 0x20) { [void]$sb.Append('+') ; continue }
        # safe characters: alnum and - . _ ~
        if ((($b -ge 0x30) -and ($b -le 0x39)) -or (($b -ge 0x41) -and ($b -le 0x5A)) -or (($b -ge 0x61) -and ($b -le 0x7A)) -or ($b -in 45,46,95,126)) {
            [void]$sb.Append([char]$b)
        } else {
            [void]$sb.Append('%' + $b.ToString('X2'))
        }
    }
    return ([string]$sb.ToString())
}

# Try to fix common UTF8<->CP125x mojibake by reinterpreting bytes
function FixMojibake([string]$s) {
    if ($null -eq $s) { return $s }
    # Quick heuristic: look for common UTF8->CP125x mojibake indicators using Unicode escapes
    if (-not [regex]::IsMatch($s, '\u00C3|\u00C4|\u00C5|\u00C2')) { return $s }
    try {
        $orig = [string]$s
        $enc1254 = [System.Text.Encoding]::GetEncoding(1254)
        $enc1252 = [System.Text.Encoding]::GetEncoding(1252)
        $utf8 = [System.Text.Encoding]::UTF8
        $try1254 = $utf8.GetString($enc1254.GetBytes($orig))
        $try1252 = $utf8.GetString($enc1252.GetBytes($orig))
        $badChar = [char]0xFFFD
        $countBad = {
            param($str)
            (@($str.ToCharArray() | Where-Object { $_ -eq $badChar -or [regex]::IsMatch($_, '\u00C3') })).Count
        }
        $scoreOrig = & $countBad $orig
        $score1254 = & $countBad $try1254
        $score1252 = & $countBad $try1252
        $best = $orig; $bestScore = $scoreOrig
        if ($score1254 -lt $bestScore) { $best = $try1254; $bestScore = $score1254 }
        if ($score1252 -lt $bestScore) { $best = $try1252; $bestScore = $score1252 }
        return $best
    } catch {
        return $s
    }
}

if ((($KatanaApiToken -and ($KatanaApiToken -eq '<use-appsettings>' -or $KatanaApiToken -ieq 'use-appsettings')) -or ($KatanaApiBaseUrl -and ($KatanaApiBaseUrl -eq '<use-appsettings>' -or $KatanaApiBaseUrl -ieq 'use-appsettings')))) {
    $cfgPath = Join-Path (Get-Location) 'src\Katana.API\appsettings.Development.json'
    if (Test-Path $cfgPath) {
        try {
            $cfg = Get-Content -Raw $cfgPath | ConvertFrom-Json -ErrorAction Stop
            if ($cfg.KatanaApi) {
                if ($cfg.KatanaApi.ApiKey -and ($KatanaApiToken -eq '<use-appsettings>' -or $KatanaApiToken -ieq 'use-appsettings' -or -not $KatanaApiToken)) {
                    $KatanaApiToken = $cfg.KatanaApi.ApiKey
                    Log "Using KatanaApi.ApiKey from appsettings.Development.json"
                }
                if ($cfg.KatanaApi.BaseUrl -and ($KatanaApiBaseUrl -eq '<use-appsettings>' -or $KatanaApiBaseUrl -ieq 'use-appsettings' -or -not $KatanaApiBaseUrl -or $KatanaApiBaseUrl -eq 'http://localhost:5000')) {
                    $KatanaApiBaseUrl = $cfg.KatanaApi.BaseUrl
                    if (-not $KatanaApiBaseUrl.EndsWith('/')) { $KatanaApiBaseUrl += '/' }
                    Log "Using KatanaApi.BaseUrl from appsettings.Development.json: $KatanaApiBaseUrl"
                }
            } else {
                Log "WARNING: appsettings.Development.json found but KatanaApi section missing"
            }
        } catch {
            Log "WARNING: failed to read appsettings.Development.json: $($_.Exception.Message)"
        }
    } else {
        Log "WARNING: appsettings.Development.json not found at $cfgPath"
    }
}

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$logDir = "./scripts/logs/sync-katana-to-luca-$ts"
New-Item -Path $logDir -ItemType Directory -Force | Out-Null
$logFile = Join-Path $logDir "run.log"

function Log([string]$m) { $m | Tee-Object -FilePath $logFile -Append; Write-Output $m }

Log "START sync-katana-to-luca: $ts"
Log "Katana API: $KatanaApiBaseUrl"
Log "Koza Base: $KozaBaseUrl"

# Excel import kısa yol
if ($ExcelFile -and (Test-Path $ExcelFile)) {
    Log "Excel kaynağı tespit edildi: $ExcelFile"

    if (!(Get-Module -ListAvailable -Name ImportExcel)) {
        Log "ImportExcel modülü yükleniyor..."
        try { Install-Module -Name ImportExcel -Force -Scope CurrentUser -AllowClobber } catch { Log "ERROR: ImportExcel kurulamadı: $($_.Exception.Message)"; exit 1 }
    }
    Import-Module ImportExcel -ErrorAction Stop

    $products = Import-Excel -Path $ExcelFile
    if (-not $products -or $products.Count -eq 0) { Log "ERROR: Excel boş veya okunamadı."; exit 1 }

    $required = @('SKU','Name','VatRate','Unit','StartDate')
    $cols = $products[0].PSObject.Properties.Name
    $missing = $required | Where-Object { $_ -notin $cols }
    if ($missing.Count -gt 0) { Log ("ERROR: Eksik kolonlar: " + ($missing -join ', ')); exit 1 }

    # Log dosyaları
    $successLog = @()
    $errorLog = @()

    foreach ($p in $products) {
        $body = @{
            SKU = $p.SKU
            Name = $p.Name
            VatRate = $p.VatRate
            Unit = $p.Unit
            StartDate = $p.StartDate
            IsActive = $p.IsActive
            IsService = $p.IsService
            TrackStock = $p.TrackStock
            Description = $p.Description
            Barcode = $p.Barcode
            CategoryCode = $p.CategoryCode
            PurchaseVatRate = $p.PurchaseVatRate
            SalesVatRate = $p.SalesVatRate
        }

        try {
            $resp = Invoke-RestMethod -Uri ("{0}api/luca/send-product-from-excel" -f $KatanaApiBaseUrl) -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 10) -ErrorAction Stop
            if ($resp.success) {
                Log "✓ $($p.SKU) gönderildi"
                $successLog += @{ SKU=$p.SKU; Name=$p.Name; timestamp=(Get-Date).ToString("s") }
            } else {
                Log "✗ $($p.SKU) gönderilemedi: $($resp.message)"
                $errorLog += @{ SKU=$p.SKU; Name=$p.Name; errorMessage=$resp.message; timestamp=(Get-Date).ToString("s") }
            }
        } catch {
            Log "✗ $($p.SKU) hata: $($_.Exception.Message)"
            $errorLog += @{ SKU=$p.SKU; Name=$p.Name; errorMessage=$_.Exception.Message; timestamp=(Get-Date).ToString("s") }
        }
    }

    if ($successLog.Count -gt 0) { $successLog | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $logDir "success.json") -Encoding utf8 }
    if ($errorLog.Count -gt 0) { $errorLog | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $logDir "error.json") -Encoding utf8 }
    $summary = @{
        totalProducts = $products.Count
        successful = $successLog.Count
        failed = $errorLog.Count
        source = "Excel: $ExcelFile"
        timestamp = (Get-Date).ToString("s")
    }
    $summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $logDir "summary.json") -Encoding utf8
    Log "Excel import tamamlandı. Başarılı: $($successLog.Count) / $($products.Count)"
    exit 0
}

# 1) Load products: from file (if provided) or from Katana API
if ($ProductsFile -and (Test-Path $ProductsFile)) {
    Log "Loading products from file: $ProductsFile"
    try {
        $raw = Get-Content $ProductsFile -Raw -ErrorAction Stop
        $prods = $raw | ConvertFrom-Json -ErrorAction Stop
    } catch {
        Log "ERROR reading products file: $($_.Exception.Message)"
        exit 1
    }
} else {
    # Fetch from Katana API (Luca-shaped endpoint) with pagination and optional token
    $prods = @()
    # Build authentication header attempts. Some Katana installations expect X-Api-Key, others Authorization: Bearer.
    $authVariants = @()
    if ($KatanaApiToken -and $KatanaApiToken.Trim().Length -gt 0) {
        # Prefer Authorization Bearer which this Katana deployment accepts for /products
        $authVariants += @{ Name = 'Authorization'; Value = "Bearer $KatanaApiToken" }
        $authVariants += @{ Name = 'X-Api-Key'; Value = $KatanaApiToken }
    } else {
        # no auth header variant - plain requests
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
                # Try the generic Katana 'products' path first (this deployment responds there), then try the Luca-shaped path
                $altUrl = ("{0}products?page={1}`&pageSize={2}" -f $KatanaApiBaseUrl, $page, $PageSize)
                $primaryUrl = ("{0}api/Luca/products?page={1}`&pageSize={2}" -f $KatanaApiBaseUrl, $page, $PageSize)
                Log "Fetching page $page -> products: $altUrl"
                try {
                    if ($headers.Count -gt 0) {
                        $pageData = Invoke-RestMethod -Uri $altUrl -Method Get -Headers $headers -ErrorAction Stop
                    } else {
                        $pageData = Invoke-RestMethod -Uri $altUrl -Method Get -ErrorAction Stop
                    }
                } catch {
                    Log "Products path failed for page $page, trying Luca-shaped path: $primaryUrl"
                    if ($headers.Count -gt 0) {
                        $pageData = Invoke-RestMethod -Uri $primaryUrl -Method Get -Headers $headers -ErrorAction Stop
                    } else {
                        $pageData = Invoke-RestMethod -Uri $primaryUrl -Method Get -ErrorAction Stop
                    }
                }
                if (-not $pageData) { break }
                # If API returns a wrapper object with a 'data' array (common), unwrap it
                $items = $null
                if ($pageData -and $pageData.PSObject.Properties.Match('data')) {
                    $items = $pageData.data
                } elseif ($pageData -is [System.Collections.IEnumerable]) {
                    $items = $pageData
                } else {
                    $items = ,$pageData
                }
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
                if ($headers.Count -gt 0) {
                    $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -Headers $headers -ErrorAction Stop
                } else {
                    $pageData = Invoke-RestMethod -Uri ("${KatanaApiBaseUrl}api/Luca/products") -Method Get -ErrorAction Stop
                }
                if ($pageData -is [System.Collections.IEnumerable]) { $prods += $pageData } else { $prods += ,$pageData }
                $fetched = $true
                break
            } catch {
                Log ("ERROR fetching products (fallback) using header '$($auth.Name)': " + $_.Exception.Message)
                # continue to next auth variant
            }
        }
    }

    if (-not $fetched) {
        Log "ERROR: Unable to fetch products from Katana API with any auth variant. If this is a local Katana API running with JWT, check SymmetricSecurityKey config or supply -KatanaApiToken."
        exit 1
    }
}

if ($Limit -gt 0) { $prods = $prods | Select-Object -First $Limit }
Log "Products fetched: $($prods.Count)"

# If PreviewOnly, build forms and save them (no Koza login/post)
if ($PreviewOnly) {
    Log "PreviewOnly mode: building forms for $($prods.Count) products and saving to $logDir"
    $previewSuccess = @()
    foreach ($p in $prods) {
        $sku = $null
        if ($p -and $p.PSObject.Properties.Match('kartKodu')) { $sku = $p.kartKodu }
        if (-not $sku -and $p -and $p.PSObject.Properties.Match('kod')) { $sku = $p.kod }
        if (-not $sku -and $p -and $p.PSObject.Properties.Match('sku')) { $sku = $p.sku }
        if (-not $sku -and $p -and $p.PSObject.Properties.Match('SKU')) { $sku = $p.SKU }

        $name = $null
        if ($p -and $p.PSObject.Properties.Match('kartAdi')) { $name = $p.kartAdi }
        if (-not $name -and $p -and $p.PSObject.Properties.Match('name')) { $name = $p.name }
        if (-not $name -and $p -and $p.PSObject.Properties.Match('adi')) { $name = $p.adi }
        if (-not $name -and $p -and $p.PSObject.Properties.Match('Name')) { $name = $p.Name }
        if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
        if (-not $name) { $name = $sku }
        # Normalize possible arrays to scalars to avoid System.Object[] when converting to string
        $sku = (@($sku) -join '')
        $name = (@($name) -join '')
        # attempt to fix mojibake in product names (common when sources mix encodings)
        $name = FixMojibake $name

        # Map fields with safe fallbacks (same logic as live path)
        $kartTuru = $p.kartTuru -as [int]
        if (-not $kartTuru) { $kartTuru = 1 }
        $olcumBirimiId = $p.olcumBirimiId -as [int]
        if (-not $olcumBirimiId) { $olcumBirimiId = 1 }
        $kartTipi = $p.kartTipi -as [int]
        if (-not $kartTipi) { $kartTipi = 4 }
        $kategoriAgacKod = ''
        if ($p -and $p.PSObject.Properties.Match('kategoriAgacKod')) { $kategoriAgacKod = (@($p.kategoriAgacKod) -join '') }

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

        $startDate = (@($p.baslangicTarihi) -join '')
        if ($startDate) {
            try { $d = [datetime]::Parse($startDate); $startDate = $d.ToString("dd'/'MM'/'yyyy") } catch { $startDate = $startDate }
        } else { $startDate = (Get-Date).ToString("dd'/'MM'/'yyyy") }

        $kartAlisKdvOran = if ($p.kartAlisKdvOran -ne $null) { $p.kartAlisKdvOran } else { 0.0 }
        $kartSatisKdvOran = if ($p.kartSatisKdvOran -ne $null) { $p.kartSatisKdvOran } else { 0.0 }

        $form = ("baslangicTarihi={0}`&kartKodu={1}`&kartAdi={2}`&kartTuru={3}`&olcumBirimiId={4}`&kartAlisKdvOran={5}`&kartSatisKdvOran={6}`&kartTipi={7}`&kategoriAgacKod={8}`&satilabilirFlag={9}`&satinAlinabilirFlag={10}`&lotNoFlag={11}`&maliyetHesaplanacakFlag={12}" -f
            ([string](UrlEncodeCp1254 $startDate)),
            ([string](UrlEncodeCp1254 $sku)),
            ([string](UrlEncodeCp1254 $name)),
            $kartTuru,
            $olcumBirimiId,
            $kartAlisKdvOran,
            $kartSatisKdvOran,
            $kartTipi,
            ([string](UrlEncodeCp1254 $kategoriAgacKod)),
            $satilabilirFlag,
            $satinAlinabilirFlag,
            $lotNoFlag,
            $maliyetHesaplanacakFlag)
        )

        $safeSku = ($sku -replace '[^a-zA-Z0-9_-]', '_')
        $formFile = Join-Path $logDir ("preview-form-$safeSku.txt")
        $formBytes = [System.Text.Encoding]::GetEncoding(1254).GetBytes($form)
        [System.IO.File]::WriteAllBytes($formFile, $formBytes)
        $previewSuccess += [pscustomobject]@{ SKU=$sku; Name=$name; FormFile=$formFile }
    }
    $previewSuccess | ConvertTo-Json -Depth 3 | Out-File (Join-Path $logDir 'preview.json') -Encoding utf8
    Log "Preview saved: $($previewSuccess.Count) forms -> $logDir/preview.json"
    exit 0
}

# 2) Login to Koza and open a session
$sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginJson = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Compress
try {
    $login = Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body $loginJson -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Log "Koza login status: $($login.StatusCode)"
} catch {
    Log "Koza login failed: $($_.Exception.Message)"
    exit 1
}

# 3) Change branch
$branchJson = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress
try {
    $branchResp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $branchJson -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Log "Change branch status: $($branchResp.StatusCode)"
} catch {
    Log "Change branch failed: $($_.Exception.Message)"
    exit 1
}

# 4) For each product, map fields and post to EkleStkWsSkart.do
$success = @()
$failed = @()
$skipped = @()
foreach ($p in $prods) {
    $sku = $null
    if ($p -and $p.PSObject.Properties.Match('kartKodu')) { $sku = $p.kartKodu }
    if (-not $sku -and $p -and $p.PSObject.Properties.Match('kod')) { $sku = $p.kod }
    if (-not $sku -and $p -and $p.PSObject.Properties.Match('sku')) { $sku = $p.sku }
    if (-not $sku -and $p -and $p.PSObject.Properties.Match('SKU')) { $sku = $p.SKU }

    $name = $null
    if ($p -and $p.PSObject.Properties.Match('kartAdi')) { $name = $p.kartAdi }
    if (-not $name -and $p -and $p.PSObject.Properties.Match('name')) { $name = $p.name }
    if (-not $name -and $p -and $p.PSObject.Properties.Match('adi')) { $name = $p.adi }
    if (-not $name -and $p -and $p.PSObject.Properties.Match('Name')) { $name = $p.Name }
    if (-not $sku) { $sku = "AUTO-$([guid]::NewGuid().ToString().Substring(0,8))" }
    if (-not $name) { $name = $sku }
    # Normalize possible arrays to scalars to avoid System.Object[] when converting to string
    $sku = (@($sku) -join '')
    $name = (@($name) -join '')
    # attempt to fix mojibake in product names (common when sources mix encodings)
    $name = FixMojibake $name

    # Map fields with safe fallbacks
    $kartTuru = $p.kartTuru -as [int]
    if (-not $kartTuru) { $kartTuru = 1 }
    $olcumBirimiId = $p.olcumBirimiId -as [int]
    if (-not $olcumBirimiId) { $olcumBirimiId = 1 }
    $kartTipi = $p.kartTipi -as [int]
    if (-not $kartTipi) { $kartTipi = 4 }
    $kategoriAgacKod = ''
    if ($p -and $p.PSObject.Properties.Match('kategoriAgacKod')) { $kategoriAgacKod = (@($p.kategoriAgacKod) -join '') }

    # Flags: ensure 1/0
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

    # Dates: prefer existing baslangicTarihi or use today
    $startDate = (@($p.baslangicTarihi) -join '')
    if ($startDate) {
        try { $d = [datetime]::Parse($startDate); $startDate = $d.ToString("dd'/'MM'/'yyyy") } catch { $startDate = $startDate }
    } else { $startDate = (Get-Date).ToString("dd'/'MM'/'yyyy") }

    # Prices/KDV
    $kartAlisKdvOran = if ($p.kartAlisKdvOran -ne $null) { $p.kartAlisKdvOran } else { 0.0 }
    $kartSatisKdvOran = if ($p.kartSatisKdvOran -ne $null) { $p.kartSatisKdvOran } else { 0.0 }

    $form = "baslangicTarihi=$([string](UrlEncodeCp1254 $startDate))`&kartKodu=$([string](UrlEncodeCp1254 $sku))`&kartAdi=$([string](UrlEncodeCp1254 $name))`&kartTuru=$kartTuru`&olcumBirimiId=$olcumBirimiId`&kartAlisKdvOran=$kartAlisKdvOran`&kartSatisKdvOran=$kartSatisKdvOran`&kartTipi=$kartTipi`&kategoriAgacKod=$([string](UrlEncodeCp1254 $kategoriAgacKod))`&satilabilirFlag=$satilabilirFlag`&satinAlinabilirFlag=$satinAlinabilirFlag`&lotNoFlag=$lotNoFlag`&maliyetHesaplanacakFlag=$maliyetHesaplanacakFlag"

    # Avoid printing the raw form (it contains '&' which PowerShell will treat as operators if re-pasted).
    Log "Built form for SKU: $sku (length: $($form.Length) chars)"
    # Check if SKU already exists in Koza via ListeleStkSkart.do
    $exists = $false
    try {
        $queryBody = @{ stkSkart = @{ kodBas = $sku; kodBit = $sku; kodOp = 'between' } } | ConvertTo-Json -Compress
        $checkResp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $queryBody -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        if ($null -ne $checkResp) {
            if ($checkResp.stkSkart -and ($checkResp.stkSkart.Count -gt 0)) { $exists = $true }
            elseif ($checkResp.data -and ($checkResp.data.Count -gt 0)) { $exists = $true }
        }
    } catch {
        Log ("WARN: Koza lookup failed for ${sku}: " + $_.Exception.Message + " -- proceeding to create")
        $exists = $false
    }

    if ($exists) {
        if ($OnDuplicate -eq 'skip') {
            Log "SKIP: $sku already exists in Koza, skipping as per OnDuplicate=skip"
            $skipped += [pscustomobject]@{ SKU=$sku; Name=$name; Reason='Exists' }
            continue
        } else {
            Log "DUPLICATE (fail): $sku exists and OnDuplicate=fail -> marking failed"
            $failed += [pscustomobject]@{ SKU=$sku; Name=$name; Error='Duplicate in Koza' }
            continue
        }
    }
    if ($DryRun) {
        # Save form to log dir using windows-1254 encoding and mark as simulated success
        $safeSku = ($sku -replace '[^a-zA-Z0-9_-]', '_')
        $formFile = Join-Path $logDir ("form-$safeSku.txt")
        $formBytes = [System.Text.Encoding]::GetEncoding(1254).GetBytes($form)
        [System.IO.File]::WriteAllBytes($formFile, $formBytes)
        Log "DRYRUN: wrote form (windows-1254) -> $formFile"
        $success += [pscustomobject]@{ SKU=$sku; Name=$name; Status='DRYRUN'; Response='(form-saved)' }
    } else {
        $bytes = [System.Text.Encoding]::GetEncoding(1254).GetBytes($form)
        try {
            $resp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'EkleStkWsSkart.do') -Method Post -Body $bytes -ContentType 'application/x-www-form-urlencoded; charset=windows-1254' -WebSession $sess -UseBasicParsing -ErrorAction Stop
            $body = $resp.Content
            # Try parse JSON response to detect server-side 'error' flag
            $parsed = $null
            try { $parsed = $body | ConvertFrom-Json -ErrorAction Stop } catch { $parsed = $null }
            if ($parsed -ne $null -and $parsed.PSObject.Properties.Match('error') -and $parsed.error) {
                $msg = $parsed.message -as [string]
                Log "KOZA-ERROR: $sku => $($resp.StatusCode) | Message: $msg"
                $bodyStr = $body -as [string]
                $dupPhrase = 'Kart kodu'
                $isDup = $false
                if ($msg) { if ($msg.IndexOf($dupPhrase, [System.StringComparison]::InvariantCultureIgnoreCase) -ge 0) { $isDup = $true } }
                if (-not $isDup -and $bodyStr) { if ($bodyStr.IndexOf($dupPhrase, [System.StringComparison]::InvariantCultureIgnoreCase) -ge 0) { $isDup = $true } }
                if ($isDup) {
                    Log "KOZA-DUPLICATE: $sku detected from Koza response; marking as skipped"
                    $skipped += [pscustomobject]@{ SKU=$sku; Name=$name; Reason='Duplicate from Koza'; Response=$body }
                } else {
                    $failed += [pscustomobject]@{ SKU=$sku; Name=$name; Status=$resp.StatusCode; Response=$body; Message=$msg }
                }
            } else {
                Log "OK: $sku => $($resp.StatusCode) | Response start: $($body.Substring(0,[Math]::Min(200,$body.Length)))"
                $success += [pscustomobject]@{ SKU=$sku; Name=$name; Status=$resp.StatusCode; Response=$body }
            }
        } catch {
            $err = $_.Exception.Message
            Log "FAILED: $sku => $err"
            $failed += [pscustomobject]@{ SKU=$sku; Name=$name; Error=$err }
        }
    }
}

# Summarize
Log "Sync complete. Success: $($success.Count), Failed: $($failed.Count)"
$success | ConvertTo-Json -Depth 3 | Out-File (Join-Path $logDir 'success.json') -Encoding utf8
$failed | ConvertTo-Json -Depth 3 | Out-File (Join-Path $logDir 'failed.json') -Encoding utf8
$skipped | ConvertTo-Json -Depth 3 | Out-File (Join-Path $logDir 'skipped.json') -Encoding utf8

Log "Logs written to: $logDir"
