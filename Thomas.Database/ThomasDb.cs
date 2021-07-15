using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Thomas.Database.Cache;

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
            var tp = typeof(T);

            var columns = GetColumns(reader);

            InfoCache infoCache = null;

            if (!CacheThomas.Instance.TryGet(tp.FullName, out infoCache))
            {
                var props = tp.GetProperties();

                if (StrictMode)
                {
                    string[] propsName = new string[props.Length];

                    for (int i = 0; i < props.Length; i++)
                    {
                        propsName[i] = props[i].Name.ToUpper();
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

                var infoProperties = props.Where(x => columns.Contains(x.Name.ToUpper())).ToDictionary(x => x.Name.ToUpper(), y =>
                                    y.PropertyType.IsGenericType ? new InfoProperty(y, Nullable.GetUnderlyingType(y.PropertyType)) : new InfoProperty(y, y.PropertyType));

                infoCache = new InfoCache(CheckContainNullables(props), infoProperties);

                CacheThomas.Instance.Set(tp.FullName, infoCache);
            }


            T[] result;

            int processors = GetMaxDegreeOfParallelism();

            if (processors > 1)
            {
                var safeList = new ConcurrentDictionary<string, InfoProperty>(infoCache.InfoProperties);

                ConcurrentDictionary<int, object[]> data2 = ExtractData2(reader, columns.Length, processors);

                if (infoCache.ContainNullables)
                {
                    result = FormatDataWithNullablesParallel<T>(data2, safeList, columns, data2.Count, processors);
                }
                else
                {
                    result = FormatDataWithoutNullablesParallel<T>(data2, safeList, columns, data2.Count, processors);
                }
            }
            else
            {
                object[][] data = ExtractData(reader, columns.Length);

                if (infoCache.ContainNullables)
                {
                    result = FormatDataWithNullables<T>(data, infoCache.InfoProperties, columns, data.Length);
                }
                else
                {
                    result = FormatDataWithoutNullables<T>(data, infoCache.InfoProperties, columns, data.Length);
                }
            }

            if (closeReader)
            {
                reader.Kill();
            }

            return result.ToList();
        }

        #region without result data
        /// <summary>
        /// Execute SQL script without result set
        /// </summary>
        /// <param name="script">string script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// Execute SQL script without result set
        /// </summary>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Full name store procedure</param>
        public DataBaseOperationResult Execute(object inputData, string procedureName)
        {
            var response = new DataBaseOperationResult() { Success = true };
            var (command, _) = PreProcessing(procedureName, true, inputData);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
                response = DataBaseOperationResult.ErrorResult(msg);
            }
            finally
            {
                command.Kill();
            }

            return response;
        }

        #endregion

        #region single row result

        /// <summary>
        /// Return single result row from execute SQL script
        /// </summary>
        /// <typeparam name="T">typed class</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Full name store procedure</param>
        public DataBaseOperationResult<T> ToSingle<T>(object paramValues, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<T>() { Success = true };

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Full name store procedure</param>
        public DataBaseOperationResult<IReadOnlyList<T>> ToList<T>(object paramValues, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<IReadOnlyList<T>>() { Success = true };

            var (command, _) = PreProcessing(procedureName, true, paramValues);

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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
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

        /// <summary>
        /// Return tuple with 2 typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">typed class 1</typeparam>
        /// <typeparam name="T2">typed class 2</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTuple<T1, T2>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTuple<T1, T2, T3>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTuple<T1, T2, T3, T4>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTuple<T1, T2, T3, T4, T5>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTuple<T1, T2, T3, T4, T5, T6>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>>();

            var (command, _) = PreProcessing(procedureName, true, paramValues);

            IDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, procedureName);

                reader.NextResult();

                IReadOnlyList<T7> t7 = DataReaderToList<T7>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, ex);
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
