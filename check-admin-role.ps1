# Admin kullanıcısının Role değerini kontrol et
Write-Host "🔍 Admin kullanıcısı Role kontrolü başlıyor..." -ForegroundColor Cyan

$connectionString = "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=Beformet2024!;TrustServerCertificate=True;"

try {
    Add-Type -AssemblyName "System.Data"
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    $query = "SELECT Id, Username, Email, Role, IsActive, CreatedAt FROM Users WHERE Username = 'admin'"
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    
    $reader = $command.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host ""
        Write-Host "✅ Admin Kullanıcısı Bulundu:" -ForegroundColor Green
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host "  ID:        $($reader['Id'])" -ForegroundColor White
        Write-Host "  Username:  $($reader['Username'])" -ForegroundColor White
        Write-Host "  Email:     $($reader['Email'])" -ForegroundColor White
        Write-Host "  Role:      $($reader['Role'])" -ForegroundColor Yellow
        Write-Host "  IsActive:  $($reader['IsActive'])" -ForegroundColor White
        Write-Host "  CreatedAt: $($reader['CreatedAt'])" -ForegroundColor White
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host ""
        
        $role = $reader['Role']
        if ($role -eq "Admin") {
            Write-Host "✅ ROL DOĞRU: '$role' (JWT ile uyumlu)" -ForegroundColor Green
        } else {
            Write-Host "❌ ROL YANLIŞ: '$role' (Beklenen: 'Admin')" -ForegroundColor Red
            Write-Host "🔧 FIX KOMUTU:" -ForegroundColor Yellow
            Write-Host "   UPDATE Users SET Role = 'Admin' WHERE Username = 'admin'" -ForegroundColor Cyan
        }
    } else {
        Write-Host "❌ Admin kullanıcısı bulunamadı!" -ForegroundColor Red
    }
    
    $reader.Close()
    $connection.Close()
    
} catch {
    Write-Host "❌ HATA: $($_.Exception.Message)" -ForegroundColor Red
}
