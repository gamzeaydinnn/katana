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
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Set<User>().FindAsync(id);
        return user == null ? null : new UserDto { Id = user.Id, Username = user.Username, Role = user.Role, IsActive = user.IsActive };
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

    user.Username = dto.Username;
    user.Role = dto.Role;
    user.IsActive = dto.IsActive;

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
        Email = user is { } ? (user.Email ?? string.Empty) : string.Empty,
        IsActive = user.IsActive
    };
}
    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = hash,
            Role = dto.Role
        };

        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        return new UserDto { Id = user.Id, Username = user.Username, Role = user.Role, IsActive = user.IsActive };
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Set<User>().FindAsync(id);
        if (user == null) return false;
        _context.Set<User>().Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetRoleAsync(int id, string role)
    {
        var user = await _context.Set<User>().FindAsync(id);
        if (user == null) return false;
        user.Role = role;
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
