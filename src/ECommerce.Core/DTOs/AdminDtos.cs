namespace Katana.Core.DTOs;

public class AdminSyncStatusDto
{
    public string IntegrationName { get; set; } = string.Empty; // Katana / Luca
    public DateTime LastSyncDate { get; set; }
    public string Status { get; set; } = string.Empty; // Success / Failed / InProgress
}

public class ErrorLogDto
{
    public int Id { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ManualSyncRequest
{
    public string IntegrationName { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class SyncReportDto
{
    public string IntegrationName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime ReportDate { get; set; }
}


