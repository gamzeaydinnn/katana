namespace Katana.Business.Configuration{

public class LucaApiSettings
{
    public const string SectionName = "LucaApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseTokenAuth { get; set; } = true;

    // API Endpoints
    public LucaEndpoints Endpoints { get; set; } = new();
}

public class LucaEndpoints
{
    public string Invoices { get; set; } = "/api/documents/invoices";
    public string Stock { get; set; } = "/api/inventory/movements";
    public string Customers { get; set; } = "/api/customers";
    public string Auth { get; set; } = "/api/auth/token";
    public string Health { get; set; } = "/api/health";
}}