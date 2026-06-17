using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class HighVolumePubSubTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    [Trait("Category", "LoadTests")]
    public async Task PublishesOneMillionMessagesInOrder()
    {
        var settings = Settings(messageCount: 1_000_000, streamId: 1002);

        var result = await RunPubSubAsync(settings, TimeSpan.FromMinutes(5));

        Output.WriteLine($"Throughput: {result.Subscribe.ThroughputMessagesPerSecond:F0} msg/sec");
        result.Subscribe.ReceivedMessages.Should().Be(1_000_000);
        result.Subscribe.IsOrderingValid.Should().BeTrue();
    }
}
