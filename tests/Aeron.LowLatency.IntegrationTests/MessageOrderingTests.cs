using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class MessageOrderingTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ReceivesSequentialOrderIdsInOrder()
    {
        var settings = Settings(messageCount: 100_000, streamId: 1003);

        var result = await RunPubSubAsync(settings, TimeSpan.FromMinutes(2));

        result.Subscribe.ReceivedMessages.Should().Be(100_000);
        result.Subscribe.IsOrderingValid.Should().BeTrue();
    }
}
