# Simple Deduplication API Test

$baseUrl = "http://localhost:5055"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "DEDUPLICATION API TEST" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# Login
Write-Host "1. Logging in..." -ForegroundColor Cyan
$loginBody = '{"Username":"admin","Password":"Katana2025!"}'

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "OK - Token received" -ForegroundColor Green
Write-Host ""

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get Rules
Write-Host "2. Getting rules..." -ForegroundColor Cyan
$rules = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/rules" -Method Get -Headers $headers
Write-Host "OK - Rules: $($rules.rules.Count)" -ForegroundColor Green
Write-Host ""

# Analyze
Write-Host "3. Analyzing duplicates (this may take a while)..." -ForegroundColor Cyan
$analysis = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/analyze" -Method Post -Headers $headers
Write-Host "OK - Analysis complete" -ForegroundColor Green
Write-Host "  Total cards: $($analysis.statistics.totalStockCards)" -ForegroundColor Gray
Write-Host "  Duplicate groups: $($analysis.statistics.duplicateGroups)" -ForegroundColor Gray
Write-Host "  Total duplicates: $($analysis.statistics.totalDuplicates)" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "TEST COMPLETE" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
