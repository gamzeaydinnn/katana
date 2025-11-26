param(
    [int]$Count = 50,
    [switch]$DryRun = $true,
    [string]$KatanaApiBaseUrl = 'http://localhost:5000/',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$KatanaApiToken = ''
)

# Generate N sample products
Write-Output "Generating $Count sample products..."
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$genScript = Join-Path $scriptDir 'generate-sample-products.ps1'
 $gen = & $genScript -Count $Count
 if (-not $gen) { Write-Error "Generation failed"; exit 1 }
 # generator prints a message and returns the file path; pick last output as the path
 if ($gen -is [System.Array]) { $genFile = $gen[-1] } else { $genFile = $gen }
 Write-Output "Generated file: $genFile"

# Run sync script once using generated file
$dryFlag = $false
if ($DryRun) { $dryFlag = $true }

$paramHash = @{
    ProductsFile = $genFile
    KozaBaseUrl = $KozaBaseUrl
    OrgCode = '1422649'
    Username = 'Admin'
    Password = 'WebServis'
    BranchId = 854
}
if ($KatanaApiToken) { $paramHash.KatanaApiToken = $KatanaApiToken }
if ($dryFlag) { $paramHash.DryRun = $true }

Write-Output "Running sync with: $($scriptDir)\sync-katana-to-luca.ps1 (DryRun=$($paramHash.DryRun -ne $null))"
$syncScript = Join-Path $scriptDir 'sync-katana-to-luca.ps1'
& $syncScript @paramHash

Write-Output "Run complete."