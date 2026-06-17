using Aeron.LowLatency.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aeron.LowLatency.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[GcServer(true)]
public class EndToEndLatencyBenchmark
{
    private AeronSettings _settings = new();

    [Params(50_000)]
    public long MessageCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _settings = new AeronSettings
        {
            MessageCount = MessageCount,
            WarmupCount = 5_000,
            BatchSize = 1_024,
            Channel = Environment.GetEnvironmentVariable("AERON_CHANNEL") ?? AeronChannels.Ipc,
            AeronDirectory = Environment.GetEnvironmentVariable("AERON_DIR")
        };
    }

    [Benchmark]
    public async Task<LatencySnapshot> PublishToConsumeLatency()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var subscriber = new OrderSubscriber(_settings, NullLogger<OrderSubscriber>.Instance);
        var publisher = new OrderPublisher(_settings, NullLogger<OrderPublisher>.Instance);

        var subscriberTask = subscriber.ReceiveAsync(cts.Token).AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(250), cts.Token).ConfigureAwait(false);
        await publisher.PublishAsync(cts.Token).ConfigureAwait(false);
        var result = await subscriberTask.ConfigureAwait(false);
        return result.Latency;
    }
}
