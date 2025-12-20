# Simple PowerShell script to analyze duplicates from sample data
# Based on user's original report

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "LUCA DUPLICATE ANALYSIS" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

# Sample data from user's original report
$stockCards = @"
1,9310011,NETSİS KONTROL ET1
2,9310011-V2,NETSİS KONTROL ET1
3,9310011-V3,NETSİS KONTROL ET1
    @{ SkartId=4; Code="9310011-V4"; Name="NETSİS KONTROL ET1" },
    
    # Versioning examples - NETSİS KONTROL ET2
    @{ SkartId=5; Code="9310024"; Name="NETSİS KONTROL ET2" },
    @{ SkartId=6; Code="9310024-V2"; Name="NETSİS KONTROL ET2" },
    @{ SkartId=7; Code="9310024-V3"; Name="NETSİS KONTROL ET2" },
    @{ SkartId=8; Code="9310024-V4"; Name="NETSİS KONTROL ET2" },
    
    # Mixed: Versioning + Encoding - KROM TALA?
    @{ SkartId=9; Code="BFM-01"; Name="KROM TALA?" },
    @{ SkartId=10; Code="BFM-01-V2"; Name="KROM TALA?" },
    @{ SkartId=11; Code="BFM-01-V3"; Name="KROM TALA?" },
    @{ SkartId=12; Code="BFM-01-V4"; Name="KROM TALA?" },
    
    # Versioning - KROM TALAŞ (correct encoding)
    @{ SkartId=13; Code="BFM-01-V5"; Name="KROM TALAŞ" },
    @{ SkartId=14; Code="BFM-01-V6"; Name="KROM TALAŞ" },
    @{ SkartId=15; Code="BFM-01-V7"; Name="KROM TALAŞ" },
    
    # Concatenation errors
    @{ SkartId=16; Code="BFM-01BFM-01"; Name="KROM TALA?KROM TALA?" },
    @{ SkartId=17; Code="HIZ01HIZ01"; Name="%1 KDV Lİ MUHTELİF ALIMLAR%1 KDV Lİ MUHTELİF ALIMLAR" },
    @{ SkartId=18; Code="93100119310011"; Name="NETSİS KONTROL ET1NETSİS KONTROL ET1" },
    @{ SkartId=19; Code="silll12344silll12344"; Name="silllsilll" },
    
    # Versioning + Encoding - %1 KDV
    @{ SkartId=20; Code="HIZ01"; Name="%1 KDV L? MUHTEL?F ALIMLAR" },
    @{ SkartId=21; Code="HIZ01-V2"; Name="%1 KDV L? MUHTEL?F ALIMLAR" },
    @{ SkartId=22; Code="HIZ01-V3"; Name="%1 KDV L? MUHTEL?F ALIMLAR" },
    @{ SkartId=23; Code="HIZ01-V4"; Name="%1 KDV L? MUHTEL?F ALIMLAR" },
    
    # More versioning examples
    @{ SkartId=24; Code="14206-HRT 1"; Name="NETSİSTEN KONTROL 505" },
    @{ SkartId=25; Code="14206-HRT 1-V2"; Name="NETSİSTEN KONTROL 505" },
    @{ SkartId=26; Code="14206-HRT 1-V3"; Name="NETSİSTEN KONTROL 505" },
    
    @{ SkartId=27; Code="1-1020-FLANS"; Name="NETSİSTEN KONTROL 505" },
    @{ SkartId=28; Code="1-1020-FLANS-V2"; Name="NETSİSTEN KONTROL 505" },
    @{ SkartId=29; Code="1-1020-FLANS-V3"; Name="NETSİSTEN KONTROL 505" },
    
    # PIPE examples (different codes, same name)
    @{ SkartId=30; Code="PIPE_1"; Name="COPPER PIPE" },
    @{ SkartId=31; Code="PIPE_1-V2"; Name="COPPER PIPE" },
    @{ SkartId=32; Code="PIPE_2"; Name="COPPER PIPE" },
    @{ SkartId=33; Code="PIPE_2-V2"; Name="COPPER PIPE" },
    @{ SkartId=34; Code="PIPE_7"; Name="COPPER PIPE" },
    @{ SkartId=35; Code="PIPE_7-V2"; Name="COPPER PIPE" },
    
    @{ SkartId=36; Code="PIPE_3"; Name="O35*1,5 PIPE" },
    @{ SkartId=37; Code="PIPE_3-V2"; Name="O35*1,5 PIPE" },
    @{ SkartId=38; Code="PIPE_4"; Name="O35*1,5 PIPE" },
    @{ SkartId=39; Code="PIPE_4-V2"; Name="O35*1,5 PIPE" },
    @{ SkartId=40; Code="PIPE_5"; Name="O35*1,5 PIPE" },
    @{ SkartId=41; Code="PIPE_5-V2"; Name="O35*1,5 PIPE" }
)

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
