param(
    [string]$BaseUrl = "https://koza.example.com",
    [string]$Endpoint = "/EkleStkWsSkart.do",
    [string]$ManualCookie = $null,

    [string]$KartAdi = "Test Ürünü",
    [string]$KartKodu = "00013225",
    [int]$KartTipi = 1,
    [double]$KartAlisKdvOran = 1,
    [int]$OlcumBirimiId = 1,
    [string]$BaslangicTarihi = (Get-Date -Date "06/04/2022").ToString("dd/MM/yyyy"),
    [int]$KartTuru = 1,
    [string]$KategoriAgacKod = $null,
    [string]$Barkod = "8888888",
    [string]$AlisTevkifatOran = "7/10",
    [string]$SatisTevkifatOran = "2/10",
    [int]$AlisTevkifatTipId = 1,
    [int]$SatisTevkifatTipId = 1,
    [int]$SatilabilirFlag = 1,
    [int]$SatinAlinabilirFlag = 1,
    [int]$LotNoFlag = 1,
    [int]$MinStokKontrol = 0,
    [bool]$MaliyetHesaplanacakFlag = $true
)

function Write-Log { param($Path, $Text) Add-Content -Path $Path -Value $Text }

# Build payload following the example shape
$payload = @{
    kartAdi = $KartAdi
    kartKodu = $KartKodu
    kartTipi = $KartTipi
    kartAlisKdvOran = $KartAlisKdvOran
    olcumBirimiId = $OlcumBirimiId
    baslangicTarihi = $BaslangicTarihi
    kartTuru = $KartTuru
    kategoriAgacKod = $KategoriAgacKod
    barkod = $Barkod
    alisTevkifatOran = $AlisTevkifatOran
    satisTevkifatOran = $SatisTevkifatOran
    alisTevkifatTipId = $AlisTevkifatTipId
    satisTevkifatTipId = $SatisTevkifatTipId
    satilabilirFlag = $SatilabilirFlag
    satinAlinabilirFlag = $SatinAlinabilirFlag
    lotNoFlag = $LotNoFlag
    minStokKontrol = $MinStokKontrol
    maliyetHesaplanacakFlag = $MaliyetHesaplanacakFlag
}

# Normalize date to literal slashes (dd/MM/yyyy) regardless of locale
try {
    $dt = Get-Date -Date $BaslangicTarihi -ErrorAction Stop
    $payload.baslangicTarihi = $dt.ToString("dd'/'MM'/'yyyy")
} catch {
    # if parsing fails, keep provided string as-is
}

# Convert to JSON (PowerShell) and ensure compact output (UTF-8)
$json = $payload | ConvertTo-Json -Depth 10

# Encode to windows-1254 (cp1254)
# Avoid RegisterProvider for older PowerShell/.NET where CodePagesEncodingProvider may not exist.
$enc = [System.Text.Encoding]::GetEncoding(1254)
$bytes = $enc.GetBytes($json)

# Resolve full URL
if ($Endpoint -match '^https?://') { $uri = $Endpoint } else { $uri = $BaseUrl.TrimEnd('/') + '/' + $Endpoint.TrimStart('/') }

# Build headers
$headers = @{
    'Content-Type' = 'application/json; charset=windows-1254'
}
if (-not [string]::IsNullOrWhiteSpace($ManualCookie)) {
    $headers['Cookie'] = $ManualCookie
}

# Logging setup
$logsDir = Join-Path (Get-Location) "scripts\logs"
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }
$ts = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logsDir "test-luca-create-stock-$ts.txt"

Write-Log $logFile "REQUEST URI: $uri"
    Write-Log $logFile "REQUEST JSON (cp1254 encoded):`n$json"

try {
    # Use byte[] body so the charset is preserved
    $resp = Invoke-WebRequest -Uri $uri -Method Post -Body $bytes -ContentType $headers['Content-Type'] -Headers $headers -UseBasicParsing
    $respBody = $resp.Content

    Write-Log $logFile "RESPONSE STATUS: $($resp.StatusCode)"
    Write-Log $logFile "RESPONSE BODY:`n$respBody"

    Write-Output "HTTP $($resp.StatusCode)"
    Write-Output $respBody

    # If response is JSON, try to parse skartId
    try {
        $parsed = $respBody | ConvertFrom-Json -ErrorAction Stop
        if ($parsed -and $parsed.skartId) {
            Write-Output "skartId: $($parsed.skartId)"
            Write-Log $logFile "PARSED skartId: $($parsed.skartId)"
        }
    } catch { }

    if (-not $resp.StatusCode -or $resp.StatusCode -ge 300) { exit 2 }
    exit 0
}
catch {
    Write-Log $logFile "EXCEPTION: $($_.Exception.Message)"
    Write-Error $_.Exception.Message
    exit 1
}
