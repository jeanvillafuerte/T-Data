using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Linq;

namespace Thomas.Tests.Performance.Column
{
    public class DetailedRuntimeColumn : IColumn
    {
        public static DetailedRuntimeColumn Default => new DetailedRuntimeColumn();

        public string Id => nameof(DetailedRuntimeColumn);
        public string ColumnName => "Detailed Runtime";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var report = summary.Reports.Single(r => r.BenchmarkCase == benchmarkCase);
            var runtimeInfo = report.GetRuntimeInfo();
            var splitIndex = runtimeInfo.IndexOf(',');
            return runtimeInfo.Substring(0, splitIndex);
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true; //false?
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Detailed runtime";
    }
}
