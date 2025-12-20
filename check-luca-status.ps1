# Check current Luca stock card status

$baseUrl = "http://localhost:5055"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "LUCA STOCK CARD STATUS CHECK" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# Login
Write-Host "Logging in..." -ForegroundColor Cyan
$loginBody = @{
    Username = "admin"
    Password = "Katana2025!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful`n" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
}

# Get all stock cards from Luca
Write-Host "Fetching all stock cards from Luca..." -ForegroundColor Cyan
try {
    $stockCards = Invoke-RestMethod -Uri "$baseUrl/api/products/luca-stock-cards" -Method Get -Headers $headers
    
    Write-Host "✓ Fetched $($stockCards.Count) stock cards`n" -ForegroundColor Green
    
    # Analyze patterns
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "ANALYSIS" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
    
    # Count version suffixes
    $withV2 = $stockCards | Where-Object { $_.kartKodu -match "-V2$" }
    $withV3 = $stockCards | Where-Object { $_.kartKodu -match "-V3$" }
    $withV4 = $stockCards | Where-Object { $_.kartKodu -match "-V4$" }
    $withV5Plus = $stockCards | Where-Object { $_.kartKodu -match "-V[5-9]$" }
    $withAnyVersion = $stockCards | Where-Object { $_.kartKodu -match "-V\d+$" }
    
    Write-Host "Version Suffixes:" -ForegroundColor Yellow
    Write-Host "  -V2: $($withV2.Count)" -ForegroundColor Cyan
    Write-Host "  -V3: $($withV3.Count)" -ForegroundColor Cyan
    Write-Host "  -V4: $($withV4.Count)" -ForegroundColor Cyan
    Write-Host "  -V5+: $($withV5Plus.Count)" -ForegroundColor Cyan
    Write-Host "  Total with version: $($withAnyVersion.Count)" -ForegroundColor White
    Write-Host "  Without version: $($stockCards.Count - $withAnyVersion.Count)`n" -ForegroundColor White
    
    # Check for concatenation errors
    $concatenationErrors = $stockCards | Where-Object {
        $code = $_.kartKodu
        if ($code.Length -ge 4 -and $code.Length % 2 -eq 0) {
            $half = $code.Length / 2
            $first = $code.Substring(0, $half)
            $second = $code.Substring($half)
            $first -eq $second
        }
    }
    
    Write-Host "Concatenation Errors:" -ForegroundColor Yellow
    Write-Host "  Found: $($concatenationErrors.Count)" -ForegroundColor Cyan
    if ($concatenationErrors.Count -gt 0) {
        Write-Host "  Examples:" -ForegroundColor Gray
        $concatenationErrors | Select-Object -First 5 | ForEach-Object {
            Write-Host "    - $($_.kartKodu) → $($_.kartAdi)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
    
    # Check for encoding issues (? marks)
    $encodingIssues = $stockCards | Where-Object { $_.kartAdi -match "\?" }
    
    Write-Host "Encoding Issues (? marks):" -ForegroundColor Yellow
    Write-Host "  Found: $($encodingIssues.Count)" -ForegroundColor Cyan
    if ($encodingIssues.Count -gt 0) {
        Write-Host "  Examples:" -ForegroundColor Gray
        $encodingIssues | Select-Object -First 5 | ForEach-Object {
            Write-Host "    - $($_.kartKodu) → $($_.kartAdi)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
    
    # Find duplicate names
    $duplicateNames = $stockCards | Group-Object -Property kartAdi | Where-Object { $_.Count -gt 1 } | Sort-Object -Property Count -Descending
    
    Write-Host "Duplicate Names:" -ForegroundColor Yellow
    Write-Host "  Groups with duplicates: $($duplicateNames.Count)" -ForegroundColor Cyan
    Write-Host "  Total duplicate cards: $(($duplicateNames | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)`n" -ForegroundColor White
    
    # Show top 10 duplicates
    Write-Host "Top 10 Duplicate Groups:" -ForegroundColor Yellow
    $topDuplicates = $duplicateNames | Select-Object -First 10
    
    foreach ($group in $topDuplicates) {
        Write-Host "`n  Name: $($group.Name)" -ForegroundColor White
        Write-Host "  Count: $($group.Count)" -ForegroundColor Cyan
        Write-Host "  Codes:" -ForegroundColor Gray
        
        $sortedCodes = $group.Group | Sort-Object {
            if ($_.kartKodu -match "-V(\d+)$") {
                [int]$matches[1]
            } else {
                0
            }
        }, kartKodu
        
        foreach ($card in $sortedCodes | Select-Object -First 5) {
            $version = if ($card.kartKodu -match "-V\d+$") { " [VERSION]" } else { "" }
            $encoding = if ($card.kartAdi -match "\?") { " [ENCODING]" } else { "" }
            Write-Host "    - $($card.kartKodu) (ID: $($card.skartId))$version$encoding" -ForegroundColor DarkGray
        }
        
        if ($group.Count -gt 5) {
            Write-Host "    ... and $($group.Count - 5) more" -ForegroundColor DarkGray
        }
    }
    
    # Save full list to CSV
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "EXPORT" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
    
    $stockCards | Select-Object skartId, kartKodu, kartAdi | Export-Csv -Path "luca-current-stock-cards.csv" -NoTypeInformation -Encoding UTF8
    Write-Host "✓ Full list exported to: luca-current-stock-cards.csv" -ForegroundColor Green
    
    # Export duplicates only
    $duplicateCards = $duplicateNames | ForEach-Object { $_.Group } | Select-Object skartId, kartKodu, kartAdi
    $duplicateCards | Export-Csv -Path "luca-duplicate-cards.csv" -NoTypeInformation -Encoding UTF8
    Write-Host "✓ Duplicates exported to: luca-duplicate-cards.csv" -ForegroundColor Green
    
    # Export version suffixed cards
    $withAnyVersion | Select-Object skartId, kartKodu, kartAdi | Export-Csv -Path "luca-versioned-cards.csv" -NoTypeInformation -Encoding UTF8
    Write-Host "✓ Versioned cards exported to: luca-versioned-cards.csv" -ForegroundColor Green
    
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "SUMMARY" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
    
    Write-Host "Total Stock Cards: $($stockCards.Count)" -ForegroundColor White
    $percentage = [math]::Round($withAnyVersion.Count / $stockCards.Count * 100, 1)
    Write-Host "Cards with version suffix: $($withAnyVersion.Count) ($percentage percent)" -ForegroundColor Cyan
    Write-Host "Concatenation errors: $($concatenationErrors.Count)" -ForegroundColor Cyan
    Write-Host "Encoding issues: $($encodingIssues.Count)" -ForegroundColor Cyan
    Write-Host "Duplicate groups: $($duplicateNames.Count)" -ForegroundColor Cyan
    
    $potentialCleanup = $withAnyVersion.Count + $concatenationErrors.Count
    Write-Host "`nPotential cleanup: ~$potentialCleanup cards" -ForegroundColor Yellow
    
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
