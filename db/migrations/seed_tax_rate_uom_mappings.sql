-- Seed default Tax Rate Mappings
-- Common Turkish VAT rates

-- Standard VAT 20%
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 1)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (1, 0.20, 'Standard VAT 20%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Standard VAT 18% (old rate)
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 2)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (2, 0.18, 'Standard VAT 18%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Reduced VAT 10%
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 3)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (3, 0.10, 'Reduced VAT 10%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Reduced VAT 8%
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 4)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (4, 0.08, 'Reduced VAT 8%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Super Reduced VAT 1%
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 5)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (5, 0.01, 'Super Reduced VAT 1%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Zero VAT 0%
IF NOT EXISTS (SELECT 1 FROM TaxRateMappings WHERE KatanaTaxRateId = 6)
BEGIN
    INSERT INTO TaxRateMappings (KatanaTaxRateId, KozaKdvOran, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES (6, 0.00, 'Zero VAT 0%', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Seed default UoM Mappings
-- Common units of measure

-- Pieces / Units (Adet)
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'PCS')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('PCS', 5, 'Pieces / Units (Adet)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'UNIT')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('UNIT', 5, 'Units (Adet)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'ADET')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('ADET', 5, 'Adet', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'EA')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('EA', 5, 'Each (Adet)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Kilogram
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'KG')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('KG', 1, 'Kilogram', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Gram
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'G')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('G', 2, 'Gram', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'GR')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('GR', 2, 'Gram', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Meter
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'M')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('M', 3, 'Meter (Metre)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'METRE')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('METRE', 3, 'Metre', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Liter
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'L')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('L', 4, 'Liter (Litre)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'LITRE')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('LITRE', 4, 'Litre', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Box (Kutu)
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'BOX')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('BOX', 6, 'Box (Kutu)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'KUTU')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('KUTU', 6, 'Kutu', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

-- Package (Paket)
IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'PKG')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('PKG', 7, 'Package (Paket)', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

IF NOT EXISTS (SELECT 1 FROM UoMMappings WHERE KatanaUoMString = 'PAKET')
BEGIN
    INSERT INTO UoMMappings (KatanaUoMString, KozaOlcumBirimiId, Description, IsActive, CreatedAt, UpdatedAt, SyncStatus)
    VALUES ('PAKET', 7, 'Paket', 1, GETUTCDATE(), GETUTCDATE(), 'SYNCED');
END

PRINT 'Tax Rate and UoM mappings seeded successfully';
