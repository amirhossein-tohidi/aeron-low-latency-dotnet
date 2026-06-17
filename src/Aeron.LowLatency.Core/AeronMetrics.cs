using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Aeron.LowLatency.Core;

public static class AeronMetrics
{
    public const string MeterName = "Aeron.LowLatency";
    public static readonly ActivitySource ActivitySource = new("Aeron.LowLatency");

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> PublishedMessagesCounter = Meter.CreateCounter<long>("aeron.published.messages");
    private static readonly Counter<long> ReceivedMessagesCounter = Meter.CreateCounter<long>("aeron.received.messages");
    private static readonly Histogram<double> ThroughputHistogram = Meter.CreateHistogram<double>("aeron.throughput.messages_per_second");
    private static readonly Histogram<double> LatencyHistogram = Meter.CreateHistogram<double>("aeron.latency.nanoseconds");

    public static void RecordPublished(long count) => PublishedMessagesCounter.Add(count);

    public static void RecordReceived(long count) => ReceivedMessagesCounter.Add(count);

    public static void RecordThroughput(double messagesPerSecond) => ThroughputHistogram.Record(messagesPerSecond);

    public static void RecordLatency(long latencyNanoseconds) => LatencyHistogram.Record(latencyNanoseconds);
}
