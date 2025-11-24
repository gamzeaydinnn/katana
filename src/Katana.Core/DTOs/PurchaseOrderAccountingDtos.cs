using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class PurchaseOrderAccountingMetadataDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("integration_type")]
    public string IntegrationType { get; set; } = string.Empty; // xero | quickbooks

    [JsonPropertyName("bill_id")]
    public string? BillId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("purchase_order_id")]
    public long PurchaseOrderId { get; set; }

    [JsonPropertyName("received_items_group_id")]
    public long? ReceivedItemsGroupId { get; set; }
}

public class PurchaseOrderAccountingMetadataListQuery
{
    [JsonPropertyName("purchase_order_id")]
    public long? PurchaseOrderId { get; set; }

    [JsonPropertyName("received_items_group_id")]
    public long? ReceivedItemsGroupId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}
