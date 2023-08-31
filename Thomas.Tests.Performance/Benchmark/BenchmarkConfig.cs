using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace Thomas.Tests.Performance.Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public const int Iterations = 25;

        public BenchmarkConfig()
        {
            AddLogger(ConsoleLogger.Default);

            AddExporter(CsvExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(HtmlExporter.Default);

            var md = MemoryDiagnoser.Default;
            AddDiagnoser(md);
            AddColumn(TargetMethodColumn.Namespace);
            AddColumn(TargetMethodColumn.Type);
            AddColumn(TargetMethodColumn.Method);
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Error);
            AddColumn(BaselineRatioColumn.RatioMean);
            AddColumn(StatisticColumn.OperationsPerSecond);
            AddColumnProvider(DefaultColumnProviders.Metrics);

            AddJob(Job.ShortRun
                   .WithLaunchCount(1)
                   .WithWarmupCount(1)
                   .WithUnrollFactor(Iterations));

            Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
            Options |= ConfigOptions.JoinSummary;

            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp31)));
            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp60)));
        }

    }
}
