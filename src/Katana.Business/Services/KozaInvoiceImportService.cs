using System.Text.Json;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using Katana.Core.Helpers;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;
public class KozaInvoiceImportService : IKozaInvoiceImportService
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<KozaInvoiceImportService> _logger;

    public KozaInvoiceImportService(
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        ILogger<KozaInvoiceImportService> logger)
    {
        _lucaService = lucaService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IntegrationTestResultDto> ImportInvoicesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? ustHareketTuru = null,
        int? altHareketTuru = null)
    {
        var result = new IntegrationTestResultDto
        {
            TestName = "Koza → Katana Fatura Import",
            Environment = "PROD",
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            
            var req = BuildListRequest(fromDate, toDate, ustHareketTuru, altHareketTuru);
            var json = await _lucaService.ListInvoicesAsync(req, detayliListe: false);

            if (json.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("KozaInvoiceImportService => Beklenen array, gelen: {Kind}", json.ValueKind);
                return result;
            }

            var invoices = new List<Invoice>();

            foreach (var item in json.EnumerateArray())
            {
                result.RecordsTested++;
                try
                {
                    var (customer, invoice) = await MapKozaInvoiceAsync(item);
                    if (invoice == null)
                    {
                        result.RecordsFailed++;
                        continue;
                    }

                    
                    var existingCustomer = await _dbContext.Customers
                        .FirstOrDefaultAsync(c => c.TaxNo == customer.TaxNo);

                    if (existingCustomer == null)
                    {
                        _dbContext.Customers.Add(customer);
                        await _dbContext.SaveChangesAsync();
                        invoice.CustomerId = customer.Id;
                    }
                    else
                    {
                        invoice.CustomerId = existingCustomer.Id;
                    }

                    
                    var exists = await _dbContext.Invoices
                        .AnyAsync(i => i.InvoiceNo == invoice.InvoiceNo && i.CustomerId == invoice.CustomerId);
                    if (exists)
                    {
                        result.RecordsFailed++;
                        continue;
                    }

                    _dbContext.Invoices.Add(invoice);
                    invoices.Add(invoice);
                    result.RecordsPassed++;
                }
                catch (Exception ex)
                {
                    result.RecordsFailed++;
                    result.ValidationDetails.Add(new TestValidationDetail
                    {
                        RecordId = item.ToString(),
                        RecordType = "KozaInvoice",
                        IsValid = false,
                        Errors = { ex.Message }
                    });
                }
            }

            if (invoices.Any())
            {
                await _dbContext.SaveChangesAsync();
            }

            result.Success = result.RecordsFailed == 0;
            _logger.LogInformation("KozaInvoiceImportService => Imported {Imported}/{Total} invoices",
                result.RecordsPassed, result.RecordsTested);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Koza → Katana fatura importu sırasında hata");
        }

        return result;
    }

    private static LucaListInvoicesRequest BuildListRequest(
        DateTime? fromDate,
        DateTime? toDate,
        int? ustHareketTuru,
        int? altHareketTuru)
    {
        var filter = new LucaInvoiceBelgeFilter();

        if (fromDate.HasValue || toDate.HasValue)
        {
            
            if (fromDate.HasValue)
                filter.BelgeTarihiBas = fromDate.Value.ToString("dd/MM/yyyy");
            if (toDate.HasValue)
                filter.BelgeTarihiBit = toDate.Value.ToString("dd/MM/yyyy");
            filter.BelgeTarihiOp = "between";
        }

        return new LucaListInvoicesRequest
        {
            FtrSsFaturaBaslik = new LucaInvoiceOrgBelgeFilter
            {
                GnlOrgSsBelge = filter
            },
            ParUstHareketTuru = ustHareketTuru,
            ParAltHareketTuru = altHareketTuru
        };
    }

    private Task<(Customer customer, Invoice? invoice)> MapKozaInvoiceAsync(JsonElement item)
    {
        
        

        var customerTaxNo = item.TryGetProperty("vergiKimlikNo", out var taxNoEl) && taxNoEl.ValueKind == JsonValueKind.String
            ? taxNoEl.GetString() ?? string.Empty
            : string.Empty;

        var customerName = item.TryGetProperty("cariTanim", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString() ?? string.Empty
            : "KOZA-CARI";

        var belgeSeriNo = item.TryGetProperty("belgeSeriNo", out var noEl) && noEl.ValueKind == JsonValueKind.String
            ? noEl.GetString() ?? string.Empty
            : string.Empty;

        DateTime belgeTarihi = DateTime.UtcNow;
        if (item.TryGetProperty("belgeTarihi", out var dateEl) && dateEl.ValueKind == JsonValueKind.String)
        {
            DateTime.TryParse(dateEl.GetString(), out belgeTarihi);
        }

        DateTime? vadeTarihi = null;
        if (item.TryGetProperty("vadeTarihi", out var dueEl) && dueEl.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(dueEl.GetString(), out var d))
                vadeTarihi = d;
        }

        decimal netTutar = 0;
        if (item.TryGetProperty("netTutar", out var netEl) &&
            (netEl.ValueKind == JsonValueKind.Number) &&
            netEl.TryGetDecimal(out var nt))
        {
            netTutar = nt;
        }

        
        decimal grossAmount = netTutar;
        decimal taxAmount = Math.Round(grossAmount * 0.18m, 2);

        var lucaDto = new LucaInvoiceDto
        {
            DocumentNo = belgeSeriNo,
            CustomerCode = customerTaxNo,
            CustomerTitle = customerName,
            CustomerTaxNo = customerTaxNo,
            DocumentDate = belgeTarihi,
            DueDate = vadeTarihi,
            NetAmount = netTutar,
            TaxAmount = taxAmount,
            GrossAmount = netTutar + taxAmount,
            Currency = "TRY",
            DocumentType = "KOZA_INVOICE"
        };

        var customer = new Customer
        {
            TaxNo = customerTaxNo,
            Title = customerName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Invoice? invoice = MappingHelper.MapFromLucaInvoice(lucaDto, 0);
        return Task.FromResult<(Customer customer, Invoice? invoice)>((customer, invoice));
    }
}
