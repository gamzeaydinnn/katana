# Test Deduplication API

$baseUrl = "http://localhost:5055"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "DEDUPLICATION API TEST" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# Step 1: Login
Write-Host "1. Logging in..." -ForegroundColor Cyan
$loginBody = @{
    Username = "admin"
    Password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Analyze Duplicates
Write-Host "`n2. Analyzing duplicates..." -ForegroundColor Cyan
try {
    $analysisResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/analyze" -Method Post -Headers $headers
    
    Write-Host "✓ Analysis complete!" -ForegroundColor Green
    Write-Host "`nStatistics:" -ForegroundColor Yellow
    Write-Host "  Total Stock Cards: $($analysisResponse.statistics.totalStockCards)" -ForegroundColor White
    Write-Host "  Duplicate Groups: $($analysisResponse.statistics.duplicateGroups)" -ForegroundColor White
    Write-Host "  Total Duplicates: $($analysisResponse.statistics.totalDuplicates)" -ForegroundColor White
    Write-Host "  Versioning: $($analysisResponse.statistics.versioningDuplicates)" -ForegroundColor Cyan
    Write-Host "  Concatenation: $($analysisResponse.statistics.concatenationErrors)" -ForegroundColor Cyan
    Write-Host "  Encoding: $($analysisResponse.statistics.encodingIssues)" -ForegroundColor Cyan
    
    # Show top 10 duplicate groups
    Write-Host "`nTop 10 Duplicate Groups:" -ForegroundColor Yellow
    $topGroups = $analysisResponse.duplicateGroups | Sort-Object { $_.stockCards.Count } -Descending | Select-Object -First 10
    
    foreach ($group in $topGroups) {
        Write-Host "`n  Group: $($group.stockName)" -ForegroundColor White
        Write-Host "  Category: $($group.category)" -ForegroundColor Magenta
        Write-Host "  Count: $($group.stockCards.Count)" -ForegroundColor Gray
        Write-Host "  Codes:" -ForegroundColor Gray
        foreach ($card in $group.stockCards | Select-Object -First 5) {
            $issues = if ($card.issueDescription) { " [$($card.issueDescription)]" } else { "" }
            Write-Host "    - $($card.stockCode) (ID: $($card.skartId))$issues" -ForegroundColor DarkGray
        }
        if ($group.stockCards.Count -gt 5) {
            Write-Host "    ... and $($group.stockCards.Count - 5) more" -ForegroundColor DarkGray
        }
    }
    
    # Save analysis for preview
    $analysisResponse | ConvertTo-Json -Depth 10 | Out-File "analysis-result.json"
    Write-Host "`n✓ Analysis saved to analysis-result.json" -ForegroundColor Green
    
    # Step 3: Generate Preview
    Write-Host "`n3. Generating preview..." -ForegroundColor Cyan
    $previewBody = $analysisResponse | ConvertTo-Json -Depth 10
    
    $previewResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/preview" -Method Post -Headers $headers -Body $previewBody
    
    Write-Host "✓ Preview generated!" -ForegroundColor Green
    Write-Host "`nPreview Statistics:" -ForegroundColor Yellow
    Write-Host "  Total Actions: $($previewResponse.statistics.totalActions)" -ForegroundColor White
    Write-Host "  Cards to Keep: $($previewResponse.statistics.cardsToKeep)" -ForegroundColor Green
    Write-Host "  Cards to Remove: $($previewResponse.statistics.cardsToRemove)" -ForegroundColor Red
    Write-Host "  Cards to Update: $($previewResponse.statistics.cardsToUpdate)" -ForegroundColor Yellow
    
    # Show sample actions
    Write-Host "`nSample Actions (first 5):" -ForegroundColor Yellow
    $sampleActions = $previewResponse.actions | Select-Object -First 5
    
    foreach ($action in $sampleActions) {
        Write-Host "`n  Action for: $($action.canonicalCard.stockName)" -ForegroundColor White
        Write-Host "  Keep: $($action.canonicalCard.stockCode) (ID: $($action.canonicalCard.skartId))" -ForegroundColor Green
        Write-Host "  Remove: $($action.cardsToRemove.Count) cards" -ForegroundColor Red
        Write-Host "  Reason: $($action.reason)" -ForegroundColor Gray
        Write-Host "  Type: $($action.type)" -ForegroundColor Magenta
    }
    
    # Save preview
    $previewResponse | ConvertTo-Json -Depth 10 | Out-File "preview-result.json"
    Write-Host "`n✓ Preview saved to preview-result.json" -ForegroundColor Green
    
    # Step 4: Get Rules
    Write-Host "`n4. Getting deduplication rules..." -ForegroundColor Cyan
    $rulesResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/rules" -Method Get -Headers $headers
    
    Write-Host "✓ Rules retrieved!" -ForegroundColor Green
    Write-Host "`nActive Rules:" -ForegroundColor Yellow
    foreach ($rule in $rulesResponse.rules | Where-Object { $_.enabled }) {
        Write-Host "  $($rule.priority). $($rule.name) ($($rule.type))" -ForegroundColor Cyan
    }
    Write-Host "`nDefault Rule:" -ForegroundColor Yellow
    Write-Host "  $($rulesResponse.defaultRule.name) ($($rulesResponse.defaultRule.type))" -ForegroundColor Cyan
    
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "TEST COMPLETE" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "`nNext Steps:" -ForegroundColor White
Write-Host "  1. Review analysis-result.json" -ForegroundColor Gray
Write-Host "  2. Review preview-result.json" -ForegroundColor Gray
Write-Host "  3. If satisfied, execute deduplication (CAREFUL!)" -ForegroundColor Gray
Write-Host "`nTo execute (USE WITH CAUTION):" -ForegroundColor Red
Write-Host '  $preview = Get-Content preview-result.json | ConvertFrom-Json' -ForegroundColor Gray
Write-Host '  $body = $preview | ConvertTo-Json -Depth 10' -ForegroundColor Gray
Write-Host '  Invoke-RestMethod -Uri "http://localhost:5055/api/admin/deduplication/execute" -Method Post -Headers $headers -Body $body' -ForegroundColor Gray
