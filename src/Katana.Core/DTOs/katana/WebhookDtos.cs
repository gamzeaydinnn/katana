using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class WebhookDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("subscribed_events")]
    public List<string> SubscribedEvents { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class WebhookCreateRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("subscribed_events")]
    public List<string> SubscribedEvents { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WebhookUpdateRequest
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("subscribed_events")]
    public List<string>? SubscribedEvents { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WebhookListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }
}

public class WebhookLogsExportRequest
{
    [JsonPropertyName("webhook_id")]
    public long? WebhookId { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("delivered")]
    public bool? Delivered { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }
}
