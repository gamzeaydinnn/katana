using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Depo içi raf / storage bin DTO.
/// </summary>
public class BinLocationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("bin_name")]
    public string BinName { get; set; } = string.Empty;

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Storage bin listeleme filtreleri.
/// </summary>
public class BinLocationListQuery
{
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("bin_name")]
    public string? BinName { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

/// <summary>
/// Storage bin güncelleme isteği (partial).
/// </summary>
public class BinLocationUpdateRequest
{
    [JsonPropertyName("bin_name")]
    public string? BinName { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }
}
