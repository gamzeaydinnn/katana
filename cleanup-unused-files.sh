#!/bin/bash

# Kullanılmayan Dosyaları Temizleme Script'i
# Tarih: 4 Aralık 2024
# Kullanım: ./cleanup-unused-files.sh [--dry-run]

set -e

DRY_RUN=false
if [ "$1" == "--dry-run" ]; then
    DRY_RUN=true
    echo "🔍 DRY RUN MODE - Hiçbir dosya silinmeyecek, sadece listeleniyor..."
fi

echo "🧹 Kullanılmayan Dosyaları Temizleme Script'i"
echo "=============================================="
echo ""

# Fonksiyon: Dosya silme veya listeleme
delete_file() {
    local file="$1"
    if [ -f "$file" ]; then
        if [ "$DRY_RUN" = true ]; then
            echo "  [DRY-RUN] Silinecek: $file"
        else
            rm -f "$file"
            echo "  ✅ Silindi: $file"
        fi
    fi
}

# Fonksiyon: Klasör silme veya listeleme
delete_dir() {
    local dir="$1"
    if [ -d "$dir" ]; then
        if [ "$DRY_RUN" = true ]; then
            echo "  [DRY-RUN] Silinecek klasör: $dir"
        else
            rm -rf "$dir"
            echo "  ✅ Silindi: $dir"
        fi
    fi
}

# 1. BACKUP DOSYALARINI SİL
echo "📝 1. Backup dosyalarını siliyorum..."
delete_file "AKSIYONLAR.md.backup"
delete_file "src/Katana.API/Controllers/AuthController.cs.bak2"
delete_file "src/Katana.API/Controllers/LucaCompatibilityController.cs.bak"
find . -name "*.bak" -o -name "*.bak2" -o -name "*.backup" 2>/dev/null | while read file; do
    delete_file "$file"
done
echo ""

# 2. KULLANILMAYAN TEST DOSYALARINI SİL
echo "🧪 2. Kullanılmayan test dosyalarını siliyorum..."
delete_file "tests/Katana.Tests/Controllers/AnalyticsControllerTests.cs"
delete_file "tests/Katana.Tests/Controllers/DashboardControllerTests.cs"
echo ""

# 3. KÖK DİZİNDEKİ LOG DOSYALARINI SİL
echo "📋 3. Kök dizindeki log dosyalarını siliyorum..."
delete_file ".build_after_fix_stderr.log"
delete_file ".build_after_fix_stdout.log"
delete_file ".build_stderr.log"
delete_file ".build_stdout.log"
delete_file ".docker_api_logs.log"
delete_file ".docker_compose_results.log"
delete_file ".docker_down_up_ps.log"
delete_file ".dotnet_run_stderr.log"
delete_file ".dotnet_run_stdout.log"
delete_file ".run_after_fix_stderr.log"
delete_file ".run_after_fix_stdout.log"
delete_file ".run_full_stderr.log"
delete_file ".run_full_stdout.log"
delete_file ".run_portfix_stderr.log"
delete_file ".run_portfix_stdout.log"
delete_file ".run_start_stderr.log"
delete_file ".run_start_stdout.log"
echo ""

# 4. GEÇİCİ TEST DOSYALARINI SİL
echo "🗑️ 4. Geçici test dosyalarını siliyorum..."
delete_file "backend_err.txt"
delete_file "backend_out.txt"
delete_file "backend_out2.txt"
delete_file "backend_output.txt"
delete_file "db_apply_err.txt"
delete_file "db_apply_out.txt"
delete_file "branches-body.txt"
delete_file "headers.txt"
delete_file "login-body.txt"
delete_file "put-enveloped.json"
delete_file "put.envelope.json"
delete_file "put.json"
delete_file "docker-nets.json"
delete_file "koza_category_tests_results.json"
delete_file "koza_debug_response.json"
delete_file "koza_debug_root.json"
delete_file "koza-setup-results.json"
delete_file "luca_categories.json"
delete_file "luca_categories_resp.html"
delete_file "luca_responses.csv"
delete_file "luca_responses.json"
delete_file "swagger.json"
delete_file "="
echo ""

# 5. ESKİ LOG DOSYALARINI TEMİZLE (30 günden eski)
echo "📁 5. Eski log dosyalarını temizliyorum (30 günden eski)..."
if [ -d "logs" ]; then
    if [ "$DRY_RUN" = true ]; then
        echo "  [DRY-RUN] 30 günden eski log dosyaları:"
        find logs/ -name "*.log" -mtime +30 2>/dev/null || true
        find logs/ -name "*.txt" -mtime +30 2>/dev/null || true
        find logs/ -name "*.json" -mtime +30 2>/dev/null || true
    else
        find logs/ -name "*.log" -mtime +30 -delete 2>/dev/null || true
        find logs/ -name "*.txt" -mtime +30 -delete 2>/dev/null || true
        find logs/ -name "*.json" -mtime +30 -delete 2>/dev/null || true
        echo "  ✅ Eski loglar temizlendi"
    fi
fi
echo ""

# 6. BOŞ KLASÖRLERI SİL
echo "📂 6. Boş klasörleri siliyorum..."
delete_dir "katana"
if [ "$DRY_RUN" = true ]; then
    echo "  [DRY-RUN] Boş klasörler:"
    find . -type d -empty 2>/dev/null || true
else
    find . -type d -empty -delete 2>/dev/null || true
    echo "  ✅ Boş klasörler silindi"
fi
echo ""

# 7. BOŞ DOSYALARI SİL
echo "📄 7. Boş dosyaları siliyorum..."
if [ "$DRY_RUN" = true ]; then
    echo "  [DRY-RUN] Boş dosyalar:"
    find . -type f -empty 2>/dev/null || true
else
    find . -type f -empty -delete 2>/dev/null || true
    echo "  ✅ Boş dosyalar silindi"
fi
echo ""

# ÖZET
echo "=============================================="
echo "✅ Temizlik tamamlandı!"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo "ℹ️  Bu bir DRY RUN idi. Gerçekten silmek için:"
    echo "   ./cleanup-unused-files.sh"
else
    echo "📊 Temizlik sonuçları:"
    echo "  - Backup dosyaları silindi"
    echo "  - Kullanılmayan test dosyaları silindi"
    echo "  - Log dosyaları temizlendi"
    echo "  - Geçici dosyalar silindi"
    echo "  - Boş klasörler ve dosyalar silindi"
    echo ""
    echo "⚠️  Sonraki adımlar:"
    echo "  1. Git status kontrol et: git status"
    echo "  2. .gitignore güncelle"
    echo "  3. Değişiklikleri commit et"
fi
