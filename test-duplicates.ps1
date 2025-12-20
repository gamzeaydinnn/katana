# Test script to detect duplicates in Luca stock cards
# This will call the Luca API and analyze duplicates

$baseUrl = "http://localhost:5055"

Write-Host "Fetching stock cards from Luca..." -ForegroundColor Cyan

# First, we need to login to get a token
$loginBody = @{
    Username = "admin"
    Password = "admin123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    Write-Host "✓ Logged in successfully" -ForegroundColor Green
    
    # Get all stock cards
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    Write-Host "Fetching stock cards..." -ForegroundColor Cyan
    $stockCards = Invoke-RestMethod -Uri "$baseUrl/api/admin/luca/stock-cards" -Method Get -Headers $headers
    
    Write-Host "✓ Fetched $($stockCards.Count) stock cards" -ForegroundColor Green
    
    # Group by name to find duplicates
    $duplicates = $stockCards | Group-Object -Property productName | Where-Object { $_.Count -gt 1 }
    
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "DUPLICATE ANALYSIS" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
    
    Write-Host "Total stock cards: $($stockCards.Count)" -ForegroundColor White
    Write-Host "Duplicate groups: $($duplicates.Count)" -ForegroundColor White
    Write-Host "Total duplicates: $(($duplicates | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)`n" -ForegroundColor White
    
    # Analyze each duplicate group
    $versioningCount = 0
    $concatenationCount = 0
    $encodingCount = 0
    
    foreach ($group in $duplicates | Sort-Object -Property Count -Descending | Select-Object -First 20) {
        Write-Host "----------------------------------------" -ForegroundColor Cyan
        Write-Host "Product Name: $($group.Name)" -ForegroundColor Yellow
        Write-Host "Duplicate Count: $($group.Count)" -ForegroundColor White
        
        # Detect category
        $hasVersion = $false
        $hasConcatenation = $false
        $hasEncoding = $false
        
        foreach ($card in $group.Group) {
            # Check for version suffix
            if ($card.productCode -match "-V\d+$") {
                $hasVersion = $true
            }
            
            # Check for concatenation (simple check)
            $code = $card.productCode
            if ($code.Length -gt 4) {
                $half = [Math]::Floor($code.Length / 2)
                $firstHalf = $code.Substring(0, $half)
                $secondHalf = $code.Substring($half)
                if ($firstHalf -eq $secondHalf) {
                    $hasConcatenation = $true
                }
            }
            
            # Check for encoding issues
            if ($card.productName -match "\?") {
                $hasEncoding = $true
            }
        }
        
        # Determine category
        $category = @()
        if ($hasVersion) { $category += "Versioning"; $versioningCount++ }
        if ($hasConcatenation) { $category += "Concatenation"; $concatenationCount++ }
        if ($hasEncoding) { $category += "Encoding"; $encodingCount++ }
        
        if ($category.Count -eq 0) { $category += "Unknown" }
        
        Write-Host "Category: $($category -join ', ')" -ForegroundColor Magenta
        
        Write-Host "`nStock Codes:" -ForegroundColor Gray
        foreach ($card in $group.Group | Sort-Object -Property productCode) {
            $issues = @()
            if ($card.productCode -match "-V\d+$") { $issues += "[VERSION]" }
            if ($card.productName -match "\?") { $issues += "[ENCODING]" }
            
            $issueStr = if ($issues.Count -gt 0) { " $($issues -join ' ')" } else { "" }
            Write-Host "  - $($card.productCode) (ID: $($card.skartId))$issueStr" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "SUMMARY BY CATEGORY" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
    Write-Host "Versioning duplicates: $versioningCount groups" -ForegroundColor Cyan
    Write-Host "Concatenation errors: $concatenationCount groups" -ForegroundColor Cyan
    Write-Host "Encoding issues: $encodingCount groups" -ForegroundColor Cyan
    
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Red
}
