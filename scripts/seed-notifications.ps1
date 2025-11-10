# Script to seed test notifications
# Usage: .\scripts\seed-notifications.ps1

$base = 'http://localhost:5055'

Write-Host "=== Notification Test Data Seeder ===" -ForegroundColor Cyan
Write-Host ""

# Login
Write-Host "[1/4] Logging in..." -ForegroundColor Yellow
$loginBody = @{ Username = 'admin'; Password = 'Katana2025!' } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
    $token = $loginResp.token
    if (-not $token) { 
        Write-Host "ERROR: No token received" -ForegroundColor Red
        exit 1 
    }
    Write-Host "SUCCESS: Logged in" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Login failed - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

# Create test notifications via webhook (simulating Katana stock changes)
Write-Host ""
Write-Host "[2/4] Creating stock change notifications..." -ForegroundColor Yellow

$notifications = @(
    @{
        sku = "TEST-STOCK-001"
        productName = "Test Urun 1"
        oldStock = 100
        newStock = 15
        type = "LowStock"
    },
    @{
        sku = "TEST-STOCK-002"
        productName = "Test Urun 2"
        oldStock = 50
        newStock = 0
        type = "OutOfStock"
    },
    @{
        sku = "TEST-STOCK-003"
        productName = "Test Urun 3"
        oldStock = 200
        newStock = 500
        type = "StockIncrease"
    },
    @{
        sku = "TEST-STOCK-004"
        productName = "Test Urun 4"
        oldStock = 75
        newStock = 8
        type = "CriticalStock"
    }
)

$createdCount = 0
foreach ($notif in $notifications) {
    # Create pending adjustment which triggers notification
    try {
        $pendingBody = @{
            sku = $notif.sku
            productName = $notif.productName
            quantity = $notif.newStock
            notes = "Test bildirim - Stok degisikigi: $($notif.oldStock) -> $($notif.newStock)"
        } | ConvertTo-Json

        $createResp = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments/test-create" `
            -Method Post -Headers $headers -ContentType 'application/json' -Body '{}'
        
        $createdCount++
        Write-Host "  + $($notif.productName) - $($notif.type)" -ForegroundColor Gray
        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host "  x Failed: $($notif.productName)" -ForegroundColor Red
    }
}

Write-Host "SUCCESS: Created $createdCount notifications" -ForegroundColor Green

# List notifications
Write-Host ""
Write-Host "[3/4] Checking notifications..." -ForegroundColor Yellow
try {
    $notifList = Invoke-RestMethod -Uri "$base/api/notifications" -Method Get -Headers $headers
    $totalNotifs = if ($notifList.PSObject.Properties.Name -contains 'total') { $notifList.total } else { $notifList.Count }
    Write-Host "SUCCESS: Found $totalNotifs total notifications" -ForegroundColor Green
    
    # Show unread count
    $unreadList = Invoke-RestMethod -Uri "$base/api/notifications?unread=true" -Method Get -Headers $headers
    $unreadCount = if ($unreadList.PSObject.Properties.Name -contains 'total') { $unreadList.total } else { $unreadList.Count }
    Write-Host "  Unread: $unreadCount" -ForegroundColor Cyan
}
catch {
    Write-Host "WARNING: Could not fetch notifications" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "[4/4] Summary" -ForegroundColor Yellow
Write-Host "  Test Data Location: Notifications API" -ForegroundColor Gray
Write-Host "  Total Created: $createdCount pending adjustments" -ForegroundColor Gray
Write-Host "  View at: http://localhost:3000/admin (Bildirimler tab)" -ForegroundColor Gray
Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open http://localhost:3000/admin" -ForegroundColor White
Write-Host "  2. Check notification bell icon" -ForegroundColor White
Write-Host "  3. Go to 'Stok Gorumu' tab to see test products" -ForegroundColor White
Write-Host ""
