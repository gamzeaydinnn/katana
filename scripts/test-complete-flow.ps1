# Complete test flow: seed data → trigger notifications → verify
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-complete-flow.ps1

$base = 'http://localhost:5055'

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Katana Integration - Complete Test Flow          ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
Write-Host "Checking API status..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$base/api/Health" -Method Get -TimeoutSec 3
    Write-Host "✓ API is running" -ForegroundColor Green
}
catch {
    Write-Host "✗ API is not running on $base" -ForegroundColor Red
    Write-Host "  Start the API first: cd src\Katana.API && dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 1: Seed test data
Write-Host "═══ Step 1: Seeding Test Data ═══" -ForegroundColor Magenta
& "$PSScriptRoot\seed-test-data.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Seeding failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Waiting 2 seconds..." -ForegroundColor Gray
Start-Sleep -Seconds 2

# Step 2: Trigger notifications
Write-Host ""
Write-Host "═══ Step 2: Triggering Stock Notifications ═══" -ForegroundColor Magenta
& "$PSScriptRoot\trigger-stock-notifications.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Notification trigger failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Waiting 2 seconds..." -ForegroundColor Gray
Start-Sleep -Seconds 2

# Step 3: Verify data
Write-Host ""
Write-Host "═══ Step 3: Verifying Results ═══" -ForegroundColor Magenta

$loginBody = @{ Username = 'admin'; Password = 'Katana2025!' } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
    $token = $loginResp.token
    $headers = @{ Authorization = "Bearer $token" }
}
catch {
    Write-Host "✗ Login failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📊 Final Statistics:" -ForegroundColor Cyan

# Products
try {
    $products = Invoke-RestMethod -Uri "$base/api/Products" -Method Get -Headers $headers
    $productList = if ($products.PSObject.Properties['data']) { $products.data } else { $products }
    Write-Host "  Products Count    : $($productList.Count)" -ForegroundColor White
}
catch {
    Write-Host "  ✗ Failed to fetch products" -ForegroundColor Red
}

# Pending Adjustments
try {
    $pending = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments" -Method Get -Headers $headers
    $pendingList = if ($pending.PSObject.Properties['items']) { $pending.items } else { $pending }
    $pendingCount = if ($pendingList) { $pendingList.Count } else { 0 }
    Write-Host "  Pending Approvals : $pendingCount" -ForegroundColor Yellow
}
catch {
    Write-Host "  ✗ Failed to fetch pending" -ForegroundColor Red
}

# Notifications (if endpoint exists)
try {
    $notifications = Invoke-RestMethod -Uri "$base/api/notifications?unread=true" -Method Get -Headers $headers
    $notifList = if ($notifications.PSObject.Properties['data']) { $notifications.data } else { $notifications }
    $notifCount = if ($notifList) { $notifList.Count } else { 0 }
    Write-Host "  Unread Notifications : $notifCount" -ForegroundColor Cyan
}
catch {
    # Notifications endpoint might not exist yet
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              Test Flow Completed Successfully         ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "🎯 Next Actions:" -ForegroundColor Cyan
Write-Host "  1. Open Frontend: http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login with admin/Katana2025!" -ForegroundColor White
Write-Host "  3. Navigate to:" -ForegroundColor White
Write-Host "     • Admin Panel → Stok Yönetimi (see test products)" -ForegroundColor Gray
Write-Host "     • Admin Panel → Bekleyen Onaylar (approve/reject)" -ForegroundColor Gray
Write-Host "     • Stok Görünümü page (public stock view)" -ForegroundColor Gray
Write-Host "     • Bell icon (🔔) for notifications" -ForegroundColor Gray
Write-Host ""
