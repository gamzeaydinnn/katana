namespace Katana.Core.DTOs;

public class IntegrationTestResultDto
{
    public string TestName { get; set; } = string.Empty;
    public string Environment { get; set; } = "TEST";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int RecordsTested { get; set; }
    public int RecordsPassed { get; set; }
    public int RecordsFailed { get; set; }
    public List<TestValidationDetail> ValidationDetails { get; set; } = new();
}

public class TestValidationDetail
{
    public string RecordId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
