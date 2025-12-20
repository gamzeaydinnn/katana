#!/bin/bash
# Quick verification script for JWT fix

echo "=== JWT Configuration Fix Verification ==="
echo ""

echo "1. Checking JWT configurations across all appsettings files..."
echo ""

# Check for any remaining KatanaDev references
echo "Searching for old 'KatanaDev' issuer..."
OLD_ISSUER=$(grep -r "KatanaDev" --include="appsettings*.json" . 2>/dev/null | wc -l)

if [ "$OLD_ISSUER" -eq 0 ]; then
    echo "✓ No old 'KatanaDev' issuer found"
else
    echo "✗ Found $OLD_ISSUER files with old issuer:"
    grep -r "KatanaDev" --include="appsettings*.json" .
fi

echo ""
echo "Searching for correct 'KatanaAPI' issuer..."
NEW_ISSUER=$(grep -r "\"Issuer\".*\"KatanaAPI\"" --include="appsettings*.json" . 2>/dev/null | wc -l)
echo "✓ Found $NEW_ISSUER files with correct issuer"

echo ""
echo "2. Checking JWT configuration consistency..."
echo ""

# Extract all JWT configurations
echo "JWT Configurations found:"
grep -A 4 "\"Jwt\":" --include="appsettings*.json" -r . | grep -E "(Issuer|Audience|Key)" | sort -u

echo ""
echo "3. Verifying Program.cs has enhanced logging..."
if grep -q "OnAuthenticationFailed" src/Katana.API/Program.cs; then
    echo "✓ Enhanced JWT logging is present in Program.cs"
else
    echo "✗ Enhanced JWT logging not found in Program.cs"
fi

echo ""
echo "=== Verification Complete ==="
echo ""
echo "Next steps:"
echo "  1. Rebuild: docker-compose down && docker-compose up --build -d"
echo "  2. Test: ./test-jwt-auth.ps1"
echo "  3. Check logs: docker logs katana-backend | grep -i jwt"
