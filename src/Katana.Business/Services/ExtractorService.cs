//ExtractorService (KatanaClient çağrıları, paging, batch)
//IKatanaClient çağır, page’le, ham DTO al, validate minimal.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Katana.Business.Validators;
using Katana.Core.DTOs;
using Katana.Business.Interfaces; // IKatanaService
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Kaynak sistem (Katana) çağrılarını yapar, basit/ucuz validasyon uygular,
/// ham DTO'ları Sync hattına iletir.
/// </summary>
public class ExtractorService
{
    private readonly IKatanaService _katana;
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(IKatanaService katana, ILogger<ExtractorService> logger)
    {
        _katana = katana;
        _logger = logger;
    }

    public async Task<List<ProductDto>> ExtractProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Extractor: products çekiliyor...");
        var items = await _katana.GetProductsAsync(ct);

        // Ucuz validasyon (filtreleme) — kokan kayıtları dönüştürmeye göndermeyelim
        var valid = new List<ProductDto>();
        foreach (var p in items)
        {
            var errs = ProductValidator.ValidateUpdate(new UpdateProductDto {
                Name = p.Name, SKU = p.SKU, Price = p.Price, Stock = p.Stock,
                CategoryId = p.CategoryId, Description = p.Description, MainImageUrl = p.MainImageUrl
            });
            if (errs.Count == 0) valid.Add(p);
            else _logger.LogWarning("Extractor: ürün atlandı SKU={SKU} -> {Errors}", p.SKU, string.Join("; ", errs));
            if (ct.IsCancellationRequested) break;
        }
        _logger.LogInformation("Extractor: {Count} ürün geçerli", valid.Count);
        return valid;
    }

    public async Task<List<InvoiceDto>> ExtractInvoicesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Extractor: invoices çekiliyor...");
        var items = await _katana.GetInvoicesAsync(ct);

        var (okList, _) = SplitByInvoiceValidation(items);
        _logger.LogInformation("Extractor: {Count} fatura geçerli", okList.Count);
        return okList;
    }

    public async Task<List<CustomerDto>> ExtractCustomersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Extractor: customers çekiliyor...");
        var items = await _katana.GetCustomersAsync(ct);
        // Buraya istersen CustomerValidator yazıp uygulayabilirsin.
        return items.ToList();
    }

    private static (List<InvoiceDto> ok, List<(InvoiceDto dto, List<string> errs)> bad) 
        SplitByInvoiceValidation(IEnumerable<InvoiceDto> items)
    {
        var ok = new List<InvoiceDto>();
        var bad = new List<(InvoiceDto, List<string>)>();
        foreach (var inv in items)
        {
            var (isValid, errors) = InvoiceValidator.ValidateCreate(new CreateInvoiceDto {
                InvoiceNo = inv.InvoiceNo,
                CustomerId = inv.CustomerId,
                InvoiceDate = inv.InvoiceDate,
                Currency = inv.Currency,
                Items = inv.Items?.ToList() ?? new List<CreateInvoiceItemDto>()
            });
            if (isValid) ok.Add(inv); else bad.Add((inv, errors));
        }
        return (ok, bad);
    }
}
