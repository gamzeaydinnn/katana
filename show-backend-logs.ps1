# Show relevant backend logs

Write-Host "=== BACKEND LOGS (Last 50 lines) ===" -ForegroundColor Cyan
Write-Host ""

docker-compose logs --tail=50 backend 2>&1 | ForEach-Object {
    $line = $_
    
    # Color code based on content
    if ($line -match "\[ERR\]|ERROR|Exception|Failed") {
        Write-Host $line -ForegroundColor Red
    }
    elseif ($line -match "\[WRN\]|WARNING|HTML") {
        Write-Host $line -ForegroundColor Yellow
    }
    elseif ($line -match "SUCCESS|✅|Login SUCCESS|cache hazır") {
        Write-Host $line -ForegroundColor Green
    }
    elseif ($line -match "ListStockCardsAsync|EnsureAuthenticated|ForceSessionRefresh") {
        Write-Host $line -ForegroundColor Cyan
    }
    else {
        Write-Host $line -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== KEY INDICATORS ===" -ForegroundColor Cyan
Write-Host ""

$logs = docker-compose logs --tail=100 backend 2>&1 | Out-String

# Check for specific patterns
$patterns = @{
    "Cache Status" = "Luca cache hazır.*stok kartı"
    "HTML Errors" = "Still HTML after retry"
    "Auth Success" = "Login SUCCESS"
    "Session Refresh" = "ForceSessionRefreshAsync"
    "Manual Cookie" = "ManualCookieValid"
}

foreach ($key in $patterns.Keys) {
    $pattern = $patterns[$key]
    $matches = [regex]::Matches($logs, $pattern)
    
    if ($matches.Count -gt 0) {
        Write-Host "$key`: Found $($matches.Count) occurrence(s)" -ForegroundColor Cyan
        $matches | Select-Object -Last 1 | ForEach-Object {
            Write-Host "   $($_.Value)" -ForegroundColor Gray
        }
    } else {
        Write-Host "$key`: Not found" -ForegroundColor Yellow
    }
}

Write-Host ""
