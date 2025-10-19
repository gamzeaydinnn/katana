using Katana.Business.Interfaces;
using Katana.Business.Validators;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Katana API'sinden veya yerel depodan ham veriyi çıkarır ve temel doğrulamaları uygular.
/// </summary>
public class ExtractorService : IExtractorService
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(
        IKatanaService katanaService,
        IntegrationDbContext dbContext,
        ILogger<ExtractorService> logger)
    {
        _katanaService = katanaService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ProductDto>> ExtractProductsAsync(DateTime? fromDate = null, CancellationToken ct = default)
    {
        _logger.LogInformation("ExtractorService => Fetching products from Katana API.");

        var katanaProducts = await _katanaService.GetProductsAsync();
        var result = new List<ProductDto>();

        foreach (var product in katanaProducts)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("ExtractorService => Product extraction cancelled.");
                break;
            }

            var dto = new ProductDto
            {
                SKU = product.SKU,
                Name = product.Name,
                Price = product.Price,
                CategoryId = product.CategoryId,
                MainImageUrl = product.ImageUrl,
                Description = product.Description,
                IsActive = product.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var validationErrors = ProductValidator.ValidateUpdate(new UpdateProductDto
            {
                SKU = dto.SKU,
                Name = dto.Name,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                Description = dto.Description,
                MainImageUrl = dto.MainImageUrl,
                Stock = dto.Stock,
                IsActive = dto.IsActive
            });

            if (validationErrors.Count == 0)
            {
                result.Add(dto);
            }
            else
            {
                _logger.LogWarning("ExtractorService => Product skipped. SKU={SKU}; Reasons={Reasons}",
                    dto.SKU, string.Join("; ", validationErrors));
            }
        }

        _logger.LogInformation("ExtractorService => {Count} products ready for transformation.", result.Count);
        return result;
    }

    public async Task<List<InvoiceDto>> ExtractInvoicesAsync(DateTime? fromDate = null, CancellationToken ct = default)
    {
        _logger.LogInformation("ExtractorService => Fetching invoices.");

        var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var end = DateTime.UtcNow;

        var katanaInvoices = await _katanaService.GetInvoicesAsync(start, end);
        var invoices = new List<InvoiceDto>();

        foreach (var invoice in katanaInvoices)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("ExtractorService => Invoice extraction cancelled.");
                break;
            }

            var dto = new InvoiceDto
            {
                InvoiceNo = invoice.InvoiceNo,
                CustomerTaxNo = invoice.CustomerTaxNo,
                CustomerName = invoice.CustomerTitle,
                Amount = invoice.Amount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Currency = invoice.Currency,
                Status = "SENT",
                Items = invoice.Items.Select(item => new InvoiceItemDto
                {
                    ProductSKU = item.ProductSKU,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    TotalAmount = item.TotalAmount
                }).ToList()
            };

            var (isValid, errors) = InvoiceValidator.ValidateCreate(new CreateInvoiceDto
            {
                InvoiceNo = dto.InvoiceNo,
                CustomerId = dto.CustomerId,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Currency = dto.Currency,
                Items = dto.Items.Select(i => new CreateInvoiceItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = i.TaxRate
                }).ToList()
            });

            if (isValid)
            {
                invoices.Add(dto);
            }
            else
            {
                _logger.LogWarning("ExtractorService => Invoice skipped. InvoiceNo={InvoiceNo}; Reasons={Reasons}",
                    dto.InvoiceNo, string.Join("; ", errors));
            }
        }

        _logger.LogInformation("ExtractorService => {Count} invoices ready for transformation.", invoices.Count);
        return invoices;
    }

    public async Task<List<CustomerDto>> ExtractCustomersAsync(DateTime? fromDate = null, CancellationToken ct = default)
    {
        _logger.LogInformation("ExtractorService => Fetching customers from integration database.");

        var query = _dbContext.Customers.AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.UpdatedAt >= fromDate.Value);
        }

        var customers = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Take(500)
            .ToListAsync(ct);

        var result = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            TaxNo = c.TaxNo,
            Title = c.Title,
            ContactPerson = c.ContactPerson,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            City = c.City,
            Country = c.Country,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        _logger.LogInformation("ExtractorService => {Count} customers ready for transformation.", result.Count);
        return result;
    }
}
