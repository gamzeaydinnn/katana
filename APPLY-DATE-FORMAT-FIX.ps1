# Apply comprehensive date format fix for all Luca DTOs
# This script adds JsonConverter attributes to ALL DateTime properties in LucaDtos.cs

Write-Host "🔧 Applying comprehensive date format fix..." -ForegroundColor Cyan

$dtoFile = "src/Katana.Core/DTOs/LucaDtos.cs"

if (-not (Test-Path $dtoFile)) {
    Write-Host "❌ File not found: $dtoFile" -ForegroundColor Red
    exit 1
}

Write-Host "📝 Reading $dtoFile..." -ForegroundColor Yellow
$content = Get-Content $dtoFile -Raw

# Count current converters
$currentConverters = ([regex]::Matches($content, '\[JsonConverter\(typeof\(Katana\.Core\.Converters\.Luca')).Count
Write-Host "📊 Current converters: $currentConverters" -ForegroundColor Gray

# Add converters to DateTime properties that don't have them
# Pattern: Find DateTime properties without [JsonConverter] attribute above them

$patterns = @(
    # LucaInvoiceItemDto - garantiBitisTarihi, uretimTarihi (if not already fixed)
    @{
        Old = '    \[JsonPropertyName\("garantiBitisTarihi"\)\]\s+public DateTime\? GarantiBitisTarihi'
        New = '    [JsonPropertyName("garantiBitisTarihi")]
    [JsonConverter(typeof(Katana.Core.Converters.LucaNullableDateConverter))]
    public DateTime? GarantiBitisTarihi'
    },
    @{
        Old = '    \[JsonPropertyName\("uretimTarihi"\)\]\s+public DateTime\? UretimTarihi'
        New = '    [JsonPropertyName("uretimTarihi")]
    [JsonConverter(typeof(Katana.Core.Converters.LucaNullableDateConverter))]
    public DateTime? UretimTarihi'
    },
    # LucaCreateOrderHeaderRequest - opsiyonTarihi, teslimTarihi, onayTarihi
    @{
        Old = '    \[JsonPropertyName\("opsiyonTarihi"\)\]\s+public DateTime\? OpsiyonTarihi'
        New = '    [JsonPropertyName("opsiyonTarihi")]
    [JsonConverter(typeof(Katana.Core.Converters.LucaNullableDateConverter))]
    public DateTime? OpsiyonTarihi'
    },
    @{
        Old = '    \[JsonPropertyName\("teslimTarihi"\)\]\s+public DateTime\? TeslimTarihi'
        New = '    [JsonPropertyName("teslimTarihi")]
    [JsonConverter(typeof(Katana.Core.Converters.LucaNullableDateConverter))]
    public DateTime? TeslimTarihi'
    },
    @{
        Old = '    \[JsonPropertyName\("onayTarihi"\)\]\s+public DateTime\? OnayTarihi'
        New = '    [JsonPropertyName("onayTarihi")]
    [JsonConverter(typeof(Katana.Core.Converters.LucaNullableDateConverter))]
    public DateTime? OnayTarihi'
    }
)

$changesMade = 0
foreach ($pattern in $patterns) {
    $matches = [regex]::Matches($content, $pattern.Old)
    if ($matches.Count -gt 0) {
        Write-Host "  ✓ Found $($matches.Count) occurrences of pattern, applying fix..." -ForegroundColor Green
        $content = $content -replace $pattern.Old, $pattern.New
        $changesMade += $matches.Count
    }
}

if ($changesMade -gt 0) {
    Write-Host "📝 Writing updated file..." -ForegroundColor Yellow
    Set-Content -Path $dtoFile -Value $content -NoNewline
    Write-Host "✅ Applied $changesMade fixes to $dtoFile" -ForegroundColor Green
} else {
    Write-Host "ℹ️ No additional fixes needed - all DateTime properties already have converters" -ForegroundColor Cyan
}

# Verify the fix
$newContent = Get-Content $dtoFile -Raw
$newConverters = ([regex]::Matches($newContent, '\[JsonConverter\(typeof\(Katana\.Core\.Converters\.Luca')).Count
Write-Host "📊 New converter count: $newConverters (added: $($newConverters - $currentConverters))" -ForegroundColor Gray

Write-Host "`n🔨 Now rebuilding backend..." -ForegroundColor Cyan
docker-compose build katana-backend

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Backend rebuilt successfully" -ForegroundColor Green
    Write-Host "`n🔄 Restarting backend..." -ForegroundColor Cyan
    docker-compose restart katana-backend
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Backend restarted successfully" -ForegroundColor Green
        Write-Host "`n⏳ Waiting 10 seconds for backend to start..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        Write-Host "`n🧪 Run test script to verify fix:" -ForegroundColor Cyan
        Write-Host "  .\test-date-format.ps1" -ForegroundColor Gray
    } else {
        Write-Host "❌ Backend restart failed" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Backend build failed" -ForegroundColor Red
}
