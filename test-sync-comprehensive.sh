#!/bin/bash

# ============================================================================
# Katana Senkronizasyon Test Scripti
# ============================================================================
# Bu script fatura ve müşteri senkronizasyonunu test eder
# Kullanım: ./test-sync-comprehensive.sh
# ============================================================================

# set -e kaldırıldı - hataları yakalayıp raporlayacağız

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color
BOLD='\033[1m'

# API Ayarları
API_URL="http://localhost:8080"
USERNAME="admin"
PASSWORD="Katana2025!"

# Test Sonuçları
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
WARNINGS=0

# Log dosyası
LOG_FILE="sync-test-$(date +%Y%m%d-%H%M%S).log"

# ============================================================================
# Yardımcı Fonksiyonlar
# ============================================================================

log() {
    echo -e "$1" | tee -a "$LOG_FILE"
}

print_header() {
    log "\n${BOLD}${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    log "${BOLD}${CYAN}$1${NC}"
    log "${BOLD}${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

print_section() {
    log "\n${BOLD}${YELLOW}▶ $1${NC}"
}

print_success() {
    log "${GREEN}✓${NC} $1"
    ((PASSED_TESTS++))
    ((TOTAL_TESTS++))
}

print_error() {
    log "${RED}✗${NC} $1"
    ((FAILED_TESTS++))
    ((TOTAL_TESTS++))
}

print_warning() {
    log "${YELLOW}⚠${NC} $1"
    ((WARNINGS++))
}

print_info() {
    log "${CYAN}ℹ${NC} $1"
}

# ============================================================================
# API Test Fonksiyonları
# ============================================================================

check_api_health() {
    print_section "API Sağlık Kontrolü"
    
    if curl -s -f "$API_URL/health" > /dev/null 2>&1; then
        print_success "API erişilebilir: $API_URL"
    else
        print_error "API erişilemiyor: $API_URL"
        log "${RED}Lütfen Docker container'ın çalıştığından emin olun: docker ps${NC}"
        exit 1
    fi
}

login() {
    print_section "Authentication"
    
    local response=$(curl -s -X POST "$API_URL/api/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")
    
    TOKEN=$(echo "$response" | jq -r '.token // empty')
    
    if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
        print_error "Login başarısız"
        log "Response: $response"
        exit 1
    fi
    
    print_success "Login başarılı"
    print_info "Token alındı (${#TOKEN} karakter)"
}

# ============================================================================
# Müşteri Senkronizasyon Testi
# ============================================================================

test_customer_sync() {
    print_header "MÜŞTERİ SENKRONİZASYONU TESTİ"
    
    print_section "Müşteri Sync Başlatılıyor..."
    
    local start_time=$(date +%s)
    local response=$(curl -s -X POST "$API_URL/api/sync/customers" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json")
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    # Response'u logla
    echo "$response" | jq '.' >> "$LOG_FILE" 2>&1
    
    # Sonuçları parse et
    local is_success=$(echo "$response" | jq -r '.isSuccess // false')
    local processed=$(echo "$response" | jq -r '.processedRecords // 0')
    local successful=$(echo "$response" | jq -r '.successfulRecords // 0')
    local failed=$(echo "$response" | jq -r '.failedRecords // 0')
    local message=$(echo "$response" | jq -r '.message // "No message"')
    
    print_info "Süre: ${duration}s"
    print_info "İşlenen: $processed"
    print_info "Başarılı: $successful"
    print_info "Başarısız: $failed"
    print_info "Mesaj: $message"
    
    # Testler
    if [ "$is_success" == "true" ]; then
        print_success "Müşteri sync başarılı"
    else
        print_error "Müşteri sync başarısız"
    fi
    
    if [ "$processed" -gt 0 ]; then
        print_success "Müşteri kaydı bulundu ($processed kayıt)"
    else
        print_warning "Hiç müşteri kaydı bulunamadı"
    fi
    
    if [ "$successful" -gt 0 ]; then
        print_success "$successful müşteri başarıyla senkronize edildi"
    else
        print_warning "Hiç müşteri senkronize edilemedi"
    fi
    
    if [ "$failed" -gt 0 ]; then
        print_warning "$failed müşteri senkronize edilemedi"
        
        # Hata detaylarını göster
        local errors=$(echo "$response" | jq -r '.errors[]? // empty' 2>/dev/null)
        if [ ! -z "$errors" ]; then
            print_info "Hata detayları:"
            echo "$errors" | while read -r error; do
                log "  - $error"
            done
        fi
    fi
    
    # Başarı oranı hesapla
    if [ "$processed" -gt 0 ]; then
        local success_rate=$((successful * 100 / processed))
        if [ "$success_rate" -ge 80 ]; then
            print_success "Başarı oranı: %$success_rate"
        elif [ "$success_rate" -ge 50 ]; then
            print_warning "Başarı oranı: %$success_rate (düşük)"
        else
            print_error "Başarı oranı: %$success_rate (çok düşük)"
        fi
    fi
}

# ============================================================================
# Fatura Senkronizasyon Testi
# ============================================================================

test_invoice_sync() {
    print_header "FATURA SENKRONİZASYONU TESTİ"
    
    print_section "Fatura Sync Başlatılıyor..."
    
    local start_time=$(date +%s)
    local response=$(curl -s -X POST "$API_URL/api/sync/invoices" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json")
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    # Response'u logla
    echo "$response" | jq '.' >> "$LOG_FILE" 2>&1
    
    # Sonuçları parse et
    local is_success=$(echo "$response" | jq -r '.isSuccess // false')
    local processed=$(echo "$response" | jq -r '.processedRecords // 0')
    local successful=$(echo "$response" | jq -r '.successfulRecords // 0')
    local failed=$(echo "$response" | jq -r '.failedRecords // 0')
    local message=$(echo "$response" | jq -r '.message // "No message"')
    
    print_info "Süre: ${duration}s"
    print_info "İşlenen: $processed"
    print_info "Başarılı: $successful"
    print_info "Başarısız: $failed"
    print_info "Mesaj: $message"
    
    # Testler
    if [ "$is_success" == "true" ]; then
        print_success "Fatura sync başarılı"
    else
        print_error "Fatura sync başarısız"
    fi
    
    if [ "$processed" -gt 0 ]; then
        print_success "Fatura kaydı bulundu ($processed kayıt)"
    else
        print_warning "Hiç fatura kaydı bulunamadı"
    fi
    
    if [ "$successful" -gt 0 ]; then
        print_success "$successful fatura başarıyla senkronize edildi"
    else
        print_warning "Hiç fatura senkronize edilemedi"
    fi
    
    if [ "$failed" -gt 0 ]; then
        print_warning "$failed fatura senkronize edilemedi"
        
        # Hata detaylarını göster
        local errors=$(echo "$response" | jq -r '.errors[]? // empty' 2>/dev/null)
        if [ ! -z "$errors" ]; then
            print_info "Hata detayları:"
            echo "$errors" | while read -r error; do
                log "  - $error"
            done
        fi
    fi
    
    # Başarı oranı hesapla
    if [ "$processed" -gt 0 ]; then
        local success_rate=$((successful * 100 / processed))
        if [ "$success_rate" -ge 80 ]; then
            print_success "Başarı oranı: %$success_rate"
        elif [ "$success_rate" -ge 50 ]; then
            print_warning "Başarı oranı: %$success_rate (düşük)"
        else
            print_error "Başarı oranı: %$success_rate (çok düşük)"
        fi
    fi
}

# ============================================================================
# Sync Durumu Kontrolü
# ============================================================================

check_sync_status() {
    print_header "SENKRONİZASYON DURUMU"
    
    local response=$(curl -s -X GET "$API_URL/api/sync/status" \
        -H "Authorization: Bearer $TOKEN")
    
    echo "$response" | jq '.' >> "$LOG_FILE" 2>&1
    
    # Her sync tipi için durumu göster
    local customer_status=$(echo "$response" | jq -r '.[] | select(.syncType=="CUSTOMER") | .currentStatus // "UNKNOWN"')
    local customer_last=$(echo "$response" | jq -r '.[] | select(.syncType=="CUSTOMER") | .lastSyncTime // "Never"')
    
    local invoice_status=$(echo "$response" | jq -r '.[] | select(.syncType=="INVOICE") | .currentStatus // "UNKNOWN"')
    local invoice_last=$(echo "$response" | jq -r '.[] | select(.syncType=="INVOICE") | .lastSyncTime // "Never"')
    
    print_section "Müşteri Sync Durumu"
    print_info "Durum: $customer_status"
    print_info "Son Sync: $customer_last"
    
    print_section "Fatura Sync Durumu"
    print_info "Durum: $invoice_status"
    print_info "Son Sync: $invoice_last"
    
    # Durum kontrolü
    if [ "$customer_status" == "SUCCESS" ]; then
        print_success "Müşteri sync durumu: SUCCESS"
    elif [ "$customer_status" == "FAILED" ]; then
        print_error "Müşteri sync durumu: FAILED"
    fi
    
    if [ "$invoice_status" == "SUCCESS" ]; then
        print_success "Fatura sync durumu: SUCCESS"
    elif [ "$invoice_status" == "FAILED" ]; then
        print_error "Fatura sync durumu: FAILED"
    fi
}

# ============================================================================
# Katana API Rate Limit Kontrolü
# ============================================================================

check_katana_rate_limit() {
    print_header "KATANA API RATE LIMIT KONTROLÜ"
    
    print_section "Docker loglarında 429 hatası aranıyor..."
    
    local rate_limit_errors=$(docker logs katana-api-1 2>&1 | grep -i "429\|rate limit\|too many requests" | tail -10)
    
    if [ -z "$rate_limit_errors" ]; then
        print_success "429 Rate Limit hatası bulunamadı"
    else
        print_warning "429 Rate Limit hataları bulundu:"
        echo "$rate_limit_errors" | while read -r line; do
            log "  $line"
        done
    fi
    
    print_section "Variant SKU çözümleme başarı oranı kontrol ediliyor..."
    
    local variant_success=$(docker logs katana-api-1 2>&1 | grep "Resolved.*variant SKUs successfully" | tail -1)
    
    if [ ! -z "$variant_success" ]; then
        print_info "$variant_success"
        print_success "Variant SKU çözümleme çalışıyor"
    else
        print_warning "Variant SKU çözümleme logu bulunamadı"
    fi
}

# ============================================================================
# Luca Authentication Kontrolü
# ============================================================================

check_luca_auth() {
    print_header "LUCA AUTHENTICATION KONTROLÜ"
    
    print_section "Luca authentication hataları kontrol ediliyor..."
    
    local auth_errors=$(docker logs katana-api-1 2>&1 | grep -i "login olunmalı\|authentication\|1002" | tail -10)
    
    if [ -z "$auth_errors" ]; then
        print_success "Luca authentication hatası bulunamadı"
    else
        print_error "Luca authentication hataları bulundu:"
        echo "$auth_errors" | while read -r line; do
            log "  $line"
        done
        print_warning "Bu hatalar fatura sync'inin başarısız olmasına neden olabilir"
    fi
}

# ============================================================================
# Özet Rapor
# ============================================================================

print_summary() {
    print_header "TEST SONUÇLARI ÖZETİ"
    
    log "\n${BOLD}Test İstatistikleri:${NC}"
    log "  Toplam Test: $TOTAL_TESTS"
    log "  ${GREEN}Başarılı: $PASSED_TESTS${NC}"
    log "  ${RED}Başarısız: $FAILED_TESTS${NC}"
    log "  ${YELLOW}Uyarı: $WARNINGS${NC}"
    
    local success_rate=0
    if [ "$TOTAL_TESTS" -gt 0 ]; then
        success_rate=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    fi
    
    log "\n${BOLD}Başarı Oranı: %$success_rate${NC}"
    
    if [ "$FAILED_TESTS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
        log "\n${GREEN}${BOLD}✓ TÜM TESTLER BAŞARILI!${NC}"
        log "${GREEN}Senkronizasyon ekranını açabilirsiniz.${NC}"
        exit 0
    elif [ "$FAILED_TESTS" -eq 0 ]; then
        log "\n${YELLOW}${BOLD}⚠ TESTLER BAŞARILI AMA UYARILAR VAR${NC}"
        log "${YELLOW}Uyarıları kontrol edin, ardından senkronizasyon ekranını açabilirsiniz.${NC}"
        exit 0
    else
        log "\n${RED}${BOLD}✗ BAZI TESTLER BAŞARISIZ${NC}"
        log "${RED}Lütfen hataları düzeltin ve tekrar test edin.${NC}"
        log "\n${CYAN}Log dosyası: $LOG_FILE${NC}"
        exit 1
    fi
}

# ============================================================================
# Ana Program
# ============================================================================

main() {
    clear
    
    print_header "KATANA SENKRONİZASYON TEST SÜRECİ"
    
    log "${CYAN}Test başlangıç: $(date '+%Y-%m-%d %H:%M:%S')${NC}"
    log "${CYAN}Log dosyası: $LOG_FILE${NC}"
    
    # Ön kontroller
    check_api_health
    login
    
    # Senkronizasyon testleri
    test_customer_sync
    sleep 2
    test_invoice_sync
    sleep 2
    
    # Durum kontrolleri
    check_sync_status
    check_katana_rate_limit
    check_luca_auth
    
    # Özet
    print_summary
}

# Scripti çalıştır
main
