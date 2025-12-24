# Test Duplicate Order Analysis API
# Duplike siparişleri analiz eden endpoint'i test eder

$baseUrl = "http://localhost:5055"
$token = Get-Content ".auth_token" -ErrorAction SilentlyContinue

if (-not $token) {
    Write-Host "Auth token not found. Please login first." -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DUPLICATE ORDER ANALYSIS TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Analyze duplicates
Write-Host "1. Analyzing duplicate orders..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/duplicates/analyze" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "   Total Orders: $($response.totalOrders)" -ForegroundColor White
    Write-Host "   Duplicate Groups: $($response.duplicateGroups)" -ForegroundColor White
    Write-Host "   Orders to Delete: $($response.ordersToDelete)" -ForegroundColor White
    Write-Host ""

    if ($response.groups -and $response.groups.Count -gt 0) {
        Write-Host "   Duplicate Groups:" -ForegroundColor Cyan
        foreach ($group in $response.groups) {
            Write-Host ""
            Write-Host "   OrderNo: $($group.orderNo) (Count: $($group.count))" -ForegroundColor Green
            Write-Host "   ├─ KEEP: ID=$($group.orderToKeep.id), Status=$($group.orderToKeep.status), Customer=$($group.orderToKeep.customerName)" -ForegroundColor White
            Write-Host "   │        Reason: $($group.orderToKeep.keepReason)" -ForegroundColor Gray
            
            foreach ($toDelete in $group.ordersToDelete) {
                Write-Host "   └─ DELETE: ID=$($toDelete.id), Status=$($toDelete.status), Customer=$($toDelete.customerName)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   No duplicate orders found!" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MALFORMED ORDER ANALYSIS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 2. Analyze malformed OrderNos
Write-Host "2. Analyzing malformed order numbers..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/malformed/analyze" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "   Total Malformed: $($response.totalMalformed)" -ForegroundColor White
    Write-Host "   Can Merge: $($response.canMerge)" -ForegroundColor White
    Write-Host "   Can Rename: $($response.canRename)" -ForegroundColor White
    Write-Host ""

    if ($response.orders -and $response.orders.Count -gt 0) {
        Write-Host "   Malformed Orders:" -ForegroundColor Cyan
        foreach ($order in $response.orders) {
            $actionColor = if ($order.action -eq "Merge") { "Yellow" } else { "Blue" }
            Write-Host "   - ID=$($order.id): '$($order.currentOrderNo)' -> '$($order.correctOrderNo)' [$($order.action)]" -ForegroundColor $actionColor
            if ($order.mergeTargetId) {
                Write-Host "     (Will merge into order ID: $($order.mergeTargetId))" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "   No malformed order numbers found!" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
