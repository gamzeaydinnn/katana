# DATA CORRECTION PANEL LOGIC TEST
# Tests the DataCorrectionPanel logic fix

$baseUrl = "http://localhost:5055/api"
$token = ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DATA CORRECTION PANEL LOGIC TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# LOGIN
Write-Host "[1/4] Logging in..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "OK Login successful!" -ForegroundColor Green
    } else {
        Write-Host "ERROR Token not received!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# GET KATANA PRODUCTS
Write-Host "[2/4] Fetching Katana products..." -ForegroundColor Yellow
try {
    $katanaResponse = Invoke-RestMethod -Uri "$baseUrl/Products/katana?sync=true" -Method Get -Headers $headers
    $katanaProducts = $katanaResponse.data
    
    Write-Host "OK Fetched $($katanaProducts.Count) products from Katana" -ForegroundColor Green
    
    if ($katanaProducts.Count -gt 0) {
        Write-Host ""
        Write-Host "   First 3 Katana Products:" -ForegroundColor Gray
        $katanaProducts | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - SKU: $($_.sku), Name: $($_.name), Price: $($_.salesPrice), Stock: $($_.onHand)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "ERROR Failed to fetch Katana products: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# GET LUCA PRODUCTS
Write-Host "[3/4] Fetching Luca products..." -ForegroundColor Yellow
try {
    $lucaResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $lucaProducts = $lucaResponse.data
    
    Write-Host "OK Fetched $($lucaProducts.Count) products from Luca" -ForegroundColor Green
    
    if ($lucaProducts.Count -gt 0) {
        Write-Host ""
        Write-Host "   First 3 Luca Products:" -ForegroundColor Gray
        $lucaProducts | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - Code: $($_.productCode), Name: $($_.productName), Price: $($_.unitPrice), Qty: $($_.quantity)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "ERROR Failed to fetch Luca products: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# LOGIC ANALYSIS
Write-Host "[4/4] Analyzing logic..." -ForegroundColor Yellow
Write-Host ""

# Products in Katana but not in Luca
$katanaOnlyProducts = @()
foreach ($katanaProduct in $katanaProducts) {
    $lucaMatch = $lucaProducts | Where-Object { $_.productCode -eq $katanaProduct.sku }
    if (-not $lucaMatch) {
        $katanaOnlyProducts += $katanaProduct
    }
}

# Products in Luca but not in Katana
$lucaOnlyProducts = @()
foreach ($lucaProduct in $lucaProducts) {
    $katanaMatch = $katanaProducts | Where-Object { $_.sku -eq $lucaProduct.productCode }
    if (-not $katanaMatch) {
        $lucaOnlyProducts += $lucaProduct
    }
}

# Price/Stock mismatches
$mismatchProducts = @()
foreach ($katanaProduct in $katanaProducts) {
    $lucaMatch = $lucaProducts | Where-Object { $_.productCode -eq $katanaProduct.sku }
    if ($lucaMatch) {
        $katanaPrice = if ($katanaProduct.salesPrice) { $katanaProduct.salesPrice } else { 0 }
        $katanaStock = if ($katanaProduct.onHand) { $katanaProduct.onHand } else { 0 }
        $priceDiff = [Math]::Abs($katanaPrice - $lucaMatch.unitPrice)
        $stockDiff = $katanaStock -ne $lucaMatch.quantity
        
        if ($priceDiff -gt 0.01 -or $stockDiff) {
            $mismatchProducts += @{
                SKU = $katanaProduct.sku
                Name = $katanaProduct.name
                KatanaPrice = $katanaPrice
                LucaPrice = $lucaMatch.unitPrice
                KatanaStock = $katanaStock
                LucaStock = $lucaMatch.quantity
                PriceMismatch = $priceDiff -gt 0.01
                StockMismatch = $stockDiff
            }
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# In Katana but not in Luca
Write-Host "[IN KATANA, NOT IN LUCA]:" -ForegroundColor Yellow
Write-Host "   (Not yet synced to Luca - NORMAL)" -ForegroundColor Gray
if ($katanaOnlyProducts.Count -eq 0) {
    Write-Host "   OK None - All Katana products exist in Luca" -ForegroundColor Green
} else {
    Write-Host "   WARNING $($katanaOnlyProducts.Count) products found:" -ForegroundColor Yellow
    $katanaOnlyProducts | Select-Object -First 5 | ForEach-Object {
        Write-Host "      - $($_.sku) - $($_.name)" -ForegroundColor Gray
    }
    if ($katanaOnlyProducts.Count -gt 5) {
        Write-Host "      ... and $($katanaOnlyProducts.Count - 5) more" -ForegroundColor Gray
    }
}

Write-Host ""

# In Luca but not in Katana
Write-Host "[IN LUCA, NOT IN KATANA]:" -ForegroundColor Magenta
Write-Host "   (Manually created in Luca - NOT A PROBLEM)" -ForegroundColor Gray
if ($lucaOnlyProducts.Count -eq 0) {
    Write-Host "   OK None - All Luca products exist in Katana" -ForegroundColor Green
} else {
    Write-Host "   INFO $($lucaOnlyProducts.Count) products found:" -ForegroundColor Cyan
    $lucaOnlyProducts | Select-Object -First 5 | ForEach-Object {
        Write-Host "      - $($_.productCode) - $($_.productName)" -ForegroundColor Gray
    }
    if ($lucaOnlyProducts.Count -gt 5) {
        Write-Host "      ... and $($lucaOnlyProducts.Count - 5) more" -ForegroundColor Gray
    }
}

Write-Host ""

# Price/Stock mismatches
Write-Host "[PRICE/STOCK MISMATCH]:" -ForegroundColor Red
if ($mismatchProducts.Count -eq 0) {
    Write-Host "   OK None - All products are synchronized" -ForegroundColor Green
} else {
    Write-Host "   ERROR $($mismatchProducts.Count) products found:" -ForegroundColor Red
    $mismatchProducts | Select-Object -First 5 | ForEach-Object {
        Write-Host "      - $($_.SKU) - $($_.Name)" -ForegroundColor Gray
        if ($_.PriceMismatch) {
            Write-Host "        Price: Katana=$($_.KatanaPrice) TL, Luca=$($_.LucaPrice) TL" -ForegroundColor Yellow
        }
        if ($_.StockMismatch) {
            Write-Host "        Stock: Katana=$($_.KatanaStock), Luca=$($_.LucaStock)" -ForegroundColor Yellow
        }
    }
    if ($mismatchProducts.Count -gt 5) {
        Write-Host "      ... and $($mismatchProducts.Count - 5) more" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LOGIC VALIDATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "DataCorrectionPanel Logic:" -ForegroundColor White
Write-Host ""
Write-Host "   CORRECT APPROACH:" -ForegroundColor Green
Write-Host "      - In Katana not in Luca -> Not synced yet (KATANA ISSUE)" -ForegroundColor Gray
Write-Host "      - In Luca not in Katana -> Manually created (NOT A PROBLEM)" -ForegroundColor Gray
Write-Host "      - Price/Stock mismatch -> Both sides may need correction" -ForegroundColor Gray
Write-Host ""
Write-Host "   WRONG APPROACH:" -ForegroundColor Red
Write-Host "      - In Luca not in Katana -> Showing as LUCA ERROR is WRONG!" -ForegroundColor Gray
Write-Host "      - Because data flow is Katana -> Luca" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RECOMMENDATIONS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Luca Errors tab should ONLY show price/stock mismatches" -ForegroundColor Yellow
Write-Host "2. In Luca not in Katana should NOT be shown as a problem" -ForegroundColor Yellow
Write-Host "3. Katana Issues tab should show not synced to Luca products" -ForegroundColor Yellow
Write-Host ""
