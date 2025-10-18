using Katana.Core.DTOs;
using Katana.Core.Enums;

namespace Katana.Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllAsync();
    Task<OrderDto?> GetByIdAsync(int id);
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
    Task<bool> DeleteAsync(int id);
}
