# Force complete backend restart with config reload

Write-Host "=== FORCING BACKEND RESTART ===" -ForegroundColor Cyan
Write-Host ""

# 1. Verify config
Write-Host "1. Checking appsettings.json..." -ForegroundColor Yellow
$config = Get-Content "src/Katana.API/appsettings.json" -Raw
if ($config -match '"ManualSessionCookie":\s*""') {
    Write-Host "   ✅ Config correct: ManualSessionCookie is empty" -ForegroundColor Green
} elseif ($config -match '"ManualSessionCookie":\s*"JSESSIONID=FILL_ME"') {
    Write-Host "   ❌ Config still has FILL_ME!" -ForegroundColor Red
    Write-Host "   Manually edit src/Katana.API/appsettings.json" -ForegroundColor Yellow
    Write-Host "   Change: 'ManualSessionCookie': 'JSESSIONID=FILL_ME'" -ForegroundColor White
    Write-Host "   To:     'ManualSessionCookie': ''" -ForegroundColor White
    exit 1
} else {
    Write-Host "   ℹ️  ManualSessionCookie has custom value" -ForegroundColor Cyan
}

# 2. Stop everything
Write-Host ""
Write-Host "2. Stopping all containers..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 2
Write-Host "   ✅ Stopped" -ForegroundColor Green

# 3. Rebuild backend (to ensure config is copied)
Write-Host ""
Write-Host "3. Rebuilding backend..." -ForegroundColor Yellow
docker-compose build backend
Write-Host "   ✅ Rebuilt" -ForegroundColor Green

# 4. Start everything
Write-Host ""
Write-Host "4. Starting containers..." -ForegroundColor Yellow
docker-compose up -d
Write-Host "   ✅ Started" -ForegroundColor Green

# 5. Wait for backend
Write-Host ""
Write-Host "5. Waiting for backend to be ready..." -ForegroundColor Yellow
$maxWait = 60
$waited = 0
$ready = $false

while ($waited -lt $maxWait -and -not $ready) {
    Start-Sleep -Seconds 2
    $waited += 2
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "   ✅ Backend ready after $waited seconds" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⏳ Waiting... ($waited/$maxWait seconds)" -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "   ❌ Backend did not start in time" -ForegroundColor Red
    Write-Host "   Check logs: docker-compose logs backend" -ForegroundColor Yellow
    exit 1
}

# 6. Wait a bit more for initialization
Write-Host ""
Write-Host "6. Waiting for Luca cache initialization..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 7. Check status
Write-Host ""
Write-Host "7. Checking status..." -ForegroundColor Yellow
$logs = docker-compose logs --tail=100 backend 2>&1

$cacheLog = $logs | Select-String "Luca cache hazır" | Select-Object -Last 1
$htmlError = $logs | Select-String "Still HTML after retry" | Select-Object -Last 1
$authSuccess = $logs | Select-String "Login SUCCESS" | Select-Object -Last 1

Write-Host ""
if ($cacheLog) {
    if ($cacheLog -match "(\d+) stok kartı") {
        $count = [int]$Matches[1]
        if ($count -gt 0) {
            Write-Host "✅ SUCCESS: $count stock cards loaded!" -ForegroundColor Green
        } else {
            Write-Host "❌ STILL 0 stock cards" -ForegroundColor Red
        }
    }
    Write-Host "   $cacheLog" -ForegroundColor Gray
} else {
    Write-Host "⚠️  Cache log not found yet" -ForegroundColor Yellow
}

Write-Host ""
if ($htmlError) {
    Write-Host "❌ HTML error still present" -ForegroundColor Red
    Write-Host "   $htmlError" -ForegroundColor Gray
} else {
    Write-Host "✅ No HTML errors" -ForegroundColor Green
}

Write-Host ""
if ($authSuccess) {
    Write-Host "✅ Authentication successful" -ForegroundColor Green
    Write-Host "   $authSuccess" -ForegroundColor Gray
} else {
    Write-Host "⚠️  No login success log" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== RESTART COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Run: .\quick-fix-check.ps1" -ForegroundColor Yellow
Write-Host ""
