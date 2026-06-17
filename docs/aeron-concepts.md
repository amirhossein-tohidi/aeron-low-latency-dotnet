# Aeron Concepts

## Publication

A publication is the writing side of an Aeron stream. Applications call `Offer` with a direct buffer. A positive return value means the message was accepted into the stream.

## Subscription

A subscription is the reading side. Applications poll it and receive fragments through a handler.

## Backpressure

Backpressure happens when the publication cannot advance because subscribers, flow control, or buffers are not ready. The correct behavior is retry/backoff, not crashing.

## FragmentHandler

The `FragmentHandler` receives each message fragment from Aeron. This project keeps messages small enough to fit in one fragment, then decodes the binary order payload.

## Media Driver Lifecycle

The Media Driver should start before clients connect and should own a stable Aeron directory. Tests start and stop it through Testcontainers; local development can use Docker Compose or a manually started driver.
