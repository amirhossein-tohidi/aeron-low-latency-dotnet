namespace Aeron.LowLatency.Core;

public sealed class LatencyStats
{
    private readonly List<long> _samples = [];

    public long Count => _samples.Count;
    public long MinNanoseconds { get; private set; } = long.MaxValue;
    public long MaxNanoseconds { get; private set; }
    public double AverageNanoseconds => _samples.Count == 0 ? 0 : _samples.Average();

    public void Record(long latencyNanoseconds)
    {
        _samples.Add(latencyNanoseconds);
        MinNanoseconds = Math.Min(MinNanoseconds, latencyNanoseconds);
        MaxNanoseconds = Math.Max(MaxNanoseconds, latencyNanoseconds);
    }

    public LatencySnapshot Snapshot()
    {
        if (_samples.Count == 0)
        {
            return new LatencySnapshot(0, 0, 0, 0, 0, 0, 0);
        }

        var sorted = _samples.ToArray();
        Array.Sort(sorted);
        return new LatencySnapshot(
            Count,
            MinNanoseconds,
            MaxNanoseconds,
            AverageNanoseconds,
            Percentile(sorted, 0.50),
            Percentile(sorted, 0.95),
            Percentile(sorted, 0.99));
    }

    private static long Percentile(long[] sortedSamples, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sortedSamples.Length) - 1;
        return sortedSamples[Math.Clamp(index, 0, sortedSamples.Length - 1)];
    }
}

public readonly record struct LatencySnapshot(
    long Count,
    long MinNanoseconds,
    long MaxNanoseconds,
    double AverageNanoseconds,
    long P50Nanoseconds,
    long P95Nanoseconds,
    long P99Nanoseconds);
