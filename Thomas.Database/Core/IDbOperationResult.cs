using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    /// <summary>
    /// Interface representing the result of a database operation.
    /// </summary>
    public interface IDbOperationResult
    {
        /// <summary>
        /// Tries to execute a script.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult TryExecute(in string script, in object parameters = null);

        /// <summary>
        /// Tries to execute a scalar.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<T> TryExecuteScalar<T>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a single result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<T> TryFetchOne<T>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a list of results.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<List<T>> TryFetchList<T>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of two lists of results.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>>> TryFetchTuple<T1, T2>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of three lists of results.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> TryFetchTuple<T1, T2, T3>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of four lists of results.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> TryFetchTuple<T1, T2, T3, T4>(in string script, in object parameters);

        /// <summary>
        /// Tries to fetch a tuple of five lists of results.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> TryFetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of six lists of results.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> TryFetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of seven lists of results with the given parameters.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="Thomas.Database.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The result of the operation.</returns>
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> TryFetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null);
    }
}
