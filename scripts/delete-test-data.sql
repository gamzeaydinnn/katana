SET QUOTED_IDENTIFIER ON;
GO

-- Test verilerini temizle
DELETE FROM Products;
DELETE FROM Categories;

-- Sonucu göster
SELECT COUNT(*) as RemainingProducts FROM Products;
SELECT COUNT(*) as RemainingCategories FROM Categories;
SELECT 'Test verileri silindi' as Status;
GO
