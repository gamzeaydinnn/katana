# Luca Product Delete Test Script
# HIZ01 urununu silme testi

$baseUrl = "http://localhost:5055/api"
$token = ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LUCA PRODUCT DELETE TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# LOGIN
Write-Host "[1/5] Logging in..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "Katana2025!"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "   ✅ Login successful!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Token not received!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# GET LUCA PRODUCTS - HIZ01'i bul
Write-Host "[2/5] Fetching Luca products to find HIZ01..." -ForegroundColor Yellow
try {
    $lucaResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $lucaProducts = $lucaResponse.data
    
    Write-Host "   ✅ Fetched $($lucaProducts.Count) products from Luca" -ForegroundColor Green
    
    # HIZ01'i bul
    $hiz01 = $lucaProducts | Where-Object { $_.productCode -eq "HIZ01" }
    
    if ($hiz01) {
        Write-Host ""
        Write-Host "   📦 HIZ01 Product Found:" -ForegroundColor Cyan
        Write-Host "      - ID: $($hiz01.id)" -ForegroundColor Gray
        Write-Host "      - Code: $($hiz01.productCode)" -ForegroundColor Gray
        Write-Host "      - Name: $($hiz01.productName)" -ForegroundColor Gray
        Write-Host "      - Price: $($hiz01.unitPrice) TL" -ForegroundColor Gray
        Write-Host "      - Quantity: $($hiz01.quantity)" -ForegroundColor Gray
    } else {
        Write-Host "   ⚠️  HIZ01 product not found in Luca!" -ForegroundColor Yellow
        Write-Host "   Available products starting with 'HIZ':" -ForegroundColor Gray
        $lucaProducts | Where-Object { $_.productCode -like "HIZ*" } | Select-Object -First 5 | ForEach-Object {
            Write-Host "      - $($_.productCode) - $($_.productName)" -ForegroundColor Gray
        }
        exit 1
    }
} catch {
    Write-Host "   ❌ Failed to fetch Luca products: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# CONFIRM DELETE
Write-Host "[3/5] Confirming delete operation..." -ForegroundColor Yellow
Write-Host "   ⚠️  WARNING: You are about to DELETE HIZ01 from Luca!" -ForegroundColor Red
Write-Host "   Product: $($hiz01.productName)" -ForegroundColor Yellow
Write-Host "   Code: $($hiz01.productCode)" -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "   Type 'DELETE' to confirm"

if ($confirmation -ne "DELETE") {
    Write-Host "   ❌ Delete operation cancelled!" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# DELETE PRODUCT
Write-Host "[4/5] Deleting HIZ01 from Luca..." -ForegroundColor Yellow
try {
    $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca/$($hiz01.id)" -Method Delete -Headers $headers
    
    Write-Host "   ✅ DELETE request successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "   Response:" -ForegroundColor Cyan
    Write-Host "   $($deleteResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    
} catch {
    Write-Host "   ❌ Failed to delete product: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Error Details:" -ForegroundColor Yellow
    Write-Host "   $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# VERIFY DELETE
Write-Host "[5/5] Verifying deletion..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

try {
    $verifyResponse = Invoke-RestMethod -Uri "$baseUrl/Products/luca" -Method Get -Headers $headers
    $verifyProducts = $verifyResponse.data
    
    $stillExists = $verifyProducts | Where-Object { $_.productCode -eq "HIZ01" }
    
    if ($stillExists) {
        Write-Host "   ⚠️  WARNING: HIZ01 still exists in Luca!" -ForegroundColor Yellow
        Write-Host "   Product may not have been deleted properly." -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ VERIFIED: HIZ01 has been deleted from Luca!" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "   Current product count: $($verifyProducts.Count)" -ForegroundColor Cyan
    
} catch {
    Write-Host "   ⚠️  Could not verify deletion: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST COMPLETED" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "- Product HIZ01 delete operation executed" -ForegroundColor Gray
Write-Host "- Check Luca system to confirm deletion" -ForegroundColor Gray
Write-Host ""
