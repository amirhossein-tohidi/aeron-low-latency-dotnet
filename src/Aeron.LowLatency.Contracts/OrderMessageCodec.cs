using System.Buffers.Binary;
using System.Text;

namespace Aeron.LowLatency.Contracts;

public static class OrderMessageCodec
{
    public const int MaxSymbolBytes = 32;
    public const int MaxEncodedLength = 8 + 8 + 4 + 1 + 8 + 1 + MaxSymbolBytes;
    private const decimal PriceScale = 10_000m;

    public static int Encode(in OrderMessage message, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, MaxEncodedLength);

        var symbolLength = Encoding.ASCII.GetByteCount(message.Symbol);
        if (symbolLength > MaxSymbolBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(message), $"Symbol must fit in {MaxSymbolBytes} ASCII bytes.");
        }

        var offset = 0;
        BinaryPrimitives.WriteInt64LittleEndian(destination[offset..], message.OrderId);
        offset += sizeof(long);
        BinaryPrimitives.WriteInt64LittleEndian(destination[offset..], decimal.ToInt64(decimal.Round(message.Price * PriceScale, 0)));
        offset += sizeof(long);
        BinaryPrimitives.WriteInt32LittleEndian(destination[offset..], message.Quantity);
        offset += sizeof(int);
        destination[offset++] = (byte)message.Side;
        BinaryPrimitives.WriteInt64LittleEndian(destination[offset..], message.CreatedAtUnixNano);
        offset += sizeof(long);
        destination[offset++] = (byte)symbolLength;
        Encoding.ASCII.GetBytes(message.Symbol, destination.Slice(offset, symbolLength));
        offset += symbolLength;

        return offset;
    }

    public static OrderMessage Decode(ReadOnlySpan<byte> source)
    {
        var minimumLength = MaxEncodedLength - MaxSymbolBytes;
        ArgumentOutOfRangeException.ThrowIfLessThan(source.Length, minimumLength);

        var offset = 0;
        var orderId = BinaryPrimitives.ReadInt64LittleEndian(source[offset..]);
        offset += sizeof(long);
        var scaledPrice = BinaryPrimitives.ReadInt64LittleEndian(source[offset..]);
        offset += sizeof(long);
        var quantity = BinaryPrimitives.ReadInt32LittleEndian(source[offset..]);
        offset += sizeof(int);
        var side = (OrderSide)source[offset++];
        var createdAtUnixNano = BinaryPrimitives.ReadInt64LittleEndian(source[offset..]);
        offset += sizeof(long);
        var symbolLength = source[offset++];

        if (symbolLength > MaxSymbolBytes || source.Length < offset + symbolLength)
        {
            throw new FormatException("Invalid encoded symbol length.");
        }

        var symbol = Encoding.ASCII.GetString(source.Slice(offset, symbolLength));
        return new OrderMessage(orderId, symbol, scaledPrice / PriceScale, quantity, side, createdAtUnixNano);
    }
}
