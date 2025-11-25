using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class SerialNumberDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("transaction_id")]
    public long TransactionId { get; set; }

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("quantity_change")]
    public decimal QuantityChange { get; set; }
}

public class SerialNumberListQuery
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class SerialNumberAssignRequest
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<string> SerialNumbers { get; set; } = new();
}

public class SerialNumberUnassignRequest
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public long ResourceId { get; set; }

    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();
}

public class SerialNumberStockDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("in_stock")]
    public bool InStock { get; set; }

    [JsonPropertyName("transactions")]
    public List<SerialNumberDto>? Transactions { get; set; }
}
