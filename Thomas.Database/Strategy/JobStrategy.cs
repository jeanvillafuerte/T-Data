using System.Collections.Generic;
using System.Globalization;

namespace Thomas.Database.Strategy
{
    using Thomas.Database.Cache.Metadata;

    public abstract class JobStrategy
    {
        protected readonly CultureInfo _cultureInfo;
        protected readonly string _culture;
        protected readonly int _processorCount;
        protected readonly uint _thresholdParallelism;

        public JobStrategy(string culture, int processorCount, uint thresholdParallelism)
        {
            _culture = culture;
            _cultureInfo = new CultureInfo(culture);
            _processorCount = processorCount;
            _thresholdParallelism = thresholdParallelism;
        }

        public abstract IEnumerable<T> FormatData<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns) where T : class, new();
    }
}
