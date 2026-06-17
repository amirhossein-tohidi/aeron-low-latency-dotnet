using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class LatencyMeasurementTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task CollectsLatencyPercentilesWithoutStrictThresholds()
    {
        var settings = Settings(messageCount: 100_000, streamId: 1004);

        var result = await RunPubSubAsync(settings, TimeSpan.FromMinutes(2));
        var latency = result.Subscribe.Latency;

        Output.WriteLine($"Latency ns: p50={latency.P50Nanoseconds}, p95={latency.P95Nanoseconds}, p99={latency.P99Nanoseconds}, max={latency.MaxNanoseconds}");
        latency.Count.Should().Be(100_000);
        latency.P50Nanoseconds.Should().BeGreaterThanOrEqualTo(0);
        latency.P95Nanoseconds.Should().BeGreaterThanOrEqualTo(latency.P50Nanoseconds);
        latency.P99Nanoseconds.Should().BeGreaterThanOrEqualTo(latency.P95Nanoseconds);
    }
}
