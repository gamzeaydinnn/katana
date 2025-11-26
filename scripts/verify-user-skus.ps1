param(
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 11746
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path (Get-Location) "scripts\logs\koza-verify-user-skus-$ts"
New-Item -Path $outDir -ItemType Directory -Force | Out-Null

Write-Output "Preparing SKU list..."
# User-provided SKU list (SKU => Name list). Edit if needed.
$skuList = @(
    @{ SKU='HIZ01'; Name='%1 KDV Lİ MUHTELİF ALIMLAR' },
    @{ SKU='HIZ10'; Name='%10 KDVLİ MUHTELİF ALIMLAR' },
    @{ SKU='HIZ20'; Name='%20 KDVLİ MUHTELİF ALIMLAR' },
    @{ SKU='6250666'; Name='093-58064-032' },
    @{ SKU='6250681'; Name='093-58064-083' },
    @{ SKU='CL-29 02 00347 01'; Name='32 20 00126 ART3000-3300-4000-4400 LİKİT DEPO ÇIKIŞ-DRYER...' },
    @{ SKU='387758'; Name='83.9945.3A Ø 38 mm JCI CUPPER DISC D.38' },
    @{ SKU='BFM-05'; Name='ALÜMİNYUM HURDA' },
    @{ SKU='BFM-06'; Name='BAKIR BORU HURDA' },
    @{ SKU='PIPE_7_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_2_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_22_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_27_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_42_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_43_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='PIPE_1_VWGH_160_V5'; Name='COPPER PIPE' },
    @{ SKU='BFM-04'; Name='DEMİR TALAŞ' },
    @{ SKU='BFM-03'; Name='HURDA DEMİR' },
    @{ SKU='BFM-02'; Name='KROM HURDA' },
    @{ SKU='BFM-01'; Name='KROM TALAŞ' },
    @{ SKU='9310011'; Name='NETSİS KONTROL ET1' },
    @{ SKU='9310024'; Name='NETSİS KONTROL ET2' },
    @{ SKU='9310030'; Name='NETSİS KONTROL ET3' },
    @{ SKU='9320487'; Name='NETSİS KONTROL ET4' },
    @{ SKU='OIO-AA19-01'; Name='NETSİS KONTROL ET5' },
    @{ SKU='OIO-AA24-01'; Name='NETSİS KONTROL ET6' },
    @{ SKU='OR-AA22-01'; Name='NETSİS KONTROL ET7' },
    @{ SKU='OR-AA24-01'; Name='NETSİS KONTROL ET8' },
    @{ SKU='1-18447-KOVAN'; Name='NETSİSTEN KONTROL 5051' },
    @{ SKU='1-1020-KOVAN'; Name='NETSİSTEN KONTROL 5054' },
    @{ SKU='A1121-EM'; Name='NETSİSTEN KONTROL 5055' },
    @{ SKU='MZ11.5650471'; Name='Ø35 BAKIR BORU-7594913C' },
    @{ SKU='PIPE_52_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_8_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_6_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_24_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_26_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_25_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_4_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_3_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_31_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_5_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_51_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_23_VWGH_160_V5'; Name='Ø35*1,5 PIPE' },
    @{ SKU='PIPE_41_VWGH_160_V5'; Name='Ø54*2 BAKIR BORU' },
    @{ SKU='P-TKZ-SAG-B12'; Name='PİRİNÇ TAKOZ ( 22*39  boy : 29 mm ) DJ COOL' },
    @{ SKU='BFM-07'; Name='PİRİNÇ TALAŞ' },
    @{ SKU='silll12344'; Name='silll' },
    @{ SKU='sillll3l3ll3l3l'; Name='silllllllll' },
    @{ SKU='TEST-001'; Name='Test ÃœrÃ¼nÃ¼' },
    @{ SKU='TEST_20251120193650'; Name='Test Ürün - Otomatik' },
    @{ SKU='TEST_20251120193736'; Name='Test Ürün - Otomatik' },
    @{ SKU='TEST_20251120193743'; Name='Test Ürün - Otomatik' },
    @{ SKU='TEST_20251120193751'; Name='Test Ürün - Otomatik' }
)

Write-Output "Logging in to Koza and switching branch..."
$sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try {
    $loginJson = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Compress
    $login = Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body $loginJson -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Write-Output "Koza login: $($login.StatusCode)"
} catch { Write-Error ("Koza login failed: {0}" -f $_.Exception.Message); exit 1 }

try {
    $branchResp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body (@{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress) -ContentType 'application/json' -WebSession $sess -UseBasicParsing -ErrorAction Stop
    Write-Output "Branch change: $($branchResp.StatusCode)"
} catch { Write-Warning ("Branch change failed: {0}" -f $_.Exception.Message) }

$results = @()
foreach ($item in $skuList) {
    $sku = $item.SKU
    $name = $item.Name
    Write-Output "Checking SKU: $sku (expect name: $name)"

    $body = @{ stkSkart = @{ kod = $sku } } | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $body -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        $rawFile = Join-Path $outDir ("raw-kod-$($sku -replace '[\\/:*?\"<>|]','_').json")
        $resp | ConvertTo-Json -Depth 10 | Out-File $rawFile -Encoding utf8

        $found = $false
        $record = $null
        if ($resp -and $resp.PSObject.Properties.Match('stkSkart') -and $resp.stkSkart.Count -gt 0) {
            foreach ($r in $resp.stkSkart) { if ($r.kod -eq $sku -or $r.kod -eq [string]$sku) { $found = $true; $record = $r; break } }
        } elseif ($resp -and $resp.PSObject.Properties.Match('data') -and $resp.data.Count -gt 0) {
            foreach ($r in $resp.data) { if ($r.kod -eq $sku -or $r.kod -eq [string]$sku) { $found = $true; $record = $r; break } }
        } else {
            if ($resp -is [System.Collections.IEnumerable]) { foreach ($r in $resp) { if ($r.kod -eq $sku) { $found = $true; $record = $r; break } } }
        }

        if ($found) { Write-Output "FOUND SKU: $sku -> skartId=$($record.skartId)" } else { Write-Output "NOT FOUND SKU: $sku" }
        $results += [pscustomobject]@{ SKU=$sku; Name=$name; found=$found; record=$record; raw=$rawFile }
    } catch {
        Write-Warning ("Lookup failed for SKU {0}: {1}" -f $sku, $_.Exception.Message)
        $results += [pscustomobject]@{ SKU=$sku; Name=$name; found=$false; record=$null; error=$_.Exception.Message }
    }
}

$out = Join-Path $outDir 'verify-user-skus-results.json'
$results | ConvertTo-Json -Depth 10 | Out-File $out -Encoding utf8
Write-Output "User-SKU verify run complete. Results: $out"
