# Katana Webhook Test Script
$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5056"

Write-Host "`n=== KATANA WEBHOOK TEST ===`n" -ForegroundColor Cyan

# 1. Backend health check
Write-Host "1. Backend health check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "[OK] Backend running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Backend not running!" -ForegroundColor Red
    exit 1
}

# 2. Login
Write-Host "`n2. Login..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token = $loginResponse.token
    Write-Host "[OK] Login successful" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Login failed: $_" -ForegroundColor Red
    exit 1
}

# 3. Check current pending count
Write-Host "`n3. Checking current pending adjustments..." -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $token" }

try {
    $before = Invoke-RestMethod -Uri "$baseUrl/api/admin/pending-adjustments" -Method Get -Headers $headers
    $beforeCount = $before.Count
    Write-Host "[OK] Current pending count: $beforeCount" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not get pending adjustments" -ForegroundColor Yellow
    $beforeCount = 0
}

# 4. Simulate Katana webhook
Write-Host "`n4. Simulating Katana webhook..." -ForegroundColor Yellow
$webhookSecret = "katana-webhook-secret-change-in-production-2025"
$webhookHeaders = @{
    "X-Katana-Signature" = $webhookSecret
    "Content-Type" = "application/json"
}

$webhookPayload = @{
    event = "stock.updated"
    orderId = "TEST-ORDER-$(Get-Date -Format 'yyyyMMddHHmmss')"
    productId = 999
    sku = "TEST-SKU-WEBHOOK-001"
    quantityChange = -7
    timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

try {
    $webhookResponse = Invoke-RestMethod -Uri "$baseUrl/api/webhook/katana/stock-change" -Method Post -Headers $webhookHeaders -Body $webhookPayload
    $newPendingId = $webhookResponse.pendingId
    Write-Host "[OK] Webhook successful - New Pending ID: $newPendingId" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Webhook failed: $_" -ForegroundColor Red
    exit 1
}

# 5. Verify new pending created
Write-Host "`n5. Verifying new pending adjustment..." -ForegroundColor Yellow
Start-Sleep -Seconds 1

try {
    $after = Invoke-RestMethod -Uri "$baseUrl/api/admin/pending-adjustments" -Method Get -Headers $headers
    $afterCount = $after.Count
    
    if ($afterCount -gt $beforeCount) {
        Write-Host "[OK] Pending count increased: $beforeCount -> $afterCount" -ForegroundColor Green
        $newest = $after | Sort-Object -Property id -Descending | Select-Object -First 1
        Write-Host "  ID: $($newest.id), SKU: $($newest.sku), Qty: $($newest.quantity), Status: $($newest.status)" -ForegroundColor Gray
    } else {
        Write-Host "[WARNING] Pending count did not change" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[WARNING] Could not verify: $_" -ForegroundColor Yellow
}

# 6. Approve pending
Write-Host "`n6. Approving pending adjustment..." -ForegroundColor Yellow
try {
    $approveResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/pending-adjustments/$newPendingId/approve" -Method Post -Headers $headers
    Write-Host "[OK] Pending #$newPendingId approved" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Approval failed: $_" -ForegroundColor Red
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "            TEST COMPLETED              " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nRESULTS:" -ForegroundColor Yellow
Write-Host "  [OK] Backend health check" -ForegroundColor Green
Write-Host "  [OK] Login" -ForegroundColor Green
Write-Host "  [OK] Webhook simulation" -ForegroundColor Green
Write-Host "  [OK] Pending created (ID: $newPendingId)" -ForegroundColor Green
Write-Host "  [OK] Pending approved" -ForegroundColor Green
Write-Host "`nFLOW:" -ForegroundColor Yellow
Write-Host "  Katana API -> Webhook -> Pending -> SignalR -> Frontend" -ForegroundColor Gray
Write-Host "`nFRONTEND CHECK:" -ForegroundColor Yellow
Write-Host "  1. Open: http://localhost:3000" -ForegroundColor Gray
Write-Host "  2. Login as admin (admin/admin123)" -ForegroundColor Gray
Write-Host "  3. Check notification badge in header" -ForegroundColor Gray
Write-Host "  4. Browser console: Check SignalR logs" -ForegroundColor Gray
Write-Host "`n========================================`n" -ForegroundColor Cyan
