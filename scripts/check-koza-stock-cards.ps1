param(
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746,
    [int[]]$SkartIds
)

if (-not $KozaBaseUrl.EndsWith('/')) { $KozaBaseUrl += '/' }

function Write-Log { param($Path, $Text) Add-Content -Path $Path -Value $Text }

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Login
$loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Compress
try {
    $login = Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body $loginPayload -ContentType 'application/json' -WebSession $session -UseBasicParsing -ErrorAction Stop
    Write-Output "Login HTTP: $($login.StatusCode)"
} catch {
    Write-Error "Login failed: $($_.Exception.Message)"; exit 1
}

# Change branch
$branchJson = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress
try {
    $branchResp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $branchJson -ContentType 'application/json' -WebSession $session -UseBasicParsing -ErrorAction Stop
    Write-Output "Branch change HTTP: $($branchResp.StatusCode)"
} catch {
    Write-Warning "Branch change failed: $($_.Exception.Message)"
}

if (-not $SkartIds -or $SkartIds.Count -eq 0) {
    Write-Error "Provide at least one skartId via -SkartIds"
    exit 1
}

foreach ($id in $SkartIds) {
    Write-Output "\n=== Querying skartId: $id ==="
    $body = @{ stkSkart = @{ skartId = [int]$id } } | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $body -ContentType 'application/json' -WebSession $session -ErrorAction Stop
        # Pretty-print key fields if available
        if ($resp -and $resp.stkSkart -and $resp.stkSkart.Count -gt 0) {
            foreach ($item in $resp.stkSkart) {
                function Get-Prop { param($o, $names) foreach ($n in $names) { if ($o -and $o.PSObject.Properties.Match($n)) { return $o.$n } } return $null }
                $out = [pscustomobject]@{
                    skartId = Get-Prop $item @('skartId','skartID')
                    kartKodu = Get-Prop $item @('kartKodu','kod')
                    kartAdi = Get-Prop $item @('kartAdi','adi')
                    olcumBirimi = Get-Prop $item @('olcumBirimi','temelOlcuBirimi')
                    olcumBirimiId = Get-Prop $item @('olcumBirimiId')
                    kategoriAgacKod = Get-Prop $item @('kategoriAgacKod','kategoriKodu','hiyerarsikKod')
                    satilabilirFlag = Get-Prop $item @('satilabilirFlag','aktif')
                    satinAlinabilirFlag = Get-Prop $item @('satinAlinabilirFlag')
                    lotNoFlag = Get-Prop $item @('lotNoFlag')
                    maliyetHesaplanacakFlag = Get-Prop $item @('maliyetHesaplanacakFlag')
                    baslangicTarihi = Get-Prop $item @('baslangicTarihi','eklemeTarihi')
                }
                $out | Format-List
            }
        } elseif ($resp -and $resp.data -and $resp.data.Count -gt 0) {
            foreach ($item in $resp.data) {
                function Get-Prop { param($o, $names) foreach ($n in $names) { if ($o -and $o.PSObject.Properties.Match($n)) { return $o.$n } } return $null }
                $out = [pscustomobject]@{
                    skartId = Get-Prop $item @('skartId','skartID')
                    kartKodu = Get-Prop $item @('kartKodu','kod')
                    kartAdi = Get-Prop $item @('kartAdi','adi')
                    olcumBirimi = Get-Prop $item @('olcumBirimi','temelOlcuBirimi')
                    olcumBirimiId = Get-Prop $item @('olcumBirimiId')
                    kategoriAgacKod = Get-Prop $item @('kategoriAgacKod','kategoriKodu','hiyerarsikKod')
                    satilabilirFlag = Get-Prop $item @('satilabilirFlag','aktif')
                    satinAlinabilirFlag = Get-Prop $item @('satinAlinabilirFlag')
                    lotNoFlag = Get-Prop $item @('lotNoFlag')
                    maliyetHesaplanacakFlag = Get-Prop $item @('maliyetHesaplanacakFlag')
                    baslangicTarihi = Get-Prop $item @('baslangicTarihi','eklemeTarihi')
                }
                $out | Format-List
            }
        } else {
            # If response is a single object
            $item = $resp
            $out = [pscustomobject]@{
                skartId = $item.skartId
                kartKodu = $item.kartKodu
                kartAdi = $item.kartAdi
                olcumBirimi = $item.olcumBirimi
                olcumBirimiId = $item.olcumBirimiId
                kategoriAgacKod = $item.kategoriAgacKod
                satilabilirFlag = $item.satilabilirFlag
                satinAlinabilirFlag = $item.satinAlinabilirFlag
                lotNoFlag = $item.lotNoFlag
                maliyetHesaplanacakFlag = $item.maliyetHesaplanacakFlag
                baslangicTarihi = $item.baslangicTarihi
            }
            $out | Format-List
        }
    } catch {
        $errMsg = $_.Exception.Message
        Write-Warning ("Query failed for {0}: {1}" -f $id, $errMsg)
    }
}

Write-Output "\nDone."
