using Katana.Business.Interfaces;
using Katana.Business.Validators;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;




public class TransformerService : ITransformerService
{
    private readonly ILogger<TransformerService> _logger;

    public TransformerService(ILogger<TransformerService> logger)
    {
        _logger = logger;
    }

    public Task<List<Product>> ToProductsAsync(IEnumerable<ProductDto> dtos)
    {
        var products = new List<Product>();

        foreach (var dto in dtos)
            {
                var entity = new Product
                {
                    SKU = dto.SKU,
                    Name = dto.Name,
                    Description = dto.Description ?? string.Empty,
                    CategoryId = dto.CategoryId,
                    Price = dto.Price,
                    
                    StockSnapshot = dto.Stock,
                    MainImageUrl = dto.MainImageUrl,
                    IsActive = dto.IsActive,
                    CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow
                };

            var validationErrors = ProductValidator.ValidateUpdate(new UpdateProductDto
            {
                SKU = entity.SKU,
                Name = entity.Name,
                Price = entity.Price,
                Stock = entity.Stock,
                CategoryId = entity.CategoryId,
                Description = entity.Description,
                MainImageUrl = entity.MainImageUrl,
                IsActive = entity.IsActive
            });

            if (validationErrors.Count == 0)
            {
                products.Add(entity);
            }
            else
            {
                _logger.LogWarning("TransformerService => Product mapping skipped. SKU={SKU}; Reasons={Reasons}",
                    dto.SKU, string.Join("; ", validationErrors));
            }
        }

        return Task.FromResult(products);
    }

    public Task<List<Invoice>> ToInvoicesAsync(IEnumerable<InvoiceDto> dtos)
    {
        var invoices = new List<Invoice>();

        foreach (var dto in dtos)
        {
            var invoice = new Invoice
            {
                InvoiceNo = dto.InvoiceNo,
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                TaxAmount = dto.TaxAmount,
                TotalAmount = dto.TotalAmount,
                Status = Enum.TryParse<InvoiceStatus>(dto.Status, true, out var status) ? status : InvoiceStatus.Draft,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency,
                Notes = dto.Notes,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            invoice.InvoiceItems = dto.Items.Select(item => new InvoiceItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSKU = item.ProductSKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate > 1 ? item.TaxRate / 100 : item.TaxRate, // Normalize: 18 -> 0.18
                TaxAmount = item.TaxAmount,
                TotalAmount = item.TotalAmount,
                Unit = item.Unit
            }).ToList();

            var (isValid, errors) = InvoiceValidator.ValidateCreate(new CreateInvoiceDto
            {
                InvoiceNo = invoice.InvoiceNo,
                CustomerId = invoice.CustomerId,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Currency = invoice.Currency,
                Items = invoice.InvoiceItems.Select(ii => new CreateInvoiceItemDto
                {
                    ProductId = ii.ProductId,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice,
                    TaxRate = ii.TaxRate
                }).ToList()
            });

            if (isValid)
            {
                invoices.Add(invoice);
            }
            else
            {
                _logger.LogWarning("TransformerService => Invoice mapping skipped. InvoiceNo={InvoiceNo}; Reasons={Reasons}",
                    dto.InvoiceNo, string.Join("; ", errors));
            }
        }

        return Task.FromResult(invoices);
    }

    public Task<List<Customer>> ToCustomersAsync(IEnumerable<CustomerDto> dtos)
    {
        var customers = dtos.Select(dto => new Customer
        {
            Id = dto.Id,
            TaxNo = dto.TaxNo,
            Title = dto.Title,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow
        }).ToList();

        return Task.FromResult(customers);
    }
}
