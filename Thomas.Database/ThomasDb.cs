using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Thomas.Database
{
    public class ThomasDb : ThomasDbBase, IThomasDb
    {
        public ThomasDb(IDatabaseProvider provider, ThomasDbStrategyOptions options)
        {
            Provider = provider;
            StringConnection = options.StringConnection;
            User = options.User;
            Password = options.Password;
            StrictMode = options.StrictMode;
            SensitiveDataLog = options.SensitiveDataLog;
            DetailErrorMessage = options.DetailErrorMessage;
            CultureInfo = options.Culture;
            Convention = options.TypeMatchConvention;
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        }

        /// <summary>
        /// Transform database result set to typed list maintaining original order
        /// </summary>
        /// <typeparam name="T">type of result</typeparam>
        /// <param name="reader">DataReader to transform</param>
        /// <param name="script">script or store procedure name</param>
        /// <param name="closeReader">flag to close reader after read. Default : false</param>
        public IReadOnlyList<T> DataReaderToList<T>(IDataReader reader, string script, bool closeReader = false) where T : new()
        {
            var props = typeof(T).GetProperties();

            var culture = new CultureInfo(CultureInfo);

            var colsDb = GetColumns(reader);

            string[] columns = new string[colsDb.Length];

            for (int i = 0; i < colsDb.Length; i++)
            {
                columns[i] = SanitizeName(colsDb[i]);
            }

            if (StrictMode)
            {
                string[] propsName = new string[props.Length];

                for (int i = 0; i < props.Length; i++)
                {
                    propsName[i] = props[i].Name;
                }

                var noMatchProperties = columns.Where(x => !propsName.Contains(x)).ToArray();

                if (noMatchProperties.Length > 0)
                {
                    throw new NoMatchPropertiesException("There are columns doesn't match with entity's fields. " +
                      "Columns : " + string.Join(", ", noMatchProperties) + Environment.NewLine +
                      "Entity Name : " + typeof(T).FullName + Environment.NewLine +
                      "Script : " + script);
                }
            }

            var matchProperties = props.Where(x => columns.Contains(x.Name)).ToArray();

            var safeList = new ConcurrentDictionary<string, PropertyInfo>();

            for (int u = 0; u < matchProperties.Length; u++)
            {
                safeList.TryAdd(matchProperties[u].Name, matchProperties[u]);
            }

            var containNullables = CheckContainNullables(typeof(T));

            T[] result;

            if (GetMaxDegreeOfParallelism() > 1)
            {
                ConcurrentDictionary<int, object[]> data2 = ExtractData2(reader, columns.Length);

                if (containNullables)
                {
                    result = FormatDataWithNullablesParallel<T>(data2, safeList, columns, safeList.Count);
                }
                else
                {
                    result = FormatDataWithoutNullablesParallel<T>(data2, safeList, columns, safeList.Count);
                }
            }
            else
            {
                object[][] data = ExtractData(reader, columns.Length);

                var dList = safeList.ToDictionary(s => s.Key, x => x.Value);

                if (containNullables)
                {
                    result = FormatDataWithNullables<T>(data, dList, columns, safeList.Count);
                }
                else
                {
                    result = FormatDataWithoutNullables<T>(data, dList, columns, safeList.Count);
                }
            }

            if (closeReader)
            {
                reader.Kill();
            }

            GC.Collect();

            return result.ToList();
        }

        #region without result data
        /// <summary>
        /// Execute SQL script without result set
        /// </summary>
        /// <param name="script">string script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult Execute(string script, bool isStoreProcedure = true)
        {
            var response = new DataBaseOperationResult() { Success = true };
            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult.ErrorResult(msg);
            }
            finally
            {
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Execute store procedure with transacction
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        //public async Task<DataBaseOperationResult> ExecuteAsync(ISearchTerm searchTerm, string procedureName)
        //{
        //    var response = new DataBaseOperationResult();

        //    var command = provider.CreateCommand(StringConnection);
        //    command.CommandText = procedureName;
        //    command.CommandType = CommandType.StoredProcedure;

        //    IDataParameter[] parameters = new List<IDataParameter>().ToArray();

        //    if (searchTerm != null)
        //    {
        //        parameters = provider.ExtractValuesFromSearchTerm(searchTerm);
        //    }

        //    command.Parameters.AddRange(parameters);

        //    DbTransaction transaction = null;

        //    try
        //    {
        //        transaction = await command.Connection.BeginTransactionAsync();

        //        await command.ExecuteNonQueryAsync();

        //        var outParameters = parameters.Where(s => s.Direction == ParameterDirection.Output).ToList();

        //        if (outParameters.Any())
        //        {
        //            foreach (var outParameter in outParameters)
        //            {
        //                response.OutParameters.Add(outParameter.ParameterName, outParameter.Value);
        //            }
        //        }

        //        await transaction.CommitAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        var msg = ErrorDetailMessage(procedureName, parameters, ex);
        //        await transaction.RollbackAsync();
        //        response = DataBaseOperationResult.ErrorResult(msg);
        //    }
        //    finally
        //    {

        //        if (command.Connection != null && command.Connection.State == ConnectionState.Open)
        //        {
        //            await command.Connection.CloseAsync();
        //            await command.Connection.DisposeAsync();
        //        }

        //        await command.DisposeAsync();
        //    }

        //    return response;
        //}
        #endregion

        #region single row result

        /// <summary>
        /// Return single result row from execute SQL script
        /// </summary>
        /// <typeparam name="T">typed class</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<T> ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DataBaseOperationResult<T>() { Success = true };

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleRow);
                response.Result = DataReaderToList<T>(reader, script, true)?.First();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<T>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return single result row from execute SQL script
        /// </summary>
        /// <typeparam name="T">typed class</typeparam>
        /// <param name="searchTerm">Match parameters from object field names with store procedure</param>
        /// <param name="procedureName">full name store procedure</param>
        public DataBaseOperationResult<T> ToSingle<T>(object searchTerm, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<T>() { Success = true };

            var (command, parameters) = PreProcessing(procedureName, true, searchTerm);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleRow);
                response.Result = DataReaderToList<T>(reader, procedureName, true)?.First();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DataBaseOperationResult<T>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        #endregion

        #region one result set
        /// <summary>
        /// Return typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T">typed class</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<IReadOnlyList<T>> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DataBaseOperationResult<IReadOnlyList<T>>() { Success = true };

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleResult);
                response.Result = DataReaderToList<T>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<IReadOnlyList<T>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T">typed class</typeparam>
        /// <param name="searchTerm">Match parameters from object field names with store procedure</param>
        /// <param name="procedureName">full name store procedure</param>
        public DataBaseOperationResult<IReadOnlyList<T>> ToList<T>(object searchTerm, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<IReadOnlyList<T>>() { Success = true };

            var (command, _) = PreProcessing(procedureName, true, searchTerm);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleResult);
                response.Result = DataReaderToList<T>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
                response = DataBaseOperationResult<IReadOnlyList<T>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }
        #endregion

        #region Multiple result set

        /// <summary>
        /// Return tuple with 2 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return tuple with 3 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <typeparam name="T3">typed class 3</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }


        /// <summary>
        /// Return tuple with 4 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <typeparam name="T3">typed class 3</typeparam>
        /// <typeparam name="T4">typed class 4</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return tuple with 5 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <typeparam name="T3">typed class 3</typeparam>
        /// <typeparam name="T4">typed class 4</typeparam>
        /// <typeparam name="T5">typed class 5</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return tuple with 6 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <typeparam name="T3">typed class 3</typeparam>
        /// <typeparam name="T4">typed class 4</typeparam>
        /// <typeparam name="T5">typed class 5</typeparam>
        /// <typeparam name="T6">typed class 6</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, script);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return tuple with 7 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <typeparam name="T3">typed class 3</typeparam>
        /// <typeparam name="T4">typed class 4</typeparam>
        /// <typeparam name="T5">typed class 5</typeparam>
        /// <typeparam name="T6">typed class 6</typeparam>
        /// <typeparam name="T7">typed class 7</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>>();

            var (command, _) = PreProcessing(script, isStoreProcedure, null);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, script);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, script);

                reader.NextResult();

                IReadOnlyList<T7> t7 = DataReaderToList<T7>(reader, script, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>>.ErrorResult(msg);
            }
            finally
            {
                reader.Kill();
                command.Kill();
            }

            return response;
        }

        #endregion

    }
}
