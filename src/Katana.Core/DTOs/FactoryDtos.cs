using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Factory genel bilgisi DTO.
/// </summary>
public class FactoryDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
