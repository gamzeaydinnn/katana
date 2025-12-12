namespace Katana.Core.DTOs;

public class SupplierImportResultDto
{
    public int TotalFromKatana { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

