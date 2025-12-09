namespace Katana.Core.DTOs;

public class LucaCacheStatusDto
{
    public bool IsWarm { get; set; }
    public long EntryCount { get; set; }
    public DateTime? LastWarmupUtc { get; set; }
    public string? Status { get; set; }
}
