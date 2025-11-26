param(
    [string]$BaseUrl = "http://85.111.1.49:57005",
    [string]$OrgCode = "7374953",
    [string]$Username = "Admin",
    [string]$Password = "2009Bfm",
    [double]$DefaultKdvOran = 0.20,
    [int]$DefaultKartTipi = 4,
    [string]$DefaultKategoriKodu = '001'
)

$ErrorActionPreference = "Stop"

$logs = Join-Path -Path (Get-Location) -ChildPath "scripts/logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$logFile = Join-Path $logs ("koza-auth-{0}.log" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$cookieJar = Join-Path $logs "koza-cookies.txt"
Remove-Item $cookieJar -ErrorAction SilentlyContinue

function Write-Log($msg) {
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg
    $line | Tee-Object -FilePath $logFile -Append
}

Write-Log "BaseUrl=$BaseUrl | OrgCode=$OrgCode | Username=$Username"

# 1) Login (JSON)
Write-Log "Step 1: Login (JSON body)"
$authBody = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json
# Try form-data first (Koza often expects form)
$login = $null
try {
    $login = Invoke-WebRequest -Uri "$BaseUrl/Yetki/Giris.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "orgCode=$OrgCode&userName=$Username&userPassword=$Password" -SessionVariable websess -ErrorAction Stop
} catch {}

# Fallback: UI-style fields (may bypass captcha if accepted)
if (-not $login) {
    try {
        $login = Invoke-WebRequest -Uri "$BaseUrl/Yetki/Giris.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "girisForm.orgCode=$OrgCode&girisForm.userName=$Username&girisForm.userPassword=$Password&girisForm.captchaInput=" -SessionVariable websess -ErrorAction Stop
    } catch {}
}

# Fallback: JSON
if (-not $login) {
    $login = Invoke-WebRequest -Uri "$BaseUrl/Yetki/Giris.do" -Method Post -ContentType "application/json" -Body $authBody -SessionVariable websess -ErrorAction Stop
}
$login.Content | Out-File -Encoding UTF8 -Append $logFile
Write-Log ("Status: {0}" -f $login.StatusCode)
if (-not $websess.Cookies.Count) { Write-Log "ERROR: no cookies received"; exit 1 }

# 2) Branches
Write-Log "Step 2: Branch list"
$branches = Invoke-WebRequest -Uri "$BaseUrl/Yetki/YdlUserResponsibilityOrgSs.do" -Method Post -ContentType "application/json" -Body "{}" -WebSession $websess -ErrorAction Stop
$branches.Content | Out-File -Encoding UTF8 -Append $logFile

# Try to parse branch id from simple array [{id:...}] or orgSirketSubeId or ack/name maps
$branchJson = $branches.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
$branchId = $null
if ($branchJson -is [System.Collections.IEnumerable]) {
    foreach ($b in $branchJson) {
        if ($b.orgSirketSubeId) { $branchId = $b.orgSirketSubeId; break }
        if ($b.id) { $branchId = $b.id; break }
    }
}
if (-not $branchId) { Write-Log "WARNING: could not parse branch id, inspect $logFile"; exit 1 }
Write-Log "Selected branch id: $branchId"

# 3) Change branch (JSON then form)
Write-Log "Step 3: Change branch (JSON)"
$changeJson = @{ orgSirketSubeId = $branchId } | ConvertTo-Json
$change = Invoke-WebRequest -Uri "$BaseUrl/Yetki/GuncelleYtkSirketSubeDegistir.do" -Method Post -ContentType "application/json" -Body $changeJson -WebSession $websess -ErrorAction SilentlyContinue
if ($change) { $change.Content | Out-File -Encoding UTF8 -Append $logFile; Write-Log ("ChangeBranch JSON status {0}" -f $change.StatusCode) }

Write-Log "Step 3b: Change branch (form) fallback"
$changeForm = "orgSirketSubeId=$branchId"
$change2 = Invoke-WebRequest -Uri "$BaseUrl/Yetki/GuncelleYtkSirketSubeDegistir.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body $changeForm -WebSession $websess -ErrorAction SilentlyContinue
if ($change2) { $change2.Content | Out-File -Encoding UTF8 -Append $logFile; Write-Log ("ChangeBranch FORM status {0}" -f $change2.StatusCode) }

# 4) Verify with a single stock card POST (Koza array kabul etmiyor)
Write-Log "Step 4: Verify auth with single stock card"
$testCard = @{
    kartAdi = "MANUAL TEST URUN"
    kartTuru = 1
    olcumBirimiId = 5
    kartKodu = "MANUALTEST001"
    maliyetHesaplanacakFlag = $true
    kartTipi = $DefaultKartTipi
    kategoriAgacKod = $DefaultKategoriKodu
    kartAlisKdvOran = $DefaultKdvOran
    kartSatisKdvOran = $DefaultKdvOran
    satilabilirFlag = $true
    satinAlinabilirFlag = $true
    perakendeAlisBirimFiyat = 100
    perakendeSatisBirimFiyat = 150
} | ConvertTo-Json

$verify = Invoke-WebRequest -Uri "$BaseUrl/Yetki/EkleStkWsSkart.do" -Method Post -ContentType "application/json" -Body $testCard -WebSession $websess -ErrorAction SilentlyContinue
if ($verify) {
    $verify.Content | Out-File -Encoding UTF8 -Append $logFile
    Write-Log ("Verify status {0}" -f $verify.StatusCode)
    if ($verify.Content -match "Şirket Şube") { Write-Log "FAIL: still requires branch selection"; exit 1 }
    if ($verify.Content.Trim().StartsWith("<")) { Write-Log "FAIL: HTML error returned"; exit 1 }
    Write-Log "SUCCESS: single stock card POST returned JSON"
} else {
    Write-Log "FAIL: verification call failed"
}

Write-Log "Auth flow finished. See $logFile for details; cookies: $cookieJar"
