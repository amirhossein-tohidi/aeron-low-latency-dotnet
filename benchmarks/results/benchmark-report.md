# Benchmark Report

Run benchmarks locally with:

```powershell
dotnet run -c Release --project benchmarks/Aeron.LowLatency.Benchmarks -- --artifacts benchmarks/results
```

For Aeron publishing and end-to-end latency benchmarks, start a media driver first and set `AERON_DIR` when using a non-default directory.

BenchmarkDotNet writes per-run markdown, CSV, and HTML artifacts under this folder. The report is intentionally not generated in CI because the numbers are hardware, OS, Docker, and CPU-governor sensitive.
