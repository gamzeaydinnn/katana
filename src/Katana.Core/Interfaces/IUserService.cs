using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> SetRoleAsync(int id, string role);
Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);
Task<bool> UpdateRoleAsync(int id, string role);
Task<UserDto?> AuthenticateAsync(string email, string password);


    
}
