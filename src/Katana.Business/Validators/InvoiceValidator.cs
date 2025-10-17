using Katana.Core.DTOs;

namespace Katana.Business.Validators;

public static class InvoiceValidator
{
    public static (bool IsValid, List<string> Errors) ValidateCreate(CreateInvoiceDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.InvoiceNo))
            errors.Add("Invoice number is required");
        else if (dto.InvoiceNo.Length > 50)
            errors.Add("Invoice number must be 50 characters or less");

        if (dto.CustomerId <= 0)
            errors.Add("Customer ID must be greater than 0");

        if (dto.InvoiceDate == default)
            errors.Add("Invoice date is required");

        if (string.IsNullOrWhiteSpace(dto.Currency))
            errors.Add("Currency is required");
        else if (dto.Currency.Length > 10)
            errors.Add("Currency must be 10 characters or less");

        if (dto.Items == null || !dto.Items.Any())
            errors.Add("Invoice must have at least one item");
        else
        {
            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0)
                    errors.Add($"Product ID must be greater than 0");
                
                if (item.Quantity <= 0)
                    errors.Add($"Quantity must be greater than 0");
                
                if (item.UnitPrice < 0)
                    errors.Add($"Unit price cannot be negative");
                
                if (item.TaxRate < 0 || item.TaxRate > 1)
                    errors.Add($"Tax rate must be between 0 and 1");
            }
        }

        return (errors.Count == 0, errors);
    }

    public static (bool IsValid, List<string> Errors) ValidateStatus(string status)
    {
        var errors = new List<string>();
        var validStatuses = new[] { "DRAFT", "SENT", "PAID", "CANCELLED", "PARTIALLY_PAID", "OVERDUE" };

        if (string.IsNullOrWhiteSpace(status))
            errors.Add("Status is required");
        else if (!validStatuses.Contains(status.ToUpper()))
            errors.Add($"Status must be one of: {string.Join(", ", validStatuses)}");

        return (errors.Count == 0, errors);
    }
}
