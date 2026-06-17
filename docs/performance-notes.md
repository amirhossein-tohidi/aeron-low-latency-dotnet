# Performance Notes

## Binary Encoding

Orders are encoded into a compact binary payload. JSON is intentionally not used. The codec writes primitives in little-endian form and stores price as a scaled `long` to avoid text formatting overhead.

## Allocation Reduction

The hot publish path reuses a fixed byte buffer and wraps it in Aeron's `UnsafeBuffer`. The subscriber rents a buffer from `ArrayPool<byte>` while polling. The current codec still allocates for decoded `string Symbol`; using fixed symbol dictionaries or SBE would reduce that further.

## GC Impact

Lower allocation pressure reduces Gen0 churn and helps latency tails. BenchmarkDotNet memory diagnosers are enabled so Gen0/Gen1/Gen2 and allocated bytes are visible in reports.

## Throughput

Throughput depends on Media Driver placement, channel type, term buffer settings, CPU scheduling, and whether Docker is involved. Bare-metal Linux usually provides the cleanest numbers.

## Latency Percentiles

The subscriber records min, max, average, p50, p95, and p99. Tests assert that statistics are collected but avoid strict thresholds because CI hosts are noisy.

## Docker vs Bare-Metal Linux

Docker is excellent for reproducible setup and CI, but IPC/shared-memory behavior depends on bind mounts and host OS. For serious latency work, use bare-metal Linux, pin CPU cores, tune the kernel, and run the Media Driver and clients with predictable scheduling.
