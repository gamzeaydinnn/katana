using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Katana.API.Controllers
{
    /// <summary>
    /// Sync Dashboard API - Provides monitoring and analytics for sync jobs
    /// </summary>
    [ApiController]
    [Route("api/sync/dashboard")]
    [Authorize(Roles = "Admin,Manager")]
    public class SyncDashboardController : ControllerBase
    {
        private readonly IntegrationDbContext _context;
        private readonly ILogger<SyncDashboardController> _logger;

        public SyncDashboardController(
            IntegrationDbContext context,
            ILogger<SyncDashboardController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /api/sync/dashboard/jobs - Get all sync jobs with status
        /// </summary>
        [HttpGet("jobs")]
        public IActionResult GetAllJobs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                
                var succeededJobs = monitoringApi.SucceededJobs(0, pageSize);
                var failedJobs = monitoringApi.FailedJobs(0, pageSize);
                var processingJobs = monitoringApi.ProcessingJobs(0, pageSize);
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, pageSize);
                var scheduledJobs = monitoringApi.ScheduledJobs(0, pageSize);

                var allJobs = new List<object>();

                // Succeeded jobs
                if (succeededJobs != null)
                {
                    foreach (var kvp in succeededJobs)
                    {
                        allJobs.Add(new
                        {
                            jobId = kvp.Key,
                            state = "Succeeded",
                            job = kvp.Value?.Job?.ToString(),
                            succeededAt = kvp.Value?.SucceededAt,
                            totalDuration = kvp.Value?.TotalDuration,
                            result = kvp.Value?.Result
                        });
                    }
                }

                // Failed jobs
                if (failedJobs != null)
                {
                    foreach (var kvp in failedJobs)
                    {
                        allJobs.Add(new
                        {
                            jobId = kvp.Key,
                            state = "Failed",
                            job = kvp.Value?.Job?.ToString(),
                            failedAt = kvp.Value?.FailedAt,
                            exceptionMessage = kvp.Value?.ExceptionMessage,
                            exceptionType = kvp.Value?.ExceptionType
                        });
                    }
                }

                // Processing jobs
                if (processingJobs != null)
                {
                    foreach (var kvp in processingJobs)
                    {
                        allJobs.Add(new
                        {
                            jobId = kvp.Key,
                            state = "Processing",
                            job = kvp.Value?.Job?.ToString(),
                            startedAt = kvp.Value?.StartedAt,
                            serverId = kvp.Value?.ServerId
                        });
                    }
                }

                // Enqueued jobs
                if (enqueuedJobs != null)
                {
                    foreach (var kvp in enqueuedJobs)
                    {
                        allJobs.Add(new
                        {
                            jobId = kvp.Key,
                            state = "Enqueued",
                            job = kvp.Value?.Job?.ToString(),
                            enqueuedAt = kvp.Value?.EnqueuedAt
                        });
                    }
                }

                // Scheduled jobs
                if (scheduledJobs != null)
                {
                    foreach (var kvp in scheduledJobs)
                    {
                        allJobs.Add(new
                        {
                            jobId = kvp.Key,
                            state = "Scheduled",
                            job = kvp.Value?.Job?.ToString(),
                            scheduledAt = kvp.Value?.ScheduledAt,
                            enqueueAt = kvp.Value?.EnqueueAt
                        });
                    }
                }

                return Ok(new
                {
                    total = allJobs.Count,
                    page,
                    pageSize,
                    jobs = allJobs.OrderByDescending(j => GetJobTimestamp(j)).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync jobs");
                return StatusCode(500, new { message = "Error retrieving jobs", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/sync/dashboard/jobs/{id} - Get job details with logs
        /// </summary>
        [HttpGet("jobs/{jobId}")]
        public IActionResult GetJobDetails(string jobId)
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobDetails = monitoringApi.JobDetails(jobId);

                if (jobDetails == null)
                {
                    return NotFound(new { message = "Job not found", jobId });
                }

                var history = jobDetails.History.Select(h => new
                {
                    stateName = h.StateName,
                    createdAt = h.CreatedAt,
                    reason = h.Reason,
                    data = h.Data
                }).ToList();

                return Ok(new
                {
                    jobId,
                    job = jobDetails.Job?.ToString(),
                    createdAt = jobDetails.CreatedAt,
                    expireAt = jobDetails.ExpireAt,
                    properties = jobDetails.Properties,
                    history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job details for {JobId}", jobId);
                return StatusCode(500, new { message = "Error retrieving job details", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/sync/dashboard/summary - Today's sync summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                var endDate = targetDate.AddDays(1);

                // Get data from SyncOperationLogs table
                var dbLogs = await _context.SyncOperationLogs
                    .Where(l => l.StartTime >= targetDate && l.StartTime < endDate)
                    .ToListAsync();

                var totalJobs = dbLogs.Count;
                var successJobs = dbLogs.Count(l => l.Status == "Success");
                var failedJobs = dbLogs.Count(l => l.Status == "Failed");
                var runningJobs = dbLogs.Count(l => l.Status == "Running" || l.Status == "InProgress");

                var totalProcessed = dbLogs.Sum(l => l.ProcessedRecords);
                var totalSuccess = dbLogs.Sum(l => l.SuccessfulRecords);
                var totalFailed = dbLogs.Sum(l => l.FailedRecords);

                // Get Hangfire stats
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var stats = monitoringApi.GetStatistics();

                var byType = dbLogs
                    .GroupBy(l => l.SyncType)
                    .Select(g => new
                    {
                        syncType = g.Key,
                        count = g.Count(),
                        success = g.Count(l => l.Status == "Success"),
                        failed = g.Count(l => l.Status == "Failed"),
                        totalProcessed = g.Sum(l => l.ProcessedRecords),
                        totalSuccess = g.Sum(l => l.SuccessfulRecords)
                    })
                    .ToList();

                return Ok(new
                {
                    date = targetDate,
                    summary = new
                    {
                        totalJobs,
                        successJobs,
                        failedJobs,
                        runningJobs,
                        totalProcessed,
                        totalSuccess,
                        totalFailed,
                        successRate = totalJobs > 0 ? (double)successJobs / totalJobs * 100 : 0
                    },
                    byType,
                    hangfireStats = new
                    {
                        enqueued = stats.Enqueued,
                        processing = stats.Processing,
                        succeeded = stats.Succeeded,
                        failed = stats.Failed,
                        scheduled = stats.Scheduled,
                        servers = stats.Servers
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync summary");
                return StatusCode(500, new { message = "Error retrieving summary", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/sync/dashboard/stats - Overall statistics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var stats = monitoringApi.GetStatistics();

                return Ok(new
                {
                    enqueued = stats.Enqueued,
                    processing = stats.Processing,
                    succeeded = stats.Succeeded,
                    failed = stats.Failed,
                    scheduled = stats.Scheduled,
                    recurring = stats.Recurring,
                    servers = stats.Servers,
                    deleted = stats.Deleted,
                    queues = stats.Queues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/sync/dashboard/jobs/{id}/retry - Retry failed job
        /// </summary>
        [HttpPost("jobs/{jobId}/retry")]
        [Authorize(Roles = "Admin")]
        public IActionResult RetryJob(string jobId)
        {
            try
            {
                BackgroundJob.Requeue(jobId);
                _logger.LogInformation("Job {JobId} requeued for retry", jobId);
                return Ok(new { message = "Job requeued successfully", jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying job {JobId}", jobId);
                return StatusCode(500, new { message = "Error retrying job", error = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/sync/dashboard/jobs/{id} - Delete job
        /// </summary>
        [HttpDelete("jobs/{jobId}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteJob(string jobId)
        {
            try
            {
                BackgroundJob.Delete(jobId);
                _logger.LogInformation("Job {JobId} deleted", jobId);
                return Ok(new { message = "Job deleted successfully", jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", jobId);
                return StatusCode(500, new { message = "Error deleting job", error = ex.Message });
            }
        }

        private DateTime? GetJobTimestamp(object job)
        {
            var jobType = job.GetType();
            var succeededAtProp = jobType.GetProperty("succeededAt");
            if (succeededAtProp != null)
                return succeededAtProp.GetValue(job) as DateTime?;

            var failedAtProp = jobType.GetProperty("failedAt");
            if (failedAtProp != null)
                return failedAtProp.GetValue(job) as DateTime?;

            var startedAtProp = jobType.GetProperty("startedAt");
            if (startedAtProp != null)
                return startedAtProp.GetValue(job) as DateTime?;

            var enqueuedAtProp = jobType.GetProperty("enqueuedAt");
            if (enqueuedAtProp != null)
                return enqueuedAtProp.GetValue(job) as DateTime?;

            return null;
        }
    }
}
