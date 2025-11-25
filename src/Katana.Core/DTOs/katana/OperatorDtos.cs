using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class OperatorDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("working_area")]
    public string? WorkingArea { get; set; } // shopFloor, warehouse

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }
}

public class OperatorListQuery
{
    [JsonPropertyName("working_area")]
    public string? WorkingArea { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}
