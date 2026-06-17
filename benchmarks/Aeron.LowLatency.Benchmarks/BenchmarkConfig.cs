using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace Aeron.LowLatency.Benchmarks;

public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(StatisticColumn.Mean, StatisticColumn.Median, StatisticColumn.P95);
        ArtifactsPath = Path.Combine("benchmarks", "results");
    }
}
