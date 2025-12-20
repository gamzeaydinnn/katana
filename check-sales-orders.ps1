# Check sales orders in database
$baseUrl = "http://localhost:5055"

# Get all sales orders
Write-Host "=== Checking Sales Orders in Database ===" -ForegroundColor Cyan
$response = Invoke-RestMethod -Uri "$baseUrl/api/sales-orders?page=1&pageSize=50" -Method GET
Write-Host "Total orders in DB: $($response.Count)" -ForegroundColor Yellow

if ($response.Count -gt 0) {
    Write-Host "`nOrders:" -ForegroundColor Green
    $response | ForEach-Object {
        Write-Host "  - $($_.orderNo): Customer=$($_.customerName), Status=$($_.status), Total=$($_.total) $($_.currency), Created=$($_.orderCreatedDate)" -ForegroundColor White
    }
} else {
    Write-Host "No orders found in database!" -ForegroundColor Red
}

# Check specific orders from Katana
Write-Host "`n=== Checking Specific Katana Orders ===" -ForegroundColor Cyan
$katanaOrders = @("SO-41", "SO-47", "SO-56")

foreach ($orderNo in $katanaOrders) {
    Write-Host "`nChecking $orderNo..." -ForegroundColor Yellow
    try {
        $debugResponse = Invoke-RestMethod -Uri "$baseUrl/api/sync/debug/katana-order/$orderNo" -Method GET
        
        Write-Host "  Found in Katana: $($debugResponse.found.inKatana)" -ForegroundColor $(if ($debugResponse.found.inKatana) { "Green" } else { "Red" })
        Write-Host "  Found in DB: $($debugResponse.found.inDatabase)" -ForegroundColor $(if ($debugResponse.found.inDatabase) { "Green" } else { "Red" })
        
        if ($debugResponse.issues.Count -gt 0) {
            Write-Host "  Issues:" -ForegroundColor Red
            $debugResponse.issues | ForEach-Object {
                Write-Host "    - $_" -ForegroundColor Red
            }
        }
        
        if ($debugResponse.katanaOrder) {
            Write-Host "  Katana: Customer ID=$($debugResponse.katanaOrder.katanaCustomerId), Status=$($debugResponse.katanaOrder.status), Total=$($debugResponse.katanaOrder.total)" -ForegroundColor Cyan
        }
        
        if ($debugResponse.dbOrder) {
            Write-Host "  DB: Customer ID=$($debugResponse.dbOrder.localCustomerId), Status=$($debugResponse.dbOrder.status), Total=$($debugResponse.dbOrder.total)" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
