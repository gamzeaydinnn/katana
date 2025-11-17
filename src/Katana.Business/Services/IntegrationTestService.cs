using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class IntegrationTestService : IIntegrationTestService
{
    private readonly ILogger<IntegrationTestService> _logger;
    private readonly IntegrationDbContext _db;
    private readonly IMappingService _mappingService;

    public IntegrationTestService(ILogger<IntegrationTestService> logger, IntegrationDbContext db, IMappingService mappingService)
    {
        _logger = logger;
        _db = db;
        _mappingService = mappingService;
    }

    public async Task<IntegrationTestResultDto> TestKatanaToLucaStockFlowAsync(int sampleSize = 10)
    {
        var result = new IntegrationTestResultDto { TestName = "Katana → Luca Stok", Environment = "TEST", ExecutedAt = DateTime.UtcNow };
        try
        {
            var stocks = await _db.Stocks.Include(s => s.Product).Where(s => s.Product != null).Take(sampleSize).ToListAsync();
            result.RecordsTested = stocks.Count;
            var mapping = await _mappingService.GetLocationMappingAsync();

            foreach (var stock in stocks)
            {
                var detail = new TestValidationDetail { RecordId = stock.Id.ToString(), RecordType = "Stock" };
                try
                {
                    var luca = MappingHelper.MapToLucaStock(stock, stock.Product!, mapping);
                    if (string.IsNullOrWhiteSpace(luca.ProductCode)) detail.Errors.Add("SKU boş");
                    if (string.IsNullOrWhiteSpace(luca.WarehouseCode)) detail.Warnings.Add($"Depo yok: {stock.Location}");
                    if (string.IsNullOrWhiteSpace(luca.EntryWarehouseCode)) detail.Warnings.Add("Giriş deposu yok");
                    if (string.IsNullOrWhiteSpace(luca.ExitWarehouseCode)) detail.Warnings.Add("Çıkış deposu yok");
                    detail.IsValid = !detail.Errors.Any();
                    if (detail.IsValid) result.RecordsPassed++; else result.RecordsFailed++;
                }
                catch (Exception ex) { detail.IsValid = false; detail.Errors.Add(ex.Message); result.RecordsFailed++; }
                result.ValidationDetails.Add(detail);
            }
            result.Success = result.RecordsFailed == 0;
            _logger.LogInformation("Stok: {P}/{T}", result.RecordsPassed, result.RecordsTested);
        }
        catch (Exception ex) { result.Success = false; result.ErrorMessage = ex.Message; }
        return result;
    }

    public async Task<IntegrationTestResultDto> TestKatanaToLucaInvoiceFlowAsync(int sampleSize = 10)
    {
        var result = new IntegrationTestResultDto { TestName = "Katana → Luca Fatura", Environment = "TEST", ExecutedAt = DateTime.UtcNow };
        try
        {
            var invoices = await _db.Invoices.Include(i => i.Customer).Include(i => i.InvoiceItems).Where(i => i.Customer != null).Take(sampleSize).ToListAsync();
            result.RecordsTested = invoices.Count;
            var mapping = await _mappingService.GetSkuToAccountMappingAsync();

            foreach (var invoice in invoices)
            {
                var detail = new TestValidationDetail { RecordId = invoice.InvoiceNo, RecordType = "Invoice" };
                try
                {
                    var luca = MappingHelper.MapToLucaInvoice(invoice, invoice.Customer!, invoice.InvoiceItems.ToList(), mapping);
                    if (luca.Lines.Count != invoice.InvoiceItems.Count) detail.Errors.Add($"Kalem {invoice.InvoiceItems.Count}→{luca.Lines.Count}");
                    if (Math.Abs(luca.GrossAmount - invoice.TotalAmount) > 0.01m) detail.Errors.Add("Tutar farkı");
                    detail.IsValid = !detail.Errors.Any();
                    if (detail.IsValid) result.RecordsPassed++; else result.RecordsFailed++;
                }
                catch (Exception ex) { detail.IsValid = false; detail.Errors.Add(ex.Message); result.RecordsFailed++; }
                result.ValidationDetails.Add(detail);
            }
            result.Success = result.RecordsFailed == 0;
            _logger.LogInformation("Fatura: {P}/{T}", result.RecordsPassed, result.RecordsTested);
        }
        catch (Exception ex) { result.Success = false; result.ErrorMessage = ex.Message; }
        return result;
    }

    public async Task<IntegrationTestResultDto> TestMappingConsistencyAsync()
    {
        var result = new IntegrationTestResultDto { TestName = "Mapping Kontrolü", Environment = "TEST", ExecutedAt = DateTime.UtcNow };
        try
        {
            var skuMap = await _mappingService.GetSkuToAccountMappingAsync();
            var locMap = await _mappingService.GetLocationMappingAsync();
            var products = await _db.Products.Where(p => p.IsActive).Select(p => p.SKU).ToListAsync();
            var locations = await _db.Stocks.Select(s => s.Location).Distinct().ToListAsync();
            result.RecordsTested = products.Count + locations.Count;

            foreach (var sku in products)
            {
                var ok = skuMap.ContainsKey(sku) || skuMap.ContainsKey("DEFAULT");
                if (ok) result.RecordsPassed++;
                result.ValidationDetails.Add(new TestValidationDetail { RecordId = sku, RecordType = "SKU", IsValid = ok });
            }
            foreach (var loc in locations)
            {
                var ok = locMap.ContainsKey(loc) || locMap.ContainsKey("DEFAULT");
                if (ok) result.RecordsPassed++;
                result.ValidationDetails.Add(new TestValidationDetail { RecordId = loc, RecordType = "Loc", IsValid = ok });
            }
            result.Success = true;
            result.RecordsFailed = result.RecordsTested - result.RecordsPassed;
            _logger.LogInformation("Mapping: {P}/{T}", result.RecordsPassed, result.RecordsTested);
        }
        catch (Exception ex) { result.Success = false; result.ErrorMessage = ex.Message; }
        return result;
    }

    public async Task<List<IntegrationTestResultDto>> RunFullUATSuiteAsync()
    {
        return new List<IntegrationTestResultDto>
        {
            await TestMappingConsistencyAsync(),
            await TestKatanaToLucaStockFlowAsync(10),
            await TestKatanaToLucaInvoiceFlowAsync(10)
        };
    }
}
