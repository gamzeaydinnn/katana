# Test Product Duplicate Analysis API

$baseUrl = "http://localhost:5055"

Write-Host "========================================"
Write-Host "PRODUCT DUPLICATE ANALYSIS TEST"
Write-Host "========================================`n"

# Step 1: Login
Write-Host "1. Logging in..."
$loginBody = @{
    Username = "admin"
    Password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "Login successful`n"
} catch {
    Write-Host "Login failed: $($_.Exception.Message)"
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Analyze Duplicates
Write-Host "2. Analyzing product duplicates..."
try {
    $analysisResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/product-deduplication/analyze" -Method Get -Headers $headers
    
    Write-Host "Analysis complete!`n"
    Write-Host "Total Duplicate Groups: $($analysisResponse.Count)`n"
    
    # Show all duplicate groups
    foreach ($group in $analysisResponse | Sort-Object -Property Count -Descending) {
        Write-Host "----------------------------------------"
        Write-Host "Product: $($group.productName)"
        Write-Host "Count: $($group.count)"
        
        if ($group.katanaOrderId) {
            Write-Host "Katana Order ID: $($group.katanaOrderId)"
        }
        
        if ($group.isKeepSeparate) {
            Write-Host "KEEP SEPARATE - Reason: $($group.keepSeparateReason)"
        }
        
        Write-Host "`nProducts:"
        foreach ($product in $group.products) {
            $canonical = if ($product.isSuggestedCanonical) { " [CANONICAL]" } else { "" }
            $active = if ($product.isActive) { "Active" } else { "Inactive" }
            
            Write-Host "  $active - ID: $($product.id) | SKU: $($product.sku)$canonical"
            Write-Host "    Category: $($product.categoryName)"
            Write-Host "    Sales Orders: $($product.salesOrderCount) | BOMs: $($product.bomCount) | Stock Movements: $($product.stockMovementCount)"
        }
        Write-Host ""
    }
    
    # Save analysis
    $analysisResponse | ConvertTo-Json -Depth 10 | Out-File "duplicate-analysis-result.json"
    Write-Host "Analysis saved to duplicate-analysis-result.json`n"
    
    # Summary
    $totalProducts = ($analysisResponse | ForEach-Object { $_.count } | Measure-Object -Sum).Sum
    $orderBasedGroups = ($analysisResponse | Where-Object { $_.katanaOrderId }).Count
    $nameBasedGroups = ($analysisResponse | Where-Object { -not $_.katanaOrderId }).Count
    $keepSeparateGroups = ($analysisResponse | Where-Object { $_.isKeepSeparate }).Count
    
    Write-Host "========================================"
    Write-Host "SUMMARY"
    Write-Host "========================================"
    Write-Host "Total Duplicate Groups: $($analysisResponse.Count)"
    Write-Host "Total Duplicate Products: $totalProducts"
    Write-Host "Order-Based Groups: $orderBasedGroups"
    Write-Host "Name-Based Groups: $nameBasedGroups"
    Write-Host "Keep Separate Groups: $keepSeparateGroups"
    
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
    exit 1
}

Write-Host "`n========================================"
Write-Host "TEST COMPLETE"
Write-Host "========================================"
