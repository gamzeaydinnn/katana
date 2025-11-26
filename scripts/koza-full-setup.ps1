<#
koza-full-setup.ps1

Tam otomatik Koza kurulum scripti.
- Eğer `-JSessionId` verilirse doğrudan o cookie ile çalışır.
- Eğer `-AutoLogin` veya JSessionId boş ise `Giris.do` ile login yapıp JSESSIONID alır.
- Ardından şube seçimi, kategori ve ölçü birimi çekme ve `appsettings.Development.json` güncellemesi yapar.
#>

param(
    [string]$JSessionId = '',
    [switch]$AutoLogin,
    [string]$BaseUrl = "http://85.111.1.49:57005/Yetki/",
    [int]$BranchId = 11746,
    [string]$ConfigPath = ".\appsettings.Development.json",
    [string]$OrgCode = '',
    [string]$Username = '',
    [string]$Password = '',
    [double]$DefaultKdvOran = 0.20,
    [int]$DefaultKartTipi = 4,
    [string]$DefaultKategoriKodu = '001'
)

function Write-JsonFile($path, $obj) {
    $json = $null
    if ($obj -is [string]) { $json = $obj } else { $json = $obj | ConvertTo-Json -Depth 10 }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "KOZA ENTEGRASYON KURULUMU (otomatik)" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

# Prepare session
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$results = @{
    CookieValid = $false
    BranchSelected = $false
    Categories = @()
    MeasurementUnits = @()
    CategoryMapping = @{}
    UnitMapping = @{}
}

# Normalize base url
if (-not $BaseUrl.EndsWith('/')) { $BaseUrl += '/' }

# Ensure logs directory exists under the config parent so we can write artifacts
$logRoot = Join-Path (Split-Path -Path $ConfigPath -Parent) 'scripts\logs'
if (-not (Test-Path $logRoot)) { New-Item -Path $logRoot -ItemType Directory -Force | Out-Null }

# Normalize and add provided JSESSIONID to the session (if present)
if (-not [string]::IsNullOrWhiteSpace($JSessionId)) {
    try {
        $cookieValue = $JSessionId
        if ($cookieValue -like 'JSESSIONID=*') { $cookieValue = $cookieValue -replace '^JSESSIONID=' , '' }
        $cookie = New-Object System.Net.Cookie('JSESSIONID', $cookieValue, '/', ([Uri]$BaseUrl).Host)
        $session.Cookies.Add($cookie)
        Write-Host "[i] Using provided JSESSIONID: $cookieValue" -ForegroundColor Cyan
    } catch {
        Write-Host "[!] Failed to add provided JSESSIONID to session: $_" -ForegroundColor Red
        exit 1
    }
} elseif ($AutoLogin) {
    Write-Host "[i] AutoLogin requested: attempting Giris.do login..." -ForegroundColor Cyan
    $login = $null
    # Initial GET to seed session cookies/firstLoad behavior (emulate browser)
    try { Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -WebSession $session -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null } catch {}
    $loginLogPath = Join-Path $logRoot ("koza-login-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
    try {
        # Try UI-style field names first (this matched the interactive success path)
        $body2 = "girisForm.orgCode=$OrgCode&girisForm.userName=$Username&girisForm.userPassword=$Password&girisForm.captchaInput="
        $login = Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body2 -WebSession $session -UseBasicParsing -ErrorAction Stop
    } catch {}

    if (-not $login) {
        try {
            # Fallback: form-data style
            $body1 = "orgCode=$OrgCode&userName=$Username&userPassword=$Password"
            $login = Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body1 -WebSession $session -UseBasicParsing -ErrorAction Stop
        } catch {}
    }

    if (-not $login) {
        try {
            # JSON fallback
            $authBody = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json
            $login = Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -Method Post -ContentType "application/json" -Body $authBody -WebSession $session -UseBasicParsing -ErrorAction Stop
        } catch {}
    }

    if ($login) {
        $login.Content | Out-File -FilePath $loginLogPath -Encoding UTF8
        Write-Host "[OK] Login response written: $loginLogPath" -ForegroundColor Green
        # extract cookie from session
        $baseUri = [Uri]$BaseUrl
        foreach ($c in $session.Cookies.GetCookies($baseUri)) {
            if ($c.Name -eq 'JSESSIONID') { $JSessionId = $c.Value }
        }
        if (-not [string]::IsNullOrWhiteSpace($JSessionId)) {
            Write-Host "[OK] Obtained JSESSIONID from server: $JSessionId" -ForegroundColor Green
        } else {
            Write-Host "[!] Login reported response but no JSESSIONID found in cookie jar." -ForegroundColor Yellow
            Write-Host "Check $loginLogPath for details." -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "[!] AutoLogin failed: no login response received." -ForegroundColor Red
        exit 1
    }
}

# ADIM 1: Cookie Doğrulama
Write-Host "[1/5] Cookie doğrulanıyor..." -ForegroundColor Yellow
try {
    $branchesUrl = "${BaseUrl}YdlUserResponsibilityOrgSs.do"
    $response = Invoke-WebRequest -Uri $branchesUrl -Method POST -WebSession $session -ContentType "application/json" -Body "{}" -UseBasicParsing -ErrorAction Stop
    $branchData = $response.Content | ConvertFrom-Json

    if ($branchData.code -eq 1002) {
        Write-Host "[FAIL] Cookie geçersiz - yeni cookie al!`n" -ForegroundColor Red
        exit 1
    }

    Write-Host "[OK] Cookie geçerli!`n" -ForegroundColor Green
    $results.CookieValid = $true
    Write-JsonFile (Join-Path (Split-Path -Path $ConfigPath -Parent) 'scripts\logs\koza-branches.json') $response.Content
} catch {
    Write-Host "[✗] HATA (cookie doğrulama): $_`n" -ForegroundColor Red
    exit 1
}

# ADIM 2: Şube Seçimi
Write-Host "[2/5] Şube seçiliyor (ID: $BranchId)..." -ForegroundColor Yellow
try {
    $changeBranchUrl = "${BaseUrl}GuncelleYtkSirketSubeDegistir.do"
    $branchPayload = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress

    $changeResponse = Invoke-WebRequest -Uri $changeBranchUrl -Method POST -WebSession $session -ContentType "application/json; charset=utf-8" -Body $branchPayload -UseBasicParsing -ErrorAction Stop
    $changeContent = $changeResponse.Content
    Write-JsonFile (Join-Path (Split-Path -Path $ConfigPath -Parent) 'scripts\logs\koza-change-branch.json') $changeContent
    $changeResult = $changeContent | ConvertFrom-Json

    # Koza sometimes returns different shapes; consider '@message' or code==0 as success
    $branchSelectedSuccess = $false
    if ($null -ne $changeResult) {
        if ($changeResult.PSObject.Properties.Name -contains 'code') {
            if ($changeResult.code -eq 1000 -or $changeResult.code -eq 0) { $branchSelectedSuccess = $true }
        }
        if (-not $branchSelectedSuccess -and $changeResult.PSObject.Properties.Name -contains '@message') {
            # presence of @message indicates success in some responses
            $branchSelectedSuccess = $true
        }
    }

    if ($branchSelectedSuccess) {
        Write-Host "[OK] Şube seçildi!`n" -ForegroundColor Green
        $results.BranchSelected = $true
    } else {
        $msg = $null
        if ($changeResult -and $changeResult.PSObject.Properties.Name -contains 'message') { $msg = $changeResult.message }
        if (-not $msg -and $changeResult -and $changeResult.PSObject.Properties.Name -contains '@message') { $msg = $changeResult.'@message' }
        Write-Host "[FAIL] Şube seçimi başarısız: $($msg)`n" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "[✗] HATA (şube seçimi): $_`n" -ForegroundColor Red
    exit 1
}

# ADIM 3: Kategori Listesi Çekme
Write-Host "[3/5] Kategori listesi çekiliyor..." -ForegroundColor Yellow
try {
    $categoryUrl = "${BaseUrl}ListeleStkSkartKategoriAgac.do"
    $categoryPayload = @{ kartTuru = 1 } | ConvertTo-Json -Compress

    $response = Invoke-WebRequest -Uri $categoryUrl -Method POST -WebSession $session -ContentType "application/json" -Body $categoryPayload -UseBasicParsing -ErrorAction Stop
    $categories = $response.Content | ConvertFrom-Json
    Write-JsonFile (Join-Path (Split-Path -Path $ConfigPath -Parent) 'scripts\logs\koza-categories.json') $response.Content

    $hasCategories = $false
    if ($categories -ne $null) {
        if ($categories.PSObject.Properties.Name -contains 'code' -and ($categories.code -eq 1000 -or $categories.code -eq 0)) { $hasCategories = $true }
        if (-not $hasCategories -and $categories.PSObject.Properties.Name -contains 'list' -and $categories.list.Count -gt 0) { $hasCategories = $true }
    }
    if ($hasCategories) {
        Write-Host "[OK] Kategoriler alındı:`n" -ForegroundColor Green
        $defaultSet = $false
        $catList = $null
        if ($categories.PSObject.Properties.Name -contains 'data') { $catList = $categories.data }
        elseif ($categories.PSObject.Properties.Name -contains 'list') { $catList = $categories.list }
        if ($null -eq $catList) { $catList = @() }
        foreach ($cat in $catList) {
            # Support multiple shapes: kategoriAgacKod/kategoriAdi or kod/tanim
            $kod = $null
            $ad = $null
            if ($cat.PSObject.Properties.Name -contains 'kategoriAgacKod') { $kod = $cat.kategoriAgacKod }
            elseif ($cat.PSObject.Properties.Name -contains 'kod') { $kod = $cat.kod }
            if ($cat.PSObject.Properties.Name -contains 'kategoriAdi') { $ad = $cat.kategoriAdi }
            elseif ($cat.PSObject.Properties.Name -contains 'tanim') { $ad = $cat.tanim }
            if (-not $kod) { $kod = '' }
            if (-not $ad) { $ad = '' }
            Write-Host "   - Kod: $kod | Ad: $ad" -ForegroundColor Gray
            $results.Categories += @{ Code = $kod; Name = $ad }

            if ($ad -match "elektronik|bilgisayar|teknoloji") {
                $results.CategoryMapping["Electronics"] = $kod
            }
            elseif ($ad -match "mobilya|ev|dekor") {
                $results.CategoryMapping["Furniture"] = $kod
            }
            elseif ($ad -match "giyim|tekstil|kıyafet") {
                $results.CategoryMapping["Clothing"] = $kod
            }
            elseif ($ad -match "gıda|yiyecek|içecek") {
                $results.CategoryMapping["Food"] = $kod
            }
            elseif (-not $defaultSet) {
                $results.CategoryMapping["default"] = $kod
                $defaultSet = $true
            }
        }
        Write-Host ""
    } else {
        Write-Host "[!] Kategori listesi alınamadı: $($categories.message)`n" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[!] Kategori hatası (devam ediliyor): $_`n" -ForegroundColor Yellow
}

# ADIM 4: Ölçü Birimi Listesi Çekme
Write-Host "[4/5] Ölçü birimi listesi çekiliyor..." -ForegroundColor Yellow
try {
    $unitUrl = "${BaseUrl}ListeleGnlOlcumBirimi.do"
    $response = Invoke-WebRequest -Uri $unitUrl -Method POST -WebSession $session -ContentType "application/json" -Body "{}" -UseBasicParsing -ErrorAction Stop
    $units = $response.Content | ConvertFrom-Json
    Write-JsonFile (Join-Path (Split-Path -Path $ConfigPath -Parent) 'scripts\logs\koza-units.json') $response.Content

    if ($units.code -eq 1000 -or $units.code -eq 0) {
        Write-Host "[OK] Ölçü birimleri alındı:`n" -ForegroundColor Green
        $unitList = $null
        if ($units.PSObject.Properties.Name -contains 'data') { $unitList = $units.data }
        elseif ($units.PSObject.Properties.Name -contains 'list') { $unitList = $units.list }
        if ($null -eq $unitList) { $unitList = @() }
        foreach ($unit in $unitList) {
            $id = $unit.id
            $tanim = $unit.tanim
            Write-Host "   - ID: $id | Adı: $tanim" -ForegroundColor Gray
            $results.MeasurementUnits += @{ Id = $id; Name = $tanim }
            switch ($tanim.ToLower()) {
                "adet" { $results.UnitMapping["PCS"] = $id; $results.UnitMapping["UNIT"] = $id }
                "kg" { $results.UnitMapping["KG"] = $id }
                "gram" { $results.UnitMapping["G"] = $id }
                "litre" { $results.UnitMapping["L"] = $id }
                "metre" { $results.UnitMapping["M"] = $id }
                "kutu" { $results.UnitMapping["BOX"] = $id }
                "koli" { $results.UnitMapping["PACK"] = $id }
            }
        }
        Write-Host ""
    } else {
        Write-Host "[!] Ölçü birimi listesi alınamadı: $($units.message)`n" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[!] Ölçü birimi hatası (devam ediliyor): $_`n" -ForegroundColor Yellow
}

# ADIM 5: Config Güncelleme
Write-Host "[5/5] Config dosyası güncelleniyor..." -ForegroundColor Yellow
try {
    if (Test-Path $ConfigPath) {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        $config.LucaApi.ManualSessionCookie = "JSESSIONID=$JSessionId"
        if ($results.CategoryMapping.Count -gt 0) {
            $config.ProductSync.CategoryMapping = [PSCustomObject]$results.CategoryMapping
        }
        if ($results.UnitMapping.Count -gt 0) {
            $config.UnitMapping = [PSCustomObject]$results.UnitMapping
        }

        # Persist Koza-required defaults so other scripts/clients can reuse
        if (-not $config.ProductSync) { $config.ProductSync = @{} }
        # Rebuild ProductSync as a mutable hashtable and merge existing properties
        $ps = @{}
        foreach ($p in $config.ProductSync.PSObject.Properties) { $ps[$p.Name] = $config.ProductSync.$($p.Name) }
        $ps.DefaultKdvOran = $DefaultKdvOran
        $ps.DefaultKartTipi = $DefaultKartTipi
        $ps.DefaultKategoriKodu = $DefaultKategoriKodu
        $config.ProductSync = [PSCustomObject]$ps
        $config | ConvertTo-Json -Depth 10 | Set-Content $ConfigPath -Encoding UTF8
        Write-Host "[OK] Config güncellendi: $ConfigPath`n" -ForegroundColor Green
    } else {
        Write-Host "[!] Config bulunamadı: $ConfigPath`n" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[!] Config güncellenemedi: $_`n" -ForegroundColor Yellow
}

# SONUÇ RAPORU
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "KURULUM SONUÇLARI" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan
Write-Host "Cookie Durumu:       " -NoNewline -ForegroundColor White
if ($results.CookieValid) { Write-Host "Geçerli" -ForegroundColor Green } else { Write-Host "Geçersiz" -ForegroundColor Red }
Write-Host "Şube Seçimi:         " -NoNewline -ForegroundColor White
if ($results.BranchSelected) { Write-Host "Başarılı (ID: $BranchId)" -ForegroundColor Green } else { Write-Host "Başarısız" -ForegroundColor Red }
Write-Host "Kategori Sayısı:     " -NoNewline -ForegroundColor White
Write-Host "$($results.Categories.Count)" -ForegroundColor Cyan
Write-Host "Ölçü Birimi Sayısı:  " -NoNewline -ForegroundColor White
Write-Host "$($results.MeasurementUnits.Count)" -ForegroundColor Cyan
Write-Host "`nÖNERİLEN CATEGORY MAPPING:" -ForegroundColor Yellow
Write-Host "===========================" -ForegroundColor Yellow
$results.CategoryMapping | ConvertTo-Json
Write-Host "`nÖNERİLEN UNIT MAPPING:" -ForegroundColor Yellow
Write-Host "======================" -ForegroundColor Yellow
$results.UnitMapping | ConvertTo-Json
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "SONRAKİ ADIMLAR:" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "1. API'yi yeniden başlat: dotnet run" -ForegroundColor White
Write-Host "2. Sync endpoint'ini test et: POST /api/Sync/stock" -ForegroundColor White
Write-Host "3. Logları kontrol et: .\scripts\logs\luca-raw.log`n" -ForegroundColor White
$results | ConvertTo-Json -Depth 10 | Out-File ".\koza-setup-results.json" -Encoding UTF8
Write-Host "[OK] Sonuçlar kaydedildi: .\koza-setup-results.json`n" -ForegroundColor Green
