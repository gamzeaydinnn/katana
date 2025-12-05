# Quick check of current status
$baseUrl = "http://localhost:5055"

Write-Host "=== QUICK STATUS CHECK ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check backend
Write-Host "1. Backend Status:" -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "$baseUrl/health" -TimeoutSec 5
    Write-Host "   ✅ Backend running" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Backend not responding" -ForegroundColor Red
    exit 1
}

# 2. Login and check stock cards
Write-Host ""
Write-Host "2. Stock Cards Count:" -ForegroundColor Yellow
try {
    $loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }
    
    $cards = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers
    $count = if ($cards -is [Array]) { $cards.Count } else { if ($cards) { 1 } else { 0 } }
    
    if ($count -eq 0) {
        Write-Host "   ❌ 0 stock cards (PROBLEM PERSISTS)" -ForegroundColor Red
    } else {
        Write-Host "   ✅ $count stock cards loaded" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Failed: $_" -ForegroundColor Red
}

# 3. Check logs quickly
Write-Host ""
Write-Host "3. Recent Log Snippets:" -ForegroundColor Yellow
$logs = docker-compose logs --tail=30 backend 2>&1

$htmlError = $logs | Select-String "HTML" | Select-Object -Last 1
$cacheLog = $logs | Select-String "cache hazır" | Select-Object -Last 1
$authLog = $logs | Select-String "Login SUCCESS|EnsureAuthenticated" | Select-Object -Last 1

if ($htmlError) {
    Write-Host "   ❌ HTML Error: $htmlError" -ForegroundColor Red
}

if ($cacheLog) {
    Write-Host "   📊 Cache: $cacheLog" -ForegroundColor Cyan
}

if ($authLog) {
    Write-Host "   🔐 Auth: $authLog" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "For detailed diagnosis, run: .\diagnose-luca-session.ps1" -ForegroundColor Yellow
Write-Host ""
