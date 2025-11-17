namespace Katana.Data.Configuration;

public class InventorySettings
{
    public const string SectionName = "Inventory";
    public string DefaultWarehouseCode { get; set; } = "001";
    public string DefaultWarehouseName { get; set; } = "MERKEZ DEPO";
    public string DefaultEntryWarehouseCode { get; set; } = "001";
    public string DefaultExitWarehouseCode { get; set; } = "001";
}
