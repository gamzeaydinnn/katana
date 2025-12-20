# Test script for Deduplication API

$baseUrl = "http://localhost:5055"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "DEDUPLICATION API TEST" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# Login first
Write-Host "1. Logging in..." -ForegroundColor Cyan
$loginBody = @{
    Username = "admin"
    Password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 30))...`n" -ForegroundColor Gray
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 1: Get Rules
Write-Host "2. Getting deduplication rules..." -ForegroundColor Cyan
try {
    $rules = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/rules" -Method Get -Headers $headers
    Write-Host "✓ Rules retrieved successfully" -ForegroundColor Green
    Write-Host "  - Total rules: $($rules.rules.Count)" -ForegroundColor Gray
    foreach ($rule in $rules.rules) {
        Write-Host "  - $($rule.name) (Priority: $($rule.priority), Enabled: $($rule.enabled))" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get rules: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Analyze Duplicates
Write-Host "3. Analyzing duplicates..." -ForegroundColor Cyan
Write-Host "   (This may take a while - fetching all stock cards from Luca)" -ForegroundColor Yellow
try {
    $analysis = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/analyze" -Method Post -Headers $headers
    Write-Host "✓ Analysis completed successfully" -ForegroundColor Green
    Write-Host "`nStatistics:" -ForegroundColor White
    Write-Host "  - Total stock cards: $($analysis.statistics.totalStockCards)" -ForegroundColor Gray
    Write-Host "  - Duplicate groups: $($analysis.statistics.duplicateGroups)" -ForegroundColor Gray
    Write-Host "  - Total duplicates: $($analysis.statistics.totalDuplicates)" -ForegroundColor Gray
    Write-Host "  - Versioning duplicates: $($analysis.statistics.versioningDuplicates)" -ForegroundColor Gray
    Write-Host "  - Concatenation errors: $($analysis.statistics.concatenationErrors)" -ForegroundColor Gray
    Write-Host "  - Encoding issues: $($analysis.statistics.encodingIssues)" -ForegroundColor Gray
    Write-Host ""
    
    # Show top 5 duplicate groups
    if ($analysis.duplicateGroups.Count -gt 0) {
        Write-Host "Top 5 Duplicate Groups:" -ForegroundColor White
        $topGroups = $analysis.duplicateGroups | Sort-Object { $_.stockCards.Count } -Descending | Select-Object -First 5
        foreach ($group in $topGroups) {
            Write-Host "  - $($group.stockName) ($($group.category)): $($group.stockCards.Count) cards" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Test 3: Generate Preview
    if ($analysis.duplicateGroups.Count -gt 0) {
        Write-Host "4. Generating preview..." -ForegroundColor Cyan
        $previewBody = $analysis | ConvertTo-Json -Depth 10
        
        try {
            $preview = Invoke-RestMethod -Uri "$baseUrl/api/admin/deduplication/preview" -Method Post -Headers $headers -Body $previewBody
            Write-Host "✓ Preview generated successfully" -ForegroundColor Green
            Write-Host "`nPreview Statistics:" -ForegroundColor White
            Write-Host "  - Total actions: $($preview.statistics.totalActions)" -ForegroundColor Gray
            Write-Host "  - Cards to keep: $($preview.statistics.cardsToKeep)" -ForegroundColor Gray
            Write-Host "  - Cards to remove: $($preview.statistics.cardsToRemove)" -ForegroundColor Gray
            Write-Host "  - Cards to update: $($preview.statistics.cardsToUpdate)" -ForegroundColor Gray
            Write-Host ""
            
            # Show sample actions
            if ($preview.actions.Count -gt 0) {
                Write-Host "Sample Actions (first 3):" -ForegroundColor White
                $sampleActions = $preview.actions | Select-Object -First 3
                foreach ($action in $sampleActions) {
                    Write-Host "  Group: $($action.canonicalCard.stockName)" -ForegroundColor Gray
                    Write-Host "    → Keep: $($action.canonicalCard.stockCode)" -ForegroundColor Green
                    Write-Host "    → Remove: $($action.cardsToRemove.Count) cards" -ForegroundColor Red
                    Write-Host "    → Reason: $($action.reason)" -ForegroundColor Yellow
                    Write-Host ""
                }
            }
            
            Write-Host "`n⚠️  NOTE: Execution is NOT run in this test (would delete real data)" -ForegroundColor Yellow
            Write-Host "To execute deduplication, use the frontend or call:" -ForegroundColor Yellow
            Write-Host "POST $baseUrl/api/admin/deduplication/execute" -ForegroundColor Gray
            Write-Host ""
            
        } catch {
            Write-Host "✗ Failed to generate preview: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "✓ No duplicates found - nothing to preview" -ForegroundColor Green
    }
    
} catch {
    Write-Host "✗ Analysis failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "TEST COMPLETE" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
