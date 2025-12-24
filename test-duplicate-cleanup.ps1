# Test Duplicate Order Cleanup API
# Duplike siparişleri temizleyen endpoint'i test eder

param(
    [switch]$Execute,  # Gerçek silme işlemi için -Execute flag'i gerekli
    [switch]$Force     # Onay sormadan çalıştır
)

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
Write-Host "  DUPLICATE ORDER CLEANUP TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. First, run analysis
Write-Host "1. Running duplicate analysis..." -ForegroundColor Yellow
try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/duplicates/analyze" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "   Total Orders: $($analysis.totalOrders)" -ForegroundColor White
    Write-Host "   Duplicate Groups: $($analysis.duplicateGroups)" -ForegroundColor White
    Write-Host "   Orders to Delete: $($analysis.ordersToDelete)" -ForegroundColor White
    Write-Host ""

    if ($analysis.ordersToDelete -eq 0) {
        Write-Host "   No duplicate orders to clean up!" -ForegroundColor Green
        exit 0
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. Run dry-run cleanup
Write-Host "2. Running DRY-RUN cleanup (no actual deletion)..." -ForegroundColor Yellow
try {
    $body = @{ dryRun = $true } | ConvertTo-Json
    $dryRunResult = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/duplicates/cleanup" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop

    Write-Host "   Success: $($dryRunResult.success)" -ForegroundColor White
    Write-Host "   Was Dry Run: $($dryRunResult.wasDryRun)" -ForegroundColor White
    Write-Host "   Would Delete: $($dryRunResult.ordersDeleted) orders" -ForegroundColor White
    Write-Host "   Duration: $($dryRunResult.duration)" -ForegroundColor White
    Write-Host ""

    if ($dryRunResult.log -and $dryRunResult.log.Count -gt 0) {
        Write-Host "   Log:" -ForegroundColor Cyan
        foreach ($entry in $dryRunResult.log) {
            Write-Host "   - [$($entry.action)] $($entry.details)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. Execute actual cleanup if -Execute flag is provided
if ($Execute) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ACTUAL CLEANUP (NOT DRY-RUN)" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""

    if (-not $Force) {
        $confirm = Read-Host "Are you sure you want to DELETE $($analysis.ordersToDelete) duplicate orders? (yes/no)"
        if ($confirm -ne "yes") {
            Write-Host "Cleanup cancelled." -ForegroundColor Yellow
            exit 0
        }
    }

    Write-Host "3. Executing ACTUAL cleanup..." -ForegroundColor Red
    try {
        $body = @{ dryRun = $false } | ConvertTo-Json
        $result = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/duplicates/cleanup" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop

        Write-Host "   Success: $($result.success)" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
        Write-Host "   Orders Deleted: $($result.ordersDeleted)" -ForegroundColor White
        Write-Host "   Lines Deleted: $($result.linesDeleted)" -ForegroundColor White
        Write-Host "   Duration: $($result.duration)" -ForegroundColor White
        Write-Host ""

        if ($result.errors -and $result.errors.Count -gt 0) {
            Write-Host "   Errors:" -ForegroundColor Red
            foreach ($error in $result.errors) {
                Write-Host "   - $error" -ForegroundColor Red
            }
        }

        if ($result.log -and $result.log.Count -gt 0) {
            Write-Host "   Log:" -ForegroundColor Cyan
            foreach ($entry in $result.log | Select-Object -First 10) {
                Write-Host "   - [$($entry.action)] OrderNo=$($entry.orderNo): $($entry.details)" -ForegroundColor Gray
            }
            if ($result.log.Count -gt 10) {
                Write-Host "   ... and $($result.log.Count - 10) more entries" -ForegroundColor Gray
            }
        }
    }
    catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
} else {
    Write-Host ""
    Write-Host "To execute actual cleanup, run:" -ForegroundColor Yellow
    Write-Host "  .\test-duplicate-cleanup.ps1 -Execute" -ForegroundColor White
    Write-Host "  .\test-duplicate-cleanup.ps1 -Execute -Force  # Skip confirmation" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
