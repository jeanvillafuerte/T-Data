using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual object[][] ExtractData(System.Data.IDataReader reader, int columnCount)
        {
            object[] values = new object[columnCount];

            var list = new List<object[]>();

            while (reader.Read())
            {
                reader.GetValues(values);
                list.Add(values);
            }

            return list.ToArray();
        }

    }
}
