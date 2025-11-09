SET QUOTED_IDENTIFIER ON;
GO

-- Kategoriler ekle
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Categories ON;
    INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (1, 'Elektronik', 'Elektronik Ürünler', 1, GETDATE(), GETDATE());
    SET IDENTITY_INSERT Categories OFF;
END

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT Categories ON;
    INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (2, 'Mobilya', 'Mobilya Ürünleri', 1, GETDATE(), GETDATE());
    SET IDENTITY_INSERT Categories OFF;
END
GO

-- Ürünler ekle
INSERT INTO Products (Name, SKU, CategoryId, Stock, Price, IsActive, CreatedAt, UpdatedAt)
VALUES 
    ('Laptop HP 15', 'LAP-001', 1, 150, 15999.99, 1, GETDATE(), GETDATE()),
    ('Mouse Logitech', 'MOU-002', 1, 5, 299.99, 1, GETDATE(), GETDATE()),
    ('Klavye Mekanik', 'KEY-003', 1, 0, 899.99, 1, GETDATE(), GETDATE()),
    ('Ofis Masası', 'DESK-004', 2, 75, 3499.99, 1, GETDATE(), GETDATE()),
    ('Ofis Koltuğu', 'CHAIR-005', 2, 8, 2999.99, 1, GETDATE(), GETDATE()),
    ('Monitor 27 inch', 'MON-006', 1, 25, 4999.99, 1, GETDATE(), GETDATE()),
    ('Webcam HD', 'WEB-007', 1, 120, 599.99, 1, GETDATE(), GETDATE()),
    ('Kulaklık Bluetooth', 'HEAD-008', 1, 3, 1299.99, 1, GETDATE(), GETDATE()),
    ('USB Hub 7 Port', 'USB-009', 1, 200, 249.99, 1, GETDATE(), GETDATE()),
    ('Kablo Organizatör', 'ORG-010', 2, 0, 149.99, 1, GETDATE(), GETDATE());
GO

SELECT COUNT(*) as TotalProducts FROM Products;
SELECT COUNT(*) as TotalCategories FROM Categories;
GO
