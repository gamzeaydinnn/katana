#!/bin/bash

# Stok Hareketleri Hatalarını Temizle ve Yeniden Dene

BASE_URL="http://localhost:8080"
USERNAME="admin"
PASSWORD="Katana2025!"

echo "🔧 Stok Hareketleri Hata Düzeltme Aracı"
echo "========================================"
echo ""

# SQL Server connection bilgileri
DB_SERVER="localhost,1433"
DB_NAME="KatanaDB"
DB_USER="sa"
DB_PASS="Admin00!S"

echo "📊 Mevcut hata durumunu kontrol ediyorum..."
echo ""

# Login
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ API'ye giriş başarısız"
    exit 1
fi

# Hatalı kayıtları say
ERROR_MOVEMENTS=$(curl -s -X GET "$BASE_URL/api/StockMovementSync/movements?syncStatus=ERROR" \
  -H "Authorization: Bearer $TOKEN")

TOTAL_ERRORS=$(echo $ERROR_MOVEMENTS | jq '. | length')

echo "📋 Toplam $TOTAL_ERRORS hatalı kayıt bulundu"
echo ""

if [ "$TOTAL_ERRORS" == "0" ]; then
    echo "✅ Hatalı kayıt yok!"
    exit 0
fi

# Kullanıcıya sor
echo "❓ Ne yapmak istersiniz?"
echo "   1) Hataları temizle ve Pending durumuna al"
echo "   2) Hataları temizle VE hemen yeniden dene"
echo "   3) İptal"
echo ""
read -p "Seçiminiz (1-3): " CHOICE

case $CHOICE in
    1)
        echo ""
        echo "🔄 Hatalar temizleniyor..."
        
        # Docker üzerinden SQL çalıştır
        docker exec katana-mssql /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U $DB_USER -P "$DB_PASS" -d $DB_NAME \
            -C -Q "
            UPDATE StockTransfers SET Status = 'Pending' WHERE Status = 'Error';
            UPDATE PendingStockAdjustments SET Status = 'Pending', RejectionReason = NULL WHERE Status = 'Error';
            SELECT 'Transfer' as Type, COUNT(*) as PendingCount FROM StockTransfers WHERE Status = 'Pending'
            UNION ALL
            SELECT 'Adjustment' as Type, COUNT(*) as PendingCount FROM PendingStockAdjustments WHERE Status = 'Pending';
            "
        
        echo ""
        echo "✅ Hatalar temizlendi! Kayıtlar Pending durumuna alındı."
        echo "💡 Kayıtları yeniden göndermek için seçenek 2'yi kullanabilirsiniz."
        ;;
        
    2)
        echo ""
        echo "🔄 Hatalar temizleniyor ve yeniden deneniyor..."
        
        # Önce hataları temizle
        docker exec katana-mssql /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U $DB_USER -P "$DB_PASS" -d $DB_NAME \
            -C -Q "
            UPDATE StockTransfers SET Status = 'Pending' WHERE Status = 'Error';
            UPDATE PendingStockAdjustments SET Status = 'Pending', RejectionReason = NULL WHERE Status = 'Error';
            " > /dev/null 2>&1
        
        echo "✅ Hatalar temizlendi"
        echo ""
        echo "🔄 Bekleyen tüm kayıtlar yeniden gönderiliyor..."
        
        # Toplu senkronizasyon endpoint'ini çağır
        RESULT=$(curl -s -X POST "$BASE_URL/api/StockMovementSync/sync/all-pending" \
          -H "Authorization: Bearer $TOKEN")
        
        TOTAL=$(echo $RESULT | jq -r '.totalCount')
        SUCCESS=$(echo $RESULT | jq -r '.successCount')
        FAILED=$(echo $RESULT | jq -r '.failedCount')
        
        echo ""
        echo "📊 Sonuç:"
        echo "  📝 Toplam: $TOTAL"
        echo "  ✅ Başarılı: $SUCCESS"
        echo "  ❌ Başarısız: $FAILED"
        
        if [ "$FAILED" -gt "0" ]; then
            echo ""
            echo "⚠️  Bazı kayıtlar başarısız oldu. Detaylar için logları kontrol edin:"
            echo "   docker logs katana-api-1 2>&1 | tail -100"
        fi
        ;;
        
    3)
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
