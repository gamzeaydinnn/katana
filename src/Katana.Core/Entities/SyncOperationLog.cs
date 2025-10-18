
using System.ComponentModel.DataAnnotations;
namespace Katana.Core.Entities;

/// <summary>
/// Her senkronizasyon işleminin kaydını tutar.
/// </summary>
public class SyncOperationLog
{
    public int Id { get; set; }
    public string SyncType { get; set; } = string.Empty; // STOCK, INVOICE, CUSTOMER
    public string Status { get; set; } = "PENDING"; // RUNNING, SUCCESS, FAILED
    public string? ErrorMessage { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string? TriggeredBy { get; set; }
    public string? Details { get; set; }
}
