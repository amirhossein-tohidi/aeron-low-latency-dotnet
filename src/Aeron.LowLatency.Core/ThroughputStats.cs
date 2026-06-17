using System.Diagnostics;

namespace Aeron.LowLatency.Core;

public sealed class ThroughputStats
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public long Count { get; private set; }

    public void Add(long count) => Count += count;

    public double MessagesPerSecond => _stopwatch.Elapsed.TotalSeconds <= 0 ? 0 : Count / _stopwatch.Elapsed.TotalSeconds;
}
