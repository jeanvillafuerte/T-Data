using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Thomas.Database.Strategy
{
    using Cache;

    public sealed class SimpleJobStrategy : JobStrategy
    {
        public SimpleJobStrategy(string cultureInfo, int processorCount) : base(cultureInfo, processorCount)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] FormatData<T>(Dictionary<string, InfoProperty> props, object[][] data, string[] columns, int length) where T : class
        {
            T[] list = new T[data.Length];

            var culture = new System.Globalization.CultureInfo(_cultureInfo);

            for (int i = 0; i < length; i++)
            {
                T item = new T();

                for (int j = 0; j < columns.Length; j++)
                {
                    props[columns[j]].Info.SetValue(item, Convert.ChangeType(data[i][j], props[columns[j]].Type), BindingFlags.Default, null, null, culture);
                }

                list[i] = item;
            }

            return list;
        }
    }
}
