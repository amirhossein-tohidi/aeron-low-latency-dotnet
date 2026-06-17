using System.Globalization;
using Aeron.LowLatency.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var settings = ArgsParser.Parse(args);
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(settings);
        services.AddSingleton<OrderPublisher>();
        services.AddOpenTelemetry()
            .WithMetrics(builder => builder.AddMeter(AeronMetrics.MeterName).AddConsoleExporter())
            .WithTracing(builder => builder.AddSource(AeronMetrics.ActivitySource.Name).AddConsoleExporter());
    })
    .Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

var publisher = host.Services.GetRequiredService<OrderPublisher>();
await publisher.PublishAsync(cts.Token).ConfigureAwait(false);

internal static class ArgsParser
{
    public static AeronSettings Parse(string[] args)
    {
        var settings = new AeronSettings();
        for (var i = 0; i < args.Length; i++)
        {
            var value = i + 1 < args.Length ? args[i + 1] : string.Empty;
            settings = args[i] switch
            {
                "--message-count" => settings with { MessageCount = long.Parse(value, CultureInfo.InvariantCulture) },
                "--batch-size" => settings with { BatchSize = int.Parse(value, CultureInfo.InvariantCulture) },
                "--channel" => settings with { Channel = value },
                "--stream-id" => settings with { StreamId = int.Parse(value, CultureInfo.InvariantCulture) },
                "--warmup-count" => settings with { WarmupCount = int.Parse(value, CultureInfo.InvariantCulture) },
                "--aeron-dir" => settings with { AeronDirectory = value },
                _ => settings
            };

            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                i++;
            }
        }

        return settings;
    }
}
