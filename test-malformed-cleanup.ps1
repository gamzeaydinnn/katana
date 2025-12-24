# Test Malformed Order Cleanup API
# Bozuk OrderNo'ları temizleyen endpoint'i test eder

param(
    [switch]$Execute,  # Gerçek işlem için -Execute flag'i gerekli
    [switch]$Force,    # Onay sormadan çalıştır
    [string]$CheckOrderNo  # Belirli bir OrderNo'yu kontrol et
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
Write-Host "  MALFORMED ORDER CLEANUP TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check specific OrderNo if provided
if ($CheckOrderNo) {
    Write-Host "Checking OrderNo: $CheckOrderNo" -ForegroundColor Yellow
    try {
        $checkResult = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/check-orderno/$CheckOrderNo" `
            -Method GET `
            -Headers $headers `
            -ErrorAction Stop

        Write-Host "   OrderNo: $($checkResult.orderNo)" -ForegroundColor White
        Write-Host "   Is Malformed: $($checkResult.isMalformed)" -ForegroundColor $(if ($checkResult.isMalformed) { "Red" } else { "Green" })
        Write-Host "   Corrected: $($checkResult.correctedOrderNo)" -ForegroundColor White
        Write-Host "   Needs Correction: $($checkResult.needsCorrection)" -ForegroundColor $(if ($checkResult.needsCorrection) { "Yellow" } else { "Green" })
    }
    catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
    exit 0
}

# 1. Run analysis
Write-Host "1. Analyzing malformed order numbers..." -ForegroundColor Yellow
try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/malformed/analyze" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "   Total Malformed: $($analysis.totalMalformed)" -ForegroundColor White
    Write-Host "   Can Merge: $($analysis.canMerge)" -ForegroundColor White
    Write-Host "   Can Rename: $($analysis.canRename)" -ForegroundColor White
    Write-Host ""

    if ($analysis.totalMalformed -eq 0) {
        Write-Host "   No malformed order numbers to clean up!" -ForegroundColor Green
        exit 0
    }

    if ($analysis.orders -and $analysis.orders.Count -gt 0) {
        Write-Host "   Malformed Orders:" -ForegroundColor Cyan
        foreach ($order in $analysis.orders | Select-Object -First 10) {
            $actionColor = if ($order.action -eq "Merge") { "Yellow" } else { "Blue" }
            Write-Host "   - ID=$($order.id): '$($order.currentOrderNo)' -> '$($order.correctOrderNo)' [$($order.action)]" -ForegroundColor $actionColor
            if ($order.mergeTargetId) {
                Write-Host "     (Will merge into order ID: $($order.mergeTargetId))" -ForegroundColor Gray
            }
        }
        if ($analysis.orders.Count -gt 10) {
            Write-Host "   ... and $($analysis.orders.Count - 10) more" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. Run dry-run cleanup
Write-Host "2. Running DRY-RUN cleanup (no actual changes)..." -ForegroundColor Yellow
try {
    $body = @{ dryRun = $true } | ConvertTo-Json
    $dryRunResult = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/malformed/cleanup" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop

    Write-Host "   Success: $($dryRunResult.success)" -ForegroundColor White
    Write-Host "   Was Dry Run: $($dryRunResult.wasDryRun)" -ForegroundColor White
    Write-Host "   Would Merge: $($dryRunResult.ordersMerged) orders" -ForegroundColor White
    Write-Host "   Would Rename: $($dryRunResult.ordersRenamed) orders" -ForegroundColor White
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
        $totalChanges = $analysis.canMerge + $analysis.canRename
        $confirm = Read-Host "Are you sure you want to process $totalChanges malformed orders? (yes/no)"
        if ($confirm -ne "yes") {
            Write-Host "Cleanup cancelled." -ForegroundColor Yellow
            exit 0
        }
    }

    Write-Host "3. Executing ACTUAL cleanup..." -ForegroundColor Red
    try {
        $body = @{ dryRun = $false } | ConvertTo-Json
        $result = Invoke-RestMethod -Uri "$baseUrl/api/admin/orders/malformed/cleanup" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop

        Write-Host "   Success: $($result.success)" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
        Write-Host "   Orders Merged: $($result.ordersMerged)" -ForegroundColor White
        Write-Host "   Orders Renamed: $($result.ordersRenamed)" -ForegroundColor White
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
    Write-Host "  .\test-malformed-cleanup.ps1 -Execute" -ForegroundColor White
    Write-Host "  .\test-malformed-cleanup.ps1 -Execute -Force  # Skip confirmation" -ForegroundColor White
    Write-Host ""
    Write-Host "To check a specific OrderNo:" -ForegroundColor Yellow
    Write-Host "  .\test-malformed-cleanup.ps1 -CheckOrderNo 'SO-SO-84'" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
