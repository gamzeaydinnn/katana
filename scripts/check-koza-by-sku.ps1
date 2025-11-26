param(
    [string]$LogDir = './scripts/logs/sync-katana-to-luca-20251126-142426',
    [int]$Limit = 10,
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$OrgCode = '1422649',
    [string]$Username = 'Admin',
    [string]$Password = 'WebServis',
    [int]$BranchId = 854
)

$successFile = Join-Path $LogDir 'success.json'
if (-not (Test-Path $successFile)) { Write-Error "success.json not found in $LogDir"; exit 1 }
$items = Get-Content $successFile -Raw | ConvertFrom-Json
$items = $items | Select-Object -First $Limit

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPayload = @{ orgCode = $OrgCode; userName = $Username; userPassword = $Password } | ConvertTo-Json -Compress
$login = Invoke-WebRequest -Uri ($KozaBaseUrl + 'Giris.do') -Method Post -Body $loginPayload -ContentType 'application/json' -WebSession $session -UseBasicParsing
$branchJson = @{ orgSirketSubeId = $BranchId } | ConvertTo-Json -Compress
$branchResp = Invoke-WebRequest -Uri ($KozaBaseUrl + 'GuncelleYtkSirketSubeDegistir.do') -Method Post -Body $branchJson -ContentType 'application/json' -WebSession $session -UseBasicParsing

foreach ($it in $items) {
    $sku = $it.SKU
    Write-Output "\n=== Querying SKU: $sku ==="
    $queryBody = @{ stkSkart = @{ kodBas = $sku; kodBit = $sku; kodOp = 'between' } } | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Uri ($KozaBaseUrl + 'ListeleStkSkart.do') -Method Post -Body $queryBody -ContentType 'application/json' -WebSession $session -ErrorAction Stop
        if ($resp -and $resp.stkSkart -and $resp.stkSkart.Count -gt 0) {
            foreach ($item in $resp.stkSkart) { $item | ConvertTo-Json -Depth 5 | Write-Output }
        } elseif ($resp -and $resp.data -and $resp.data.Count -gt 0) {
            foreach ($item in $resp.data) { $item | ConvertTo-Json -Depth 5 | Write-Output }
        } else {
            Write-Warning "No data returned for SKU $sku"
        }
    } catch {
        Write-Warning ("Query failed for {0}: {1}" -f $sku, $_.Exception.Message)
    }
}

Write-Output "Done."