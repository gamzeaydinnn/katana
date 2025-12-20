namespace Katana.Data.Configuration;

public class InventorySettings
{
    public const string SectionName = "Inventory";
    public string DefaultWarehouseCode { get; set; } = "002";
    public string DefaultWarehouseName { get; set; } = "MERKEZ DEPO";
    public string DefaultEntryWarehouseCode { get; set; } = "002";
    public string DefaultExitWarehouseCode { get; set; } = "002";
    
    /// <summary>Luca depo kategori ağacı ID'si (Koza'daki depoKategoriAgacId)</summary>
    public long LucaDepoKategoriAgacId { get; set; } = 11356;
    
    /// <summary>Luca depo kategori ağacı kodu (Koza'daki sisDepoKategoriAgacKodu)</summary>
    public string LucaDepoKategoriAgacKodu { get; set; } = "002";
}
