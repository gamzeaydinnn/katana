# Test Stock Card Creation Fix
# This script restarts backend and monitors logs for stock card creation

Write-Host "=== TESTING STOCK CARD CREATION FIX ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Restart backend
Write-Host "1. Restarting backend..." -ForegroundColor Yellow
docker-compose restart backend
Start-Sleep -Seconds 5

# Step 2: Wait for backend to be ready
Write-Host "2. Waiting for backend to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$ready = $false

while ($attempt -lt $maxAttempts -and -not $ready) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "   ✅ Backend is ready!" -ForegroundColor Green
        }
    } catch {
        $attempt++
        Write-Host "   Waiting... ($attempt/$maxAttempts)" -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

if (-not $ready) {
    Write-Host "   ❌ Backend did not start in time" -ForegroundColor Red
    exit 1
}

# Step 3: Check Luca cache
Write-Host ""
Write-Host "3. Checking Luca cache status..." -ForegroundColor Yellow
$logs = docker-compose logs --tail=100 backend | Select-String "Luca|cache|stock card" | Select-Object -Last 20
$logs | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }

# Step 4: Monitor for stock card creation attempts
Write-Host ""
Write-Host "4. Monitoring logs for stock card creation..." -ForegroundColor Yellow
Write-Host "   Press Ctrl+C to stop monitoring" -ForegroundColor Gray
Write-Host ""

docker-compose logs -f backend | ForEach-Object {
    $line = $_
    
    # Highlight important lines
    if ($line -match "LUCA JSON REQUEST") {
        Write-Host $line -ForegroundColor Cyan
    }
    elseif ($line -match "error.*true") {
        Write-Host $line -ForegroundColor Red
    }
    elseif ($line -match "error.*false|successfully|başarılı") {
        Write-Host $line -ForegroundColor Green
    }
    elseif ($line -match "Stock card|stok kart") {
        Write-Host $line -ForegroundColor Yellow
    }
    elseif ($line -match "kategoriAgacKod|MinStokKontrol|tevkifat") {
        Write-Host $line -ForegroundColor Magenta
    }
}
