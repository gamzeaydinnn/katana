# Check Invoice JSON from logs
Write-Host "=== Checking Invoice JSON from Docker Logs ===" -ForegroundColor Cyan

# Get last 200 lines from docker logs and filter for CreateInvoice JSON
docker logs katana-api-1 --tail 500 2>&1 | Select-String -Pattern "CreateInvoice - Sending JSON" -Context 0,5 | Select-Object -Last 1

Write-Host "`n=== Full JSON ===" -ForegroundColor Yellow
docker logs katana-api-1 --tail 500 2>&1 | Select-String -Pattern "Sending JSON:" -Context 0,0 | Select-Object -Last 1
