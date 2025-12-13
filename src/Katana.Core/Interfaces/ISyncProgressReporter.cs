using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface ISyncProgressReporter
{
    Task ReportAsync(SyncProgressDto progress, CancellationToken ct = default);
}

public sealed class NoopSyncProgressReporter : ISyncProgressReporter
{
    public Task ReportAsync(SyncProgressDto progress, CancellationToken ct = default) => Task.CompletedTask;
}

