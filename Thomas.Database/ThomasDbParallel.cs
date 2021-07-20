using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Thomas.Database.Cache;

namespace Thomas.Database
{
    public abstract partial class ThomasDbBase
    {
        internal T[] FormatDataWithoutNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                           ConcurrentDictionary<string, InfoProperty> properties,
                                           string[] columns, int length, int processors) where T : new()
        {
            int pageSize = data.Count == 1 ? 1 : data.Count / processors;

            if (pageSize == 1 || processors <= 1)
            {
                var dataArray = data.Select(s => s.Value).ToArray();
                return FormatDataWithoutNullables<T>(dataArray, properties, columns, length);
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
                                           IDictionary<string, InfoProperty> properties,
                                           string[] columns,
                                           CultureInfo culture)
            {
                foreach (var d in data)
                {
                    T item = new T();
                    yield return (GetItemWithoutNullables<T>(item, length, properties, columns, d.Value, culture), d.Key);
                }

            }
        }

        internal T[] FormatDataWithNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                      ConcurrentDictionary<string, InfoProperty> properties,
                                      string[] columns, int length, int processors) where T : new()
        {

            int pageSize = data.Count == 1 ? 1 : data.Count / processors;

            if (pageSize == 1 || processors <= 1)
            {
                var dataArray = data.Select(s => s.Value).ToArray();
                return FormatDataWithNullables<T>(dataArray, properties, columns, length);
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

            ConcurrentDictionary<int, T> listResult = new ConcurrentDictionary<int, T>(processors, data.Count);

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
                                  IDictionary<string, InfoProperty> properties,
                                  string[] columns,
                                  CultureInfo culture)
            {
                foreach (var d in data)
                {
                    T item = new T();
                    yield return (GetItemWithNullables<T>(item, length, properties, columns, d.Value, culture), d.Key);
                }

            }
        }

    }
}
