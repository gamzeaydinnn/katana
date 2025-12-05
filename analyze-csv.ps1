# Analyze duplicates from CSV file

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "LUCA DUPLICATE ANALYSIS" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# Load data from CSV
$stockCards = Import-Csv -Path "sample-stock-data.csv"

Write-Host "Loaded $($stockCards.Count) stock cards from CSV`n" -ForegroundColor Green

# Group by name
$duplicateGroups = $stockCards | Group-Object -Property Name | Where-Object { $_.Count -gt 1 }

Write-Host "Total stock cards: $($stockCards.Count)" -ForegroundColor White
Write-Host "Duplicate groups: $($duplicateGroups.Count)" -ForegroundColor White
Write-Host "Total duplicates: $(($duplicateGroups | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)`n" -ForegroundColor White

# Counters
$versioningCount = 0
$concatenationCount = 0
$encodingCount = 0
$mixedCount = 0

# Analyze each group
foreach ($group in $duplicateGroups | Sort-Object -Property Count -Descending) {
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    Write-Host "Product Name: $($group.Name)" -ForegroundColor Yellow
    Write-Host "Duplicate Count: $($group.Count)" -ForegroundColor White
    
    # Detect issues
    $hasVersion = $false
    $hasConcatenation = $false
    $hasEncoding = $false
    
    foreach ($card in $group.Group) {
        # Check for version suffix
        if ($card.Code -match "-V\d+$") {
            $hasVersion = $true
        }
        
        # Check for concatenation
        $code = $card.Code
        if ($code.Length -ge 4 -and $code.Length % 2 -eq 0) {
            $half = $code.Length / 2
            $firstHalf = $code.Substring(0, $half)
            $secondHalf = $code.Substring($half)
            if ($firstHalf -eq $secondHalf) {
                $hasConcatenation = $true
            }
        }
        
        $name = $card.Name
        if ($name.Length -ge 4 -and $name.Length % 2 -eq 0) {
            $half = $name.Length / 2
            $firstHalf = $name.Substring(0, $half)
            $secondHalf = $name.Substring($half)
            if ($firstHalf -eq $secondHalf) {
                $hasConcatenation = $true
            }
        }
        
        # Check for encoding issues
        if ($card.Name -match "\?") {
            $hasEncoding = $true
        }
    }
    
    # Determine category
    $categories = @()
    if ($hasVersion) { $categories += "Versioning" }
    if ($hasConcatenation) { $categories += "Concatenation" }
    if ($hasEncoding) { $categories += "Encoding" }
    
    if ($categories.Count -gt 1) {
        $category = "Mixed"
        $mixedCount++
    } elseif ($categories.Count -eq 1) {
        $category = $categories[0]
        if ($category -eq "Versioning") { $versioningCount++ }
        elseif ($category -eq "Concatenation") { $concatenationCount++ }
        elseif ($category -eq "Encoding") { $encodingCount++ }
    } else {
        $category = "Unknown"
    }
    
    Write-Host "Category: $category" -ForegroundColor Magenta
    if ($categories.Count -gt 1) {
        Write-Host "  Issues: $($categories -join ', ')" -ForegroundColor DarkMagenta
    }
    
    Write-Host "`nStock Codes:" -ForegroundColor Gray
    
    # Sort by version number
    $sortedCards = $group.Group | Sort-Object {
        if ($_.Code -match "-V(\d+)$") {
            [int]$matches[1]
        } else {
            0
        }
    }, Code
    
    foreach ($card in $sortedCards) {
        $issues = @()
        
        if ($card.Code -match "-V\d+$") {
            $issues += "[VERSION]"
        }
        
        # Check concatenation in code
        $code = $card.Code
        if ($code.Length -ge 4 -and $code.Length % 2 -eq 0) {
            $half = $code.Length / 2
            if ($code.Substring(0, $half) -eq $code.Substring($half)) {
                $corrected = $code.Substring(0, $half)
                $issues += "[CONCAT-CODE → $corrected]"
            }
        }
        
        # Check concatenation in name
        $name = $card.Name
        if ($name.Length -ge 4 -and $name.Length % 2 -eq 0) {
            $half = $name.Length / 2
            if ($name.Substring(0, $half) -eq $name.Substring($half)) {
                $corrected = $name.Substring(0, $half)
                $issues += "[CONCAT-NAME → $corrected]"
            }
        }
        
        if ($card.Name -match "\?") {
            $issues += "[ENCODING]"
        }
        
        $issueStr = if ($issues.Count -gt 0) { " $($issues -join ' ')" } else { "" }
        Write-Host "  - $($card.Code) (ID: $($card.SkartId))$issueStr" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "SUMMARY BY CATEGORY" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow
Write-Host "Versioning duplicates: $versioningCount groups" -ForegroundColor Cyan
Write-Host "Concatenation errors: $concatenationCount groups" -ForegroundColor Cyan
Write-Host "Encoding issues: $encodingCount groups" -ForegroundColor Cyan
Write-Host "Mixed issues: $mixedCount groups" -ForegroundColor Cyan

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "RECOMMENDATIONS" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow
Write-Host "1. Versioning duplicates ($versioningCount groups):" -ForegroundColor White
Write-Host "   → Keep base version (without -V suffix) or lowest version" -ForegroundColor Gray
Write-Host "   → Delete higher versions (-V2, -V3, -V4, etc.)`n" -ForegroundColor Gray

Write-Host "2. Concatenation errors ($concatenationCount groups):" -ForegroundColor White
Write-Host "   → Fix the malformed codes/names to their corrected values" -ForegroundColor Gray
Write-Host "   → Example: BFM-01BFM-01 → BFM-01`n" -ForegroundColor Gray

Write-Host "3. Encoding issues ($encodingCount groups):" -ForegroundColor White
Write-Host "   → Keep properly encoded Turkish characters (Ş, İ, Ğ, Ç, Ö, Ü)" -ForegroundColor Gray
Write-Host "   → Delete versions with ? marks`n" -ForegroundColor Gray

Write-Host "4. Mixed issues ($mixedCount groups):" -ForegroundColor White
Write-Host "   → Apply multiple rules in priority order" -ForegroundColor Gray
Write-Host "   → Fix encoding first, then handle versioning`n" -ForegroundColor Gray
