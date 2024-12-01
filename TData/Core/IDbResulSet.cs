using System;
using System.Collections.Generic;
using System.Threading;
using TData.DbResult;

namespace TData.Core
{
    /// <summary>
    /// Represents a result set interface for fetching data from a database.
    /// </summary>
    public interface IDbResultSet
    {
        /// <summary>
        /// Fetches a single result of type <typeparamref name="T"/> from the database.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A single result of type <typeparamref name="T"/>.</returns>
        T FetchOne<T>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a list of results of type <typeparamref name="T"/> from the database.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A list of results of type <typeparamref name="T"/>.</returns>
        List<T> FetchList<T>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of two lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing two lists of results.</returns>
        Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of three lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing three lists of results.</returns>
        Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of four lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing four lists of results.</returns>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of five lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing five lists of results.</returns>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of six lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing six lists of results.</returns>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null);

        /// <summary>
        /// Fetches a tuple of seven lists of results from the database.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing seven lists of results.</returns>
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null);

        IEnumerable<List<T>> FetchPagedList<T>(string script, int offset, int pageSize, object parameters = null);
        IEnumerable<List<TDataRow>> FetchPagedRows(in string script,in int offset, in int pageSize, in object parameters = null);
        IAsyncEnumerable<List<T>> FetchPagedListAsync<T>(string script, int offset, int pageSize, object parameters, CancellationToken cancellationToken);
    }
}