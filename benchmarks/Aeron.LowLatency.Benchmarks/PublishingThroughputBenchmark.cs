using Aeron.LowLatency.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aeron.LowLatency.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[GcServer(true)]
public class PublishingThroughputBenchmark
{
    private AeronSettings _settings = new();

    [Params(100_000)]
    public long MessageCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _settings = new AeronSettings
        {
            MessageCount = MessageCount,
            WarmupCount = 10_000,
            BatchSize = 1_024,
            Channel = Environment.GetEnvironmentVariable("AERON_CHANNEL") ?? AeronChannels.Ipc,
            AeronDirectory = Environment.GetEnvironmentVariable("AERON_DIR")
        };
    }

    [Benchmark]
    public async Task<PublishResult> PublishMessages()
    {
        var publisher = new OrderPublisher(_settings, NullLogger<OrderPublisher>.Instance);
        return await publisher.PublishAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
