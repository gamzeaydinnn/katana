# Docker container'ın kullandığı database'i kontrol et
Write-Host "=== DOCKER CONTAINER DATABASE KONTROLÜ ===" -ForegroundColor Cyan

# Docker container içinden SQL sorgusu çalıştır
Write-Host "`n1. Container içinden tablo listesi:" -ForegroundColor Yellow
docker exec katana-db-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME" -W

Write-Host "`n2. Products tablosu kayıt sayısı:" -ForegroundColor Yellow
docker exec katana-db-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q "SELECT COUNT(*) as ProductCount FROM Products" -W

Write-Host "`n3. Tüm tabloların kayıt sayıları:" -ForegroundColor Yellow
docker exec katana-db-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q "SELECT t.name AS TableName, SUM(p.rows) AS RowCount FROM sys.tables t INNER JOIN sys.partitions p ON t.object_id = p.object_id WHERE p.index_id IN (0,1) GROUP BY t.name ORDER BY t.name" -W
