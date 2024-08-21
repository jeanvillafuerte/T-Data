using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
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

            // Ensure the directory exists
            if (!Directory.Exists(Program.BenchmarkResultsPath))
            {
                Directory.CreateDirectory(Program.BenchmarkResultsPath);
            }
          
            ArtifactsPath = Program.BenchmarkResultsPath;

            // Add the JSON exporter
            AddExporter(JsonExporter.FullCompressed);
        }

    }
}
