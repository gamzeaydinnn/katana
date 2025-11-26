namespace Katana.Data.Configuration;




public class KatanaMappingSettings
{
    
    
    
    public Dictionary<string, string> LocationToWarehouse { get; set; } = new();

    
    
    
    public Dictionary<string, string> UnitToMeasurementUnit { get; set; } = new();

    
    
    
    public string DefaultWarehouseCode { get; set; } = "MAIN";

    
    
    
    public string DefaultUnit { get; set; } = "ADET";
}
