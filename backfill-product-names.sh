#!/bin/bash
# ProductName Backfill Script
# Katana API'den variant ve product bilgilerini çekip database'i günceller

API_KEY="ed8c38d1-4015-45e5-9c28-381d3fe148b6"
DB_SERVER="localhost,1433"
DB_NAME="KatanaDB"
DB_USER="sa"
DB_PASS="Admin00!S"

# Unique variant ID'leri al
echo "📋 NULL ProductName olan variant ID'leri alınıyor..."
VARIANT_IDS=$(sqlcmd -S "$DB_SERVER" -d "$DB_NAME" -U "$DB_USER" -P "$DB_PASS" -C -Q "
SELECT DISTINCT VariantId 
FROM SalesOrderLines 
WHERE ProductName IS NULL OR ProductName LIKE 'VARIANT-%'
ORDER BY VariantId
" -h -1 -W | grep -E '^[0-9]+$')

TOTAL=$(echo "$VARIANT_IDS" | wc -l | tr -d ' ')
echo "📊 Toplam $TOTAL unique variant bulundu"

UPDATED=0
FAILED=0
COUNT=0

for VARIANT_ID in $VARIANT_IDS; do
    COUNT=$((COUNT + 1))
    echo -n "[$COUNT/$TOTAL] Variant $VARIANT_ID işleniyor... "
    
    # Katana API'den variant bilgisi çek
    VARIANT_RESP=$(curl -s -X GET "https://api.katanamrp.com/v1/variants/$VARIANT_ID" \
        -H "Authorization: Bearer $API_KEY")
    
    SKU=$(echo "$VARIANT_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('sku',''))" 2>/dev/null)
    PRODUCT_ID=$(echo "$VARIANT_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('product_id',''))" 2>/dev/null)
    
    if [ -z "$SKU" ] || [ -z "$PRODUCT_ID" ]; then
        echo "❌ Variant bilgisi alınamadı"
        FAILED=$((FAILED + 1))
        continue
    fi
    
    # Katana API'den product bilgisi çek
    PRODUCT_RESP=$(curl -s -X GET "https://api.katanamrp.com/v1/products/$PRODUCT_ID" \
        -H "Authorization: Bearer $API_KEY")
    
    PRODUCT_NAME=$(echo "$PRODUCT_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('name',''))" 2>/dev/null)
    
    if [ -z "$PRODUCT_NAME" ]; then
        echo "❌ Product bilgisi alınamadı"
        FAILED=$((FAILED + 1))
        continue
    fi
    
    # SQL escape için tek tırnak'ları çift yap
    PRODUCT_NAME_ESCAPED=$(echo "$PRODUCT_NAME" | sed "s/'/''/g")
    SKU_ESCAPED=$(echo "$SKU" | sed "s/'/''/g")
    
    # Database'i güncelle
    sqlcmd -S "$DB_SERVER" -d "$DB_NAME" -U "$DB_USER" -P "$DB_PASS" -C -Q "
    UPDATE SalesOrderLines 
    SET SKU = '$SKU_ESCAPED', 
        ProductName = '$PRODUCT_NAME_ESCAPED'
    WHERE VariantId = $VARIANT_ID AND (ProductName IS NULL OR ProductName LIKE 'VARIANT-%')
    " > /dev/null 2>&1
    
    echo "✅ SKU='$SKU', Name='${PRODUCT_NAME:0:40}...'"
    UPDATED=$((UPDATED + 1))
    
    # Rate limit için kısa bekleme
    sleep 0.2
done

echo ""
echo "🎉 Backfill tamamlandı!"
echo "   ✅ Güncellenen: $UPDATED"
echo "   ❌ Başarısız: $FAILED"
