# Test script for Katana cleanup delete endpoint
# This script calls the delete-from-katana endpoint with dry-run and actual deletion

param(
    [switch]$Execute = $false
)

$baseUrl = "http://localhost:5055"
$apiKey = "test-api-key-12345"

Write-Host "=== Katana Cleanup Delete Test ===" -ForegroundColor Cyan
Write-Host ""

# Login to get JWT token
Write-Host "Logging in..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "Login successful" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "X-API-Key" = $apiKey
}

# First, get analysis to see what SKUs exist
Write-Host "Getting analysis..." -ForegroundColor Yellow
try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/analyze" -Method Get -Headers $headers
    
    if ($analysis.orderProducts.Count -eq 0) {
        Write-Host "No products found to delete" -ForegroundColor Yellow
        exit 0
    }
    
    # Get unique SKUs
    $skus = $analysis.orderProducts | Select-Object -ExpandProperty sku -Unique
    Write-Host "Found $($skus.Count) unique SKUs" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host "Failed to get analysis: $_" -ForegroundColor Red
    exit 1
}

# Dry-run deletion
Write-Host "=== DRY RUN MODE ===" -ForegroundColor Yellow
Write-Host "Testing deletion for $($skus.Count) SKUs (no actual deletion will occur)" -ForegroundColor Yellow
Write-Host ""

$dryRunBody = @{
    skus = $skus
    dryRun = $true
} | ConvertTo-Json

try {
    $dryRunResult = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/delete-from-katana" -Method Post -Body $dryRunBody -Headers $headers -ContentType "application/json"
    
    Write-Host "Dry Run Results:" -ForegroundColor Green
    Write-Host "  Total Attempted: $($dryRunResult.totalAttempted)" -ForegroundColor White
    Write-Host "  Success Count: $($dryRunResult.successCount)" -ForegroundColor White
    Write-Host "  Fail Count: $($dryRunResult.failCount)" -ForegroundColor White
    Write-Host "  Duration: $($dryRunResult.duration)" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host "Dry run failed: $_" -ForegroundColor Red
    exit 1
}

# Actual deletion (only if -Execute flag is provided)
if ($Execute) {
    Write-Host "=== ACTUAL DELETION ===" -ForegroundColor Red
    Write-Host "WARNING: This will actually delete products from Katana!" -ForegroundColor Red
    Write-Host ""
    
    $confirmation = Read-Host "Are you sure you want to proceed? (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "Deletion cancelled" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "Deleting $($skus.Count) products from Katana..." -ForegroundColor Yellow
    
    $deleteBody = @{
        skus = $skus
        dryRun = $false
    } | ConvertTo-Json
    
    try {
        $deleteResult = Invoke-RestMethod -Uri "$baseUrl/api/katanacleanup/delete-from-katana" -Method Post -Body $deleteBody -Headers $headers -ContentType "application/json"
        
        Write-Host "Deletion Results:" -ForegroundColor Green
        Write-Host "  Total Attempted: $($deleteResult.totalAttempted)" -ForegroundColor White
        Write-Host "  Success Count: $($deleteResult.successCount)" -ForegroundColor White
        Write-Host "  Fail Count: $($deleteResult.failCount)" -ForegroundColor White
        Write-Host "  Duration: $($deleteResult.duration)" -ForegroundColor White
        Write-Host ""
        
        if ($deleteResult.errors.Count -gt 0) {
            Write-Host "Errors:" -ForegroundColor Red
            $deleteResult.errors | ForEach-Object {
                Write-Host "  $($_.message): $($_.details)" -ForegroundColor Red
            }
            Write-Host ""
        }
        
        Write-Host "Deletion completed" -ForegroundColor Green
        
    } catch {
        Write-Host "Deletion failed: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "To execute actual deletion, run with -Execute flag:" -ForegroundColor Yellow
    Write-Host "  .\test-katana-cleanup-delete.ps1 -Execute" -ForegroundColor White
    Write-Host ""
}
