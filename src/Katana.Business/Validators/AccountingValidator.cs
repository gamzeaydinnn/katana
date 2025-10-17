using Katana.Core.DTOs;

namespace Katana.Business.Validators;

public static class AccountingValidator
{
    public static List<string> ValidateCreate(CreateAccountingRecordDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Type))
            errors.Add("İşlem tipi gereklidir");
        else if (dto.Type.ToUpper() != "INCOME" && dto.Type.ToUpper() != "EXPENSE")
            errors.Add("İşlem tipi INCOME veya EXPENSE olmalıdır");

        if (string.IsNullOrWhiteSpace(dto.Category))
            errors.Add("Kategori gereklidir");
        else if (dto.Category.Length > 100)
            errors.Add("Kategori 100 karakterden uzun olamaz");

        if (dto.Amount <= 0)
            errors.Add("Tutar 0'dan büyük olmalıdır");

        if (string.IsNullOrWhiteSpace(dto.Currency))
            errors.Add("Para birimi gereklidir");
        else if (dto.Currency.Length > 10)
            errors.Add("Para birimi 10 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 500)
            errors.Add("Açıklama 500 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.PaymentMethod) && dto.PaymentMethod.Length > 100)
            errors.Add("Ödeme yöntemi 100 karakterden uzun olamaz");

        if (dto.TransactionDate == default)
            errors.Add("İşlem tarihi gereklidir");

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateAccountingRecordDto dto)
    {
        return ValidateCreate(new CreateAccountingRecordDto
        {
            Type = dto.Type,
            Category = dto.Category,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Description = dto.Description,
            InvoiceId = dto.InvoiceId,
            CustomerId = dto.CustomerId,
            PaymentMethod = dto.PaymentMethod,
            TransactionDate = dto.TransactionDate
        });
    }

    public static List<string> ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        var errors = new List<string>();

        if (startDate == default)
            errors.Add("Başlangıç tarihi gereklidir");

        if (endDate == default)
            errors.Add("Bitiş tarihi gereklidir");

        if (startDate > endDate)
            errors.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz");

        return errors;
    }
}
