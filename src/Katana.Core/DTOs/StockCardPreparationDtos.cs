using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Result of stock card preparation for an order.
/// Contains summary statistics and individual results for each SKU.
/// </summary>
public class StockCardPreparationResult
{
    /// <summary>
    /// True if all stock cards were successfully processed (exists or created)
    /// </summary>
    [JsonPropertyName("allSucceeded")]
    public bool AllSucceeded { get; set; }

    /// <summary>
    /// Total number of order lines processed
    /// </summary>
    [JsonPropertyName("totalLines")]
    public int TotalLines { get; set; }

    /// <summary>
    /// Number of SKUs successfully processed (exists or created)
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of SKUs that failed to process
    /// </summary>
    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of SKUs skipped (empty/null SKU)
    /// </summary>
    [JsonPropertyName("skippedCount")]
    public int SkippedCount { get; set; }

    /// <summary>
    /// Individual results for each SKU
    /// </summary>
    [JsonPropertyName("results")]
    public List<StockCardOperationResult> Results { get; set; } = new();

    /// <summary>
    /// True if all SKUs failed (critical failure)
    /// </summary>
    [JsonIgnore]
    public bool HasCriticalFailures => FailedCount > 0 && SuccessCount == 0;
}

/// <summary>
/// Result of a single stock card operation (check/create).
/// </summary>
public class StockCardOperationResult
{
    /// <summary>
    /// SKU/KartKodu that was processed
    /// </summary>
    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Product name from order line
    /// </summary>
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Action taken: "exists", "created", "failed", "skipped"
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Luca stock card ID (skartId) if found or created
    /// </summary>
    [JsonPropertyName("skartId")]
    public long? SkartId { get; set; }

    /// <summary>
    /// Success message or additional info
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Timestamp when this SKU was processed
    /// </summary>
    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
