<#
Push multiple products from a SKU list to Koza/Luca using the existing test harness script.
Place this file in the repository `scripts/` folder and run from the repo root or run directly from PowerShell.

Usage examples:

# Push SKUs from a file (one SKU per line):
# powershell -ExecutionPolicy Bypass -File .\scripts\push-products-to-luca.ps1 \
#   -SessionCookie 'JSESSIONID=ts-8Y-jORgR6hQrcb6O1wYJMV3xtJrVWYuVqS-TYCPwe3vrfClmG!735453627' \
#   -ForcedBranchId 864 -BaseUrl 'http://85.111.1.49:57005/Yetki/' -OrgCode 1422649 -UserName Admin -Password WebServis \
#   -SkuFile '.\scripts\skus.txt' -DelayMs 500

# Push a single SKU:
# powershell -ExecutionPolicy Bypass -File .\scripts\push-products-to-luca.ps1 \
#   -SessionCookie 'JSESSIONID=...' -ForcedBranchId 864 -BaseUrl 'http://85.111.1.49:57005/Yetki/' -OrgCode 1422649 -UserName Admin -Password WebServis \
#   -SingleSku 'MY-SKU-1'
#
# Notes:
# - This script relies on the repository's `test-luca-session-clean.ps1` (or `test-luca-session.ps1`) harness.
# - Each SKU run is logged to `scripts/logs/push-<timestamp>/SKU.log` and the raw request/response artifacts are still saved by the harness.
# - It injects the provided `SessionCookie` (JSESSIONID=...) and `ForcedBranchId` into each run.
#
# Parameters:
#>
param(
    [Parameter(Mandatory=$true)] [string] $SessionCookie,
    [Parameter(Mandatory=$true)] [int] $ForcedBranchId,
    [Parameter(Mandatory=$true)] [string] $BaseUrl,
    [Parameter(Mandatory=$true)] [int] $OrgCode,
    [Parameter(Mandatory=$true)] [string] $UserName,
    [Parameter(Mandatory=$true)] [string] $Password,
    [string] $SkuFile = "",
    [string] $SingleSku = "",
    [int] $DelayMs = 500,
    [switch] $DryRun
)

Set-StrictMode -Version Latest

# Determine helper harness path (prefer clean script)
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$harnessCandidates = @("test-luca-session-clean.ps1", "test-luca-session.ps1")
$harnessPath = $null
foreach ($c in $harnessCandidates) {
    $p = Join-Path $scriptRoot $c
    if (Test-Path $p) { $harnessPath = $p; break }
}
if (-not $harnessPath) {
    Write-Error "Could not find test harness script (test-luca-session-clean.ps1 or test-luca-session.ps1) in $scriptRoot."
    exit 1
}

# Read SKU list
$skus = @()
if ($SingleSku -ne "") {
    $skus += $SingleSku.Trim()
}
elseif ($SkuFile -ne "") {
    if (-not (Test-Path $SkuFile)) {
        Write-Error "SkuFile not found: $SkuFile"
        exit 1
    }
    $lines = Get-Content $SkuFile -ErrorAction Stop
    foreach ($ln in $lines) {
        $s = $ln.Trim()
        if ([string]::IsNullOrWhiteSpace($s)) { continue }
        if ($s.StartsWith('#')) { continue }
        $skus += $s
    }
}
else {
    Write-Error "Provide either -SkuFile or -SingleSku"
    exit 1
}

if ($skus.Count -eq 0) {
    Write-Host "No SKUs to process. Exiting." -ForegroundColor Yellow
    exit 0
}

# Prepare log dir
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = Join-Path $scriptRoot "logs\push-$timestamp"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

Write-Host "Found harness: $harnessPath"
Write-Host "Will process $($skus.Count) SKU(s). Logs -> $logDir"

if ($DryRun) {
    Write-Host "DRY RUN - commands that would be executed:" -ForegroundColor Cyan
    foreach ($sku in $skus) {
        $example = "& `"$harnessPath`" -BaseUrl '$BaseUrl' -OrgCode $OrgCode -UserName '$UserName' -Password '<REDACTED>' -ForcedBranchId $ForcedBranchId -ProductSKU '$sku' -ProductName '$sku' -SessionCookie 'JSESSIONID=...'
"
        Write-Host $example
    }
    exit 0
}

foreach ($sku in $skus) {
    try {
        Write-Host "--- Processing SKU: $sku ---" -ForegroundColor Green
        $outFile = Join-Path $logDir "${sku}.log"
        Write-Host "Running harness: $harnessPath (SKU: $sku)"

        # Call the harness script directly with strongly-typed parameters to avoid shell parsing issues.
        try {
            & $harnessPath -BaseUrl $BaseUrl -OrgCode $OrgCode -UserName $UserName -Password $Password -ForcedBranchId $ForcedBranchId -ProductSKU $sku -ProductName $sku -SessionCookie $SessionCookie 2>&1 | Tee-Object -FilePath $outFile
            Write-Host "Saved log: $outFile"
        }
        catch {
            $err = $_
            Write-Warning ("Harness invocation failed for {0}: {1}" -f $sku, $err)
        }
    }
    catch {
        $err = $_
        Write-Warning ("Error while processing {0}: {1}" -f $sku, $err)
    }

    Start-Sleep -Milliseconds $DelayMs
}

Write-Host "Done. Check harness logs and $logDir for details." -ForegroundColor Cyan
# End of script
