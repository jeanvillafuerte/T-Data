using System.Collections.Generic;

namespace Thomas.Database.Strategy
{
    using Cache;

    public abstract class JobStrategy
    {
        protected readonly string _cultureInfo;
        protected readonly int _processorCount;

        public JobStrategy(string cultureInfo, int processorCount)
        {
            _cultureInfo = cultureInfo;
            _processorCount = processorCount;
        }

        public abstract T[] FormatData<T>(Dictionary<string, InfoProperty> props, object[][] data, string[] columns, int length) where T : class, new();

    }
}
