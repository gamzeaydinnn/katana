param(
    [int]$Count = 50,
    [int]$IntervalSeconds = 60,
    [string]$KatanaApiBaseUrl = 'http://localhost:5000/',
    [string]$KozaBaseUrl = 'http://85.111.1.49:57005/Yetki/',
    [string]$KatanaApiToken = ''
)

while ($true) {
    Write-Output "Loop iteration: generating and running sync at $(Get-Date)"
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $runOnce = Join-Path $scriptDir 'run-sync-once.ps1'
    & $runOnce -Count $Count -DryRun -KatanaApiBaseUrl $KatanaApiBaseUrl -KozaBaseUrl $KozaBaseUrl -KatanaApiToken $KatanaApiToken
    Write-Output "Sleeping $IntervalSeconds seconds..."
    Start-Sleep -Seconds $IntervalSeconds
}
