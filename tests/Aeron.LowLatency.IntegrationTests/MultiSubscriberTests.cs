using Aeron.LowLatency.Core;
using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class MultiSubscriberTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task IpcPublicationIsMulticastToIndependentSubscribers()
    {
        var settings = Settings(messageCount: 10_000, streamId: 1006);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var subscriberOne = new OrderSubscriber(settings, NullLogger<OrderSubscriber>.Instance);
        var subscriberTwo = new OrderSubscriber(settings, NullLogger<OrderSubscriber>.Instance);
        var publisher = new OrderPublisher(settings, NullLogger<OrderPublisher>.Instance);

        var oneTask = subscriberOne.ReceiveAsync(cts.Token).AsTask();
        var twoTask = subscriberTwo.ReceiveAsync(cts.Token).AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token);
        await publisher.PublishAsync(cts.Token);

        var results = await Task.WhenAll(oneTask, twoTask);

        Output.WriteLine("Aeron IPC semantics: each matching subscription receives the publication stream independently.");
        results[0].ReceivedMessages.Should().Be(10_000);
        results[1].ReceivedMessages.Should().Be(10_000);
    }
}
