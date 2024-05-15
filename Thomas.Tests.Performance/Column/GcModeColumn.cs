using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Thomas.Tests.Performance.Column
{
    public class GcModeColumn : IColumn
    {
        public static GcModeColumn Default => new();

        public string Id => "GcMode";
        public string ColumnName => "GcMode";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            // Using the Job's Id to display GC mode information
            return benchmarkCase.Job.Id;
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public string Legend => "GC Mode";

        public bool IsNumeric => false;

        public UnitType UnitType => UnitType.Dimensionless;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
    }
}
