using Aeron.LowLatency.Core;
using Aeron.LowLatency.IntegrationTests.Testcontainers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aeron.LowLatency.IntegrationTests;

[Collection(AeronTestCollection.Name)]
public abstract class AeronIntegrationTestBase
{
    private readonly AeronContainerFixture _fixture;

    protected AeronIntegrationTestBase(AeronContainerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        Output = output;
    }

    protected ITestOutputHelper Output { get; }

    protected AeronSettings Settings(long messageCount, int streamId) =>
        new()
        {
            AeronDirectory = _fixture.AeronDirectory,
            Channel = AeronChannels.Ipc,
            StreamId = streamId,
            MessageCount = messageCount,
            BatchSize = 512
        };

    protected static async Task<(PublishResult Publish, SubscriberResult Subscribe)> RunPubSubAsync(AeronSettings settings, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var subscriber = new OrderSubscriber(settings, NullLogger<OrderSubscriber>.Instance);
        var publisher = new OrderPublisher(settings, NullLogger<OrderPublisher>.Instance);

        var subscribeTask = subscriber.ReceiveAsync(cts.Token).AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token).ConfigureAwait(false);
        var publish = await publisher.PublishAsync(cts.Token).ConfigureAwait(false);
        var subscribe = await subscribeTask.ConfigureAwait(false);
        return (publish, subscribe);
    }
}

[CollectionDefinition(Name)]
public sealed class AeronTestCollection : ICollectionFixture<AeronContainerFixture>
{
    public const string Name = "Aeron integration runtime";
}
