# Delete Malformed Orders from Katana API
# SO-SO-84, SO-SO-SO-56 gibi bozuk siparişleri Katana API'den siler

$katanaApiKey = "ed8c38d1-4015-45e5-9c28-381d3fe148b6"
$katanaBaseUrl = "https://api.katanamrp.com/v1"

$headers = @{
    "Authorization" = "Bearer $katanaApiKey"
    "accept" = "application/json"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DELETE MALFORMED KATANA ORDERS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Malformed order patterns to search for
$malformedPatterns = @("SO-SO-", "SO-SO-SO-")

Write-Host "1. Fetching recent sales orders from Katana..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$katanaBaseUrl/sales_orders" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    # Katana API returns data in a wrapper
    if ($response.data) {
        $allOrders = $response.data
    } else {
        $allOrders = $response
    }

    Write-Host "   Total orders fetched: $($allOrders.Count)" -ForegroundColor White
    Write-Host ""

    # Find malformed orders
    $malformedOrders = $allOrders | Where-Object {
        $orderNo = $_.order_no
        $isMalformed = $false
        foreach ($pattern in $malformedPatterns) {
            if ($orderNo -like "$pattern*") {
                $isMalformed = $true
                break
            }
        }
        $isMalformed
    }

    if ($malformedOrders.Count -eq 0) {
        Write-Host "   No malformed orders found!" -ForegroundColor Green
        exit 0
    }

    Write-Host "   Found $($malformedOrders.Count) malformed orders:" -ForegroundColor Yellow
    foreach ($order in $malformedOrders) {
        Write-Host "   - ID: $($order.id), OrderNo: $($order.order_no), Status: $($order.status)" -ForegroundColor Gray
    }
    Write-Host ""
}
catch {
    Write-Host "   ERROR fetching orders: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Delete each malformed order
Write-Host "2. Deleting malformed orders from Katana API..." -ForegroundColor Red
$successCount = 0
$failCount = 0

foreach ($order in $malformedOrders) {
    try {
        Write-Host "   Deleting order ID $($order.id) ($($order.order_no))..." -ForegroundColor Yellow
        
        $response = Invoke-RestMethod -Uri "$katanaBaseUrl/sales_orders/$($order.id)" `
            -Method DELETE `
            -Headers $headers `
            -ErrorAction Stop

        Write-Host "   ✅ Successfully deleted order $($order.id)" -ForegroundColor Green
        $successCount++
        
        # Rate limit: 500ms delay
        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host "   ❌ Failed to delete order $($order.id): $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Total Malformed Orders: $($malformedOrders.Count)" -ForegroundColor White
Write-Host "   Successfully Deleted: $successCount" -ForegroundColor Green
Write-Host "   Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "White" })
Write-Host ""
