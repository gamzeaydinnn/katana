param(
    [string]$SqlHost = 'localhost',
    [int]$SqlPort = 1433,
    [string]$DbName = 'KatanaLucaIntegration',
    [string]$SaPassword = 'Admin00!S'
)

Write-Host "=== Apply EF Migrations helper ==="

function Ensure-DotnetEf {
    $installed = & dotnet tool list -g 2>$null | Select-String -Pattern 'dotnet-ef' -Quiet
    if (-not $installed) {
        Write-Host "dotnet-ef not found; installing global tool (requires internet)." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef --version 8.0.0
    }
}

function Check-DbPort {
    Write-Host "Checking SQL Server reachable at $SqlHost`:$SqlPort ..."
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $async = $tcp.BeginConnect($SqlHost, $SqlPort, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne(3000)
        if (-not $ok) { $tcp.Close(); return $false }
        $tcp.EndConnect($async)
        $tcp.Close()
        return $true
    } catch { return $false }
}

Ensure-DotnetEf

if (-not (Check-DbPort)) {
    Write-Host "Cannot reach SQL Server at $($SqlHost)`:$($SqlPort). Make sure docker compose is up and port is published." -ForegroundColor Red
    Write-Host "Run: docker compose up -d and wait for DB startup logs ('ready for client connections')."
    exit 2
}

# Set temporary environment variable so dotnet ef picks it up
$env:ConnectionStrings__SqlServerConnection = "Server=$SqlHost,$SqlPort;Database=$DbName;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;"

Write-Host "Using connection: Server=$SqlHost,$SqlPort;Database=$DbName;User=sa"

Push-Location $PSScriptRoot/..\
try {
    # Normalize path to repository root
    $repoRoot = (Get-Location).ProviderPath
    Write-Host "Repository root: $repoRoot"

    $project = 'src/Katana.Data'
    $startup = 'src/Katana.API'

    Write-Host "Listing migrations (project: $project, startup: $startup) ..."
    & dotnet ef migrations list --project $project --startup-project $startup

    Write-Host "Applying migrations to database '$DbName'..." -ForegroundColor Cyan
    $rc = & dotnet ef database update --project $project --startup-project $startup
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet ef database update failed." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Migrations applied successfully." -ForegroundColor Green

    Write-Host "Restarting API container (docker compose) to pick new DB state..."
    docker compose restart api
    Write-Host "Waiting 5s for API to warm up..."
    Start-Sleep -Seconds 5

    Write-Host "Test API: GET /api/Products (should return 200 or empty list)"
    try {
        $resp = Invoke-RestMethod -Uri 'http://localhost:8080/api/Products' -Method Get -TimeoutSec 10
        $count = if ($resp -is [System.Collections.IEnumerable]) { ($resp | Measure-Object).Count } else { 1 }
        Write-Host "API responded. Items count: $count" -ForegroundColor Green
    } catch {
        Write-Host "API test failed: $_" -ForegroundColor Yellow
    }
} finally {
    Pop-Location
}

Write-Host "=== Done ==="
