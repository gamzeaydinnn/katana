using System;


namespace Katana.Data.Configuration;

public class KatanaApiSettings
{
    
    public string BaseUrl { get; set; } = "https://api.katanamrp.com/v1/";

    
    public string ApiKey { get; set; } = string.Empty;

    public bool UseBasicAuth { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int MaxRetryAttempts { get; set; } = 3;

    
    public int TimeoutSeconds { get; set; } = 30;

    public string AuthHeaderType { get; set; } = "X-Api-Key";
    public string AcceptHeader { get; set; } = "application/json";
    public string ContentType { get; set; } = "application/json; charset=utf-8";

    public RateLimitSettings RateLimit { get; set; } = new();
    public PaginationSettings Pagination { get; set; } = new();

    public KatanaApiEndpoints Endpoints { get; set; } = new();
    public KatanaWebhookEvents WebhookEvents { get; set; } = new();
}

public class RateLimitSettings
{
    public int RequestsPerSecond { get; set; } = 10;
    public int RequestsPerMinute { get; set; } = 100;
    public int BurstSize { get; set; } = 20;
    public int RetryAfterSeconds { get; set; } = 5;
}

public class PaginationSettings
{
    public int DefaultPageSize { get; set; } = 100;
    public int MaxPageSize { get; set; } = 100;
    public int DefaultStartPage { get; set; } = 1;
}

public class KatanaApiEndpoints
{
    public string Products { get; set; } = "products";
    public string Variants { get; set; } = "variants";
    public string Locations { get; set; } = "locations";
    public string BinLocations { get; set; } = "bin-locations";

    public string Contacts { get; set; } = "contacts";
    public string ContactAddresses { get; set; } = "contact-addresses";

    public string SalesOrders { get; set; } = "sales-orders";
    public string PurchaseOrders { get; set; } = "purchase-orders";
    public string ManufacturingOrders { get; set; } = "manufacturing-orders";

    public string StockAdjustments { get; set; } = "stock-adjustments";
    public string Batches { get; set; } = "batches";
    public string Stocktakes { get; set; } = "stocktakes";
    public string StocktakeRows { get; set; } = "stocktake-rows";

    public string BomRows { get; set; } = "bom-rows";
    public string Materials { get; set; } = "materials";
    public string Recipes { get; set; } = "recipes";

    public string PriceLists { get; set; } = "price-lists";
    public string TaxRates { get; set; } = "tax-rates";

    
    public string Stock => StockAdjustments;
    public string Invoices => SalesOrders;
    public string Health { get; set; } = "health";
    public string Customers => Contacts;
}

public class KatanaWebhookEvents
{
    public string StockAdjustmentCreated { get; set; } = "stock_adjustment.created";
    public string StockAdjustmentUpdated { get; set; } = "stock_adjustment.updated";
    public string ProductCreated { get; set; } = "product.created";
    public string ProductUpdated { get; set; } = "product.updated";
    public string SalesOrderCreated { get; set; } = "sales_order.created";
    public string SalesOrderStatusChanged { get; set; } = "sales_order.status_changed";
    public string PurchaseOrderCreated { get; set; } = "purchase_order.created";
    public string ManufacturingOrderCompleted { get; set; } = "manufacturing_order.completed";
}
