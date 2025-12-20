param(
    [string]$ScriptPath = '..\sync-katana-to-luca.ps1',
    [string]$Args = "-PreviewOnly -Limit 50 -KatanaApiBaseUrl '<use-appsettings>' -KatanaApiToken '<use-appsettings>' -KozaBaseUrl 'http://85.111.1.49:57005/Yetki/'"
)

$full = Join-Path $PSScriptRoot $ScriptPath
if (-not (Test-Path $full)) { Write-Error "Script not found: $full"; exit 1 }
$temp = Join-Path $PSScriptRoot "sync-katana-to-luca.temp.ps1"

# Read and strip markdown fences
$lines = Get-Content -Raw -Path $full -ErrorAction Stop -Encoding UTF8
$clean = ($lines -split "`n") | Where-Object { ($_ -notmatch '^```') }
$clean -join "`n" | Set-Content -Path $temp -Encoding UTF8

Write-Host "Wrote temporary script: $temp"

# Execute temp script with passed args
$cmd = "& `"$temp`" $Args"
Write-Host "Running: $cmd"
Invoke-Expression $cmd

# Clean up temp file
Remove-Item -Path $temp -Force -ErrorAction SilentlyContinue
Write-Host "Done. Temp removed."