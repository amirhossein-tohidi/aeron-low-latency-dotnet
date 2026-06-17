using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class BasicPubSubTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task PublishesAndReceivesTenThousandMessages()
    {
        var settings = Settings(messageCount: 10_000, streamId: 1001);

        var result = await RunPubSubAsync(settings, TimeSpan.FromSeconds(60));

        result.Publish.PublishedMessages.Should().Be(10_000);
        result.Subscribe.ReceivedMessages.Should().Be(10_000);
    }
}
