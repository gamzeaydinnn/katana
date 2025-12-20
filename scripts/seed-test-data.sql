-- ============================================
-- Test Verileri: Siparişler + Hata Kayıtları
-- ============================================
USE KatanaDB;
GO

-- 1. Test Müşterileri Ekle (eğer yoksa)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE TaxNo = '1234567890')
BEGIN
    INSERT INTO Customers (TaxNo, Title, Address, Phone, Email, IsActive, IsSynced, CreatedAt, UpdatedAt)
    VALUES 
    ('1234567890', 'Test Müşteri A.Ş.', 'İstanbul, Türkiye', '+905551234567', 'test@example.com', 1, 0, GETUTCDATE(), GETUTCDATE()),
    ('0987654321', 'Demo Ticaret Ltd.', 'Ankara, Türkiye', '+905559876543', 'demo@example.com', 1, 0, GETUTCDATE(), GETUTCDATE());
END
GO

-- 2. Test Ürünleri Ekle (eğer yoksa)
IF NOT EXISTS (SELECT 1 FROM Products WHERE SKU = 'TEST-001')
BEGIN
    INSERT INTO Products (SKU, Name, Description, Price, Stock, CategoryId, IsActive, CreatedAt, UpdatedAt)
    VALUES 
    ('TEST-001', 'Test Ürün 1', 'Test amaçlı ürün', 100.00, 50, 1, 1, GETUTCDATE(), GETUTCDATE()),
    ('TEST-002', 'Test Ürün 2', 'Test amaçlı ürün', 200.00, 30, 1, 1, GETUTCDATE(), GETUTCDATE()),
    ('TEST-003', 'Test Ürün 3', 'Test amaçlı ürün', 150.00, 20, 1, 1, GETUTCDATE(), GETUTCDATE());
END
GO

-- 3. Test Siparişleri Ekle
DECLARE @CustomerId1 INT = (SELECT TOP 1 Id FROM Customers WHERE TaxNo = '1234567890');
DECLARE @CustomerId2 INT = (SELECT TOP 1 Id FROM Customers WHERE TaxNo = '0987654321');
DECLARE @ProductId1 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-001');
DECLARE @ProductId2 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-002');
DECLARE @ProductId3 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-003');

