using System;
using System.Collections.Generic;
using System.Linq;

namespace Thomas.Cache.Helpers
{
    internal class ReflectionHelper
    {
        public static Type[] GetTupleGenericArguments(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericArguments().Select(x => GetIEnumerableElementType(x)).ToArray();

            return null;
        }

        public static Type GetIEnumerableElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type[] genericArguments = type.GetGenericArguments();

                if (genericArguments.Length == 1)
                    return genericArguments[0];
            }

            return null;
        }
    }
}
