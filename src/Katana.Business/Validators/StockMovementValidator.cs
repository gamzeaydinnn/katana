using Katana.Core.DTOs;

namespace Katana.Business.Validators;




public static class StockMovementValidator
{
    
    
    
    public static (bool IsValid, List<string> Errors) Validate(CreateStockMovementDto dto)
    {
        var errors = new List<string>();

        if (dto.ProductId <= 0)
        {
            errors.Add("ProductId must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(dto.Location))
        {
            errors.Add("Location is required");
        }
        else if (dto.Location.Length > 100)
        {
            errors.Add("Location must be 100 characters or less");
        }

        if (dto.Quantity <= 0)
        {
            errors.Add("Quantity must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            errors.Add("Type is required");
        }
        else if (!IsValidType(dto.Type))
        {
            errors.Add("Type must be one of: IN, OUT, ADJUSTMENT");
        }

        if (!string.IsNullOrEmpty(dto.Reason) && dto.Reason.Length > 500)
        {
            errors.Add("Reason must be 500 characters or less");
        }

        if (!string.IsNullOrEmpty(dto.Reference) && dto.Reference.Length > 100)
        {
            errors.Add("Reference must be 100 characters or less");
        }

        return (errors.Count == 0, errors);
    }

    private static bool IsValidType(string type)
    {
        var validTypes = new[] { "IN", "OUT", "ADJUSTMENT" };
        return validTypes.Contains(type.ToUpper());
    }
}
