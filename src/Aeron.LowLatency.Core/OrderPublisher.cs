using Adaptive.Aeron;
using Adaptive.Agrona.Concurrent;
using Aeron.LowLatency.Contracts;
using Microsoft.Extensions.Logging;

namespace Aeron.LowLatency.Core;

public sealed class OrderPublisher
{
    private readonly AeronSettings _settings;
    private readonly ILogger<OrderPublisher> _logger;

    public OrderPublisher(AeronSettings settings, ILogger<OrderPublisher> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async ValueTask<PublishResult> PublishAsync(CancellationToken cancellationToken)
    {
        using var activity = AeronMetrics.ActivitySource.StartActivity("publish-orders");
        using var aeron = AeronClientFactory.Connect(_settings, "order-publisher");
        using var publication = aeron.AddPublication(_settings.Channel, _settings.StreamId);

        await WaitForConnectionAsync(publication, cancellationToken).ConfigureAwait(false);
        if (_settings.WarmupCount > 0)
        {
            await PublishRangeAsync(publication, startOrderId: -_settings.WarmupCount, _settings.WarmupCount, countMetrics: false, cancellationToken).ConfigureAwait(false);
        }

        return await PublishRangeAsync(publication, startOrderId: 1, _settings.MessageCount, countMetrics: true, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<PublishResult> PublishRangeAsync(Publication publication, long startOrderId, long messageCount, bool countMetrics, CancellationToken cancellationToken)
    {
        var bytes = new byte[OrderMessageCodec.MaxEncodedLength];
        var directBuffer = new UnsafeBuffer(bytes);
        long published = 0;
        long backPressureEvents = 0;
        long notConnectedEvents = 0;
        long adminActionEvents = 0;

        for (var i = 0L; i < messageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var order = CreateOrder(startOrderId + i);
            var encodedLength = OrderMessageCodec.Encode(order, bytes);
            directBuffer.Wrap(bytes, 0, encodedLength);

            while (true)
            {
                var position = publication.Offer(directBuffer, 0, encodedLength, null);
                var result = ClassifyOffer(position);
                if (result == PublicationOfferResult.Success)
                {
                    published++;
                    if (countMetrics)
                    {
                        AeronMetrics.RecordPublished(1);
                    }

                    break;
                }

                switch (result)
                {
                    case PublicationOfferResult.BackPressured:
                        backPressureEvents++;
                        break;
                    case PublicationOfferResult.NotConnected:
                        notConnectedEvents++;
                        break;
                    case PublicationOfferResult.AdminAction:
                        adminActionEvents++;
                        break;
                    case PublicationOfferResult.Closed:
                    case PublicationOfferResult.MaxPositionExceeded:
                        throw new InvalidOperationException($"Publication offer failed permanently: {result}.");
                }

                await BackoffAsync(result, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation(
            "Published {PublishedMessages} messages to {Channel}/{StreamId}; backpressure={BackPressureEvents}, notConnected={NotConnectedEvents}, adminAction={AdminActionEvents}",
            published,
            _settings.Channel,
            _settings.StreamId,
            backPressureEvents,
            notConnectedEvents,
            adminActionEvents);

        return new PublishResult(published, backPressureEvents, notConnectedEvents, adminActionEvents);
    }

    private static OrderMessage CreateOrder(long orderId) =>
        new(orderId, "AERON", 100.25m + (orderId % 10), 100 + (int)(orderId % 50), orderId % 2 == 0 ? OrderSide.Buy : OrderSide.Sell, HighResolutionClock.GetUnixTimeNanoseconds());

    private static PublicationOfferResult ClassifyOffer(long position)
    {
        if (position > 0)
        {
            return PublicationOfferResult.Success;
        }

        if (position == Publication.BACK_PRESSURED)
        {
            return PublicationOfferResult.BackPressured;
        }

        if (position == Publication.NOT_CONNECTED)
        {
            return PublicationOfferResult.NotConnected;
        }

        if (position == Publication.ADMIN_ACTION)
        {
            return PublicationOfferResult.AdminAction;
        }

        if (position == Publication.CLOSED)
        {
            return PublicationOfferResult.Closed;
        }

        return PublicationOfferResult.MaxPositionExceeded;
    }

    private async ValueTask WaitForConnectionAsync(Publication publication, CancellationToken cancellationToken)
    {
        var startedAt = HighResolutionClock.GetTimestamp();
        while (!publication.IsConnected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (HighResolutionClock.ElapsedNanoseconds(startedAt) > _settings.ConnectionTimeout.TotalNanoseconds)
            {
                throw new TimeoutException($"Publication did not connect within {_settings.ConnectionTimeout}. Channel={_settings.Channel}, StreamId={_settings.StreamId}, AeronDirectory={_settings.AeronDirectory ?? "<default>"}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
        }
    }

    private static ValueTask BackoffAsync(PublicationOfferResult result, CancellationToken cancellationToken)
    {
        var delay = result == PublicationOfferResult.NotConnected ? TimeSpan.FromMilliseconds(5) : TimeSpan.FromTicks(1);
        return new ValueTask(Task.Delay(delay, cancellationToken));
    }
}
