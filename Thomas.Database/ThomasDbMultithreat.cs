using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thomas.Database
{
    public abstract partial class ThomasDbBase
    {
        protected T[] FormatDataWithoutNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                           ConcurrentDictionary<string, PropertyInfo> properties,
                                           string[] columns, int length) where T : new()
        {
            int processors = GetMaxDegreeOfParallelism();

            int pageSize = data.Count == 1 ? 1 : data.Count / processors;

            if (pageSize == 1 || processors <= 1)
            {
                var dataArray = data.Select(s => s.Value).ToArray();
                var props = properties.ToDictionary(x => x.Key, y => y.Value);

                return FormatDataWithoutNullables<T>(dataArray, props, columns, length);
            }

            int page = 1;
            int localLen = length;

            ConcurrentDictionary<int, Dictionary<int, object[]>> masterList = new ConcurrentDictionary<int, Dictionary<int, object[]>>();

            int mod = 0;

            for (int i = 0; i < processors; i++)
            {
                if (i + 1 == processors)
                {
                    mod = data.Count % processors;
                }

                var insideList = data.Skip((page - 1) * pageSize).Take(pageSize + mod);

                masterList[page - 1] = new Dictionary<int, object[]>(insideList);
                page++;
            }

            ConcurrentDictionary<int, T> listResult = new ConcurrentDictionary<int, T>(processors, data.Count);

            Parallel.For(0, processors, (i) =>
            {
                var splitData = masterList[i];

                CultureInfo culture = new CultureInfo(CultureInfo);

                foreach (var item in GetItemsForParallel(splitData, length, properties, columns, culture))
                {
                    listResult[item.Item2] = item.Item1;
                }
            });

            return listResult.Select(x => x.Value).ToArray();

            IEnumerable<(T, int)> GetItemsForParallel(Dictionary<int, object[]> data,
                                           int length,
                                           IDictionary<string, PropertyInfo> properties,
                                           string[] columns,
                                           CultureInfo culture)
            {
                foreach (var d in data)
                {
                    T item = new();
                    yield return (GetItemWithoutNullables<T>(item, length, properties, columns, d.Value, culture), d.Key);
                }

            }
        }

        protected T[] FormatDataWithNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                      ConcurrentDictionary<string, PropertyInfo> properties,
                                      string[] columns, int length) where T : new()
        {
            int processors = GetMaxDegreeOfParallelism();

            int pageSize = data.Count == 1 ? 1 : data.Count / processors;

            if (pageSize == 1 || processors <= 1)
            {
                var dataArray = data.Select(s => s.Value).ToArray();
                var props = properties.ToDictionary(x => x.Key, y => y.Value);

                return FormatDataWithNullables<T>(dataArray, props, columns, length);
            }

            int page = 1;

            ConcurrentDictionary<int, Dictionary<int, object[]>> masterList = new ConcurrentDictionary<int, Dictionary<int, object[]>>();

            int mod = 0;

            for (int i = 0; i < processors; i++)
            {
                if (i + 1 == processors)
                {
                    mod = data.Count % processors;
                }

                var insideList = data.Skip((page - 1) * pageSize).Take(pageSize + mod);

                masterList[page - 1] = new Dictionary<int, object[]>(insideList);
                page++;
            }

            IDictionary<int, T> listResult = new ConcurrentDictionary<int, T>(processors, data.Count);

            Parallel.For(0, processors, (i) =>
            {
                var splitData = masterList[i];

                CultureInfo culture = new CultureInfo(CultureInfo);

                foreach (var item in GetItemsWithNullablesForParallel(splitData, length, properties, columns, culture))
                {
                    listResult[item.Item2] = item.Item1;
                }
            });

            return listResult.Select(x => x.Value).ToArray();

            IEnumerable<(T, int)> GetItemsWithNullablesForParallel(IDictionary<int, object[]> data,
                                  int length,
                                  IDictionary<string, PropertyInfo> properties,
                                  string[] columns,
                                  CultureInfo culture)
            {
                foreach (var d in data)
                {
                    T item = new();
                    yield return (GetItemWithNullables<T>(item, length, properties, columns, d.Value, culture), d.Key);
                }

            }
        }

    }
}
