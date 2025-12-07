# Fix Category Code Issue
# The category code "01" doesn't exist in Luca, changing to null

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          CATEGORY CODE FIX - APPLY & RESTART              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔍 PROBLEM IDENTIFIED:" -ForegroundColor Red
Write-Host "   Luca API error: 'Kategori bulunamadı' (Category not found)" -ForegroundColor Red
Write-Host "   Sending: kategoriAgacKod='01' (2 digits)" -ForegroundColor Red
Write-Host "   But Luca expects: null or valid 3-digit codes like '001', '002', '220'" -ForegroundColor Red
Write-Host ""

Write-Host "✅ FIX APPLIED:" -ForegroundColor Green
Write-Host "   Changed DefaultKategoriKodu: '01' → null" -ForegroundColor Green
Write-Host "   Changed CategoryMapping.default: '01' → null" -ForegroundColor Green
Write-Host "   (User's working example shows null is acceptable)" -ForegroundColor Green
Write-Host ""

Write-Host "🔄 Restarting backend..." -ForegroundColor Yellow
docker-compose restart backend

Write-Host ""
Write-Host "⏳ Waiting for backend to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host ""
Write-Host "✅ Backend restarted!" -ForegroundColor Green
Write-Host ""

Write-Host "📊 WHAT TO EXPECT:" -ForegroundColor Cyan
Write-Host ""
Write-Host "BEFORE (with '01'):" -ForegroundColor White
Write-Host '  {"kategoriAgacKod":"01",...}' -ForegroundColor Gray
Write-Host '  Response: {"error":true,"message":"Kategori bulunamadı."}' -ForegroundColor Red
Write-Host ""
Write-Host "AFTER (with null):" -ForegroundColor White
Write-Host '  {"kategoriAgacKod":null,...}' -ForegroundColor Gray
Write-Host '  Response: {"error":false,"skartId":XXXXX,"message":"...başarılı..."}' -ForegroundColor Green
Write-Host ""

Write-Host "📝 NEXT STEPS:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Monitor logs:" -ForegroundColor White
Write-Host "   docker-compose logs -f backend | Select-String 'kategoriAgacKod|Kategori|Stock card'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Look for:" -ForegroundColor White
Write-Host '   ✅ "kategoriAgacKod":null (not "01")' -ForegroundColor Gray
Write-Host '   ✅ {"error":false,"skartId":XXXXX}' -ForegroundColor Gray
Write-Host '   ✅ "...başarılı bir şekilde kaydedilmiştir."' -ForegroundColor Gray
Write-Host ""
Write-Host "3. If you want to use specific categories:" -ForegroundColor White
Write-Host "   - First, list available categories in Luca" -ForegroundColor Gray
Write-Host "   - Then update CategoryMapping with valid 3-digit codes" -ForegroundColor Gray
Write-Host "   - Example: '001', '002', '220', etc." -ForegroundColor Gray
Write-Host ""

Write-Host "🎯 CATEGORY MAPPING (Current):" -ForegroundColor Cyan
Write-Host '  "1MAMUL": "001"' -ForegroundColor Gray
Write-Host '  "2HAMMADDE": "002"' -ForegroundColor Gray
Write-Host '  "3YARI MAMUL": "220"' -ForegroundColor Gray
Write-Host '  "4YARDIMCI MALZEME": "004"' -ForegroundColor Gray
Write-Host '  "5AMBALAJ": "005"' -ForegroundColor Gray
Write-Host '  "default": null  ← Products without category will use null' -ForegroundColor Yellow
Write-Host ""
