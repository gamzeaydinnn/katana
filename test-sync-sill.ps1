# Test sync for sill product
$baseUrl = "http://localhost:5055"

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{ Username = "admin"; Password = "Katana2025!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ "Authorization" = "Bearer $($loginResponse.token)" }

Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Trigger sync with limit=5
Write-Host "`nTriggering sync (limit=5, dryRun=false)..." -ForegroundColor Cyan
$syncBody = @{ 
    limit = 5
    dryRun = $false
    forceSendDuplicates = $false
} | ConvertTo-Json

$syncResult = Invoke-RestMethod -Uri "$baseUrl/api/sync/to-luca/stock-cards" -Method Post -Body $syncBody -ContentType "application/json" -Headers $headers

Write-Host "`n=== SYNC RESULT ===" -ForegroundColor Yellow
Write-Host "Success: $($syncResult.isSuccess)" -ForegroundColor $(if ($syncResult.isSuccess) { "Green" } else { "Red" })
Write-Host "Processed: $($syncResult.processedRecords)"
Write-Host "Successful: $($syncResult.successfulRecords)"
Write-Host "Failed: $($syncResult.failedRecords)"
Write-Host "Message: $($syncResult.message)"

if ($syncResult.errors -and $syncResult.errors.Count -gt 0) {
    Write-Host "`nErrors:" -ForegroundColor Red
    $syncResult.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

Write-Host "`n✅ Done" -ForegroundColor Green
