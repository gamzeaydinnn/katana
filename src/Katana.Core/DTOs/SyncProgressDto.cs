namespace Katana.Core.DTOs;

public sealed record SyncProgressDto(
    string SyncType,
    string Stage,
    int Total,
    int Processed,
    int Success,
    int Failed,
    string? CurrentSku,
    DateTime TimestampUtc);

