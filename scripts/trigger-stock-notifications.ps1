# Trigger stock change notifications via webhook simulation
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\trigger-stock-notifications.ps1

$base = 'http://localhost:5055'

Write-Host "=== Katana Stock Notification Trigger ===" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "[1/3] Logging in..." -ForegroundColor Yellow
$loginBody = @{ Username = 'admin'; Password = 'Katana2025!' } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
}
catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$token = $loginResp.token
Write-Host "✓ Logged in" -ForegroundColor Green
$headers = @{ Authorization = "Bearer $token" }

# 2. Get current products
Write-Host ""
Write-Host "[2/3] Fetching products..." -ForegroundColor Yellow
try {
    $products = Invoke-RestMethod -Uri "$base/api/Products" -Method Get -Headers $headers
    $productList = if ($products.PSObject.Properties['data']) { $products.data } else { $products }
    Write-Host "✓ Found $($productList.Count) products" -ForegroundColor Green
}
catch {
    Write-Host "Failed to fetch products" -ForegroundColor Red
    exit 1
}

# 3. Simulate stock changes
Write-Host ""
Write-Host "[3/3] Simulating stock changes..." -ForegroundColor Yellow
$scenarios = @(
    @{ title = "Low Stock Alert"; filter = { $_.stock -le 10 -and $_.stock -gt 0 }; change = 5; message = "Düşük stok: Yeni sipariş geldi" },
    @{ title = "Out of Stock Recovery"; filter = { $_.stock -eq 0 }; change = 20; message = "Stokta yoktu, yeni tedarik geldi" },
    @{ title = "High Volume Sale"; filter = { $_.stock -gt 50 }; change = -30; message = "Büyük sipariş çıkışı" }
)

foreach ($scenario in $scenarios) {
    $matchingProducts = $productList | Where-Object $scenario.filter | Select-Object -First 2
    
    if ($matchingProducts.Count -eq 0) {
        Write-Host "  ⚠ No products found for: $($scenario.title)" -ForegroundColor Yellow
        continue
    }

    Write-Host ""
    Write-Host "  Scenario: $($scenario.title)" -ForegroundColor Cyan
    
    foreach ($prod in $matchingProducts) {
        $newStock = $prod.stock + $scenario.change
        if ($newStock -lt 0) { $newStock = 0 }

        # Create pending adjustment
        try {
            $pendingResp = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments/test-create" -Method Post -Headers $headers
            
            Write-Host "    ✓ $($prod.sku): $($prod.stock) → $newStock" -ForegroundColor Green
            Write-Host "      Pending ID: $($pendingResp.pendingId) | $($scenario.message)" -ForegroundColor Gray
            
            Start-Sleep -Milliseconds 800
        }
        catch {
            Write-Host "    ✗ Failed for $($prod.sku)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=== Notifications Triggered ===" -ForegroundColor Green
Write-Host ""
Write-Host "Check your admin panel:" -ForegroundColor Cyan
Write-Host "  • Bell icon (🔔) for notifications" -ForegroundColor White
Write-Host "  • 'Bekleyen Onaylar' tab for pending approvals" -ForegroundColor White
Write-Host "  • 'Stok Görünümü' page for stock overview" -ForegroundColor White
