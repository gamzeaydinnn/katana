-- Check Manager user's role in database
SELECT 
    Id,
    Username,
    Email,
    Role,
    IsActive,
    CreatedAt,
    UpdatedAt
FROM Users
WHERE Username = 'gamze' OR Role = 'Manager';
