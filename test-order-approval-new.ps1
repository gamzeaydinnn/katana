# Test Sales Order Approval - New Single Order Flow
# Bu script yeni approval flow'u test eder
# Sipariş tek bir Katana order olarak gönderilir, her satır ayrı row olarak

$baseUrl = "http://localhost:5000/api"

# Login
Write-Host "=== Login ===" -ForegroundColor Cyan
$loginBody = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "Login successful" -ForegroundColor Green
} catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get pending orders
Write-Host "`n=== Pending Orders ===" -ForegroundColor Cyan
try {
    $orders = Invoke-RestMethod -Uri "$baseUrl/sales-orders?status=NOT_SHIPPED&pageSize=10" -Method Get -Headers $headers
    Write-Host "Found $($orders.Count) pending orders" -ForegroundColor Yellow
    
    if ($orders.Count -eq 0) {
        Write-Host "No pending orders to approve" -ForegroundColor Yellow
        exit 0
    }
    
    # Show first few orders
    $orders | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.orderNo): $($_.customerName) - $($_.total) $($_.currency)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Failed to get orders: $_" -ForegroundColor Red
    exit 1
}

# Select first order for approval
$orderToApprove = $orders[0]
Write-Host "`n=== Approving Order: $($orderToApprove.orderNo) ===" -ForegroundColor Cyan

# Get order details first
try {
    $orderDetail = Invoke-RestMethod -Uri "$baseUrl/sales-orders/$($orderToApprove.id)" -Method Get -Headers $headers
    Write-Host "Order has $($orderDetail.lines.Count) lines:" -ForegroundColor Yellow
    $orderDetail.lines | ForEach-Object {
        Write-Host "  - SKU: $($_.sku), Qty: $($_.quantity), VariantId: $($_.variantId)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Failed to get order details: $_" -ForegroundColor Red
}

# Approve order
Write-Host "`n=== Sending Approval Request ===" -ForegroundColor Cyan
try {
    $approvalResponse = Invoke-RestMethod -Uri "$baseUrl/sales-orders/$($orderToApprove.id)/approve" -Method Post -Headers $headers
    
    Write-Host "`nApproval Response:" -ForegroundColor Green
    Write-Host "  Success: $($approvalResponse.success)" -ForegroundColor $(if ($approvalResponse.success) { "Green" } else { "Red" })
    Write-Host "  Message: $($approvalResponse.message)" -ForegroundColor Yellow
    Write-Host "  Order No: $($approvalResponse.orderNo)" -ForegroundColor Gray
    Write-Host "  Order Status: $($approvalResponse.orderStatus)" -ForegroundColor Gray
    Write-Host "  Katana Order ID: $($approvalResponse.katanaOrderId)" -ForegroundColor Cyan
    Write-Host "  Line Count: $($approvalResponse.lineCount)" -ForegroundColor Gray
    
    if ($approvalResponse.lucaSync) {
        Write-Host "`n  Luca Sync:" -ForegroundColor Yellow
        Write-Host "    Attempted: $($approvalResponse.lucaSync.attempted)" -ForegroundColor Gray
        if ($approvalResponse.lucaSync.attempted) {
            Write-Host "    Success: $($approvalResponse.lucaSync.isSuccess)" -ForegroundColor $(if ($approvalResponse.lucaSync.isSuccess) { "Green" } else { "Red" })
            Write-Host "    Luca Order ID: $($approvalResponse.lucaSync.lucaOrderId)" -ForegroundColor Gray
        } else {
            Write-Host "    Reason: $($approvalResponse.lucaSync.reason)" -ForegroundColor Yellow
        }
    }
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($errorResponse) {
        Write-Host "Approval failed:" -ForegroundColor Red
        Write-Host "  Message: $($errorResponse.message)" -ForegroundColor Red
        if ($errorResponse.errors) {
            Write-Host "  Errors:" -ForegroundColor Red
            $errorResponse.errors | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
        }
    } else {
        Write-Host "Approval failed: $_" -ForegroundColor Red
    }
}

# Verify order status after approval
Write-Host "`n=== Verifying Order Status ===" -ForegroundColor Cyan
try {
    $updatedOrder = Invoke-RestMethod -Uri "$baseUrl/sales-orders/$($orderToApprove.id)" -Method Get -Headers $headers
    Write-Host "Order Status: $($updatedOrder.status)" -ForegroundColor $(if ($updatedOrder.status -eq "APPROVED") { "Green" } else { "Yellow" })
    Write-Host "Katana Order ID: $($updatedOrder.katanaOrderId)" -ForegroundColor Cyan
    Write-Host "Luca Order ID: $($updatedOrder.lucaOrderId)" -ForegroundColor Gray
    Write-Host "Is Synced to Luca: $($updatedOrder.isSyncedToLuca)" -ForegroundColor Gray
    
    # Check all lines have same KatanaOrderId
    $lineKatanaIds = $updatedOrder.lines | Select-Object -ExpandProperty katanaOrderId -Unique
    if ($lineKatanaIds.Count -eq 1) {
        Write-Host "All lines have same KatanaOrderId: $lineKatanaIds" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Lines have different KatanaOrderIds: $($lineKatanaIds -join ', ')" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Failed to verify order: $_" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
