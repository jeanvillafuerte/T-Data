using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Thomas.Cache.MemoryCache
{
    public interface IDbResulCachedSet
    {
        /// <summary>
        /// Get the cached result of the query of a single object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        T? ToSingle<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new();

        T? ToSingle<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new();

        IEnumerable<T> ToList<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new();

        /// <summary>
        /// Get the cached result of the query of a result set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        IEnumerable<T> ToList<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        /// <summary>
        /// Get the cached result of the query of multiple result sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script">Sql script or store procedure name</param>
        /// <param name="parameters">required parameters to execute query</param>
        /// <param name="refresh">force call database and update cached value</param>
        /// <param name="key">custom key to store in cache</param>
        /// <returns></returns>
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new();
    }
}
