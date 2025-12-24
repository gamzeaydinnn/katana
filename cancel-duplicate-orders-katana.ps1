# Cancel Duplicate Orders in Katana API
# Database'den silinen tekrar eden siparişleri Katana API'de cancelled yapar

param(
    [switch]$Execute,      # Gerçek iptal işlemi için -Execute flag'i gerekli
    [switch]$Force,        # Onay sormadan çalıştır
    [long[]]$OrderIds,     # Belirli Katana Order ID'leri (opsiyonel)
    [int]$DaysBack = 30    # Kaç gün geriye bakılacak (varsayılan 30)
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
Write-Host "  KATANA DUPLICATE ORDER CANCELLATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Eğer belirli OrderIds verilmişse onları kullan
if ($OrderIds -and $OrderIds.Count -gt 0) {
    Write-Host "Using provided Katana Order IDs: $($OrderIds -join ', ')" -ForegroundColor Yellow
    $katanaOrderIds = $OrderIds
} else {
    # 1. Önce Katana'daki duplicate siparişleri bul
    Write-Host "1. Finding duplicate orders in Katana (last $DaysBack days)..." -ForegroundColor Yellow
    try {
        $fromDate = (Get-Date).AddDays(-$DaysBack).ToString("yyyy-MM-dd")
        $duplicates = Invoke-RestMethod -Uri "$baseUrl/api/KatanaCleanup/duplicate-orders?fromDate=$fromDate" `
            -Method GET `
            -Headers $headers `
            -ErrorAction Stop

        if (-not $duplicates -or $duplicates.Count -eq 0) {
            Write-Host "   No duplicate orders found in Katana!" -ForegroundColor Green
            exit 0
        }

        Write-Host "   Found $($duplicates.Count) duplicate order groups" -ForegroundColor White
        Write-Host ""

        # Her grup için detay göster
        $katanaOrderIds = @()
        foreach ($orderNo in $duplicates.PSObject.Properties.Name) {
            $ids = $duplicates.$orderNo
            Write-Host "   OrderNo: $orderNo" -ForegroundColor Cyan
            Write-Host "      IDs: $($ids -join ', ')" -ForegroundColor Gray
            
            # İlk ID'yi tut, diğerlerini iptal listesine ekle
            if ($ids.Count -gt 1) {
                $keepId = $ids | Sort-Object | Select-Object -First 1
                $cancelIds = $ids | Where-Object { $_ -ne $keepId }
                Write-Host "      Keep: $keepId, Cancel: $($cancelIds -join ', ')" -ForegroundColor Yellow
                $katanaOrderIds += $cancelIds
            }
        }
        Write-Host ""
        Write-Host "   Total orders to cancel: $($katanaOrderIds.Count)" -ForegroundColor White
    }
    catch {
        Write-Host "   ERROR finding duplicates: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
        exit 1
    }
}

if ($katanaOrderIds.Count -eq 0) {
    Write-Host "No orders to cancel." -ForegroundColor Green
    exit 0
}

# 2. Dry-run önce
Write-Host ""
Write-Host "2. Running DRY-RUN cancellation..." -ForegroundColor Yellow
try {
    $body = @{
        katanaOrderIds = @($katanaOrderIds)
        dryRun = $true
    } | ConvertTo-Json

    $dryRunResult = Invoke-RestMethod -Uri "$baseUrl/api/KatanaCleanup/cancel-duplicate-orders" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop

    Write-Host "   Total Attempted: $($dryRunResult.totalAttempted)" -ForegroundColor White
    Write-Host "   Would Cancel: $($dryRunResult.successCount)" -ForegroundColor White
    Write-Host "   Dry Run: $($dryRunResult.dryRun)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "   ERROR in dry-run: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. Gerçek iptal işlemi
if ($Execute) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ACTUAL CANCELLATION (NOT DRY-RUN)" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""

    if (-not $Force) {
        Write-Host "Orders to cancel:" -ForegroundColor Yellow
        $katanaOrderIds | ForEach-Object { Write-Host "   - $_" -ForegroundColor Gray }
        Write-Host ""
        $confirm = Read-Host "Are you sure you want to CANCEL $($katanaOrderIds.Count) orders in Katana? (yes/no)"
        if ($confirm -ne "yes") {
            Write-Host "Cancellation aborted." -ForegroundColor Yellow
            exit 0
        }
    }

    Write-Host "3. Executing ACTUAL cancellation in Katana..." -ForegroundColor Red
    try {
        $body = @{
            katanaOrderIds = @($katanaOrderIds)
            dryRun = $false
        } | ConvertTo-Json

        $result = Invoke-RestMethod -Uri "$baseUrl/api/KatanaCleanup/cancel-duplicate-orders" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop

        Write-Host ""
        Write-Host "   Success: $($result.success)" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
        Write-Host "   Total Attempted: $($result.totalAttempted)" -ForegroundColor White
        Write-Host "   Successfully Cancelled: $($result.successCount)" -ForegroundColor Green
        Write-Host "   Failed: $($result.failCount)" -ForegroundColor $(if ($result.failCount -gt 0) { "Red" } else { "White" })
        Write-Host ""

        if ($result.cancelledOrderIds -and $result.cancelledOrderIds.Count -gt 0) {
            Write-Host "   Cancelled Order IDs:" -ForegroundColor Green
            $result.cancelledOrderIds | ForEach-Object { Write-Host "      - $_" -ForegroundColor Gray }
        }

        if ($result.failedOrderIds -and $result.failedOrderIds.Count -gt 0) {
            Write-Host ""
            Write-Host "   Failed Order IDs:" -ForegroundColor Red
            $result.failedOrderIds | ForEach-Object { Write-Host "      - $_" -ForegroundColor Red }
        }

        if ($result.errors -and $result.errors.Count -gt 0) {
            Write-Host ""
            Write-Host "   Errors:" -ForegroundColor Red
            foreach ($err in $result.errors) {
                Write-Host "      - $($err.message)" -ForegroundColor Red
                if ($err.details) {
                    Write-Host "        Details: $($err.details)" -ForegroundColor Gray
                }
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
    Write-Host "To execute actual cancellation, run:" -ForegroundColor Yellow
    Write-Host "  .\cancel-duplicate-orders-katana.ps1 -Execute" -ForegroundColor White
    Write-Host "  .\cancel-duplicate-orders-katana.ps1 -Execute -Force  # Skip confirmation" -ForegroundColor White
    Write-Host ""
    Write-Host "To cancel specific order IDs:" -ForegroundColor Yellow
    Write-Host "  .\cancel-duplicate-orders-katana.ps1 -OrderIds 12345,12346,12347 -Execute" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
