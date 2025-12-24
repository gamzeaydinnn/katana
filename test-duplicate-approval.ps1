# Test Duplicate Approval Prevention
# Bu script aynı siparişi iki kez onaylamaya çalışır
# İkinci deneme reddedilmeli

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

# Get an already approved order
Write-Host "`n=== Finding Approved Order ===" -ForegroundColor Cyan
try {
    $orders = Invoke-RestMethod -Uri "$baseUrl/sales-orders?status=APPROVED&pageSize=5" -Method Get -Headers $headers
    
    if ($orders.Count -eq 0) {
        Write-Host "No approved orders found. Run test-order-approval-new.ps1 first." -ForegroundColor Yellow
        exit 0
    }
    
    $approvedOrder = $orders[0]
    Write-Host "Found approved order: $($approvedOrder.orderNo)" -ForegroundColor Yellow
} catch {
    Write-Host "Failed to get orders: $_" -ForegroundColor Red
    exit 1
}

# Try to approve again
Write-Host "`n=== Attempting Duplicate Approval ===" -ForegroundColor Cyan
Write-Host "Order: $($approvedOrder.orderNo) (ID: $($approvedOrder.id))" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sales-orders/$($approvedOrder.id)/approve" -Method Post -Headers $headers
    
    # If we get here, something is wrong - duplicate should be rejected
    Write-Host "UNEXPECTED: Duplicate approval was accepted!" -ForegroundColor Red
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    
    if ($statusCode -eq 400) {
        Write-Host "SUCCESS: Duplicate approval was correctly rejected!" -ForegroundColor Green
        Write-Host "Status Code: $statusCode" -ForegroundColor Gray
        if ($errorResponse) {
            Write-Host "Message: $($errorResponse.message)" -ForegroundColor Yellow
            if ($errorResponse.katanaOrderId) {
                Write-Host "Existing KatanaOrderId: $($errorResponse.katanaOrderId)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "UNEXPECTED ERROR: Status $statusCode" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
