using System;
using System.Collections.Generic;
using System.Linq;

namespace Thomas.Cache.Helpers
{
    internal class ReflectionHelper
    {
        public static bool IsTuple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>))
                return true;

            return false;
        }

        public static Type[] GetTupleGenericArguments(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>))
                return type.GetGenericArguments().Select(x => GetIEnumerableElementType(x)).ToArray();

            return null;
        }

        public static bool IsIEnumerable(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return true;
            }

            return false;
        }

        public static Type GetIEnumerableElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type[] genericArguments = type.GetGenericArguments();

                if (genericArguments.Length == 1)
                    return genericArguments[0];
            }

            return null;
        }
    }
}
