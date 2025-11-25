namespace Katana.Data.Configuration;

/// <summary>
/// Katana tarafı için mapping yapılandırmaları (depo, birim vb).
/// </summary>
public class KatanaMappingSettings
{
    /// <summary>
    /// Katana lokasyon kodu -> depo kodu eşlemesi.
    /// </summary>
    public Dictionary<string, string> LocationToWarehouse { get; set; } = new();

    /// <summary>
    /// Katana ölçü birimi -> Luca ölçü birimi eşlemesi.
    /// </summary>
    public Dictionary<string, string> UnitToMeasurementUnit { get; set; } = new();

    /// <summary>
    /// Eşleme bulunamazsa kullanılacak varsayılan depo kodu.
    /// </summary>
    public string DefaultWarehouseCode { get; set; } = "MAIN";

    /// <summary>
    /// Eşleme bulunamazsa kullanılacak varsayılan ölçü birimi.
    /// </summary>
    public string DefaultUnit { get; set; } = "ADET";
}
