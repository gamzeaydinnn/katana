using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Üretim emri senkronizasyon servisi interface'i
/// </summary>
public interface IManufacturingOrderSyncService
{
    /// <summary>
    /// Üretim emrini Luca'ya senkronize eder
    /// </summary>
    Task<ManufacturingOrderSyncResult> SyncManufacturingOrderToLucaAsync(long manufacturingOrderId);

    /// <summary>
    /// Üretim tamamlama işlemini Luca'ya bildirir
    /// </summary>
    Task<ManufacturingOrderSyncResult> SyncProductionCompletionAsync(
        long manufacturingOrderId, 
        decimal completedQty, 
        List<MaterialConsumption> consumedMaterials);

    /// <summary>
    /// Üretim emri senkronizasyon durumunu getirir
    /// </summary>
    Task<ManufacturingOrderSyncResult?> GetSyncStatusAsync(long manufacturingOrderId);
}
