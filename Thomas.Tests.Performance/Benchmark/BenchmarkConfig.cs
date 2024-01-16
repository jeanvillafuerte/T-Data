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
using Thomas.Tests.Performance.Column;

namespace Thomas.Tests.Performance.Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public const int Iterations = 50;

        public BenchmarkConfig()
        {
            AddLogger(ConsoleLogger.Default);

            AddExporter(CsvExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(HtmlExporter.Default);

            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumnProvider(DefaultColumnProviders.Metrics);

            AddColumn(DetailedRuntimeColumn.Default);
            AddColumn(GcModeColumn.Default);
            AddColumn(TargetMethodColumn.Type);
            AddColumn(TargetMethodColumn.Method);
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Error);
            AddColumn(StatisticColumn.OperationsPerSecond);

            AddJob(Job.ShortRun
                   .WithLaunchCount(1)
                   .WithWarmupCount(1)
                   .WithUnrollFactor(Iterations)
                   .WithIterationCount(1));

            Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
            Options |= ConfigOptions.JoinSummary;

            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp31)));
            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp60)));
            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp80)));
            AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(true).WithId("ServerForce"));
            AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(false).WithId("Server"));
            AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation"));
            AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(false).WithId("WorkstationForce"));
        }

    }
}
