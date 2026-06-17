using Aeron.LowLatency.Core;
using Aeron.LowLatency.IntegrationTests.Testcontainers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aeron.LowLatency.IntegrationTests;

public sealed class BackpressureHandlingTests(AeronContainerFixture fixture, ITestOutputHelper output) : AeronIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task PublisherRetriesAndDoesNotCrashWhenSubscriberStartsLate()
    {
        var settings = Settings(messageCount: 25_000, streamId: 1005);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var publisher = new OrderPublisher(settings, NullLogger<OrderPublisher>.Instance);
        var subscriber = new OrderSubscriber(settings, NullLogger<OrderSubscriber>.Instance);

        var publishTask = publisher.PublishAsync(cts.Token).AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(750), cts.Token);
        var subscribeTask = subscriber.ReceiveAsync(cts.Token).AsTask();

        var publish = await publishTask;
        var subscribe = await subscribeTask;

        publish.PublishedMessages.Should().Be(25_000);
        publish.BackPressureEvents.Should().BeGreaterThanOrEqualTo(0);
        publish.NotConnectedEvents.Should().BeGreaterThanOrEqualTo(0);
        publish.AdminActionEvents.Should().BeGreaterThanOrEqualTo(0);
        subscribe.ReceivedMessages.Should().Be(25_000);
    }
}
