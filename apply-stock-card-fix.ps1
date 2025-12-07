# Apply Stock Card Creation Fix
# Quick script to restart backend and verify the fix

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     STOCK CARD CREATION FIX - APPLY & TEST                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 CHANGES APPLIED:" -ForegroundColor Yellow
Write-Host "   ✅ KategoriAgacKod: Now uses mapping result or null" -ForegroundColor Green
Write-Host "   ✅ MinStokKontrol: Added (value: 0)" -ForegroundColor Green
Write-Host "   ✅ AlisTevkifatOran: Added (value: '0')" -ForegroundColor Green
Write-Host "   ✅ SatisTevkifatOran: Added (value: '0')" -ForegroundColor Green
Write-Host "   ✅ AlisTevkifatKod: Added (value: 0)" -ForegroundColor Green
Write-Host "   ✅ SatisTevkifatKod: Added (value: 0)" -ForegroundColor Green
Write-Host "   ✅ DTO fields made nullable (KategoriAgacKod, Barkod)" -ForegroundColor Green
Write-Host ""

Write-Host "🔄 Restarting backend..." -ForegroundColor Yellow
docker-compose restart backend

Write-Host ""
Write-Host "⏳ Waiting for backend to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host ""
Write-Host "✅ Backend restarted!" -ForegroundColor Green
Write-Host ""

Write-Host "📊 NEXT STEPS:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Monitor logs for stock card creation:" -ForegroundColor White
Write-Host "   docker-compose logs -f backend | Select-String 'LUCA|Stock card|error'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Trigger sync manually:" -ForegroundColor White
Write-Host "   - Via API: POST http://localhost:5055/api/sync/trigger" -ForegroundColor Gray
Write-Host "   - Via Frontend: Click 'Sync Now' button" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Check for success response:" -ForegroundColor White
Write-Host "   Look for: {`"error`":false,`"skartId`":XXXXX,`"message`":`"...başarılı...`"}" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Verify new products created:" -ForegroundColor White
Write-Host "   .\check-luca-simple.ps1" -ForegroundColor Gray
Write-Host ""

Write-Host "🎯 EXPECTED RESULTS:" -ForegroundColor Cyan
Write-Host "   ✅ New products (cliplok1, Ø38x1,5-2, etc.) should be created" -ForegroundColor Green
Write-Host "   ✅ No more {`"error`":true} responses" -ForegroundColor Green
Write-Host "   ✅ No unnecessary -V2, -V3 versions for existing products" -ForegroundColor Green
Write-Host ""

Write-Host "📝 If still failing, check logs for actual error message" -ForegroundColor Yellow
Write-Host ""
