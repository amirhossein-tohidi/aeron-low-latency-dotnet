namespace Aeron.LowLatency.Core;

public sealed record AeronSettings
{
    public string Channel { get; init; } = AeronChannels.Ipc;
    public int StreamId { get; init; } = 1001;
    public long MessageCount { get; init; } = 10_000;
    public int BatchSize { get; init; } = 256;
    public int WarmupCount { get; init; }
    public string? AeronDirectory { get; init; }
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
