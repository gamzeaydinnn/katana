USE KatanaDB;
SELECT Username, PasswordHash, Role, Email, IsActive FROM Users WHERE Username = 'admin';
