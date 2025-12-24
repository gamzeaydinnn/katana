namespace Katana.Core.DTOs;

/// <summary>
/// SKU doğrulama sonucu
/// </summary>
public class SKUValidationResult
{
    public bool IsValid { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuggestedFormat { get; set; }
    public SKUComponents? ParsedComponents { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// SKU bileşenleri
/// </summary>
public class SKUComponents
{
    public string ProductCode { get; set; } = string.Empty;
    public string? VariantCode { get; set; }
    public string? AttributeCode { get; set; }
    public string FullSKU => string.Join("-", new[] { ProductCode, VariantCode, AttributeCode }.Where(x => !string.IsNullOrEmpty(x)));
}

/// <summary>
/// SKU yeniden adlandırma sonucu
/// </summary>
public class SKURenameResult
{
    public bool Success { get; set; }
    public string OldSKU { get; set; } = string.Empty;
    public string NewSKU { get; set; } = string.Empty;
    public int UpdatedProducts { get; set; }
    public int UpdatedOrderLines { get; set; }
    public int UpdatedStockMovements { get; set; }
    public int UpdatedLucaMappings { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// SKU yeniden adlandırma önizlemesi
/// </summary>
public class SKURenamePreview
{
    public string OldSKU { get; set; } = string.Empty;
    public string NewSKU { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    public int AffectedProducts { get; set; }
    public int AffectedOrderLines { get; set; }
    public int AffectedStockMovements { get; set; }
    public int AffectedLucaMappings { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictMessage { get; set; }
}

/// <summary>
/// SKU yeniden adlandırma isteği
/// </summary>
public class SKURenameRequest
{
    public string OldSKU { get; set; } = string.Empty;
    public string NewSKU { get; set; } = string.Empty;
}

/// <summary>
/// Toplu SKU yeniden adlandırma sonucu
/// </summary>
public class BulkSKURenameResult
{
    public bool Success { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<SKURenameResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// SKU doğrulama isteği
/// </summary>
public class SKUValidateRequest
{
    public string SKU { get; set; } = string.Empty;
    public bool AutoCorrect { get; set; } = false;
}

/// <summary>
/// Toplu SKU önizleme isteği
/// </summary>
public class BulkSKURenamePreviewRequest
{
    public List<SKURenameRequest> Renames { get; set; } = new();
    public bool ValidateOnly { get; set; } = true;
}
