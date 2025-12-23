-- Script çalıştırıldıktan sonra sonuçları kontrol et
-- Transaction hala açık, COMMIT veya ROLLBACK yapılmadı

-- 1. Duplicate gruplarını göster (birleştirme öncesi durum)
SELECT 
    Name,
    COUNT(*) as ToplamUrun,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as UrunIDleri,
    STRING_AGG(SKU, ', ') as SKUlar,
    STRING_AGG(CASE WHEN katana_order_id IS NULL THEN 'Manuel' ELSE 'Katana' END, ', ') as Tipler
FROM Products
GROUP BY Name
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 2. Canonical (master) ürünleri göster
WITH DuplicateGroups AS (
    SELECT 
        Name,
        Id,
        SKU,
        katana_order_id,
        ROW_NUMBER() OVER (
            PARTITION BY Name 
            ORDER BY 
                CASE WHEN katana_order_id IS NULL THEN 0 ELSE 1 END,
                Id,
                LEN(SKU)
        ) as Rank
    FROM Products
    WHERE Name IN (
        SELECT Name 
        FROM Products 
        GROUP BY Name 
        HAVING COUNT(*) > 1
    )
)
SELECT 
    Name,
    Id as CanonicalID,
    SKU as CanonicalSKU,
    CASE WHEN katana_order_id IS NULL THEN 'Manuel' ELSE 'Katana' END as Tip
FROM DuplicateGroups
WHERE Rank = 1
ORDER BY Name;

-- 3. Silinecek ürünleri göster
WITH DuplicateGroups AS (
    SELECT 
        Name,
        Id,
        SKU,
        katana_order_id,
        ROW_NUMBER() OVER (
            PARTITION BY Name 
            ORDER BY 
                CASE WHEN katana_order_id IS NULL THEN 0 ELSE 1 END,
                Id,
                LEN(SKU)
        ) as Rank
    FROM Products
    WHERE Name IN (
        SELECT Name 
        FROM Products 
        GROUP BY Name 
        HAVING COUNT(*) > 1
    )
)
SELECT 
    Name,
    Id as SilinecekID,
    SKU as SilinecekSKU,
    CASE WHEN katana_order_id IS NULL THEN 'Manuel' ELSE 'Katana' END as Tip
FROM DuplicateGroups
WHERE Rank > 1
ORDER BY Name, Rank;

-- 4. Etkilenecek SalesOrderLines sayısı
WITH DuplicateGroups AS (
    SELECT 
        Name,
        Id,
        SKU,
        ROW_NUMBER() OVER (
            PARTITION BY Name 
            ORDER BY 
                CASE WHEN katana_order_id IS NULL THEN 0 ELSE 1 END,
                Id,
                LEN(SKU)
        ) as Rank
    FROM Products
    WHERE Name IN (
        SELECT Name 
        FROM Products 
        GROUP BY Name 
        HAVING COUNT(*) > 1
    )
)
SELECT 
    COUNT(*) as EtkilenecekSiparisKalemSayisi
FROM SalesOrderLines sol
WHERE sol.SKU IN (
    SELECT SKU 
    FROM DuplicateGroups 
    WHERE Rank > 1
);

-- 5. Etkilenecek diğer kayıtlar
WITH DuplicateGroups AS (
    SELECT 
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY Name 
            ORDER BY 
                CASE WHEN katana_order_id IS NULL THEN 0 ELSE 1 END,
                Id,
                LEN(SKU)
        ) as Rank
    FROM Products
    WHERE Name IN (
        SELECT Name 
        FROM Products 
        GROUP BY Name 
        HAVING COUNT(*) > 1
    )
)
SELECT 
    'StockMovements' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM StockMovements
WHERE ProductId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1)
UNION ALL
SELECT 
    'BillOfMaterials (ProductId)' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM BillOfMaterials
WHERE ProductId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1)
UNION ALL
SELECT 
    'BillOfMaterials (MaterialId)' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM BillOfMaterials
WHERE MaterialId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1)
UNION ALL
SELECT 
    'InvoiceItems' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM InvoiceItems
WHERE ProductId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1)
UNION ALL
SELECT 
    'Stocks' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM Stocks
WHERE ProductId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1)
UNION ALL
SELECT 
    'ProductVariants' as Tablo,
    COUNT(*) as EtkilenecekKayit
FROM ProductVariants
WHERE ProductId IN (SELECT Id FROM DuplicateGroups WHERE Rank > 1);

-- 6. Toplam özet
SELECT 
    (SELECT COUNT(DISTINCT Name) FROM Products WHERE Name IN (
        SELECT Name FROM Products GROUP BY Name HAVING COUNT(*) > 1
    )) as ToplamDuplicateGrupSayisi,
    (SELECT COUNT(*) FROM Products WHERE Name IN (
        SELECT Name FROM Products GROUP BY Name HAVING COUNT(*) > 1
    )) as ToplamDuplicateUrunSayisi,
    (SELECT COUNT(*) FROM Products) as ToplamUrunSayisi;
