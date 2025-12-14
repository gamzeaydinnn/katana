# Test Stock Card Mapping (Category & Unit)
# Bu script Katana'dan Luca'ya stok kartı senkronizasyonunda
# kategori ve ölçü birimi mapping'lerinin doğru çalıştığını test eder

Write-Host "🧪 Stok Kartı Mapping Testi Başlıyor..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$API_URL = "http://localhost:5055"

# Admin token al
Write-Host "🔐 Admin token alınıyor..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$API_URL/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body '{"username":"admin","password":"Katana2025!"}'
    
    $TOKEN = $loginResponse.token
    Write-Host "✅ Token alındı" -ForegroundColor Green
} catch {
    Write-Host "❌ Token alınamadı: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 1. Katana'dan ürünleri çek
Write-Host "📥 Katana'dan ürünler çekiliyor..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
    }
    
    $katanaProducts = Invoke-RestMethod -Uri "$API_URL/api/katana/products?limit=5" `
        -Method Get `
        -Headers $headers
    
    Write-Host "Katana'dan gelen ilk 5 ürün:" -ForegroundColor Blue
    foreach ($product in $katanaProducts) {
        $sku = if ($product.sku) { $product.sku } else { $product.SKU }
        $name = if ($product.name) { $product.name } else { $product.Name }
        $category = if ($product.category) { $product.category } else { $product.Category }
        $unit = if ($product.unit) { $product.unit } else { $product.Unit }
        
        Write-Host "  - SKU: $sku, Name: $name, Category: $category, Unit: $unit"
    }
} catch {
    Write-Host "⚠️  Katana ürünleri çekilemedi: $_" -ForegroundColor Yellow
}

Write-Host ""

# 2. Dry-run payload'ı kontrol et
Write-Host "🔍 Luca'ya gönderilecek payload kontrol ediliyor (dry-run)..." -ForegroundColor Yellow
try {
    $dryPayload = Invoke-RestMethod -Uri "$API_URL/api/koza-debug/dry-payload?limit=5" `
        -Method Get `
        -Headers $headers
    
    Write-Host "Luca'ya gönderilecek mapping'li veriler:" -ForegroundColor Blue
    foreach ($item in $dryPayload) {
        $kategori = if ($item.KategoriAgacKod) { $item.KategoriAgacKod } else { "null" }
        $barkod = if ($item.Barkod) { $item.Barkod } else { "null" }
        
        Write-Host "  - SKU: $($item.Sku), KartKodu: $($item.KartKodu), Kategori: $kategori, Barkod: $barkod"
    }
} catch {
    Write-Host "⚠️  Dry payload alınamadı: $_" -ForegroundColor Yellow
}

Write-Host ""

# 3. Mapping kontrolü
Write-Host "🔎 Mapping Kontrolü:" -ForegroundColor Cyan
Write-Host "-------------------" -ForegroundColor Cyan

$appsettings = Get-Content "src/Katana.API/appsettings.json" | ConvertFrom-Json

Write-Host "Kategori Mapping'leri:" -ForegroundColor Blue
$appsettings.LucaApi.CategoryMapping | Format-Table -AutoSize

Write-Host "Ölçü Birimi Mapping'leri:" -ForegroundColor Blue
$appsettings.LucaApi.UnitMapping | Format-Table -AutoSize

Write-Host ""

# 4. Test: Dry-run senkronizasyon
Write-Host "🧪 Test: Dry-run ile senkronizasyon simülasyonu..." -ForegroundColor Yellow
try {
    $syncBody = @{
        dryRun = $true
        limit = 3
    } | ConvertTo-Json
    
    $syncResult = Invoke-RestMethod -Uri "$API_URL/api/sync/products-to-luca" `
        -Method Post `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $syncBody
    
    Write-Host "Senkronizasyon sonucu:" -ForegroundColor Blue
    Write-Host "  - Dry Run: $($syncResult.isDryRun)"
    Write-Host "  - İşlenen: $($syncResult.processedRecords)"
    Write-Host "  - Yeni: $($syncResult.newCreated)"
    Write-Host "  - Mevcut: $($syncResult.alreadyExists)"
    Write-Host "  - Mesaj: $($syncResult.message)"
} catch {
    Write-Host "⚠️  Senkronizasyon testi başarısız: $_" -ForegroundColor Yellow
}

Write-Host ""

# 5. Backend log kontrolü
Write-Host "📋 Backend log'larını kontrol ediyoruz..." -ForegroundColor Yellow
Write-Host "Son mapping ile ilgili log'lar:" -ForegroundColor Blue
try {
    $logs = docker logs katana-backend 2>&1 | Select-String -Pattern "ÖLÇÜ BİRİMİ|MAPPING|KategoriAgacKod|OlcumBirimiId" | Select-Object -Last 20
    $logs | ForEach-Object { Write-Host "  $_" }
} catch {
    Write-Host "⚠️  Docker log'ları okunamadı" -ForegroundColor Yellow
}

Write-Host ""

# Özet
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "📊 Test Özeti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$categoryCount = ($appsettings.LucaApi.CategoryMapping | Get-Member -MemberType NoteProperty).Count
$unitCount = ($appsettings.LucaApi.UnitMapping | Get-Member -MemberType NoteProperty).Count

Write-Host "✅ Kategori Mapping Sayısı: $categoryCount" -ForegroundColor Green
Write-Host "✅ Ölçü Birimi Mapping Sayısı: $unitCount" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🎯 Manuel Kontrol Önerileri:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Backend log'larında şu mesajları arayın:" -ForegroundColor Yellow
Write-Host "   ✅ ÖLÇÜ BİRİMİ MAPPING: 'adet' → Luca ID: 5" -ForegroundColor Blue
Write-Host "   ⚠️ ÖLÇÜ BİRİMİ MAPPING BULUNAMADI: 'xyz'" -ForegroundColor Blue
Write-Host ""
Write-Host "2. Luca'da bir stok kartı açın ve kontrol edin:" -ForegroundColor Yellow
Write-Host "   - Kategori doğru mu?" -ForegroundColor Blue
Write-Host "   - Ölçü birimi doğru mu?" -ForegroundColor Blue
Write-Host ""
Write-Host "3. Gerçek senkronizasyon için (dry-run olmadan):" -ForegroundColor Yellow
Write-Host '   $syncBody = @{ dryRun = $false; limit = 1 } | ConvertTo-Json' -ForegroundColor Blue
Write-Host '   Invoke-RestMethod -Uri "$API_URL/api/sync/products-to-luca" -Method Post -Headers $headers -ContentType "application/json" -Body $syncBody' -ForegroundColor Blue
Write-Host ""
Write-Host "✅ Test tamamlandı!" -ForegroundColor Green
