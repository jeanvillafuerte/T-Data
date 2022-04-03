using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Thomas.Database
{
    using Cache;
    using Strategy;

    public sealed class ThomasDb : DbBase, IThomasDb
    {
        private readonly JobStrategy JobStrategy;

        public ThomasDb(IDatabaseProvider provider, JobStrategy jobStrategy, ThomasDbStrategyOptions options)
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
            JobStrategy = jobStrategy;
            TimeOut = options.ConnectionTimeout;
        }

        /// <summary>
        /// Transform database result set to Typed list maintaining original order
        /// </summary>
        /// <typeparam name="T">type of result</typeparam>
        /// <param name="reader">DataReader to transform</param>
        /// <param name="script">script or store procedure name</param>
        /// <param name="closeReader">flag to close reader after read. Default : false</param>
        private IReadOnlyList<T> DataReaderToList<T>(IDataReader reader, string script, bool isStoreProcedure = false) where T : class, new()
        {
            var columns = GetColumns(reader);

            if(columns.Length == 0)
            {
                return new List<T>();
            }

            Type tp = typeof(T);

            if (!MetadataCache.Instance.TryGet(tp.FullName + (isStoreProcedure ? script : ""), out MetadataProperties infoCache))
            {
                var props = tp.GetProperties();

                var infoProperties = props.Where(x => columns.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToDictionary(x => x.Name, y =>
                                     y.PropertyType.IsGenericType ? new InfoProperty(y, Nullable.GetUnderlyingType(y.PropertyType)) : new InfoProperty(y, y.PropertyType));

                infoCache = new MetadataProperties(infoProperties);

                MetadataCache.Instance.Set(tp.FullName, infoCache);

                if (StrictMode)
                {
                    string[] propsName = new string[props.Length];

                    for (int i = 0; i < props.Length; i++)
                    {
                        propsName[i] = props[i].Name;
                    }

                    var noMatchProperties = columns.Where(x => !propsName.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();

                    if (noMatchProperties.Length > 0)
                    {
                        throw new NoMatchPropertiesException("There are columns doesn't match with entity's fields. " +
                          "Columns : " + string.Join(", ", noMatchProperties) + Environment.NewLine +
                          "Entity Name : " + typeof(T).FullName + Environment.NewLine +
                          "Script : " + script);
                    }
                }
            }

            object[] values = new object[columns.Length];

            var list = new List<object[]>();

            while (reader.Read())
            {
                reader.GetValues(values);
                list.Add(values);
            }

            object[][] data = list.ToArray();

            T[] result = JobStrategy.FormatData<T>(infoCache.InfoProperties, data, columns, data.Length);

            GC.Collect(2, GCCollectionMode.Optimized);

            return result.ToList();
        }

        #region without result data
        /// <summary>
        /// Return Operation Result from Execute SQL script without result set
        /// </summary>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult ExecuteOp(string script, bool isStoreProcedure = true)
        {
            var response = new DataBaseOperationResult() { Success = true };
            var command = PreProcessing(script, isStoreProcedure);

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
        /// Return Operation Result from Execute SQL script without result set
        /// </summary>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult ExecuteOp(object inputData, string procedureName)
        {
            var response = new DataBaseOperationResult() { Success = true };
            var (command, parameters) = PreProcessing(procedureName, true, inputData);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DataBaseOperationResult.ErrorResult(msg);
            }
            finally
            {
                command.Kill();
            }

            return response;
        }

        /// <summary>
        /// Return rows affected from Execute Sql script
        /// </summary>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public int Execute(string script, bool isStoreProcedure = true)
        {
            var command = PreProcessing(script, isStoreProcedure);
            var rowsAffected = command.ExecuteNonQuery();
            command.Kill();
            return rowsAffected;
        }

        /// <summary>
        /// Return rows affected from Execute SQL script without result set
        /// </summary>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns></returns>
        public int Execute(object inputData, string procedureName)
        {
            var (command, _) = PreProcessing(procedureName, true, inputData);
            var rowsAffected = command.ExecuteNonQuery();
            command.Kill();
            return rowsAffected;
        }

        #endregion

        #region single row result

        /// <summary>
        /// Return Operation Result from Execute SQL script in an item
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<T> ToSingleOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DataBaseOperationResult<T>() { Success = true };

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleRow);
                var list = DataReaderToList<T>(reader, script);

                if(list?.Count > 0)
                {
                    response.Result = list[0];
                }
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
        /// Return Operation Result from Execute SQL script in an item
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<T> ToSingleOp<T>(object inputData, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<T>() { Success = true };

            var (command, parameters) = PreProcessing(procedureName, true, inputData);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleRow);
                var list = DataReaderToList<T>(reader, procedureName ,true);

                if (list?.Count > 0)
                {
                    response.Result = list[0];
                }
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

        /// <summary>
        /// Return item from Execute SQL script
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public T? ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);

            var list = DataReaderToList<T>(reader, script);

            reader.Kill();
            command.Kill();

            if (list?.Count > 0)
            {
                return list[0];
            }

            return null;
        }

        /// <summary>
        /// Return item from Execute SQL script
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns></returns>
        public T? ToSingle<T>(object inputData, string procedureName) where T : class, new()
        {
            var (command, _) = PreProcessing(procedureName, true, inputData);

            IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);

            var list = DataReaderToList<T>(reader, procedureName, true);

            reader.Kill();
            command.Kill();

            if (list?.Count > 0)
            {
                return list[0];
            }

            return null;
        }

        #endregion

        #region one result set

        /// <summary>
        /// Return Operation Result from execute SQL script in a list of T
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<IReadOnlyList<T>> ToListOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DataBaseOperationResult<IReadOnlyList<T>>() { Success = true };

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleResult);
                response.Result = DataReaderToList<T>(reader, script);
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
        /// Return Operation Result from execute SQL script in a list of T
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<IReadOnlyList<T>> ToListOp<T>(object inputData, string procedureName) where T : class, new()
        {
            var response = new DataBaseOperationResult<IReadOnlyList<T>>() { Success = true };

            var (command, parameters) = PreProcessing(procedureName, true, inputData);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader(CommandBehavior.SingleResult);
                response.Result = DataReaderToList<T>(reader, procedureName, true);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return list of T from Execute SQL script
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public IReadOnlyList<T> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult);

            var data =  DataReaderToList<T>(reader, script);

            reader.Kill();
            command.Kill();

            return data;
        }

        /// <summary>
        /// Return list of T from Execute SQL script
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns></returns>
        public IReadOnlyList<T> ToList<T>(object inputData, string procedureName) where T : class, new()
        {
            var (command, _) = PreProcessing(procedureName, true, inputData);

            IDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult);
            var data = DataReaderToList<T>(reader, procedureName, true);

            reader.Kill();
            command.Kill();

            return data;
        }
        #endregion

        #region Multiple result set

        /// <summary>
        /// Return tuple with 2 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTupleOp<T1, T2>(string script, bool isStoreProcedure = true)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>(t1, t2);
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
        /// Return tuple with 2 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

            IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

            reader.NextResult();

            IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>(t1, t2);
        }

        /// <summary>
        /// Return tuple with 3 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTupleOp<T1, T2, T3>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>(t1, t2, t3);

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
        /// Return tuple with 3 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

            IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

            reader.NextResult();

            IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

            reader.NextResult();

            IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>(t1, t2, t3);
        }

        /// <summary>
        /// Return tuple with 4 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

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

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>(t1, t2, t3, t4);
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
        /// Return tuple with 4 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        /// <returns></returns>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

            IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

            reader.NextResult();

            IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

            reader.NextResult();

            IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

            reader.NextResult();

            IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script);

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>(t1, t2, t3, t4);
        }

        /// <summary>
        /// Return tuple with 5 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

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

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>(t1, t2, t3, t4, t5);
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
        /// Return tuple with 5 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

            IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, script);

            reader.NextResult();

            IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, script);

            reader.NextResult();

            IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, script);

            reader.NextResult();

            IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, script);

            reader.NextResult();

            IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, script);

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>(t1, t2, t3, t4, t5);
        }

        /// <summary>
        /// Return tuple with 6 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

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

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>(t1, t2, t3, t4, t5, t6);
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
        /// Return tuple with 6 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

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

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>(t1, t2, t3, t4, t5, t6);
        }

        /// <summary>
        /// Return tuple with 7 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <typeparam name="T7">Typed class 7</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>>();

            var command = PreProcessing(script, isStoreProcedure);

            IDataReader? reader = null;

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

                IReadOnlyList<T7> t7 = DataReaderToList<T7>(reader, script);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>(t1, t2, t3, t4, t5, t6, t7);
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
        /// Return tuple with 7 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <typeparam name="T7">Typed class 7</typeparam>
        /// <param name="script">SQL script</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new()
        {
            var command = PreProcessing(script, isStoreProcedure);

            IDataReader reader = command.ExecuteReader();

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

            IReadOnlyList<T7> t7 = DataReaderToList<T7>(reader, script);

            reader.Kill();
            command.Kill();

            return new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        /// <summary>
        /// Return tuple with 2 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTupleOp<T1, T2>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>(t1, t2);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return tuple with 3 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTupleOp<T1, T2, T3>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>(t1, t2, t3);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return tuple with 4 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTupleOp<T1, T2, T3, T4>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>(t1, t2, t3, t4);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return tuple with 5 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>(t1, t2, t3, t4, t5);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return tuple with 6 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>(t1, t2, t3, t4, t5, t6);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
        /// Return tuple with 7 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <typeparam name="T3">Typed class 3</typeparam>
        /// <typeparam name="T4">Typed class 4</typeparam>
        /// <typeparam name="T5">Typed class 5</typeparam>
        /// <typeparam name="T6">Typed class 6</typeparam>
        /// <typeparam name="T7">Typed class 7</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(object paramValues, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>>();

            var (command, parameters) = PreProcessing(procedureName, true, paramValues);

            IDataReader? reader = null;

            try
            {
                reader = command.ExecuteReader();

                IReadOnlyList<T1> t1 = DataReaderToList<T1>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T2> t2 = DataReaderToList<T2>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T3> t3 = DataReaderToList<T3>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T4> t4 = DataReaderToList<T4>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T5> t5 = DataReaderToList<T5>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T6> t6 = DataReaderToList<T6>(reader, procedureName, true);

                reader.NextResult();

                IReadOnlyList<T7> t7 = DataReaderToList<T7>(reader, procedureName, true);

                response.Result = new Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>(t1, t2, t3, t4, t5, t6, t7);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
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
