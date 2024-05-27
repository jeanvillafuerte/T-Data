using BenchmarkDotNet.Attributes;
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

            Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
            Options |= ConfigOptions.JoinSummary;

            //AddJob(Job.Default.WithToolchain(CsProjClassicNetToolchain.Net48));
            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp80)));
            //AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(true).WithId("ServerForce"));
            //AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(false).WithId("Server"));
            //AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation"));
            //AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(false).WithId("WorkstationForce"));
        }

    }
}
