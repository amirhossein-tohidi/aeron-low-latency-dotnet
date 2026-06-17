# Architecture

This repository is intentionally narrow: it teaches Aeron by publishing and consuming real order messages through Aeron, then validating behavior with integration tests and benchmarks.

## Media Driver

The Media Driver is the Aeron runtime process that owns transport, shared memory buffers, flow control, and liveness. .NET clients connect to it through the Aeron CnC directory. Locally this is usually the default Aeron directory; in Docker and Testcontainers the repository uses an explicit `aeron.dir` bind mount.

## Publication

`OrderPublisher` creates a `Publication` for a channel and stream id. It encodes `OrderMessage` as compact binary and calls `Publication.Offer`. The offer result is handled explicitly for success, back pressure, not connected, admin action, closed, and max position exceeded.

## Subscription

`OrderSubscriber` creates a `Subscription` for the same channel and stream id. It polls fragments with a `FragmentHandler`, decodes the binary order payload, validates ordering, and records latency and throughput.

## Channel

The default channel is `aeron:ipc`, which uses the Media Driver and shared memory on the same machine. UDP is supported with `aeron:udp?endpoint=localhost:40123` for network transport experiments.

## StreamId

The default stream id is `1001`. Aeron matches publications and subscriptions by channel and stream id. Tests use separate stream ids to avoid cross-test contamination.

## IPC Transport

IPC is the preferred learning path here because it exposes Aeron's low-latency shared-memory behavior without involving network tuning first.

## UDP Transport

UDP is available through the CLI `--channel` option. It is useful when validating transport, MTU, socket buffer, and host networking behavior.
