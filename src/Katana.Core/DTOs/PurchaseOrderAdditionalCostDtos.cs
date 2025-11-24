using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class PurchaseOrderAdditionalCostRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("additional_cost_id")]
    public long AdditionalCostId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string DistributionMethod { get; set; } = string.Empty; // BY_VALUE, NON_DISTRIBUTED

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class PurchaseOrderAdditionalCostRowCreateRequest
{
    [JsonPropertyName("additional_cost_id")]
    public long AdditionalCostId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string DistributionMethod { get; set; } = "BY_VALUE";
}

public class PurchaseOrderAdditionalCostRowUpdateRequest
{
    [JsonPropertyName("additional_cost_id")]
    public long? AdditionalCostId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("distribution_method")]
    public string? DistributionMethod { get; set; }
}

public class PurchaseOrderAdditionalCostRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("additional_cost_id")]
    public long? AdditionalCostId { get; set; }

    [JsonPropertyName("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("distribution_method")]
    public string? DistributionMethod { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}
