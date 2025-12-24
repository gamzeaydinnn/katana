-- Birleştirme işleminin başarılı olduğunu doğrula

-- 1. Hala duplicate var mı?
SELECT 
    'Kalan Duplicate Gruplar' as Kontrol,
    COUNT(DISTINCT Name) as Sayi
FROM Products
WHERE Name IN (
    SELECT Name 
    FROM Products 
    GROUP BY Name 
    HAVING COUNT(*) > 1
);

-- 2. Toplam ürün sayısı
SELECT 
    'Toplam Ürün Sayısı' as Kontrol,
    COUNT(*) as Sayi
FROM Products;

-- 3. Manuel ürünler (katana_order_id NULL olanlar)
SELECT 
    'Manuel Ürünler' as Kontrol,
    COUNT(*) as Sayi
FROM Products
WHERE katana_order_id IS NULL;

-- 4. Katana'dan gelen ürünler
SELECT 
    'Katana Ürünleri' as Kontrol,
    COUNT(*) as Sayi
FROM Products
WHERE katana_order_id IS NOT NULL;

-- 5. Örnek: İlk 10 ürün
SELECT TOP 10
    Id,
    Name,
    SKU,
    CASE WHEN katana_order_id IS NULL THEN 'Manuel' ELSE 'Katana' END as Tip
FROM Products
ORDER BY Name;
