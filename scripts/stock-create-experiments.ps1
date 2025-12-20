param(
    [string]$BaseUrl = 'https://akozas.luca.com.tr/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746,
    [string]$ProductSKU = 'EXP-TEST-001',
    [string]$ProductName = 'EXPERIMENT PRODUCT',
    [int]$OlcumBirimiId = 945,
    [double]$Kdv = 0.18,
    [int]$KartTipi = 4,
    [string]$Kategori = '01'
)

# Ensure log dir
$logDir = Join-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent) 'logs'
if (-not (Test-Path $logDir)) { New-Item -Path $logDir -ItemType Directory -Force | Out-Null }

function Write-JsonFile($path, $obj) {
    $json = $null
    if ($obj -is [string]) { $json = $obj } else { $json = $obj | ConvertTo-Json -Depth 10 }
    Set-Content -Path $path -Value $json -Encoding UTF8
}

# Login (browser-like)
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try { Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -WebSession $session -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null } catch {}
$body = "girisForm.orgCode=$OrgCode&girisForm.userName=$Username&girisForm.userPassword=$Password&girisForm.captchaInput="
$login = Invoke-WebRequest -Uri "${BaseUrl}Giris.do" -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body -WebSession $session -UseBasicParsing -ErrorAction Stop
Write-JsonFile (Join-Path $logDir 'exp-login-response.json') $login.Content

# ensure branch selected
$change = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress
try { $chg = Invoke-WebRequest -Uri "${BaseUrl}GuncelleYtkSirketSubeDegistir.do" -Method Post -Body $change -WebSession $session -ContentType 'application/json' -UseBasicParsing -ErrorAction Stop; Write-JsonFile (Join-Path $logDir 'exp-change-branch.json') $chg.Content } catch { Write-Warning "change-branch failed: $_" }

# Build stock payload with additional flags
$stockObj = @{
    kartAdi = $ProductName
    kartTuru = 1
    baslangicTarihi = (Get-Date -Format 'yyyy-MM-dd')
    olcumBirimiId = $OlcumBirimiId
    kartKodu = $ProductSKU
    kartAlisKdvOran = $Kdv
    kartSatisKdvOran = $Kdv
    kartTipi = $KartTipi
    kategoriAgacKod = $Kategori
    perakendeSatisBirimFiyat = 100.0
    perakendeAlisBirimFiyat = 80.0
    # Additional flags to test
    maliyetHesaplanacakFlag = $true
    satilabilirFlag = $true
    satinAlinabilirFlag = $true
    stokEtkin = $true
}

$stockJson = $stockObj | ConvertTo-Json -Depth 10
Write-JsonFile (Join-Path $logDir 'exp-stock-request.json') $stockObj

# Helper: send cp1254 using HttpWebRequest (copied/minified from other scripts)
function Send-Cp1254([string]$relative, [string]$bodyString, [string]$outPrefix) {
    $url = "${BaseUrl}$relative"
    try {
        $enc = [System.Text.Encoding]::GetEncoding(1254)
    } catch { $enc = [System.Text.Encoding]::UTF8 }
    $bytes = $enc.GetBytes($bodyString)
    $req = [System.Net.HttpWebRequest]::Create($url)
    $req.Method = 'POST'
    $req.ContentType = 'application/json; charset=windows-1254'
    $req.Accept = 'application/json'
    $req.CookieContainer = New-Object System.Net.CookieContainer
    $baseUri = [System.Uri]$BaseUrl
    foreach ($c in $session.Cookies.GetCookies($baseUri)) { $netCookie = New-Object System.Net.Cookie($c.Name, $c.Value, $c.Path, $c.Domain); $req.CookieContainer.Add($netCookie) }
    $req.ContentLength = $bytes.Length
    $reqStream = $req.GetRequestStream(); $reqStream.Write($bytes,0,$bytes.Length); $reqStream.Close()
    $resp = $req.GetResponse()
    $respStream = $resp.GetResponseStream(); $sr = New-Object System.IO.StreamReader($respStream); $text = $sr.ReadToEnd(); $sr.Close()
    $ts = (Get-Date).ToString('yyyyMMdd-HHmmss')
    $respPath = Join-Path $logDir ("$outPrefix-cp1254-$ts.txt")
    Set-Content -Path $respPath -Value $text -Encoding UTF8
    return $respPath
}

# 1) cp1254 JSON
$cpPath = Send-Cp1254 'EkleStkWsSkart.do' $stockJson 'exp-stock'
Write-Host "cp1254 response saved: $cpPath"

# 2) utf-8 JSON via Invoke-WebRequest
try {
    $utfResp = Invoke-WebRequest -Uri "${BaseUrl}EkleStkWsSkart.do" -Method Post -Body $stockJson -ContentType 'application/json; charset=utf-8' -WebSession $session -UseBasicParsing -ErrorAction Stop
    $utfPath = Join-Path $logDir ("exp-stock-utf8-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
    $utfResp.Content | Out-File $utfPath -Encoding UTF8
    Write-Host "utf8 response saved: $utfPath"
} catch { Write-Warning "utf8 request failed: $_" }

# 3) form-encoded fallback (flatten object to key=value)
function To-Form([hashtable]$h) {
    $pairs = @()
    foreach ($k in $h.Keys) {
        $v = $h[$k]
        if ($v -is [System.Collections.Hashtable] -or $v -is [System.Collections.ArrayList] -or $v -is [System.Management.Automation.PSCustomObject]) { $v = ($v | ConvertTo-Json -Compress) }
        $pairs += ([System.Uri]::EscapeDataString($k) + '=' + [System.Uri]::EscapeDataString([string]$v))
    }
    return ($pairs -join '&')
}
$formBody = To-Form $stockObj
try {
    $formResp = Invoke-WebRequest -Uri "${BaseUrl}EkleStkWsSkart.do" -Method Post -Body $formBody -ContentType 'application/x-www-form-urlencoded' -WebSession $session -UseBasicParsing -ErrorAction Stop
    $formPath = Join-Path $logDir ("exp-stock-form-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
    $formResp.Content | Out-File $formPath -Encoding UTF8
    Write-Host "form response saved: $formPath"
} catch { Write-Warning "form request failed: $_" }

Write-Host "Experiments complete. Check $logDir for exp- files."