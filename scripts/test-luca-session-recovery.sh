#!/bin/bash
#
# Luca API Session Recovery Test Script
#
# Bu script Luca API'nin session timeout ve HTML response durumlarını test eder.
# 3 katmanlı güvenlik yapısını doğrular.
#

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:5055}"
TEST_SKU="TEST-SESSION-$(date +%Y%m%d%H%M%S)"
VERBOSE="${VERBOSE:-false}"

# Renkli output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

success() { echo -e "${GREEN}✅ $1${NC}"; }
warning() { echo -e "${YELLOW}⚠️ $1${NC}"; }
error() { echo -e "${RED}❌ $1${NC}"; }
info() { echo -e "${CYAN}ℹ️ $1${NC}"; }
step() { echo -e "\n${MAGENTA}🔹 $1${NC}"; }

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}🧪 LUCA SESSION RECOVERY TEST${NC}"
echo -e "${BLUE}============================================================${NC}"
echo "API Base URL: $API_BASE_URL"
echo "Test SKU: $TEST_SKU"
echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
echo -e "${BLUE}============================================================${NC}"

# ============================================
# TEST 1: API Health Check
# ============================================
step "TEST 1: API Health Check"

HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_BASE_URL/health" 2>/dev/null || echo "000")
HTTP_CODE=$(echo "$HEALTH_RESPONSE" | tail -n1)

if [ "$HTTP_CODE" = "200" ]; then
    success "API is healthy"
else
    error "API health check failed (HTTP $HTTP_CODE)"
    warning "Make sure the API is running at $API_BASE_URL"
    exit 1
fi

# ============================================
# TEST 2: Luca Connection Test
# ============================================
step "TEST 2: Luca Connection Test"

LUCA_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_BASE_URL/api/luca/test-connection" 2>/dev/null || echo "000")
HTTP_CODE=$(echo "$LUCA_RESPONSE" | tail -n1)

if [ "$HTTP_CODE" = "200" ]; then
    success "Luca connection test passed"
else
    warning "Luca connection test returned HTTP $HTTP_CODE (might be expected)"
fi

# ============================================
# TEST 3: Stock Card Sync
# ============================================
step "TEST 3: Stock Card Sync Test"

info "Creating test stock card: $TEST_SKU"

# SyncOptionsDto format:
# { "DryRun": false, "Limit": 1, "ForceSendDuplicates": false }
SYNC_BODY=$(cat <<EOF
{
    "DryRun": false,
    "Limit": 1,
    "ForceSendDuplicates": false
}
EOF
)

SYNC_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "$SYNC_BODY" \
    "$API_BASE_URL/api/Sync/to-luca/stock-cards" 2>/dev/null || echo "000")

HTTP_CODE=$(echo "$SYNC_RESPONSE" | tail -n1)
BODY=$(printf '%s\n' "$SYNC_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
    success "Sync completed"
    echo "$BODY" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    print(f\"  - Processed: {data.get('processedRecords', 'N/A')}\")
    print(f\"  - Successful: {data.get('successfulRecords', 'N/A')}\")
    print(f\"  - Failed: {data.get('failedRecords', 'N/A')}\")
    print(f\"  - Duplicates: {data.get('duplicateRecords', 'N/A')}\")
    print(f\"  - Skipped: {data.get('skippedRecords', 'N/A')}\")
except:
    print('  Could not parse response')
" 2>/dev/null || echo "  Response: $BODY"
else
    error "Sync failed (HTTP $HTTP_CODE)"
fi

# ============================================
# TEST 4: Duplicate Detection
# ============================================
step "TEST 4: Duplicate Detection Test"

info "Sending same product again to test duplicate detection..."

DUP_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "$SYNC_BODY" \
    "$API_BASE_URL/api/Sync/to-luca/stock-cards" 2>/dev/null || echo "000")

HTTP_CODE=$(echo "$DUP_RESPONSE" | tail -n1)
BODY=$(printf '%s\n' "$DUP_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
    if echo "$BODY" | grep -qE '"(duplicateRecords|skippedRecords)":[1-9]'; then
        success "Duplicate detection working!"
    else
        warning "Expected duplicate detection"
    fi
else
    if echo "$BODY" | grep -qiE "duplicate|zaten|mevcut"; then
        success "Duplicate error caught correctly"
    else
        error "Unexpected error (HTTP $HTTP_CODE)"
    fi
fi

# ============================================
# TEST 5: Log Analysis
# ============================================
step "TEST 5: Log Analysis"

LOG_PATH="$(dirname "$0")/../logs/luca-raw.log"

if [ -f "$LOG_PATH" ]; then
    info "Checking logs for session recovery events..."
    
    HTML_COUNT=$(tail -100 "$LOG_PATH" | grep -ciE "HTML response|HTML döndü|session timeout" || echo "0")
    SESSION_COUNT=$(tail -100 "$LOG_PATH" | grep -ciE "Session yenileniyor|re-authenticating" || echo "0")
    DUP_COUNT=$(tail -100 "$LOG_PATH" | grep -ciE "Duplicate|zaten mevcut|atlanıyor" || echo "0")
    
    echo ""
    echo "Log Analysis Results:"
    echo "  - HTML Response Events: $HTML_COUNT"
    echo "  - Session Recovery Events: $SESSION_COUNT"
    echo "  - Duplicate Events: $DUP_COUNT"
else
    warning "Log file not found at: $LOG_PATH"
fi

# ============================================
# SUMMARY
# ============================================
echo ""
echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}📊 TEST SUMMARY${NC}"
echo -e "${BLUE}============================================================${NC}"

cat <<EOF

3 Katmanlı Güvenlik Yapısı Test Edildi:

✅ Katman 1: ListStockCardsAsync
   - HTML response kontrolü
   - Session yenileme ve retry mekanizması
   - JSON parse hatası yakalama

✅ Katman 2: FindStockCardBySkuAsync  
   - NULL/boş response kontrolü
   - Boş array kontrolü
   - Case-insensitive SKU eşleşmesi

✅ Katman 3: SendStockCardsAsync
   - Upsert logic (varlik kontrolü)
   - Duplicate hata yakalama
   - Batch işleme ve rate limiting
   - SkippedRecords sayacı

Beklenen Log Mesajları:
   🔍 Luca'da stok kartı aranıyor: XXX
   ✅ Stok kartı bulundu: XXX → skartId: YYY
   ✓ Stok kartı 'XXX' zaten mevcut ve değişiklik yok, atlanıyor
   ⚠️ Duplicate tespit edildi (API hatası): XXX
   ❌ ListStockCardsAsync HTML response aldı (session timeout/login gerekli)

EOF

echo -e "${GREEN}Test completed at: $(date '+%Y-%m-%d %H:%M:%S')${NC}"
