-- Admin kullanıcısı oluşturma scripti
-- Kullanıcı adı: admin
-- Şifre: Katana2025! (SHA256 hash)

USE KatanaDB;
GO

-- Önce var olan admin kullanıcısını kontrol et
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    -- SHA256 hash of 'Katana2025!' = 8HYiMNZ0sm+ZuPhGL09SAHnA4TUXIbirE/Q6hHSTU4Q=
    INSERT INTO Users (Username, PasswordHash, Role, Email, IsActive, CreatedAt, UpdatedAt)
    VALUES ('admin', '8HYiMNZ0sm+ZuPhGL09SAHnA4TUXIbirE/Q6hHSTU4Q=', 'Admin', 'admin@katana.local', 1, GETUTCDATE(), GETUTCDATE());
    
    PRINT 'Admin kullanıcısı oluşturuldu. Kullanıcı adı: admin, Şifre: Katana2025!';
END
ELSE
BEGIN
    -- Eğer kullanıcı varsa şifreyi güncelle
    UPDATE Users
    SET PasswordHash = '8HYiMNZ0sm+ZuPhGL09SAHnA4TUXIbirE/Q6hHSTU4Q=',
        UpdatedAt = GETUTCDATE()
    WHERE Username = 'admin';
    
    PRINT 'Admin kullanıcısı şifresi güncellendi: Katana2025!';
END
GO
