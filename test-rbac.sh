#!/bin/bash

# RBAC Test Script - Tüm rolleri test eder
# Kullanım: ./test-rbac.sh

API_URL="http://localhost:5055/api"
BOLD='\033[1m'
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BOLD}🧪 RBAC Test Suite - Katana Integration${NC}\n"

# Test counter
PASSED=0
FAILED=0

# Test function
test_endpoint() {
    local role=$1
    local method=$2
    local endpoint=$3
    local token=$4
    local expected_status=$5
    local description=$6
    
    echo -e "${BLUE}Testing:${NC} ${description}"
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" -X GET "${API_URL}${endpoint}" \
            -H "Authorization: Bearer ${token}")
    elif [ "$method" = "POST" ]; then
        response=$(curl -s -w "\n%{http_code}" -X POST "${API_URL}${endpoint}" \
            -H "Authorization: Bearer ${token}" \
            -H "Content-Type: application/json" \
            -d '{"syncType":"STOCK"}')
    elif [ "$method" = "DELETE" ]; then
        response=$(curl -s -w "\n%{http_code}" -X DELETE "${API_URL}${endpoint}" \
            -H "Authorization: Bearer ${token}")
    fi
    
    status=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)
    
    if [ "$status" = "$expected_status" ]; then
        echo -e "  ${GREEN}✓ PASS${NC} - Status: ${status}"
        ((PASSED++))
    else
        echo -e "  ${RED}✗ FAIL${NC} - Expected: ${expected_status}, Got: ${status}"
        echo -e "  Response: ${body}"
        ((FAILED++))
    fi
    echo ""
}

# ========================
# 1. LOGIN TESTS
# ========================
echo -e "${BOLD}${YELLOW}=== AUTHENTICATION TESTS ===${NC}\n"

# Admin Login
echo -e "${BLUE}Logging in as Admin...${NC}"
ADMIN_TOKEN=$(curl -s -X POST "${API_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"Katana2025!"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -z "$ADMIN_TOKEN" ]; then
    echo -e "${RED}✗ Admin login failed!${NC}\n"
    exit 1
else
    echo -e "${GREEN}✓ Admin login successful${NC}\n"
fi

# Manager Login (sare kullanıcısı)
echo -e "${BLUE}Logging in as Manager (sare)...${NC}"
MANAGER_TOKEN=$(curl -s -X POST "${API_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"sare","password":"123456"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -z "$MANAGER_TOKEN" ]; then
    echo -e "${YELLOW}⚠ Manager login failed - will skip Manager tests${NC}\n"
    MANAGER_TOKEN=""
else
    echo -e "${GREEN}✓ Manager login successful${NC}\n"
fi

# Staff Login - önce staff kullanıcısı oluşturmalıyız
echo -e "${BLUE}Creating Staff user...${NC}"
STAFF_CREATE=$(curl -s -X POST "${API_URL}/Users" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{"username":"staff_test","password":"staff123","role":"Staff","email":"staff@test.com"}')
echo -e "${GREEN}Staff user creation attempted${NC}\n"

echo -e "${BLUE}Logging in as Staff...${NC}"
STAFF_TOKEN=$(curl -s -X POST "${API_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"staff_test","password":"staff123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -z "$STAFF_TOKEN" ]; then
    echo -e "${YELLOW}⚠ Staff login failed - will skip Staff tests${NC}\n"
    STAFF_TOKEN=""
else
    echo -e "${GREEN}✓ Staff login successful${NC}\n"
fi

# ========================
# 2. ADMIN TESTS
# ========================
echo -e "${BOLD}${YELLOW}=== ADMIN ROLE TESTS (Full Access) ===${NC}\n"

test_endpoint "Admin" "GET" "/Products" "$ADMIN_TOKEN" "200" "View all products"
test_endpoint "Admin" "GET" "/adminpanel/products" "$ADMIN_TOKEN" "200" "View admin panel products"
test_endpoint "Admin" "GET" "/Users" "$ADMIN_TOKEN" "200" "View all users"
test_endpoint "Admin" "POST" "/Sync/start" "$ADMIN_TOKEN" "200" "Start sync (Admin only)"
test_endpoint "Admin" "GET" "/Sync/history" "$ADMIN_TOKEN" "200" "View sync history"
test_endpoint "Admin" "GET" "/Reports/stock" "$ADMIN_TOKEN" "200" "View stock report"

# ========================
# 3. MANAGER TESTS
# ========================
if [ -n "$MANAGER_TOKEN" ]; then
    echo -e "${BOLD}${YELLOW}=== MANAGER ROLE TESTS (Read-Only) ===${NC}\n"
    
    test_endpoint "Manager" "GET" "/Products" "$MANAGER_TOKEN" "200" "View all products (allowed)"
    test_endpoint "Manager" "GET" "/adminpanel/products" "$MANAGER_TOKEN" "200" "View admin products (allowed)"
    test_endpoint "Manager" "GET" "/Users" "$MANAGER_TOKEN" "200" "View users list (allowed)"
    test_endpoint "Manager" "GET" "/Sync/history" "$MANAGER_TOKEN" "200" "View sync history (allowed)"
    test_endpoint "Manager" "GET" "/Reports/stock" "$MANAGER_TOKEN" "200" "View stock report (allowed)"
    test_endpoint "Manager" "POST" "/Sync/start" "$MANAGER_TOKEN" "403" "Start sync (should be denied)"
    test_endpoint "Manager" "DELETE" "/Users/999" "$MANAGER_TOKEN" "403" "Delete user (should be denied)"
fi

# ========================
# 4. STAFF TESTS
# ========================
if [ -n "$STAFF_TOKEN" ]; then
    echo -e "${BOLD}${YELLOW}=== STAFF ROLE TESTS (Limited Access) ===${NC}\n"
    
    test_endpoint "Staff" "GET" "/Products" "$STAFF_TOKEN" "200" "View products (allowed)"
    test_endpoint "Staff" "GET" "/Dashboard" "$STAFF_TOKEN" "200" "View dashboard (allowed)"
    test_endpoint "Staff" "GET" "/Users" "$STAFF_TOKEN" "403" "View users (should be denied)"
    test_endpoint "Staff" "GET" "/adminpanel/products" "$STAFF_TOKEN" "200" "View admin products (allowed)"
    test_endpoint "Staff" "POST" "/Sync/start" "$STAFF_TOKEN" "403" "Start sync (should be denied)"
    test_endpoint "Staff" "DELETE" "/Products/999" "$STAFF_TOKEN" "403" "Delete product (should be denied)"
fi

# ========================
# SUMMARY
# ========================
echo -e "${BOLD}${YELLOW}=== TEST SUMMARY ===${NC}\n"
TOTAL=$((PASSED + FAILED))
echo -e "${GREEN}Passed: ${PASSED}${NC}"
echo -e "${RED}Failed: ${FAILED}${NC}"
echo -e "Total: ${TOTAL}\n"

if [ $FAILED -eq 0 ]; then
    echo -e "${BOLD}${GREEN}🎉 All tests passed!${NC}"
    exit 0
else
    echo -e "${BOLD}${RED}❌ Some tests failed${NC}"
    exit 1
fi
