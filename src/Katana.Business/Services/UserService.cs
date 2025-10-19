using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Katana.Business.Services;

public class UserService : IUserService
{
    private readonly IntegrationDbContext _context;

    public UserService(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await _context.Set<User>()
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                Email = u.Email,
                IsActive = u.IsActive
            }).ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : new UserDto { Id = user.Id, Username = user.Username, Role = user.Role, Email = user.Email, IsActive = user.IsActive };
    }
    public async Task<bool> UpdateRoleAsync(int id, string role)
    {
        var user = await _context.Set<User>().FindAsync(id);
        if (user == null) return false;

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
{
    var user = await _context.Set<User>().FindAsync(id);
    if (user == null)
        throw new KeyNotFoundException($"Kullanıcı bulunamadı: {id}");

    // Uniqueness checks if changed
    if (!string.Equals(user.Username, dto.Username, StringComparison.OrdinalIgnoreCase))
    {
        var exists = await _context.Set<User>().AnyAsync(u => u.Username == dto.Username && u.Id != id);
        if (exists) throw new InvalidOperationException($"Kullanıcı adı zaten kullanılıyor: {dto.Username}");
    }
    if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
    {
        var existsEmail = await _context.Set<User>().AnyAsync(u => u.Email == dto.Email && u.Id != id);
        if (existsEmail) throw new InvalidOperationException($"Email zaten kullanılıyor: {dto.Email}");
    }

    user.Username = dto.Username;
    user.Role = dto.Role;
    user.IsActive = dto.IsActive;
    if (!string.IsNullOrWhiteSpace(dto.Email))
    {
        user.Email = dto.Email!;
    }

    if (!string.IsNullOrWhiteSpace(dto.Password))
    {
        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));
        user.PasswordHash = hash;
    }

    user.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        Email = user.Email ?? string.Empty,
        IsActive = user.IsActive
    };
}
    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        // Uniqueness checks
        if (await _context.Set<User>().AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException($"Kullanıcı adı zaten kullanılıyor: {dto.Username}");
        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Set<User>().AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email zaten kullanılıyor: {dto.Email}");

        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = hash,
            Role = dto.Role,
            Email = dto.Email ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        return new UserDto { Id = user.Id, Username = user.Username, Role = user.Role, Email = user.Email, IsActive = user.IsActive };
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Set<User>().FindAsync(id);
        if (user == null) return false;
        _context.Set<User>().Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> AuthenticateAsync(string email, string password)
{
    var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    if (user == null) return null;

    using var sha = SHA256.Create();
    var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    if (user.PasswordHash != hash) return null;

    return new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        Email = user.Email ?? string.Empty,
        IsActive = user.IsActive
    };
}

}
