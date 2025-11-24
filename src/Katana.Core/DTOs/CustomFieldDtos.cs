using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Custom fields collection (listeleme).
/// </summary>
public class CustomFieldsCollectionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
