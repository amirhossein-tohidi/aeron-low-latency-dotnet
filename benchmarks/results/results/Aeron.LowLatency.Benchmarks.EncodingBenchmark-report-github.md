```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5549/23H2/2023Update/SunValley3)
Intel Core i7-6700HQ CPU 2.60GHz (Max: 2.59GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  Job-UFXZUX : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Server=True  

```
| Method | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev   | Median    | P95       | Gen0   | Allocated |
|------- |----------- |--------------- |------------ |------------ |----------:|----------:|---------:|----------:|----------:|-------:|----------:|
| Encode | Job-UFXZUX | Default        | Default     | Default     |  48.07 ns |  1.003 ns | 2.661 ns |  47.49 ns |  52.01 ns |      - |         - |
| Decode | Job-UFXZUX | Default        | Default     | Default     | 112.95 ns |  2.314 ns | 6.411 ns | 112.10 ns | 122.82 ns | 0.0015 |      32 B |
| Encode | ShortRun   | 3              | 1           | 3           |  54.60 ns | 84.049 ns | 4.607 ns |  52.53 ns |  59.15 ns |      - |         - |
| Decode | ShortRun   | 3              | 1           | 3           | 125.67 ns | 94.433 ns | 5.176 ns | 122.72 ns | 130.76 ns | 0.0014 |      32 B |
