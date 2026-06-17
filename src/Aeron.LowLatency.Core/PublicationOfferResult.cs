namespace Aeron.LowLatency.Core;

public enum PublicationOfferResult
{
    Success,
    BackPressured,
    NotConnected,
    AdminAction,
    Closed,
    MaxPositionExceeded
}

public readonly record struct PublishResult(
    long PublishedMessages,
    long BackPressureEvents,
    long NotConnectedEvents,
    long AdminActionEvents);
