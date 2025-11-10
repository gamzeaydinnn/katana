# Seed test data: products, stock movements, and trigger notifications
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\seed-test-data.ps1

$base = 'http://localhost:5055'

function Read-ResponseBodyFromException($ex) {
    if ($ex.Response -ne $null) {
        try {
            $resp = $ex.Response
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            return $reader.ReadToEnd()
        }
        catch {
            return "(failed to read response body: $($_.Exception.Message))"
        }
    }
    return $null
}

Write-Host "=== Katana Test Data Seeder ===" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "[1/4] Logging in..." -ForegroundColor Yellow
$loginBody = @{ Username = 'admin'; Password = 'Katana2025!' } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
}
catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$token = $loginResp.token
if (-not $token) { 
    Write-Host 'No token returned' -ForegroundColor Red
    exit 1 
}
Write-Host "[OK] Logged in successfully" -ForegroundColor Green
$headers = @{ Authorization = "Bearer $token" }

# 2. Create test products
Write-Host ""
Write-Host "[2/4] Creating test products..." -ForegroundColor Yellow
$testProducts = @(
    @{ sku = "TEST-001"; name = "Test Urun 1"; price = 100.50; stock = 50 },
    @{ sku = "TEST-002"; name = "Test Urun 2"; price = 250.75; stock = 30 },
    @{ sku = "TEST-003"; name = "Test Urun 3"; price = 75.00; stock = 5 },
    @{ sku = "TEST-004"; name = "Test Urun 4"; price = 500.00; stock = 0 },
    @{ sku = "TEST-005"; name = "Test Urun 5"; price = 150.25; stock = 100 }
)

$createdProducts = @()
foreach ($prod in $testProducts) {
    $body = @{
        name = $prod.name
        sku = $prod.sku
        price = [decimal]$prod.price
        stock = [int]$prod.stock
        categoryId = 1
    }

    try {
        $jsonBody = $body | ConvertTo-Json -Depth 10
        $createResp = Invoke-RestMethod -Uri "$base/api/Products" -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $jsonBody
        Write-Host "  [OK] Created: $($prod.sku) - $($prod.name) (Stock: $($prod.stock))" -ForegroundColor Green
        $createdProducts += $createResp
    }
    catch {
        $errorBody = Read-ResponseBodyFromException($_.Exception)
        if ($errorBody -like "*already exists*" -or $errorBody -like "*duplicate*" -or $errorBody -like "*SKU*") {
            Write-Host "  [WARN] Already exists: $($prod.sku)" -ForegroundColor Yellow
        }
        else {
            Write-Host "  [FAIL] Failed to create $($prod.sku)" -ForegroundColor Red
        }
    }
}

# 3. Create stock movements (pending adjustments)
Write-Host ""
Write-Host "[3/4] Creating stock movement notifications..." -ForegroundColor Yellow
$movements = @(
    @{ sku = "TEST-001"; quantity = 20; type = "Giris"; note = "Yeni stok girisi" },
    @{ sku = "TEST-002"; quantity = -10; type = "Cikis"; note = "Satis yapildi" },
    @{ sku = "TEST-003"; quantity = 15; type = "Giris"; note = "Acil stok takviyesi (Dusuk stok uyarisi)" },
    @{ sku = "TEST-004"; quantity = 25; type = "Giris"; note = "Stok doldurma (Stokta yoktu)" }
)

$notifCount = 0
foreach ($move in $movements) {
    try {
        # Create pending adjustment (simulates Katana webhook)
        $createResp = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments/test-create" -Method Post -Headers $headers -ContentType 'application/json' -Body '{}'
        Write-Host "  [OK] Notification: $($move.sku) ($($move.type) $($move.quantity))" -ForegroundColor Green
        $notifCount++
        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host "  [WARN] Skipped: $($move.sku)" -ForegroundColor Yellow
    }
}

# 4. Get statistics
Write-Host ""
Write-Host "[4/4] Fetching statistics..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$base/api/Products/statistics" -Method Get -Headers $headers
    Write-Host "  ────────────────────────────────" -ForegroundColor Cyan
    Write-Host "  Total Products    : $($stats.totalProducts)" -ForegroundColor White
    Write-Host "  Active Products   : $($stats.activeProducts)" -ForegroundColor Green
    Write-Host "  Low Stock         : $($stats.lowStockProducts)" -ForegroundColor Yellow
    Write-Host "  Out of Stock      : $($stats.outOfStockProducts)" -ForegroundColor Red
    Write-Host "  Total Value       : ₺$($stats.totalInventoryValue.ToString('N2'))" -ForegroundColor Cyan
    Write-Host "  ────────────────────────────────" -ForegroundColor Cyan
}
catch {
    Write-Host "  [FAIL] Failed to get statistics" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Seeding Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Products Created  : $($createdProducts.Count)" -ForegroundColor White
Write-Host "  Notifications     : $notifCount" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open: http://localhost:3000/admin" -ForegroundColor White
Write-Host "  2. Go to 'Stok Gorumu' tab to see test products" -ForegroundColor White
Write-Host "  3. Click notification bell icon for alerts" -ForegroundColor White
Write-Host "  4. Go to 'Bekleyen Onaylar' tab for pending items" -ForegroundColor White
Write-Host ""
