namespace ECommerce.Business.Configuration;

public class KatanaApiSettings
{
    public const string SectionName = "KatanaApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseBasicAuth { get; set; } = false;

    // API Endpoints
    public KatanaEndpoints Endpoints { get; set; } = new();
}

public class KatanaEndpoints
{
    public string Products { get; set; } = "/api/products";
    public string Stock { get; set; } = "/api/stock/movements";
    public string Invoices { get; set; } = "/api/invoices";
    public string Customers { get; set; } = "/api/customers";
    public string Health { get; set; } = "/api/health";
}
