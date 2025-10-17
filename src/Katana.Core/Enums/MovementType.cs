namespace Katana.Core.Enums;

/// <summary>
/// Stok hareket tipi
/// </summary>
public enum MovementType
{
    /// <summary>
    /// Giriş (Alış, İade, Transfer In)
    /// </summary>
    In = 1,
    
    /// <summary>
    /// Çıkış (Satış, Fire, Transfer Out)
    /// </summary>
    Out = 2,
    
    /// <summary>
    /// Düzeltme (Sayım, Düzeltme)
    /// </summary>
    Adjustment = 3
}
