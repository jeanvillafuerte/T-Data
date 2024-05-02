using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    public interface IDbResulSet
    {
        T? ToSingle<T>(in string script, in object? parameters = null) where T : class, new();

        List<T> ToList<T>(in string script, in object? parameters = null) where T : class, new();

        Tuple<List<T1>, List<T2>> ToTuple<T1, T2>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new();

        Tuple<List<T1>, List<T2>, List<T3>> ToTuple<T1, T2, T3>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> ToTuple<T1, T2, T3, T4>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> ToTuple<T1, T2, T3, T4, T5>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new();
    }
}
