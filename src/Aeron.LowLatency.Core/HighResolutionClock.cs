using System.Diagnostics;

namespace Aeron.LowLatency.Core;

public static class HighResolutionClock
{
    private static readonly double NanosecondsPerTimestamp = 1_000_000_000d / Stopwatch.Frequency;

    public static long GetTimestamp() => Stopwatch.GetTimestamp();

    public static long GetUnixTimeNanoseconds()
    {
        var now = DateTimeOffset.UtcNow;
        return now.ToUnixTimeMilliseconds() * 1_000_000L + (Stopwatch.GetTimestamp() % TimeSpan.TicksPerMillisecond) * 100L;
    }

    public static long ElapsedNanoseconds(long startedAtTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startedAtTimestamp;
        return (long)(elapsedTicks * NanosecondsPerTimestamp);
    }
}
