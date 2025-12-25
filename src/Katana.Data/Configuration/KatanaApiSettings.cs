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
    public string ContentType { get; set; } = "application/json";
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
    public int DefaultPageSize { get; set; } = 10000;
    public int MaxPageSize { get; set; } = 10000;
    public int DefaultStartPage { get; set; } = 1;
}
public class KatanaApiEndpoints
{
    public string Products { get; set; } = "products";
    public string Variants { get; set; } = "variants";
    public string Locations { get; set; } = "locations";
    public string BinLocations { get; set; } = "bin_locations";
    public string Contacts { get; set; } = "contacts";
    public string ContactAddresses { get; set; } = "contact_addresses";
    public string Customers { get; set; } = "customers";
    public string SalesOrders { get; set; } = "sales_orders";
    public string PurchaseOrders { get; set; } = "purchase_orders";
    public string ManufacturingOrders { get; set; } = "manufacturing_orders";
    public string StockAdjustments { get; set; } = "stock_adjustments";
    public string Batches { get; set; } = "batches";
    public string Stocktakes { get; set; } = "stocktakes";
    public string StocktakeRows { get; set; } = "stocktake_rows";

    public string BomRows { get; set; } = "bom_rows";
    public string Materials { get; set; } = "materials";
    public string Recipes { get; set; } = "recipes";

    public string PriceLists { get; set; } = "price_lists";
    public string TaxRates { get; set; } = "tax_rates";
    public string Suppliers { get; set; } = "suppliers";
    public string Webhooks { get; set; } = "webhooks";

    
    public string Stock => StockAdjustments;
    public string Invoices => SalesOrders;
    public string Health { get; set; } = "health";
}

public class KatanaWebhookEvents
{
    // Stock Adjustment Events
    public string StockAdjustmentCreated { get; set; } = "stock_adjustment.created";
    public string StockAdjustmentUpdated { get; set; } = "stock_adjustment.updated";
    
    // Product Events
    public string ProductCreated { get; set; } = "product.created";
    public string ProductUpdated { get; set; } = "product.updated";
    public string ProductDeleted { get; set; } = "product.deleted";
    
    // Material Events
    public string MaterialCreated { get; set; } = "material.created";
    public string MaterialUpdated { get; set; } = "material.updated";
    public string MaterialDeleted { get; set; } = "material.deleted";
    
    // Variant Events
    public string VariantCreated { get; set; } = "variant.created";
    public string VariantUpdated { get; set; } = "variant.updated";
    public string VariantDeleted { get; set; } = "variant.deleted";
    
    // Sales Order Events
    public string SalesOrderCreated { get; set; } = "sales_order.created";
    public string SalesOrderUpdated { get; set; } = "sales_order.updated";
    public string SalesOrderDeleted { get; set; } = "sales_order.deleted";
    public string SalesOrderPacked { get; set; } = "sales_order.packed";
    public string SalesOrderDelivered { get; set; } = "sales_order.delivered";
    public string SalesOrderStatusChanged { get; set; } = "sales_order.status_changed";
    
    // Purchase Order Events
    public string PurchaseOrderCreated { get; set; } = "purchase_order.created";
    public string PurchaseOrderUpdated { get; set; } = "purchase_order.updated";
    public string PurchaseOrderDeleted { get; set; } = "purchase_order.deleted";
    public string PurchaseOrderReceived { get; set; } = "purchase_order.received";
    
    // Manufacturing Order Events
    public string ManufacturingOrderCreated { get; set; } = "manufacturing_order.created";
    public string ManufacturingOrderUpdated { get; set; } = "manufacturing_order.updated";
    public string ManufacturingOrderDeleted { get; set; } = "manufacturing_order.deleted";
    public string ManufacturingOrderDone { get; set; } = "manufacturing_order.done";
    public string ManufacturingOrderCompleted { get; set; } = "manufacturing_order.completed";
    
    // Current Inventory Events
    public string CurrentInventoryProductUpdated { get; set; } = "current_inventory.product_updated";
    public string CurrentInventoryMaterialUpdated { get; set; } = "current_inventory.material_updated";
}