-- Sipariş 1
INSERT INTO Orders (CustomerId, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES (@CustomerId1, 2, 350.00, DATEADD(DAY, -5, GETUTCDATE()), GETUTCDATE()); -- Status 2 = Completed

DECLARE @OrderId1 INT = SCOPE_IDENTITY();

INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
VALUES 
(@OrderId1, @ProductId1, 2, 100.00),
(@OrderId1, @ProductId3, 1, 150.00);
GO

-- Sipariş 2
DECLARE @CustomerId2b INT = (SELECT TOP 1 Id FROM Customers WHERE TaxNo = '0987654321');
DECLARE @ProductId2b INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-002');

INSERT INTO Orders (CustomerId, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES (@CustomerId2b, 1, 400.00, DATEADD(DAY, -2, GETUTCDATE()), GETUTCDATE()); -- Status 1 = Processing

DECLARE @OrderId2 INT = SCOPE_IDENTITY();

INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
VALUES (@OrderId2, @ProductId2b, 2, 200.00);
GO

-- Sipariş 3
DECLARE @CustomerId1c INT = (SELECT TOP 1 Id FROM Customers WHERE TaxNo = '1234567890');
DECLARE @ProductId1c INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-001');
DECLARE @ProductId2c INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'TEST-002');

INSERT INTO Orders (CustomerId, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES (@CustomerId1c, 0, 500.00, DATEADD(DAY, -1, GETUTCDATE()), GETUTCDATE()); -- Status 0 = Pending

DECLARE @OrderId3 INT = SCOPE_IDENTITY();

INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
VALUES 
(@OrderId3, @ProductId1c, 3, 100.00),
(@OrderId3, @ProductId2c, 1, 200.00);
GO

-- 4. Hatalı Sync Kayıtları Ekle (FailedSyncRecords)
DECLARE @IntegrationLogId INT = (SELECT TOP 1 Id FROM IntegrationLogs ORDER BY StartTime DESC);

IF @IntegrationLogId IS NOT NULL
BEGIN
    INSERT INTO FailedSyncRecords (
        IntegrationLogId, 
        RecordType, 
        RawData, 
        ErrorMessage, 
        Status, 
        RetryCount, 
        FailedAt, 
        LastRetryAt, 
        NextRetryAt
    )
    VALUES 
    -- Stok hatası
    (
        @IntegrationLogId,
        'STOCK',
        '{"SKU":"TEST-001","Quantity":-5,"Location":"MAIN"}',
        'Validation failed: Quantity cannot be negative',
        'FAILED',
        3,
        DATEADD(HOUR, -2, GETUTCDATE()),
        DATEADD(HOUR, -1, GETUTCDATE()),
        DATEADD(HOUR, 1, GETUTCDATE())
    ),
    -- Fiyat hatası
    (
        @IntegrationLogId,
        'PRODUCT',
        '{"SKU":"TEST-002","Name":"Test Product 2","Price":0}',
        'Validation failed: Price must be greater than zero',
        'FAILED',
        2,
        DATEADD(HOUR, -3, GETUTCDATE()),
        DATEADD(HOUR, -2, GETUTCDATE()),
        DATEADD(HOUR, 2, GETUTCDATE())
    ),
    -- Sipariş hatası
    (
        @IntegrationLogId,
        'ORDER',
        '{"OrderId":999,"CustomerId":null,"TotalAmount":0}',
        'Validation failed: Customer not found',
        'FAILED',
        1,
        DATEADD(HOUR, -1, GETUTCDATE()),
        DATEADD(MINUTE, -30, GETUTCDATE()),
        DATEADD(MINUTE, 30, GETUTCDATE())
    );
END
GO

-- 5. Data Correction Logs Ekle (Düzeltme bekleyen kayıtlar)
INSERT INTO DataCorrectionLogs (
    SourceSystem,
    EntityType,
    EntityId,
    FieldName,
    OriginalValue,
    CorrectedValue,
    ValidationError,
    CorrectionReason,
    IsApproved,
    IsSynced,
    CreatedAt,
    CreatedBy
)
VALUES 
-- Stok düzeltmesi bekliyor
(
    'Luca',
    'Product',
    'TEST-001',
    'Stock',
    '-5',
    '10',
    'Negative stock quantity',
    'Stok değeri negatif olamaz, düzeltildi',
    0,
    0,
    DATEADD(HOUR, -2, GETUTCDATE()),
    'admin@katana.com'
),
-- Fiyat düzeltmesi bekliyor
(
    'Luca',
    'Product',
    'TEST-002',
    'Price',
    '0.00',
    '200.00',
    'Price cannot be zero',
    'Fiyat sıfır olamaz, düzeltildi',
    0,
    0,
    DATEADD(HOUR, -3, GETUTCDATE()),
    'admin@katana.com'
),
-- Onaylanmış ama sync edilmemiş düzeltme
(
    'Katana',
    'Product',
    'TEST-003',
    'Name',
    'Test Ürün 3',
    'Test Ürün 3 - Güncellenmiş',
    'Name mismatch',
    'Ürün ismi güncellendi',
    1,
    0,
    DATEADD(HOUR, -1, GETUTCDATE()),
    'admin@katana.com'
);
GO

-- 6. Integration Logs Ekle (Son sync işlemleri)
INSERT INTO IntegrationLogs (
    SyncType,
    Source,
    Status,
    StartTime,
    EndTime,
    ProcessedRecords,
    SuccessfulRecords,
    Duration
)
VALUES 
-- Başarılı Katana → Luca sync
(
    'KATANA_TO_LUCA',
    2,
    3,
    DATEADD(MINUTE, -10, GETUTCDATE()),
    DATEADD(MINUTE, -9, GETUTCDATE()),
    5,
    5,
    60000
),
-- Hatalı Luca → Katana sync
(
    'LUCA_TO_KATANA_STOCK',
    2,
    2,
    DATEADD(MINUTE, -5, GETUTCDATE()),
    DATEADD(MINUTE, -4, GETUTCDATE()),
    10,
    7,
    45000
);
GO

-- Sonuç göster
SELECT 'Test verileri başarıyla eklendi!' AS Result;
SELECT COUNT(*) AS OrderCount FROM Orders;
SELECT COUNT(*) AS FailedRecordCount FROM FailedSyncRecords WHERE RecordType IN ('STOCK', 'PRODUCT', 'ORDER');
SELECT COUNT(*) AS PendingCorrectionCount FROM DataCorrectionLogs WHERE IsApproved = 0;
GO
