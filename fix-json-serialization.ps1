# Fix JSON Serialization - Include Null Values
# Luca API requires kategoriAgacKod field to be present (even if null)

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       JSON SERIALIZATION FIX - INCLUDE NULL VALUES        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔍 PROBLEM IDENTIFIED:" -ForegroundColor Red
Write-Host "   kategoriAgacKod field is MISSING from JSON request" -ForegroundColor Red
Write-Host "   JSON serializer was configured to OMIT null values" -ForegroundColor Red
Write-Host ""
Write-Host "   BEFORE:" -ForegroundColor White
Write-Host '   {"kartAdi":"...","kartKodu":"...",...}' -ForegroundColor Gray
Write-Host '   ❌ No kategoriAgacKod field at all!' -ForegroundColor Red
Write-Host ""

Write-Host "✅ FIX APPLIED:" -ForegroundColor Green
Write-Host "   Changed JSON serialization in LucaService.StockCards.cs:" -ForegroundColor Green
Write-Host '   DefaultIgnoreCondition: WhenWritingNull → Never' -ForegroundColor Green
Write-Host "   Now null fields WILL be included in JSON" -ForegroundColor Green
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
Write-Host "BEFORE (null omitted):" -ForegroundColor White
Write-Host '  {"kartAdi":"Presli Boru","kartKodu":"PUT. Ø22*1,5",...}' -ForegroundColor Gray
Write-Host '  ❌ kategoriAgacKod field missing' -ForegroundColor Red
Write-Host '  Response: {"error":true}' -ForegroundColor Red
Write-Host ""
Write-Host "AFTER (null included):" -ForegroundColor White
Write-Host '  {"kartAdi":"Presli Boru","kartKodu":"PUT. Ø22*1,5",...,"kategoriAgacKod":null,...}' -ForegroundColor Gray
Write-Host '  ✅ kategoriAgacKod field present with null value' -ForegroundColor Green
Write-Host '  Response: {"error":false,"skartId":XXXXX,"message":"...başarılı..."}' -ForegroundColor Green
Write-Host ""

Write-Host "📝 NEXT STEPS:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Monitor logs:" -ForegroundColor White
Write-Host "   docker-compose logs -f backend | Select-String 'LUCA JSON REQUEST|kategoriAgacKod|Stock card'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Look for:" -ForegroundColor White
Write-Host '   ✅ "kategoriAgacKod":null in the JSON request' -ForegroundColor Gray
Write-Host '   ✅ {"error":false,"skartId":XXXXX}' -ForegroundColor Gray
Write-Host '   ✅ "...başarılı bir şekilde kaydedilmiştir."' -ForegroundColor Gray
Write-Host ""

Write-Host "🎯 COMPLETE FIX SUMMARY:" -ForegroundColor Cyan
Write-Host "   1. ✅ Added missing fields (MinStokKontrol, tevkifat, etc.)" -ForegroundColor Green
Write-Host "   2. ✅ Made DTO fields nullable (KategoriAgacKod, Barkod)" -ForegroundColor Green
Write-Host "   3. ✅ Fixed category code (01 → null)" -ForegroundColor Green
Write-Host "   4. ✅ Fixed JSON serialization (include null values)" -ForegroundColor Green
Write-Host ""
Write-Host "   Stock cards should now be created successfully! 🎉" -ForegroundColor Green
Write-Host ""
