namespace Katana.Core.DTOs;

/// <summary>
/// Result class for order validation operations
/// </summary>
public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();

    public static OrderValidationResult Success() =>
        new() { IsValid = true };

    public static OrderValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };

    public static OrderValidationResult Fail(string message, List<string> errors) =>
        new() { IsValid = false, ErrorMessage = message, ValidationErrors = errors };
}
