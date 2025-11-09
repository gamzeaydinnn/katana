using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly IMappingService _mappingService;

    public ValidationService(ILogger<ValidationService> logger, IMappingService mappingService)
    {
        _logger = logger;
        _mappingService = mappingService;
    }

    public async Task<ValidationResultDto> ValidateStockMappingAsync(Stock stock, Product product)
    {
        var result = new ValidationResultDto { ValidatedAt = DateTime.UtcNow };
        var locationMapping = await _mappingService.GetLocationMappingAsync();

        if (string.IsNullOrWhiteSpace(product.SKU))
            result.Errors.Add(new ValidationError { Code = "STK001", Field = "ProductSKU", Message = "Ürün kodu boş olamaz" });

        if (string.IsNullOrWhiteSpace(stock.Location))
            result.Errors.Add(new ValidationError { Code = "STK002", Field = "Location", Message = "Depo lokasyonu boş olamaz" });
        else if (!locationMapping.ContainsKey(stock.Location) && !locationMapping.ContainsKey("DEFAULT"))
            result.Warnings.Add(new ValidationWarning 
            { 
                Code = "STK002W", 
                Field = "Location", 
                Message = $"'{stock.Location}' için depo eşleşmesi bulunamadı",
                Suggestion = "Mapping tablosuna varsayılan depo ekleyin" 
            });

        if (stock.Quantity == 0)
            result.Warnings.Add(new ValidationWarning { Code = "STK003W", Field = "Quantity", Message = "Miktar sıfır" });

        if (string.IsNullOrWhiteSpace(stock.Type))
            result.Errors.Add(new ValidationError { Code = "STK004", Field = "Type", Message = "Hareket tipi boş olamaz" });

        result.IsValid = !result.Errors.Any();
        return result;
    }

    public async Task<ValidationResultDto> ValidateInvoiceMappingAsync(Invoice invoice, Customer customer)
    {
        var result = new ValidationResultDto { ValidatedAt = DateTime.UtcNow };

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNo))
            result.Errors.Add(new ValidationError { Code = "INV001", Field = "InvoiceNo", Message = "Fatura numarası boş olamaz" });

        if (string.IsNullOrWhiteSpace(customer.TaxNo))
            result.Errors.Add(new ValidationError { Code = "INV002", Field = "CustomerTaxNo", Message = "Müşteri vergi numarası boş olamaz" });
        else if (customer.TaxNo.Length != 10 && customer.TaxNo.Length != 11)
            result.Warnings.Add(new ValidationWarning 
            { 
                Code = "INV002W", 
                Field = "CustomerTaxNo", 
                Message = "Vergi numarası 10 veya 11 haneli olmalıdır",
                Suggestion = "Vergi numarasını kontrol edin" 
            });

        if (invoice.TotalAmount <= 0)
            result.Errors.Add(new ValidationError { Code = "INV003", Field = "TotalAmount", Message = "Toplam tutar sıfırdan büyük olmalıdır" });

        if (invoice.TaxAmount < 0)
            result.Errors.Add(new ValidationError { Code = "INV004", Field = "TaxAmount", Message = "KDV tutarı negatif olamaz" });

        if (string.IsNullOrWhiteSpace(invoice.Currency))
            result.Errors.Add(new ValidationError { Code = "INV005", Field = "Currency", Message = "Para birimi boş olamaz" });
        else if (invoice.Currency != "TRY" && invoice.Currency != "USD" && invoice.Currency != "EUR")
            result.Warnings.Add(new ValidationWarning 
            { 
                Code = "INV005W", 
                Field = "Currency", 
                Message = $"Standart dışı para birimi: {invoice.Currency}",
                Suggestion = "TRY, USD veya EUR kullanılması önerilir" 
            });

        if (invoice.InvoiceDate > DateTime.UtcNow.AddDays(1))
            result.Warnings.Add(new ValidationWarning { Code = "INV006W", Field = "InvoiceDate", Message = "Fatura tarihi ileri tarihli" });

        result.IsValid = !result.Errors.Any();
        return result;
    }

    public async Task<ValidationResultDto> ValidateCustomerMappingAsync(Customer customer)
    {
        var result = new ValidationResultDto { ValidatedAt = DateTime.UtcNow };

        if (string.IsNullOrWhiteSpace(customer.TaxNo))
            result.Errors.Add(new ValidationError { Code = "CUS001", Field = "TaxNo", Message = "Vergi numarası boş olamaz" });
        else if (customer.TaxNo.Length != 10 && customer.TaxNo.Length != 11)
            result.Errors.Add(new ValidationError 
            { 
                Code = "CUS002", 
                Field = "TaxNo", 
                Message = "Vergi numarası 10 (kurumsal) veya 11 (bireysel) haneli olmalıdır",
                Value = customer.TaxNo 
            });

        if (string.IsNullOrWhiteSpace(customer.Title))
            result.Errors.Add(new ValidationError { Code = "CUS003", Field = "Title", Message = "Müşteri ünvanı boş olamaz" });

        if (!string.IsNullOrWhiteSpace(customer.Email) && !IsValidEmail(customer.Email))
            result.Warnings.Add(new ValidationWarning 
            { 
                Code = "CUS004W", 
                Field = "Email", 
                Message = "Geçersiz e-posta formatı",
                Suggestion = "E-posta adresini kontrol edin" 
            });

        result.IsValid = !result.Errors.Any();
        return await Task.FromResult(result);
    }

    public async Task<List<ValidationResultDto>> ValidateBatchDataAsync<T>(List<T> data) where T : class
    {
        var results = new List<ValidationResultDto>();
        
        foreach (var item in data)
        {
            if (item is Stock stock && item is Product product)
                results.Add(await ValidateStockMappingAsync(stock, product));
            else if (item is Invoice invoice && item is Customer customer)
                results.Add(await ValidateInvoiceMappingAsync(invoice, customer));
            else if (item is Customer cust)
                results.Add(await ValidateCustomerMappingAsync(cust));
        }

        return results;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
