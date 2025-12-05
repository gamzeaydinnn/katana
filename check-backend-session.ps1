# Check Backend Session Status
# Quick script to verify Luca session is working

Write-Host "=== BACKEND SESSION STATUS ===" -ForegroundColor Cyan
Write-Host ""

# Check if backend is running
Write-Host "Checking backend status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -Method Get -TimeoutSec 5
    Write-Host "✅ Backend is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Backend is not responding" -ForegroundColor Red
    Write-Host "   Run: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Checking recent backend logs for session info..." -ForegroundColor Yellow
Write-Host ""

# Get last 100 lines of backend logs
$logs = docker-compose logs --tail=100 backend 2>&1

# Check for key indicators
$lucaCacheLog = $logs | Select-String "Luca cache hazır" | Select-Object -Last 1
$htmlErrorLog = $logs | Select-String "Still HTML after retry" | Select-Object -Last 1
$loginSuccessLog = $logs | Select-String "Login SUCCESS: JSESSIONID" | Select-Object -Last 1
$sessionRefreshLog = $logs | Select-String "ForceSessionRefreshAsync" | Select-Object -Last 1

Write-Host "📊 Session Status:" -ForegroundColor Cyan
Write-Host ""

if ($lucaCacheLog) {
    $cacheMatch = $lucaCacheLog -match "(\d+) stok kartı"
    if ($cacheMatch -and $Matches[1] -gt 0) {
        Write-Host "✅ Luca Cache: $($Matches[1]) stock cards loaded" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Luca Cache: 0 stock cards (session issue)" -ForegroundColor Yellow
    }
    Write-Host "   $lucaCacheLog" -ForegroundColor Gray
} else {
    Write-Host "⚠️  No cache log found (backend may not have started yet)" -ForegroundColor Yellow
}

Write-Host ""

if ($htmlErrorLog) {
    Write-Host "❌ HTML Response Error Detected:" -ForegroundColor Red
    Write-Host "   $htmlErrorLog" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   This means Luca API is returning HTML instead of JSON" -ForegroundColor Yellow
    Write-Host "   Possible causes:" -ForegroundColor Yellow
    Write-Host "   - Invalid ManualSessionCookie in appsettings.json" -ForegroundColor White
    Write-Host "   - Session expired" -ForegroundColor White
    Write-Host "   - Authentication failed" -ForegroundColor White
} else {
    Write-Host "✅ No HTML response errors" -ForegroundColor Green
}

Write-Host ""

if ($loginSuccessLog) {
    Write-Host "✅ Recent Login Success:" -ForegroundColor Green
    Write-Host "   $loginSuccessLog" -ForegroundColor Gray
} else {
    Write-Host "⚠️  No recent login success found" -ForegroundColor Yellow
}

Write-Host ""

if ($sessionRefreshLog) {
    Write-Host "🔄 Session Refresh Activity:" -ForegroundColor Cyan
    Write-Host "   $sessionRefreshLog" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== RECOMMENDATIONS ===" -ForegroundColor Cyan
Write-Host ""

# Analyze and provide recommendations
$hasIssue = $false

if ($htmlErrorLog) {
    $hasIssue = $true
    Write-Host "1. ❌ Fix session issue:" -ForegroundColor Red
    Write-Host "   - Check appsettings.json ManualSessionCookie value" -ForegroundColor White
    Write-Host "   - Should be empty '' or valid JSESSIONID" -ForegroundColor White
    Write-Host "   - Restart backend: docker-compose restart backend" -ForegroundColor White
}

if ($lucaCacheLog -and $lucaCacheLog -match "0 stok kartı") {
    $hasIssue = $true
    Write-Host "2. ⚠️  Empty cache detected:" -ForegroundColor Yellow
    Write-Host "   - Verify Luca API credentials in appsettings.json" -ForegroundColor White
    Write-Host "   - Check Luca API is accessible" -ForegroundColor White
    Write-Host "   - Review full logs: docker-compose logs backend | grep Luca" -ForegroundColor White
}

if (-not $hasIssue) {
    Write-Host "✅ Everything looks good!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run sync test: .\test-luca-session-fix.ps1" -ForegroundColor White
    Write-Host "2. Check for sill products: .\check-sill-in-luca.ps1" -ForegroundColor White
}

Write-Host ""
