# Test Strategy

## Integration Tests

Integration tests start a real Aeron Media Driver through Testcontainers and run the production publisher and subscriber code against it.

## Load Tests

Heavy tests are marked with `[Trait("Category", "LoadTests")]`. CI excludes them by default. Run them manually when Docker and the host machine are ready for sustained traffic.

## Benchmark Tests

Benchmarks are not CI tests. They are local performance measurements produced by BenchmarkDotNet under `benchmarks/results`.

## Aeron Runtime Setup

The fixture uses a real Java Aeron Media Driver.

On Linux, it starts an `eclipse-temurin:21-jre` container through Testcontainers by default, mounts an explicit Aeron directory, and waits for `cnc.dat`.

GitHub Actions sets `AERON_USE_LOCAL_DRIVER=true` and uses `actions/setup-java` with Temurin 21. This avoids a Linux bind-mount permission problem where a root-owned container-created `cnc.dat` cannot be opened by the .NET test process on the host.

On Windows, it starts a local Java process instead. This is intentional: Aeron IPC relies on host-local memory-mapped files and driver heartbeat updates. Docker Desktop Linux bind mounts can create `cnc.dat`, but the .NET client on Windows may still fail with `no driver heartbeat detected`.

If Windows only has Java 8, the fixture downloads a portable Temurin JRE 21 into the user-local cache and uses it for the Media Driver.

## Why Strict Latency Assertions Are Avoided

Latency depends on CPU load, virtualization, Docker, OS scheduling, and clock behavior. The tests verify that latency is measured and percentiles are sane, but they do not pretend CI can enforce production latency SLOs.

## Known Runtime Limitation

On Windows or Docker Desktop, Aeron IPC through a bind-mounted CnC directory can be less reliable than native Linux. The tests therefore prefer a local Windows Media Driver process on Windows, Testcontainers on Linux local runs, and a local Java 21 Media Driver in GitHub Actions.
