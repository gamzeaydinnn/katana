#!/bin/bash

# ============================================
# Fatura/Sipariş Sync Log Kontrolü
# ============================================
# Kullanım: ./scripts/check_order_sync_logs.sh

set -e

echo "🔍 Fatura/Sipariş Sync Log Kontrolü Başlatılıyor..."
echo "=================================================="

LOG_DIR="logs"
LOG_FILE="${LOG_DIR}/luca-raw.log"

# Log dosyası var mı kontrol et
if [ ! -f "$LOG_FILE" ]; then
    echo "❌ Log dosyası bulunamadı: $LOG_FILE"
    exit 1
fi

echo ""
echo "📊 1. ORDER/INVOICE Hata Sayısı"
echo "--------------------------------"
ORDER_ERRORS=$(grep -i "ORDER.*ERROR" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
INVOICE_ERRORS=$(grep -i "INVOICE.*ERROR" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
echo "ORDER hataları: $ORDER_ERRORS"
echo "INVOICE hataları: $INVOICE_ERRORS"

echo ""
echo "📊 2. Son 10 ORDER Hatası"
echo "--------------------------------"
grep -i "ORDER.*ERROR" "$LOG_FILE" 2>/dev/null | tail -10 || echo "Hata bulunamadı"

echo ""
echo "📊 3. Son 10 INVOICE Hatası"
echo "--------------------------------"
grep -i "INVOICE.*ERROR" "$LOG_FILE" 2>/dev/null | tail -10 || echo "Hata bulunamadı"

echo ""
echo "📊 4. Başarılı ORDER Sync Sayısı (Son 24 saat)"
echo "--------------------------------"
SUCCESS_COUNT=$(grep -i "ORDER.*SUCCESS\|Successfully sent.*order" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
echo "Başarılı sync: $SUCCESS_COUNT"

echo ""
echo "📊 5. Duplicate Uyarıları"
echo "--------------------------------"
DUPLICATE_COUNT=$(grep -i "duplicate\|zaten mevcut\|already exists" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
echo "Duplicate uyarı sayısı: $DUPLICATE_COUNT"

if [ "$DUPLICATE_COUNT" -gt 0 ]; then
    echo ""
    echo "Son 5 duplicate uyarısı:"
    grep -i "duplicate\|zaten mevcut\|already exists" "$LOG_FILE" 2>/dev/null | tail -5 || echo "Bulunamadı"
fi

echo ""
echo "📊 6. Session/Auth Hataları"
echo "--------------------------------"
SESSION_ERRORS=$(grep -i "session.*expired\|unauthorized\|authentication.*failed" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
echo "Session/Auth hata sayısı: $SESSION_ERRORS"

if [ "$SESSION_ERRORS" -gt 0 ]; then
    echo ""
    echo "Son 5 session hatası:"
    grep -i "session.*expired\|unauthorized\|authentication.*failed" "$LOG_FILE" 2>/dev/null | tail -5 || echo "Bulunamadı"
fi

echo ""
echo "📊 7. HTTP Hataları (4xx, 5xx)"
echo "--------------------------------"
HTTP_4XX=$(grep -i "HTTP 4[0-9][0-9]" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
HTTP_5XX=$(grep -i "HTTP 5[0-9][0-9]" "$LOG_FILE" 2>/dev/null | wc -l || echo "0")
echo "4xx hataları: $HTTP_4XX"
echo "5xx hataları: $HTTP_5XX"

echo ""
echo "📊 8. Son 10 Log Girişi"
echo "--------------------------------"
tail -10 "$LOG_FILE"

echo ""
echo "=================================================="
echo "✅ Log kontrolü tamamlandı"
echo ""
echo "💡 Detaylı analiz için:"
echo "   - ORDER hataları: grep -i 'ORDER.*ERROR' $LOG_FILE"
echo "   - INVOICE hataları: grep -i 'INVOICE.*ERROR' $LOG_FILE"
echo "   - Tüm hatalar: grep -i 'ERROR\|FAIL' $LOG_FILE"
echo "   - API endpoint: GET /api/orderinvoicesync/validate"
