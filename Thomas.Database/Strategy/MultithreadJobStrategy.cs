using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Thomas.Database.Strategy
{
    using Cache;

    public sealed class MultithreadJobStrategy : JobStrategy
    {
        public MultithreadJobStrategy(string cultureInfo, int processorCount) : base(cultureInfo, processorCount)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] FormatData<T>(Dictionary<string, InfoProperty> props, object[][] data, string[] columns, int length) where T : class
        {
            int pageSize = data.Length == 1 ? 1 : data.Length / _processorCount;

            if (pageSize == 1)
            {
                return new SimpleJobStrategy(_cultureInfo, 1).FormatData<T>(new Dictionary<string, InfoProperty>(props), data, columns, length);
            }

            int page = 0;

            var masterList = new System.Collections.Concurrent.ConcurrentDictionary<int, IReadOnlyDictionary<int, object[]>>();

            int mod = 0;

            for (int i = 0; i < _processorCount; i++)
            {
                if (i + 1 == _processorCount)
                {
                    mod = data.Length % _processorCount;
                }

                int counter = 0;

                var insideList = data.Skip(page * pageSize).Take(pageSize + mod).Select(x => new KeyValuePair<int, object[]>(counter++, x)).ToDictionary(s => (page * pageSize) + s.Key, v => v.Value);

                masterList[page] = new Dictionary<int, object[]>(insideList);
                page++;
            }

            var listResult = new System.Collections.Concurrent.ConcurrentDictionary<int, T>(_processorCount, data.Length);

            var concurrentProps = new System.Collections.Concurrent.ConcurrentDictionary<string, InfoProperty>(props);

            Parallel.For(0, _processorCount, (i) =>
            {
                var culture = new System.Globalization.CultureInfo(_cultureInfo);

                foreach (var element in masterList[i])
                {
                    T item = new T();

                    for (int j = 0; j < columns.Length; j++)
                    {
                        concurrentProps[columns[j]].Info.SetValue(item, Convert.ChangeType(element.Value[j], concurrentProps[columns[j]].Type), BindingFlags.Default, null, null, culture);
                    }

                    listResult[element.Key] = item;
                }

            });

            return listResult.Select(x => x.Value).ToArray();
        }


    }
}
