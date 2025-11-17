namespace Katana.Data.Configuration;

public class CatalogVisibilitySettings
{
    public const string SectionName = "CatalogVisibility";

    /// <summary>
    /// When true, zero-stock products are hidden from the customer catalog.
    /// Default is false so customers can still see zero-stock items with a warning.
    /// </summary>
    public bool HideZeroStockProducts { get; set; } = false;
}
