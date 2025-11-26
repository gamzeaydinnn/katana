param(
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path (Get-Location) "scripts\\logs\\koza-verify-specific-$ts"
New-Item -Path $outDir -ItemType Directory -Force | Out-Null

Write-Output "Preparing mapping list..."
$map = @(
    @{ SKU='AUTO-1beaac29'; skartId=79426 },
    @{ SKU='AUTO-03d066d7'; skartId=79427 },
    @{ SKU='AUTO-607d298f'; skartId=79428 },
    @{ SKU='AUTO-4e115bd3'; skartId=79429 },
    @{ SKU='AUTO-21ed45f9'; skartId=79430 },
    @{ SKU='AUTO-426fd2bf'; skartId=79431 },
    @{ SKU='AUTO-c0e5d819'; skartId=79432 },
    @{ SKU='AUTO-f538d225'; skartId=79433 },
    @{ SKU='AUTO-a313b3b5'; skartId=79434 },
    @{ SKU='AUTO-05b7c7eb'; skartId=79435 },
    @{ SKU='AUTO-fd5bf688'; skartId=79436 },
    @{ SKU='AUTO-eded2cde'; skartId=79437 },
    @{ SKU='AUTO-c199ace7'; skartId=79438 },
    @{ SKU='AUTO-4037eab1'; skartId=79439 },
    @{ SKU='AUTO-41a78a73'; skartId=79440 },
    @{ SKU='AUTO-fe63c29f'; skartId=79441 },
    @{ SKU='AUTO-d8d751f8'; skartId=79442 },
    @{ SKU='AUTO-c94fce36'; skartId=79443 },
    @{ SKU='AUTO-556110e1'; skartId=79444 },
    @{ SKU='AUTO-ffae0077'; skartId=79445 }
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
foreach ($t in $map) {
    $sku = $t.SKU
    $id = $t.skartId
    Write-Output "Checking skartId: $id (SKU: $sku)"
    # check by skartId
    $bodyId = @{ stkSkart = @{ skartId = $id } } | ConvertTo-Json -Compress
    try {
        $respId = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $bodyId -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        $rawFileId = Join-Path $outDir ("raw-skartid-$id.json")
        $respId | ConvertTo-Json -Depth 10 | Out-File $rawFileId -Encoding utf8
        $foundById = $false; $recordId = $null
        if ($respId -and $respId.PSObject.Properties.Match('stkSkart') -and $respId.stkSkart.Count -gt 0) { foreach ($r in $respId.stkSkart) { if ($r.skartId -eq $id) { $foundById = $true; $recordId = $r; break } } }
        elseif ($respId -and $respId.PSObject.Properties.Match('data') -and $respId.data.Count -gt 0) { foreach ($r in $respId.data) { if ($r.skartId -eq $id) { $foundById = $true; $recordId = $r; break } } }
    } catch { Write-Warning ("skartId lookup failed for {0}: {1}" -f $id, $_.Exception.Message); $foundById=$false; $recordId=$null }

    # check by kod
    Write-Output "Checking SKU: $sku (by kod)"
    $bodyKod = @{ stkSkart = @{ kod = $sku } } | ConvertTo-Json -Compress
    try {
        $respKod = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $bodyKod -ContentType 'application/json' -WebSession $sess -ErrorAction Stop
        $rawFileKod = Join-Path $outDir ("raw-kod-$($sku -replace '[\\/:*?\"<>|]','_').json")
        $respKod | ConvertTo-Json -Depth 10 | Out-File $rawFileKod -Encoding utf8
        $foundByKod = $false; $recordKod = $null
        if ($respKod -and $respKod.PSObject.Properties.Match('stkSkart') -and $respKod.stkSkart.Count -gt 0) { foreach ($r in $respKod.stkSkart) { if ($r.kod -eq $sku) { $foundByKod = $true; $recordKod = $r; break } } }
        elseif ($respKod -and $respKod.PSObject.Properties.Match('data') -and $respKod.data.Count -gt 0) { foreach ($r in $respKod.data) { if ($r.kod -eq $sku) { $foundByKod = $true; $recordKod = $r; break } } }
    } catch { Write-Warning ("kod lookup failed for {0}: {1}" -f $sku, $_.Exception.Message); $foundByKod=$false; $recordKod=$null }

    $results += [pscustomobject]@{ SKU=$sku; skartId=$id; foundBySkartId=$foundById; recordBySkartId=$recordId; foundByKod=$foundByKod; recordByKod=$recordKod; rawSkartId=$rawFileId; rawKod=$rawFileKod }
}

$out = Join-Path $outDir 'verify-specific-results.json'
$results | ConvertTo-Json -Depth 10 | Out-File $out -Encoding utf8
Write-Output "Specific verify run complete. Results: $out"
