param(
    [Parameter(Mandatory=$false)]
    [string] $Cookie,
    [switch] $RunTest,
    [string] $BaseUrl = 'http://localhost:5055',
    [int] $Take = 10,
    [int] $WaitSeconds = 6
)

if (-not $Cookie) {
    Write-Host "Usage: .\set-luca-cookie.ps1 -Cookie 'firstLoad=done; JSESSIONID=...' [-RunTest] [-BaseUrl 'http://localhost:5055']" -ForegroundColor Yellow
    return
}

# Set the env var for the current PowerShell session so ASP.NET config binding picks it up
$env:LucaApi__ManualSessionCookie = $Cookie

Write-Host "Set environment variable `LucaApi__ManualSessionCookie` for this session." -ForegroundColor Green
Write-Host "Cookie (truncated): $($Cookie.Substring(0, [Math]::Min(60, $Cookie.Length)))..."

if ($RunTest) {
    Write-Host "Running test script `scripts/test-katana-to-luca.ps1` with -BaseUrl $BaseUrl -Take $Take -WaitSeconds $WaitSeconds" -ForegroundColor Cyan
    & .\scripts\test-katana-to-luca.ps1 -BaseUrl $BaseUrl -Take $Take -WaitSeconds $WaitSeconds
}

Write-Host "Note: This sets the variable only for the current PowerShell process. Close the shell to clear it." -ForegroundColor Yellow
