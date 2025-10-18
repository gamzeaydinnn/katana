//TransformerService (mapping, format dönüşümleri, validation)
//DTO -> Domain (veya Application DTO) dönüşümü, mapping service çağır.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;
using Katana.Business.Validators;

namespace Katana.Business.Services;

/// <summary>
/// DTO -> Domain dönüşümleri, alan map'leri, derin validasyon.
/// (Gerekirse MappingHelper/MappingService kullan)
/// </summary>
public class TransformerService
{
    private readonly MappingHelper _mapping; // projende var
    private readonly ILogger<TransformerService> _logger;

    public TransformerService(MappingHelper mapping, ILogger<TransformerService> logger)
    {
        _mapping = mapping;
        _logger = logger;
    }

    public Task<List<Product>> ToProductsAsync(IEnumerable<ProductDto> dtos)
    {
        var list = new List<Product>();
        foreach (var dto in dtos)
        {
            // Gerekirse mapping tablolarını kullan
            var product = new Product
            {
                SKU = dto.SKU,
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                IsActive = dto.IsActive
            };

            // Derin doğrulama (ör: genel DataValidator, iş kuralları)
            var errs = ProductValidator.ValidateUpdate(new UpdateProductDto {
                Name = product.Name, SKU = product.SKU, Price = product.Price,
                Stock = dto.Stock, CategoryId = product.CategoryId, Description = product.Description
            });
            if (errs.Count > 0)
            {
                _logger.LogWarning("Transformer: ürün map başarısız SKU={SKU} -> {Errors}", dto.SKU, string.Join("; ", errs));
                continue;
            }

            list.Add(product);
        }
        return Task.FromResult(list);
    }

    public Task<List<Invoice>> ToInvoicesAsync(IEnumerable<InvoiceDto> dtos)
    {
        var list = new List<Invoice>();
        foreach (var dto in dtos)
        {
            var inv = new Invoice
            {
                InvoiceNo = dto.InvoiceNo,
                CustomerId = dto.CustomerId,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency,
                Amount = dto.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m,
                TaxAmount = dto.Items?.Sum(i => (i.UnitPrice * i.Quantity) * i.TaxRate) ?? 0m,
                TotalAmount = 0m, // aşağıda setlenecek
                Status = dto.Status ?? "DRAFT",
                Notes = dto.Notes
            };
            inv.TotalAmount = inv.Amount + inv.TaxAmount;

            inv.InvoiceItems = dto.Items?.Select(i => new InvoiceItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = (i.UnitPrice * i.Quantity) * i.TaxRate,
                TotalAmount = (i.UnitPrice * i.Quantity) * (1 + i.TaxRate)
            }).ToList() ?? new List<InvoiceItem>();

            // Derin validasyon (InvoiceValidator)
            var (valid, errs) = InvoiceValidator.ValidateCreate(new CreateInvoiceDto {
                InvoiceNo = inv.InvoiceNo,
                CustomerId = inv.CustomerId,
                InvoiceDate = inv.InvoiceDate,
                Currency = inv.Currency,
                Items = inv.InvoiceItems.Select(ii => new CreateInvoiceItemDto {
                    ProductId = ii.ProductId,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice,
                    TaxRate = ii.TaxRate
                }).ToList()
            });

            if (!valid)
            {
                _logger.LogWarning("Transformer: invoice map başarısız No={No} -> {Errors}", inv.InvoiceNo, string.Join("; ", errs));
                continue;
            }

            list.Add(inv);
        }

        return Task.FromResult(list);
    }
}
