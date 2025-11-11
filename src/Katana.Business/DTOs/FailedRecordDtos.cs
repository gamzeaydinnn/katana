namespace Katana.Business.DTOs;

public class ResolveFailedRecordDto
{
    public string? Resolution { get; set; }
    public string? CorrectedData { get; set; }
    public bool Resend { get; set; }
}

public class IgnoreFailedRecordDto
{
    public string? Reason { get; set; }
}
