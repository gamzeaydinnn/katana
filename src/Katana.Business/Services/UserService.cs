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
}
