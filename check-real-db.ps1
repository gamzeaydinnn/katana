# Backend'in kullandığı GERÇEK database'e bağlan
Write-Host "=== BACKEND DATABASE KONTROLÜ ===" -ForegroundColor Cyan
Write-Host "Connection: Server=localhost,1433;Database=KatanaDB;User=sa" -ForegroundColor Gray

# Backend'in kullandığı connection string ile bağlan
$server = "localhost,1433"
$database = "KatanaDB"
$username = "sa"
$password = "Admin00!S"

Write-Host "`n1. Tablo Listesi:" -ForegroundColor Yellow
sqlcmd -S $server -d $database -U $username -P $password -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME" -W

Write-Host "`n2. Products Tablosu Var mı?" -ForegroundColor Yellow
sqlcmd -S $server -d $database -U $username -P $password -Q "IF OBJECT_ID('Products', 'U') IS NOT NULL SELECT 'Products tablosu VAR' as Status, COUNT(*) as RowCount FROM Products ELSE SELECT 'Products tablosu YOK' as Status, 0 as RowCount" -W

Write-Host "`n3. Tüm Tabloların Kayıt Sayıları:" -ForegroundColor Yellow
sqlcmd -S $server -d $database -U $username -P $password -Q "SELECT t.name AS TableName, SUM(p.rows) AS RowCount FROM sys.tables t INNER JOIN sys.partitions p ON t.object_id = p.object_id WHERE p.index_id IN (0,1) GROUP BY t.name ORDER BY SUM(p.rows) DESC" -W

Write-Host "`n4. Migration Durumu:" -ForegroundColor Yellow
sqlcmd -S $server -d $database -U $username -P $password -Q "SELECT TOP 5 MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId DESC" -W
