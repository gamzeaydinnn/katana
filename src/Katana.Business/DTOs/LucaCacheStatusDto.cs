namespace Katana.Business.DTOs;

/// <summary>
/// Luca cache durumu (stok kartı cache'i) için basit DTO.
/// </summary>
public sealed class LucaCacheStatusDto
{
    public bool IsWarm { get; set; }
    public int CacheEntries { get; set; }
    public DateTime? LastWarmupUtc { get; set; }
    public double AgeMinutes { get; set; }
}
