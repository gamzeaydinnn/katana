using Katana.Core.DTOs;
using System.Collections.Concurrent;

namespace Katana.Business.Interfaces;

/// <summary>
/// Batch job yönetim servisi interface'i
/// </summary>
public interface IBatchJobService
{
    /// <summary>
    /// Yeni batch job oluşturur ve kuyruğa ekler
    /// </summary>
    Task<BatchJobCreatedResponse> CreateBatchJobAsync(BatchPushRequest request, string createdBy);
    
    /// <summary>
    /// Job durumunu getirir
    /// </summary>
    BatchJobStatusDto? GetJobStatus(string jobId);
    
    /// <summary>
    /// Tüm aktif job'ları getirir
    /// </summary>
    ActiveBatchJobsResponse GetActiveJobs();
    
    /// <summary>
    /// Job'u iptal eder
    /// </summary>
    bool CancelJob(string jobId, string cancelledBy, string reason);
    
    /// <summary>
    /// Bekleyen job'u alır (worker için)
    /// </summary>
    BatchJobItem? DequeuePendingJob();
    
    /// <summary>
    /// Job durumunu günceller
    /// </summary>
    void UpdateJobStatus(string jobId, Action<BatchJobItem> updateAction);
    
    /// <summary>
    /// Job geçmişini temizler (tamamlanmış/iptal edilmiş eski job'ları siler)
    /// </summary>
    int CleanupOldJobs(TimeSpan olderThan);
    
    /// <summary>
    /// Bekleyen job var mı kontrol eder
    /// </summary>
    bool HasPendingJobs();
}
