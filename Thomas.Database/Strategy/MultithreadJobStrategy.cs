using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Thomas.Database.Strategy
{
    using Thomas.Database.Cache.Metadata;

    public sealed class MultithreadJobStrategy : JobStrategy
    {
        public MultithreadJobStrategy(string culture, int processorCount) : base(culture, processorCount) { }

        public override IEnumerable<T> FormatData<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns)
        {
            if (data.Length == 1)
            {
                var dictionary = new Dictionary<string, MetadataPropertyInfo>(props);
                return new SimpleJobStrategy(_culture, 1).FormatData<T>(dictionary, data, columns);
            }

            int pageSize = data.Length < 100 ? 1 : data.Length / _processorCount;

            var main = new ConcurrentDictionary<int, (CultureInfo, object[][])>(1, _processorCount);

            int mod = 0;

            for (int i = 0; i < _processorCount; i++)
            {
                if (i + 1 == _processorCount)
                {
                    mod = data.Length % _processorCount;
                }

                main.TryAdd(i, ((CultureInfo)_cultureInfo.Clone(), data.Skip(i * pageSize).Take(pageSize + mod).Select(x => x).ToArray()));
            }

            var listResult = new ConcurrentDictionary<int, T[]>(_processorCount, data.Length);

            Parallel.For(0, _processorCount, (i) =>
            {
                if (main.TryGetValue(i, out var tuple))
                {
                    var data = tuple.Item2;
                    var cultureInfo = tuple.Item1;

                    var length = data.Length;
                    var list = new T[length];
                    var index = 0;

                    for (int j = 0; j < length; j++)
                    {
                        T item = new T();

                        for (int k = 0; k < columns.Length; k++)
                            props[columns[k]].SetValue(item, data[j][k], _cultureInfo);

                        list[index++] = item;
                    }

                    listResult.TryAdd(i, list);
                }
            });

            return listResult.OrderBy(pair => pair.Key).SelectMany(x => x.Value);
        }
    }
}
