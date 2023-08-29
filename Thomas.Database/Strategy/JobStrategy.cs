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

        public JobStrategy(string culture, int processorCount)
        {
            _culture = culture;
            _cultureInfo = new CultureInfo(culture);
            _processorCount = processorCount;
        }

        public abstract IEnumerable<T> FormatData<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns) where T : class, new();
    }
}
