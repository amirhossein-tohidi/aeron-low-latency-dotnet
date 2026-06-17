using System.Buffers;
using Adaptive.Aeron;
using Adaptive.Aeron.LogBuffer;
using Aeron.LowLatency.Contracts;
using Microsoft.Extensions.Logging;

namespace Aeron.LowLatency.Core;

public sealed class OrderSubscriber
{
    private readonly AeronSettings _settings;
    private readonly ILogger<OrderSubscriber> _logger;
    private readonly LatencyStats _latencyStats = new();
    private readonly ThroughputStats _throughputStats = new();
    private long _lastOrderId;

    public OrderSubscriber(AeronSettings settings, ILogger<OrderSubscriber> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public long Received => _throughputStats.Count;
    public LatencySnapshot Latency => _latencyStats.Snapshot();
    public bool IsOrderingValid { get; private set; } = true;

    public async ValueTask<SubscriberResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var activity = AeronMetrics.ActivitySource.StartActivity("receive-orders");
        using var aeron = AeronClientFactory.Connect(_settings, "order-subscriber");
        using var subscription = aeron.AddSubscription(_settings.Channel, _settings.StreamId);

        var rented = ArrayPool<byte>.Shared.Rent(OrderMessageCodec.MaxEncodedLength);
        try
        {
            FragmentHandler handler = (buffer, offset, length, header) => OnFragment(buffer, offset, length, header, rented);
            var nextReport = DateTimeOffset.UtcNow.AddSeconds(1);

            while (_throughputStats.Count < _settings.MessageCount)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fragments = subscription.Poll(handler, _settings.BatchSize);
                if (fragments == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
                }

                if (DateTimeOffset.UtcNow >= nextReport)
                {
                    Report();
                    nextReport = DateTimeOffset.UtcNow.AddSeconds(1);
                }
            }

            Report();
            return new SubscriberResult(_throughputStats.Count, _throughputStats.MessagesPerSecond, _latencyStats.Snapshot(), IsOrderingValid);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private void OnFragment(Adaptive.Agrona.IDirectBuffer buffer, int offset, int length, Header header, byte[] rented)
    {
        buffer.GetBytes(offset, rented, 0, length);
        var message = OrderMessageCodec.Decode(rented.AsSpan(0, length));
        if (_lastOrderId > 0 && message.OrderId != _lastOrderId + 1)
        {
            IsOrderingValid = false;
        }

        _lastOrderId = message.OrderId;
        var latency = Math.Max(0, HighResolutionClock.GetUnixTimeNanoseconds() - message.CreatedAtUnixNano);
        _latencyStats.Record(latency);
        _throughputStats.Add(1);
        AeronMetrics.RecordReceived(1);
        AeronMetrics.RecordLatency(latency);
    }

    private void Report()
    {
        var latency = _latencyStats.Snapshot();
        AeronMetrics.RecordThroughput(_throughputStats.MessagesPerSecond);
        _logger.LogInformation(
            "Received={Received} throughput={Throughput:F0} msg/sec latency(ns) min={Min} avg={Average:F0} p50={P50} p95={P95} p99={P99} max={Max}",
            _throughputStats.Count,
            _throughputStats.MessagesPerSecond,
            latency.MinNanoseconds,
            latency.AverageNanoseconds,
            latency.P50Nanoseconds,
            latency.P95Nanoseconds,
            latency.P99Nanoseconds,
            latency.MaxNanoseconds);
    }
}

public readonly record struct SubscriberResult(long ReceivedMessages, double ThroughputMessagesPerSecond, LatencySnapshot Latency, bool IsOrderingValid);
