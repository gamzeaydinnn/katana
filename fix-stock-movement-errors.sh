#!/bin/bash

# Stok Hareketleri Hata Düzeltme Script'i
# Bu script hatalı stok hareketlerini tespit edip düzeltir

BASE_URL="http://localhost:8080"
USERNAME="admin"
PASSWORD="Katana2025!"

echo "🔐 Giriş yapılıyor..."

# Login
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Giriş başarısız"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "✅ Giriş başarılı"

# Hatalı hareketleri listele
echo ""
echo "📊 Hatalı stok hareketleri kontrol ediliyor..."

ERROR_MOVEMENTS=$(curl -s -X GET "$BASE_URL/api/StockMovementSync/movements?syncStatus=ERROR" \
  -H "Authorization: Bearer $TOKEN")

TOTAL_ERRORS=$(echo $ERROR_MOVEMENTS | jq '. | length')

echo "📋 Toplam $TOTAL_ERRORS hatalı kayıt bulundu"

if [ "$TOTAL_ERRORS" == "0" ]; then
    echo "✅ Hatalı kayıt yok!"
    exit 0
fi

# Hata tiplerini kategorize et
TRANSFER_ERRORS=$(echo $ERROR_MOVEMENTS | jq '[.[] | select(.movementType == "TRANSFER")] | length')
ADJUSTMENT_ERRORS=$(echo $ERROR_MOVEMENTS | jq '[.[] | select(.movementType == "ADJUSTMENT")] | length')

echo ""
echo "📊 Hata Dağılımı:"
echo "  - Transfer Hataları: $TRANSFER_ERRORS"
echo "  - Düzeltme Hataları: $ADJUSTMENT_ERRORS"

# İlk 10 hatayı göster
echo ""
echo "📝 İlk 10 Hata:"
echo $ERROR_MOVEMENTS | jq -r '.[:10] | .[] | "  [\(.documentNo)] \(.movementType) - \(.errorMessage // "Hata mesajı yok")"'

# Kullanıcıya sor
echo ""
echo "❓ Hatalı kayıtları düzeltmek ister misiniz?"
echo "   1) Tüm hataları yeniden dene (Retry All)"
echo "   2) Sadece Transfer hatalarını yeniden dene"
echo "   3) Sadece Düzeltme hatalarını yeniden dene"
echo "   4) İptal"
echo ""
read -p "Seçiminiz (1-4): " CHOICE

case $CHOICE in
    1)
        echo ""
        echo "🔄 Tüm hatalı kayıtlar yeniden deneniyor..."
        
        SUCCESS_COUNT=0
        FAIL_COUNT=0
        
        # Her bir hareketi işle
        echo $ERROR_MOVEMENTS | jq -c '.[]' | while read -r movement; do
            DOC_NO=$(echo $movement | jq -r '.documentNo')
            MOVEMENT_TYPE=$(echo $movement | jq -r '.movementType')
            MOVEMENT_ID=$(echo $movement | jq -r '.id')
            
            echo "  🔄 $DOC_NO işleniyor..."
            
            RESULT=$(curl -s -X POST "$BASE_URL/api/StockMovementSync/sync-movement/$MOVEMENT_TYPE/$MOVEMENT_ID" \
              -H "Authorization: Bearer $TOKEN")
            
            SUCCESS=$(echo $RESULT | jq -r '.success')
            
            if [ "$SUCCESS" == "true" ]; then
                echo "    ✅ Başarılı"
                ((SUCCESS_COUNT++))
            else
                ERROR_MSG=$(echo $RESULT | jq -r '.message // .errorMessage // "Bilinmeyen hata"')
                echo "    ❌ Başarısız: $ERROR_MSG"
                ((FAIL_COUNT++))
            fi
            
            sleep 0.5
        done
        
        echo ""
        echo "📊 Sonuç:"
        echo "  ✅ Başarılı: $SUCCESS_COUNT"
        echo "  ❌ Başarısız: $FAIL_COUNT"
        ;;
        
    2)
        echo ""
        echo "🔄 Transfer hataları yeniden deneniyor..."
        
        SUCCESS_COUNT=0
        FAIL_COUNT=0
        
        # Transfer hatalarını işle
        echo $ERROR_MOVEMENTS | jq -c '.[] | select(.movementType == "TRANSFER")' | while read -r movement; do
            DOC_NO=$(echo $movement | jq -r '.documentNo')
            MOVEMENT_ID=$(echo $movement | jq -r '.id')
            
            echo "  🔄 $DOC_NO işleniyor..."
            
            RESULT=$(curl -s -X POST "$BASE_URL/api/StockMovementSync/sync/transfer/$MOVEMENT_ID" \
              -H "Authorization: Bearer $TOKEN")
            
            SUCCESS=$(echo $RESULT | jq -r '.success')
            
            if [ "$SUCCESS" == "true" ]; then
                echo "    ✅ Başarılı"
                ((SUCCESS_COUNT++))
            else
                ERROR_MSG=$(echo $RESULT | jq -r '.errorMessage // "Bilinmeyen hata"')
                echo "    ❌ Başarısız: $ERROR_MSG"
                ((FAIL_COUNT++))
            fi
            
            sleep 0.5
        done
        
        echo ""
        echo "📊 Sonuç:"
        echo "  ✅ Başarılı: $SUCCESS_COUNT"
        echo "  ❌ Başarısız: $FAIL_COUNT"
        ;;
        
    3)
        echo ""
        echo "🔄 Düzeltme hataları yeniden deneniyor..."
        
        SUCCESS_COUNT=0
        FAIL_COUNT=0
        
        # Adjustment hatalarını işle
        echo $ERROR_MOVEMENTS | jq -c '.[] | select(.movementType == "ADJUSTMENT")' | while read -r movement; do
            DOC_NO=$(echo $movement | jq -r '.documentNo')
            MOVEMENT_ID=$(echo $movement | jq -r '.id')
            
            echo "  🔄 $DOC_NO işleniyor..."
            
            RESULT=$(curl -s -X POST "$BASE_URL/api/StockMovementSync/sync/adjustment/$MOVEMENT_ID" \
              -H "Authorization: Bearer $TOKEN")
            
            SUCCESS=$(echo $RESULT | jq -r '.success')
            
            if [ "$SUCCESS" == "true" ]; then
                echo "    ✅ Başarılı"
                ((SUCCESS_COUNT++))
            else
                ERROR_MSG=$(echo $RESULT | jq -r '.errorMessage // "Bilinmeyen hata"')
                echo "    ❌ Başarısız: $ERROR_MSG"
                ((FAIL_COUNT++))
            fi
            
            sleep 0.5
        done
        
        echo ""
        echo "📊 Sonuç:"
        echo "  ✅ Başarılı: $SUCCESS_COUNT"
        echo "  ❌ Başarısız: $FAIL_COUNT"
        ;;
        
    4)
        echo ""
        echo "❌ İptal edildi"
        exit 0
        ;;
        
    *)
        echo ""
        echo "❌ Geçersiz seçim"
        exit 1
        ;;
esac

echo ""
echo "✅ İşlem tamamlandı"
