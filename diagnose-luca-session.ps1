# Diagnose Luca Session Issue
# Deep dive into why we're still getting 0 stock cards

Write-Host "=== LUCA SESSION DIAGNOSIS ===" -ForegroundColor Cyan
Write-Host ""

# Get comprehensive backend logs
Write-Host "Fetching backend logs (last 200 lines)..." -ForegroundColor Yellow
$logs = docker-compose logs --tail=200 backend 2>&1 | Out-String

# Save logs to file for analysis
$logs | Out-File "backend-diagnosis.log" -Encoding UTF8
Write-Host "✅ Logs saved to backend-diagnosis.log" -ForegroundColor Green
Write-Host ""

# Analyze key patterns
Write-Host "=== ANALYSIS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check authentication
Write-Host "1. Authentication Status:" -ForegroundColor Yellow
$authLogs = $logs | Select-String -Pattern "EnsureAuthenticatedAsync|Login|JSESSIONID" -Context 0,1
if ($authLogs) {
    $authLogs | Select-Object -First 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ⚠️  No authentication logs found" -ForegroundColor Yellow
}
Write-Host ""

# 2. Check manual cookie
Write-Host "2. Manual Cookie Configuration:" -ForegroundColor Yellow
$manualCookieLogs = $logs | Select-String -Pattern "ManualSessionCookie|FILL_ME|ManualCookieValid"
if ($manualCookieLogs) {
    $manualCookieLogs | Select-Object -First 3 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ℹ️  No manual cookie logs" -ForegroundColor Cyan
}
Write-Host ""

# 3. Check ListStockCardsAsync
Write-Host "3. ListStockCardsAsync Calls:" -ForegroundColor Yellow
$listLogs = $logs | Select-String -Pattern "ListStockCardsAsync"
if ($listLogs) {
    $listLogs | Select-Object -First 10 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ⚠️  No ListStockCardsAsync logs found" -ForegroundColor Yellow
}
Write-Host ""

# 4. Check HTML responses
Write-Host "4. HTML Response Errors:" -ForegroundColor Yellow
$htmlLogs = $logs | Select-String -Pattern "HTML|html|<html|<!DOCTYPE"
if ($htmlLogs) {
    Write-Host "   ❌ HTML responses detected!" -ForegroundColor Red
    $htmlLogs | Select-Object -First 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ✅ No HTML response errors" -ForegroundColor Green
}
Write-Host ""

# 5. Check cache initialization
Write-Host "5. Cache Initialization:" -ForegroundColor Yellow
$cacheLogs = $logs | Select-String -Pattern "Luca cache|cache hazır|stok kartı yüklendi"
if ($cacheLogs) {
    $cacheLogs | ForEach-Object {
        if ($_ -match "0 stok kartı") {
            Write-Host "   ❌ $_" -ForegroundColor Red
        } else {
            Write-Host "   $_" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "   ⚠️  No cache initialization logs" -ForegroundColor Yellow
}
Write-Host ""

# 6. Check for errors
Write-Host "6. Recent Errors:" -ForegroundColor Yellow
$errorLogs = $logs | Select-String -Pattern "\[ERR\]|\[ERROR\]|Exception|Failed" | Select-Object -Last 10
if ($errorLogs) {
    $errorLogs | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Red
    }
} else {
    Write-Host "   ✅ No recent errors" -ForegroundColor Green
}
Write-Host ""

# 7. Check session state
Write-Host "7. Session State:" -ForegroundColor Yellow
$sessionLogs = $logs | Select-String -Pattern "IsAuthenticated|HasSession|CookieExpiry|ForceSessionRefresh"
if ($sessionLogs) {
    $sessionLogs | Select-Object -Last 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ⚠️  No session state logs" -ForegroundColor Yellow
}
Write-Host ""

# 8. Check Luca API endpoint
Write-Host "8. Luca API Endpoint Calls:" -ForegroundColor Yellow
$endpointLogs = $logs | Select-String -Pattern "ListeleStkSkart|StockCards|Endpoints\.StockCards"
if ($endpointLogs) {
    $endpointLogs | Select-Object -First 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ⚠️  No endpoint call logs" -ForegroundColor Yellow
}
Write-Host ""

# Summary and recommendations
Write-Host "=== RECOMMENDATIONS ===" -ForegroundColor Cyan
Write-Host ""

$issues = @()

if ($htmlLogs) {
    $issues += "HTML responses detected - session authentication failing"
}

if ($cacheLogs -and ($cacheLogs | Select-String "0 stok kartı")) {
    $issues += "Cache is empty - API calls not returning data"
}

if (-not $authLogs) {
    $issues += "No authentication logs - service may not be initializing"
}

if ($issues.Count -eq 0) {
    Write-Host "✅ No obvious issues detected" -ForegroundColor Green
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "- Luca API credentials incorrect" -ForegroundColor White
    Write-Host "- Luca API endpoint changed" -ForegroundColor White
    Write-Host "- Network connectivity issue" -ForegroundColor White
    Write-Host "- Branch selection failing" -ForegroundColor White
} else {
    Write-Host "❌ Issues detected:" -ForegroundColor Red
    $issues | ForEach-Object {
        Write-Host "   - $_" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review backend-diagnosis.log for full details" -ForegroundColor White
Write-Host "2. Check appsettings.json Luca credentials" -ForegroundColor White
Write-Host "3. Verify Luca API is accessible" -ForegroundColor White
Write-Host "4. Try manual login to Luca web interface" -ForegroundColor White
Write-Host ""
