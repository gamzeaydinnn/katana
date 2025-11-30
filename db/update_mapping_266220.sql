-- Backup first! (export current MappingTables rows that match)
-- Example (SQL Server):
-- SELECT * INTO MappingTables_Backup_266220 FROM MappingTables WHERE MappingType = 'CATEGORY' AND (TargetValue = '1' OR TargetValue = '266220');

-- Temizlik: eski yanlış denemeleri kaldır (varsa)
DELETE FROM MappingTables
WHERE TargetValue = '1'
  AND MappingType = 'CATEGORY';

-- Kural 1: Katana'dan '1MAMUL' gelirse -> Gerçek Luca ID'si 266220
INSERT INTO MappingTables (SourceValue, TargetValue, MappingType, IsActive)
VALUES ('1MAMUL', '266220', 'CATEGORY', 1);

-- Kural 2: Katana'dan '3MAMUL' gelirse -> Gerçek Luca ID'si 266220
INSERT INTO MappingTables (SourceValue, TargetValue, MappingType, IsActive)
VALUES ('3MAMUL', '266220', 'CATEGORY', 1);

-- Doğrulama için:
-- SELECT * FROM MappingTables WHERE MappingType = 'CATEGORY' AND TargetValue = '266220';
