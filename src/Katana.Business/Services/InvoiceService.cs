using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(IntegrationDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<InvoiceSummaryDto>> GetAllInvoicesAsync()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
                .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNo)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
                .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<List<InvoiceSummaryDto>> GetInvoicesByCustomerIdAsync(int customerId)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<InvoiceSummaryDto>> GetInvoicesByStatusAsync(string status)
    {
        if (!Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
            return new List<InvoiceSummaryDto>();
            
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Where(i => i.Status == statusEnum)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<InvoiceSummaryDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<InvoiceSummaryDto>> GetOverdueInvoicesAsync()
    {
        var today = DateTime.UtcNow;
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Where(i => i.DueDate.HasValue && i.DueDate.Value < today && i.Status != InvoiceStatus.Paid)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<InvoiceSummaryDto>> GetUnsyncedInvoicesAsync()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.InvoiceItems)
            .Where(i => !i.IsSynced)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToSummaryDto).ToList();
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            throw new ArgumentException($"Customer with ID {dto.CustomerId} not found");

        decimal totalAmount = 0;
        decimal totalTax = 0;

        var invoice = new Invoice
        {
            InvoiceNo = dto.InvoiceNo,
            CustomerId = dto.CustomerId,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            Currency = dto.Currency,
            Notes = dto.Notes,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null)
                throw new ArgumentException($"Product with ID {itemDto.ProductId} not found");

            var itemTotal = itemDto.Quantity * itemDto.UnitPrice;
            var itemTax = itemTotal * itemDto.TaxRate;
            
            var item = new InvoiceItem
            {
                ProductId = itemDto.ProductId,
                ProductName = product.Name,
                ProductSKU = product.SKU,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TaxRate = itemDto.TaxRate,
                TaxAmount = itemTax,
                TotalAmount = itemTotal + itemTax
            };

            invoice.InvoiceItems.Add(item);
            totalAmount += itemTotal;
            totalTax += itemTax;
        }

        invoice.Amount = totalAmount;
        invoice.TaxAmount = totalTax;
        invoice.TotalAmount = totalAmount + totalTax;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created invoice {InvoiceNo} with total {Total}", invoice.InvoiceNo, invoice.TotalAmount);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto dto)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return null;

        if (dto.Notes != null)
            invoice.Notes = dto.Notes;
        
        if (dto.DueDate.HasValue)
            invoice.DueDate = dto.DueDate;

        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated invoice {Id}", id);

        return await GetInvoiceByIdAsync(id);
    }

    public async Task<bool> UpdateInvoiceStatusAsync(int id, UpdateInvoiceStatusDto dto)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return false;

        if (Enum.TryParse<InvoiceStatus>(dto.Status, true, out var statusEnum))
            invoice.Status = statusEnum;
        else
            return false;
            
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated invoice {Id} status to {Status}", id, dto.Status);

        return true;
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.Id == id);
        
        if (invoice == null)
            return false;

        _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted invoice {Id}", id);

        return true;
    }

    public async Task<bool> MarkAsSyncedAsync(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return false;

        invoice.IsSynced = true;
        invoice.SyncedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked invoice {Id} as synced", id);

        return true;
    }

    public async Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync()
    {
        var invoices = await _context.Invoices.ToListAsync();

        return CalculateStatistics(invoices);
    }

    public async Task<InvoiceStatisticsDto> GetInvoiceStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var invoices = await _context.Invoices
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .ToListAsync();

        return CalculateStatistics(invoices);
    }

    private InvoiceStatisticsDto CalculateStatistics(List<Invoice> invoices)
    {
        var today = DateTime.UtcNow;
        
        return new InvoiceStatisticsDto
        {
            TotalInvoices = invoices.Count,
            TotalAmount = invoices.Sum(i => i.TotalAmount),
            PaidAmount = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount),
            UnpaidAmount = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled).Sum(i => i.TotalAmount),
            OverdueAmount = invoices.Where(i => i.DueDate.HasValue && i.DueDate.Value < today && i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount),
            DraftCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            SentCount = invoices.Count(i => i.Status == InvoiceStatus.Sent),
            PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueCount = invoices.Count(i => i.DueDate.HasValue && i.DueDate.Value < today && i.Status != InvoiceStatus.Paid)
        };
    }

    private InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Title ?? "",
            CustomerTaxNo = invoice.Customer?.TaxNo ?? "",
            Amount = invoice.Amount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status.ToString(),
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Currency = invoice.Currency,
            Notes = invoice.Notes,
            IsSynced = invoice.IsSynced,
            SyncedAt = invoice.SyncedAt,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            Items = invoice.InvoiceItems.Select(ii => new InvoiceItemDto
            {
                Id = ii.Id,
                ProductId = ii.ProductId,
                ProductName = ii.ProductName,
                ProductSKU = ii.ProductSKU,
                Quantity = ii.Quantity,
                UnitPrice = ii.UnitPrice,
                TaxRate = ii.TaxRate,
                TaxAmount = ii.TaxAmount,
                TotalAmount = ii.TotalAmount,
                Unit = ii.Unit
            }).ToList()
        };
    }

    private InvoiceSummaryDto MapToSummaryDto(Invoice invoice)
    {
        return new InvoiceSummaryDto
        {
            Id = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            CustomerName = invoice.Customer?.Title ?? "",
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status.ToString(),
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            IsSynced = invoice.IsSynced,
            ItemCount = invoice.InvoiceItems.Count
        };
    }
}
