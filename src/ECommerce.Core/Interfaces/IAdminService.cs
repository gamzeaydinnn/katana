using ECommerce.Core.DTOs;

namespace ECommerce.Core.Interfaces;

public interface IAdminService
{
    Task<List<AdminSyncStatusDto>> GetSyncStatusesAsync();
    Task<List<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 50);
    Task<SyncReportDto> GetSyncReportAsync(string integrationName);
    Task<bool> RunManualSyncAsync(ManualSyncRequest request);
}
