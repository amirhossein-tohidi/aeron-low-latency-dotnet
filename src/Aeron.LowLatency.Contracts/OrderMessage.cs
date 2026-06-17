namespace Aeron.LowLatency.Contracts;

public readonly record struct OrderMessage(
    long OrderId,
    string Symbol,
    decimal Price,
    int Quantity,
    OrderSide Side,
    long CreatedAtUnixNano);
