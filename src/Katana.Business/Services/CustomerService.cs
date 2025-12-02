using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly IntegrationDbContext _context;

    public CustomerService(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.Title)
            .ToListAsync();

        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerSummaryDto>> GetActiveCustomersAsync()
    {
        var customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Title)
            .ToListAsync();

        return customers.Select(MapToSummaryDto);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<Customer?> GetCustomerEntityByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<CustomerDto?> GetCustomerByTaxNoAsync(string taxNo)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TaxNo == taxNo);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm)
    {
        var customers = await _context.Customers
            .Where(c => c.Title.Contains(searchTerm) || 
                       c.TaxNo.Contains(searchTerm) ||
                       (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                       (c.Email != null && c.Email.Contains(searchTerm)))
            .OrderBy(c => c.Title)
            .ToListAsync();

        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TaxNo == dto.TaxNo);

        if (existingCustomer != null)
            throw new InvalidOperationException($"Bu vergi numarasına sahip müşteri zaten mevcut: {dto.TaxNo}");

        // Type otomatik belirle: TaxNo 11 hane ise Şahıs (2), değilse Şirket (1)
        var type = dto.Type > 0 ? dto.Type : 
            (!string.IsNullOrWhiteSpace(dto.TaxNo) && dto.TaxNo.Length == 11 ? 2 : 1);

        var customer = new Customer
        {
            TaxNo = dto.TaxNo,
            Title = dto.Title,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            // Luca fields
            Type = type,
            TaxOffice = dto.TaxOffice,
            District = dto.District,
            GroupCode = dto.GroupCode
        };
        
        // LucaCode oluştur
        customer.LucaCode = customer.GenerateLucaCode();

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            throw new KeyNotFoundException($"Müşteri bulunamadı: {id}");

        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TaxNo == dto.TaxNo && c.Id != id);

        if (existingCustomer != null)
            throw new InvalidOperationException($"Bu vergi numarasına sahip başka bir müşteri mevcut: {dto.TaxNo}");

        // Type otomatik belirle
        var type = dto.Type > 0 ? dto.Type : 
            (!string.IsNullOrWhiteSpace(dto.TaxNo) && dto.TaxNo.Length == 11 ? 2 : 1);

        customer.TaxNo = dto.TaxNo;
        customer.Title = dto.Title;
        customer.ContactPerson = dto.ContactPerson;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.Country = dto.Country;
        customer.IsActive = dto.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        // Luca fields
        customer.Type = type;
        customer.TaxOffice = dto.TaxOffice;
        customer.District = dto.District;
        customer.GroupCode = dto.GroupCode;

        await _context.SaveChangesAsync();
        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return false;

        var hasInvoices = await _context.Invoices
            .AnyAsync(i => i.CustomerId == id);

        if (hasInvoices)
            throw new InvalidOperationException("Faturası olan müşteri silinemez. Önce müşteriyi pasif yapın.");

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return false;

        customer.IsActive = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return false;

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync()
    {
        var allCustomers = await _context.Customers.ToListAsync();

        return new CustomerStatisticsDto
        {
            TotalCustomers = allCustomers.Count,
            ActiveCustomers = allCustomers.Count(c => c.IsActive),
            InactiveCustomers = allCustomers.Count(c => !c.IsActive),
            TotalBalance = 0,
            TotalCreditLimit = 0
        };
    }

    public async Task<decimal> GetCustomerBalanceAsync(int customerId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId && i.Status != "PAID" && i.Status != "CANCELLED")
            .ToListAsync();

        return invoices.Sum(i => i.TotalAmount);
    }

    private CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            TaxNo = customer.TaxNo,
            Title = customer.Title,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            City = customer.City,
            Country = customer.Country,
            PostalCode = null,
            CreditLimit = null,
            CurrentBalance = 0,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            // Luca fields
            Type = customer.Type,
            TaxOffice = customer.TaxOffice,
            District = customer.District,
            LucaCode = customer.LucaCode ?? customer.GenerateLucaCode(),
            LucaFinansalNesneId = customer.LucaFinansalNesneId,
            LastSyncError = customer.LastSyncError,
            GroupCode = customer.GroupCode
        };
    }

    private CustomerSummaryDto MapToSummaryDto(Customer customer)
    {
        return new CustomerSummaryDto
        {
            Id = customer.Id,
            TaxNo = customer.TaxNo,
            Title = customer.Title,
            Phone = customer.Phone,
            Email = customer.Email,
            CurrentBalance = 0,
            IsActive = customer.IsActive,
            // Luca status fields
            LucaCode = customer.LucaCode ?? customer.GenerateLucaCode(),
            LucaFinansalNesneId = customer.LucaFinansalNesneId,
            LastSyncError = customer.LastSyncError
        };
    }

    public async Task UpdateLastSyncErrorAsync(int customerId, string? errorMessage, long? lucaFinansalNesneId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return;

        customer.LastSyncError = errorMessage;
        customer.UpdatedAt = DateTime.UtcNow;
        
        if (lucaFinansalNesneId.HasValue)
        {
            customer.LucaFinansalNesneId = lucaFinansalNesneId.Value;
            customer.LucaCode = customer.GenerateLucaCode();
        }
        
        await _context.SaveChangesAsync();
    }
}
