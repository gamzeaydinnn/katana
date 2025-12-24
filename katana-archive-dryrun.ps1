# Katana Product Archive - Dry Run Script
# Database'deki urunler baz alinir, Katana'da olup DB'de olmayan urunler arsivlenir
# Bu script sadece preview yapar, gercek arsivleme yapmaz

param(
    [switch]$Execute,  # Gercek arsivleme icin -Execute flag'i kullan
    [string]$BaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  KATANA PRODUCT ARCHIVE - DRY RUN SCRIPT  " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($Execute) {
    Write-Host "[!] EXECUTE MODE - Gercek arsivleme yapilacak!" -ForegroundColor Red
    Write-Host "    5 saniye icinde iptal etmek icin Ctrl+C basin..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
} else {
    Write-Host "[i] DRY RUN MODE - Sadece preview, degisiklik yapilmayacak" -ForegroundColor Green
}
Write-Host ""

# Login ve token al
Write-Host "[*] Login yapiliyor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if (-not $token) {
        Write-Host "[X] Token alinamadi!" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] Login basarili" -ForegroundColor Green
} catch {
    Write-Host "[X] Login hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Preview endpoint'ini cagir
Write-Host ""
Write-Host "[*] Archive preview aliniyor..." -ForegroundColor Yellow
Write-Host "   (Database'deki urunler baz aliniyor, Katana'da olup DB'de olmayanlar listeleniyor)" -ForegroundColor Gray
Write-Host ""

try {
    $previewResponse = Invoke-RestMethod -Uri "$BaseUrl/api/admin/katana-archive-sync/preview" -Method Get -Headers $headers
    
    $totalToArchive = $previewResponse.Count
    $alreadyArchived = ($previewResponse | Where-Object { $_.isAlreadyArchived -eq $true }).Count
    $pendingArchive = $totalToArchive - $alreadyArchived
    
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  ARCHIVE PREVIEW SONUCLARI" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "[i] Toplam arsivlenecek urun sayisi: $totalToArchive" -ForegroundColor White
    Write-Host "    - Zaten arsivlenmis: $alreadyArchived" -ForegroundColor Gray
    Write-Host "    - Arsivlenmeyi bekleyen: $pendingArchive" -ForegroundColor Yellow
    Write-Host ""
    
    if ($totalToArchive -gt 0) {
        Write-Host "[*] Arsivlenecek urunler listesi:" -ForegroundColor Cyan
        Write-Host "---------------------------------------------------------------------" -ForegroundColor Gray
        Write-Host ("{0,-12} {1,-25} {2,-30} {3}" -f "Katana ID", "SKU", "Urun Adi", "Durum") -ForegroundColor White
        Write-Host "---------------------------------------------------------------------" -ForegroundColor Gray
        
        foreach ($product in $previewResponse) {
            $status = if ($product.isAlreadyArchived) { "[OK] Zaten arsiv" } else { "[..] Bekliyor" }
            $statusColor = if ($product.isAlreadyArchived) { "Gray" } else { "Yellow" }
            
            $name = if ($product.name.Length -gt 28) { $product.name.Substring(0, 25) + "..." } else { $product.name }
            $sku = if ($product.sku.Length -gt 23) { $product.sku.Substring(0, 20) + "..." } else { $product.sku }
            
            Write-Host ("{0,-12} {1,-25} {2,-30} " -f $product.katanaProductId, $sku, $name) -NoNewline
            Write-Host $status -ForegroundColor $statusColor
        }
        Write-Host "---------------------------------------------------------------------" -ForegroundColor Gray
        Write-Host ""
        
        # JSON olarak da kaydet
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $jsonFile = "katana-archive-preview-$timestamp.json"
        $previewResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonFile -Encoding UTF8
        Write-Host "[OK] Preview sonuclari kaydedildi: $jsonFile" -ForegroundColor Green
        Write-Host ""
    }
    
} catch {
    Write-Host "[X] Preview hatasi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Execute mode ise gercek arsivleme yap
if ($Execute -and $pendingArchive -gt 0) {
    Write-Host "============================================" -ForegroundColor Red
    Write-Host "  ARSIVLEME BASLIYOR" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "[*] $pendingArchive urun arsivlenecek..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        $syncResponse = Invoke-RestMethod -Uri "$BaseUrl/api/admin/katana-archive-sync/sync?previewOnly=false" -Method Post -Headers $headers
        
        Write-Host "============================================" -ForegroundColor Green
        Write-Host "  ARSIVLEME SONUCLARI" -ForegroundColor Green
        Write-Host "============================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "[OK] Basarili: $($syncResponse.archivedSuccessfully)" -ForegroundColor Green
        Write-Host "[X] Basarisiz: $($syncResponse.archiveFailed)" -ForegroundColor Red
        Write-Host "[i] Sure: $($syncResponse.durationSeconds) saniye" -ForegroundColor Cyan
        Write-Host ""
        
        if ($syncResponse.errors -and $syncResponse.errors.Count -gt 0) {
            Write-Host "[!] Hatalar:" -ForegroundColor Yellow
            foreach ($err in $syncResponse.errors) {
                Write-Host "   - SKU: $($err.sku), ID: $($err.katanaProductId)" -ForegroundColor Red
                Write-Host "     Hata: $($err.errorMessage)" -ForegroundColor Gray
            }
        }
        
        # Sonuclari kaydet
        $resultFile = "katana-archive-result-$timestamp.json"
        $syncResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultFile -Encoding UTF8
        Write-Host ""
        Write-Host "[OK] Arsivleme sonuclari kaydedildi: $resultFile" -ForegroundColor Green
        
    } catch {
        Write-Host "[X] Arsivleme hatasi: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} elseif (-not $Execute) {
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "  DRY RUN TAMAMLANDI" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "[i] Bu sadece bir preview'di. Gercek arsivleme icin:" -ForegroundColor Cyan
    Write-Host "    .\katana-archive-dryrun.ps1 -Execute" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "[OK] Arsivlenecek urun yok." -ForegroundColor Green
}

Write-Host ""
Write-Host "Script tamamlandi." -ForegroundColor Gray
