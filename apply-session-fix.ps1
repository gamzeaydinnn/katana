# Apply Luca Session Fix
# This script restarts the backend to apply the configuration changes

$ErrorActionPreference = "Stop"

Write-Host "=== APPLYING LUCA SESSION FIX ===" -ForegroundColor Cyan
Write-Host ""

# 1. Verify changes
Write-Host "1. Verifying configuration changes..." -ForegroundColor Yellow
$configPath = "src/Katana.API/appsettings.json"

if (-not (Test-Path $configPath)) {
    Write-Host "   ❌ Config file not found: $configPath" -ForegroundColor Red
    exit 1
}

$config = Get-Content $configPath -Raw
if ($config -match '"ManualSessionCookie":\s*"JSESSIONID=FILL_ME"') {
    Write-Host "   ❌ Config still has FILL_ME placeholder!" -ForegroundColor Red
    Write-Host "   Please ensure the fix was applied correctly" -ForegroundColor Yellow
    exit 1
} elseif ($config -match '"ManualSessionCookie":\s*""') {
    Write-Host "   ✅ Config updated: ManualSessionCookie is empty (automatic login)" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  Config has custom ManualSessionCookie value" -ForegroundColor Cyan
}

# 2. Stop backend
Write-Host ""
Write-Host "2. Stopping backend..." -ForegroundColor Yellow
try {
    docker-compose stop backend
    Write-Host "   ✅ Backend stopped" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️  Failed to stop backend: $_" -ForegroundColor Yellow
}

# 3. Start backend
Write-Host ""
Write-Host "3. Starting backend with new configuration..." -ForegroundColor Yellow
try {
    docker-compose up -d backend
    Write-Host "   ✅ Backend started" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Failed to start backend: $_" -ForegroundColor Red
    exit 1
}

# 4. Wait for backend to be ready
Write-Host ""
Write-Host "4. Waiting for backend to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$ready = $false

while ($attempt -lt $maxAttempts -and -not $ready) {
    $attempt++
    Start-Sleep -Seconds 2
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "   ✅ Backend is ready (attempt $attempt)" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⏳ Waiting... (attempt $attempt/$maxAttempts)" -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "   ❌ Backend did not become ready in time" -ForegroundColor Red
    Write-Host "   Check logs: docker-compose logs backend" -ForegroundColor Yellow
    exit 1
}

# 5. Check logs for session status
Write-Host ""
Write-Host "5. Checking session initialization..." -ForegroundColor Yellow
Start-Sleep -Seconds 3  # Give it a moment to initialize

$logs = docker-compose logs --tail=50 backend 2>&1

# Look for cache initialization
$cacheLog = $logs | Select-String "Luca cache hazır" | Select-Object -Last 1
$htmlError = $logs | Select-String "Still HTML after retry" | Select-Object -Last 1
$loginSuccess = $logs | Select-String "Login SUCCESS" | Select-Object -Last 1

Write-Host ""
Write-Host "📊 Session Status:" -ForegroundColor Cyan

if ($cacheLog) {
    if ($cacheLog -match "(\d+) stok kartı") {
        $count = [int]$Matches[1]
        if ($count -gt 0) {
            Write-Host "   ✅ SUCCESS: $count stock cards loaded from Luca" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  WARNING: 0 stock cards loaded" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ℹ️  Cache initialization not logged yet" -ForegroundColor Cyan
}

if ($htmlError) {
    Write-Host "   ❌ HTML response error still present" -ForegroundColor Red
    Write-Host "   Check full logs: docker-compose logs backend | grep HTML" -ForegroundColor Yellow
} else {
    Write-Host "   ✅ No HTML response errors" -ForegroundColor Green
}

if ($loginSuccess) {
    Write-Host "   ✅ Login successful" -ForegroundColor Green
}

# 6. Summary
Write-Host ""
Write-Host "=== FIX APPLIED ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run session check: .\check-backend-session.ps1" -ForegroundColor White
Write-Host "2. Run full test: .\test-luca-session-fix.ps1" -ForegroundColor White
Write-Host "3. Test sync: .\test-sync-sill-specific.ps1" -ForegroundColor White
Write-Host "4. Check for sill products: .\check-sill-in-luca.ps1" -ForegroundColor White
Write-Host ""
Write-Host "To view live logs:" -ForegroundColor Yellow
Write-Host "   docker-compose logs -f backend" -ForegroundColor White
Write-Host ""
