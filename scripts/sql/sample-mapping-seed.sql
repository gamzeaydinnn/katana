-- Sample mapping seed for Luca/Katana integration
-- Adjust values to match your environment (SKU codes, warehouse codes, customer types).

-- SKU → Account code mapping (use DEFAULT as fallback)
INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
('SKU_ACCOUNT', 'PROD001', '150.01.001', 'Elektronik ürün hesap kodu', 1, GETDATE(), GETDATE()),
('SKU_ACCOUNT', 'PROD002', '150.01.002', 'Elektronik ürün hesap kodu', 1, GETDATE(), GETDATE()),
('SKU_ACCOUNT', 'DEFAULT', '150.01.999', 'Varsayılan ürün hesap kodu', 1, GETDATE(), GETDATE());

-- LocationId → Warehouse code mapping (DEFAULT as fallback)
INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
('LOCATION_WAREHOUSE', '1', '0001-0001', 'Ana depo', 1, GETDATE(), GETDATE()),
('LOCATION_WAREHOUSE', '2', '0002-0001', 'Şube depo', 1, GETDATE(), GETDATE()),
('LOCATION_WAREHOUSE', 'DEFAULT', '0001-0001', 'Varsayılan depo', 1, GETDATE(), GETDATE());

-- Customer type mapping (adapt SourceValue to your flag/field)
INSERT INTO MappingTables (MappingType, SourceValue, TargetValue, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
('CUSTOMER_TYPE', 'TR', '1', 'Türkiye kurumsal müşteri', 1, GETDATE(), GETDATE()),
('CUSTOMER_TYPE', 'PERSON', '2', 'Bireysel müşteri', 1, GETDATE(), GETDATE()),
('CUSTOMER_TYPE', 'COMPANY', '1', 'Kurumsal müşteri', 1, GETDATE(), GETDATE()),
('CUSTOMER_TYPE', 'DEFAULT', '1', 'Varsayılan cari tip', 1, GETDATE(), GETDATE());

-- Quick check
SELECT MappingType,
       COUNT(*) AS TotalMappings,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveMappings
FROM MappingTables
GROUP BY MappingType;
