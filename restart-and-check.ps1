# Complete Restart and Check
# This script does everything: restart, apply migrations, check status

Write-Host "=== COMPLETE RESTART & CHECK ===" -ForegroundColor Cyan
Write-Host ""

# 1. Stop everything
Write-Host "1. Stopping containers..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 2
Write-Host "   ✅ Stopped" -ForegroundColor Green

# 2. Start everything (migrations will auto-apply)
Write-Host ""
Write-Host "2. Starting containers (migrations will auto-apply)..." -ForegroundColor Yellow
docker-compose up -d
Write-Host "   ✅ Started" -ForegroundColor Green

# 3. Wait for backend
Write-Host ""
Write-Host "3. Waiting for backend..." -ForegroundColor Yellow
$maxWait = 60
$waited = 0
$ready = $false

while ($waited -lt $maxWait -and -not $ready) {
    Start-Sleep -Seconds 3
    $waited += 3
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "   ✅ Backend ready ($waited seconds)" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⏳ Waiting... ($waited/$maxWait)" -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "   ❌ Backend timeout" -ForegroundColor Red
    Write-Host ""
    Write-Host "Logs:" -ForegroundColor Yellow
    docker-compose logs --tail=30 backend
    exit 1
}

# 4. Wait for Luca cache
Write-Host ""
Write-Host "4. Waiting for Luca cache initialization..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# 5. Check logs
Write-Host ""
Write-Host "5. Checking logs..." -ForegroundColor Yellow
$logs = docker-compose logs --tail=100 backend 2>&1 | Out-String

# Migration check
if ($logs -match "Applying migration") {
    Write-Host "   ✅ Migrations applied" -ForegroundColor Green
} elseif ($logs -match "No migrations") {
    Write-Host "   ℹ️  No new migrations to apply" -ForegroundColor Cyan
}

# Cache check
if ($logs -match "Luca cache hazır.*?(\d+) stok kartı") {
    $count = $Matches[1]
    if ([int]$count -gt 0) {
        Write-Host "   ✅ Luca cache: $count cards" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Luca cache: 0 cards (PROBLEM)" -ForegroundColor Red
    }
}

# HTML error check
if ($logs -match "Still HTML after retry") {
    Write-Host "   ❌ HTML response errors detected" -ForegroundColor Red
} else {
    Write-Host "   ✅ No HTML errors" -ForegroundColor Green
}

# Auth check
if ($logs -match "Login SUCCESS") {
    Write-Host "   ✅ Authentication successful" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  No login success log" -ForegroundColor Yellow
}

# 6. Test API
Write-Host ""
Write-Host "6. Testing API..." -ForegroundColor Yellow
try {
    $loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5055/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }
    
    $cards = Invoke-RestMethod -Uri "http://localhost:5055/api/products/luca-stock-cards" -Method Get -Headers $headers
    $count = if ($cards -is [Array]) { $cards.Count } else { if ($cards) { 1 } else { 0 } }
    
    if ($count -gt 0) {
        Write-Host "   ✅ API returns $count stock cards" -ForegroundColor Green
    } else {
        Write-Host "   ❌ API returns 0 stock cards" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ API test failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== COMPLETE ===" -ForegroundColor Cyan
Write-Host ""

# Show relevant log snippets
Write-Host "Recent logs:" -ForegroundColor Yellow
docker-compose logs --tail=30 backend | Select-String -Pattern "cache|HTML|Login|Migration|ERROR" | ForEach-Object {
    if ($_ -match "ERROR|HTML") {
        Write-Host $_ -ForegroundColor Red
    } elseif ($_ -match "SUCCESS|cache") {
        Write-Host $_ -ForegroundColor Green
    } else {
        Write-Host $_ -ForegroundColor Gray
    }
}

Write-Host ""
