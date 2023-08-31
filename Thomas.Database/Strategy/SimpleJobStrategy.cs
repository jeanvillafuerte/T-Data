using System.Collections.Generic;

namespace Thomas.Database.Strategy
{
    using Thomas.Database.Cache.Metadata;

    public sealed class SimpleJobStrategy : JobStrategy
    {
        public SimpleJobStrategy(string cultureInfo, int processorCount, uint thresholdParallelism) : base(cultureInfo, processorCount, thresholdParallelism)
        {
        }

        public override IEnumerable<T> FormatData<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns)
        {
            var length = data.Length;

            T[] list = new T[length];
            
            for (int i = 0; i < length; i++)
            {
                T item = new T();

                for (int j = 0; j < columns.Length; j++)
                    props[columns[j]].SetValue(item, data[i][j], _cultureInfo);

                list[i] = item;
            }

            return list;
        }
    }
}
