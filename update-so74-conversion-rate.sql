-- SO-74 siparişi için ConversionRate değerini güncelle
-- EUR için güncel kur değeri (yaklaşık 37-38 TRY)

-- Önce mevcut durumu kontrol et
SELECT 
    Id, 
    OrderNo, 
    Currency, 
    ConversionRate, 
    Total, 
    TotalInBaseCurrency,
    IsSyncedToLuca,
    LastSyncError
FROM SalesOrders 
WHERE OrderNo LIKE '%SO-74%' OR OrderNo = 'SO-74';

-- ConversionRate değerini güncelle (TotalInBaseCurrency / Total formülü ile hesapla)
-- Eğer TotalInBaseCurrency varsa, kuru hesapla
UPDATE SalesOrders 
SET ConversionRate = CASE 
    WHEN Total > 0 AND TotalInBaseCurrency > 0 THEN TotalInBaseCurrency / Total
    ELSE 37.5 -- Varsayılan EUR/TRY kuru
END
WHERE (OrderNo LIKE '%SO-74%' OR OrderNo = 'SO-74')
  AND Currency = 'EUR'
  AND (ConversionRate IS NULL OR ConversionRate = 0);

-- Güncelleme sonrası kontrol
SELECT 
    Id, 
    OrderNo, 
    Currency, 
    ConversionRate, 
    Total, 
    TotalInBaseCurrency
FROM SalesOrders 
WHERE OrderNo LIKE '%SO-74%' OR OrderNo = 'SO-74';
