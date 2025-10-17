using Katana.Business.DTOs;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IAdminService
{
    Task<List<AdminSyncStatusDto>> GetSyncStatusesAsync();
    Task<List<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 50);
    Task<SyncReportDto> GetSyncReportAsync(string integrationName);
    Task<bool> RunManualSyncAsync(ManualSyncRequest request);
}


/*Interfaces/
│   │       ├── IKatanaClient.cs
│   │       ├── ILucaClient.cs
│   │       ├── IExtractor.cs
│   │       ├── ITransformer.cs
│   │       ├── ILoader.cs
│   │       ├── IMappingService.cs
│   │       ├── ISyncManager.cs
│   │       ├── IAuditLogger.cs
│   │       ├── IErrorHandler.cs
│   │       ├── IRepository{T}.cs
│   │       └── IUnitOfWork.cs*/