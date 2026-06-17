using Aeron.LowLatency.Contracts;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Aeron.LowLatency.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[GcServer(true)]
public class EncodingBenchmark
{
    private readonly byte[] _buffer = new byte[OrderMessageCodec.MaxEncodedLength];
    private readonly OrderMessage _message = new(42, "AERON", 101.2500m, 250, OrderSide.Buy, 1_700_000_000_000_000_000);

    [Benchmark]
    public int Encode() => OrderMessageCodec.Encode(_message, _buffer);

    [Benchmark]
    public OrderMessage Decode()
    {
        var length = OrderMessageCodec.Encode(_message, _buffer);
        return OrderMessageCodec.Decode(_buffer.AsSpan(0, length));
    }
}
