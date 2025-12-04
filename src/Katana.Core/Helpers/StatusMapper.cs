using Katana.Core.Enums;

namespace Katana.Core.Helpers;

/// <summary>
/// Backend OrderStatus enum ile Katana API string status değerleri arasında mapping yapar
/// </summary>
public static class StatusMapper
{
    // Katana API -> Backend OrderStatus mapping
    private static readonly Dictionary<string, OrderStatus> KatanaToOrderStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        { "NOT_SHIPPED", OrderStatus.Pending },
        { "PARTIALLY_SHIPPED", OrderStatus.Processing },
        { "FULLY_SHIPPED", OrderStatus.Shipped },
        { "DELIVERED", OrderStatus.Delivered },
        { "CANCELLED", OrderStatus.Cancelled },
        { "RETURNED", OrderStatus.Returned }
    };

    // Backend OrderStatus -> Katana API mapping
    private static readonly Dictionary<OrderStatus, string> OrderStatusToKatana = new()
    {
        { OrderStatus.Pending, "NOT_SHIPPED" },
        { OrderStatus.Processing, "PARTIALLY_SHIPPED" },
        { OrderStatus.Shipped, "FULLY_SHIPPED" },
        { OrderStatus.Delivered, "DELIVERED" },
        { OrderStatus.Cancelled, "CANCELLED" },
        { OrderStatus.Returned, "RETURNED" }
    };

    // Katana API -> Backend PurchaseOrderStatus mapping
    private static readonly Dictionary<string, PurchaseOrderStatus> KatanaToPurchaseOrderStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        { "NOT_RECEIVED", PurchaseOrderStatus.Pending },
        { "PARTIALLY_RECEIVED", PurchaseOrderStatus.Approved },
        { "FULLY_RECEIVED", PurchaseOrderStatus.Received },
        { "CANCELLED", PurchaseOrderStatus.Cancelled }
    };

    // Backend PurchaseOrderStatus -> Katana API mapping
    private static readonly Dictionary<PurchaseOrderStatus, string> PurchaseOrderStatusToKatana = new()
    {
        { PurchaseOrderStatus.Pending, "NOT_RECEIVED" },
        { PurchaseOrderStatus.Approved, "PARTIALLY_RECEIVED" },
        { PurchaseOrderStatus.Received, "FULLY_RECEIVED" },
        { PurchaseOrderStatus.Cancelled, "CANCELLED" }
    };

    /// <summary>
    /// Katana API status string'ini Backend OrderStatus enum'a çevirir
    /// </summary>
    public static OrderStatus MapToOrderStatus(string katanaStatus)
    {
        if (string.IsNullOrWhiteSpace(katanaStatus))
            return OrderStatus.Pending;

        return KatanaToOrderStatus.TryGetValue(katanaStatus, out var status) 
            ? status 
            : OrderStatus.Pending;
    }

    /// <summary>
    /// Backend OrderStatus enum'ını Katana API status string'ine çevirir
    /// </summary>
    public static string MapToKatanaStatus(OrderStatus status)
    {
        return OrderStatusToKatana.TryGetValue(status, out var katanaStatus)
            ? katanaStatus
            : "NOT_SHIPPED";
    }

    /// <summary>
    /// Katana API status string'ini Backend PurchaseOrderStatus enum'a çevirir
    /// </summary>
    public static PurchaseOrderStatus MapToPurchaseOrderStatus(string katanaStatus)
    {
        if (string.IsNullOrWhiteSpace(katanaStatus))
            return PurchaseOrderStatus.Pending;

        return KatanaToPurchaseOrderStatus.TryGetValue(katanaStatus, out var status)
            ? status
            : PurchaseOrderStatus.Pending;
    }

    /// <summary>
    /// Backend PurchaseOrderStatus enum'ını Katana API status string'ine çevirir
    /// </summary>
    public static string MapToKatanaStatus(PurchaseOrderStatus status)
    {
        return PurchaseOrderStatusToKatana.TryGetValue(status, out var katanaStatus)
            ? katanaStatus
            : "NOT_RECEIVED";
    }

    /// <summary>
    /// Status geçişinin valid olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidTransition(OrderStatus from, OrderStatus to)
    {
        // İptal her zaman yapılabilir
        if (to == OrderStatus.Cancelled)
            return true;

        // Aynı status'e geçiş geçerli
        if (from == to)
            return true;

        // Valid transitions
        return (from, to) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Pending, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Delivered, OrderStatus.Returned) => true,
            _ => false
        };
    }

    /// <summary>
    /// PurchaseOrder status geçişinin valid olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidTransition(PurchaseOrderStatus from, PurchaseOrderStatus to)
    {
        // İptal her zaman yapılabilir
        if (to == PurchaseOrderStatus.Cancelled)
            return true;

        // Aynı status'e geçiş geçerli
        if (from == to)
            return true;

        // Valid transitions
        return (from, to) switch
        {
            (PurchaseOrderStatus.Pending, PurchaseOrderStatus.Approved) => true,
            (PurchaseOrderStatus.Approved, PurchaseOrderStatus.Received) => true,
            _ => false
        };
    }
}
