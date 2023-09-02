using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Thomas.Database
{
    using Strategy;
    using System.Threading;
    using System.Threading.Tasks;
    using Thomas.Database.Database;

    public sealed class DatabaseBase : DbBase, IDatabase
    {
        public DatabaseBase(IDatabaseProvider provider, ThomasDbStrategyOptions options)
        {
            Provider = provider;
            Options = options;
        }

        #region without result data
        /// <summary>
        /// Return Operation Result from Execute SQL script without result set
        /// </summary>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DbOpResult ExecuteOp(string script, bool isStoreProcedure = true)
        {
            var response = new DbOpResult() { Success = true };

            try
            {
                response.RowsAffected = Execute(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult>(msg);
            }

            return response;
        }

        /// <summary>
        /// Return Operation Result from Execute SQL script without result set
        /// </summary>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DbOpResult ExecuteOp(object inputData, string procedureName)
        {
            var response = new DbOpResult() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);
                var affected = command.ExecuteNonQuery();
                command.RescueOutParamValues();
                command.CloseConnetion();
                command.SetValuesOutFields();
                response.RowsAffected = affected;
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult>(msg);
            }

            return response;
        }

        /// <summary>
        /// Return rows affected from Execute Sql script
        /// </summary>
        /// <param name="script">Script text</param>
        /// <returns>Number of rows affected</returns>
        public int Execute(string script, bool isStoreProcedure = true)
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Return rows affected from Execute SQL script
        /// </summary>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns>Number of rows affected and load values in fields flagged as output params</returns>
        public int Execute(object inputData, string procedureName)
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(procedureName, true, inputData);
            var affected = command.ExecuteNonQuery();
            command.RescueOutParamValues();
            command.CloseConnetion();
            command.SetValuesOutFields();
            return affected;
        }

        public async Task<DbOpAsyncResult> ExecuteOpAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult() { Success = true };

            try
            {
                response.RowsAffected = await ExecuteAsync(script, isStoreProcedure, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult>(msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult> ExecuteOpAsync(object inputData, string procedureName, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = await command.PrepareAsync(procedureName, true, inputData, cancellationToken);
                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                command.RescueOutParamValues();
                await command.CloseConnetionAsync();
                command.SetValuesOutFields();
                response.RowsAffected = affected;
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult>(msg);
            }

            return response;
        }

        public async Task<int> ExecuteAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken)
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(script, isStoreProcedure, cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> ExecuteAsync(object inputData, string procedureName, CancellationToken cancellationToken)
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(procedureName, true, inputData, cancellationToken);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            command.RescueOutParamValues();
            await command.CloseConnetionAsync();
            command.SetValuesOutFields();
            return affected;
        }

        #endregion

        #region single row result

        /// <summary>
        /// Return Operation Result from Execute SQL script in an item
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DbOpResult<T> ToSingleOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DbOpResult<T>() { Success = true };

            try
            {
                response.Result = ToSingle<T>(script, isStoreProcedure)!;
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<T>>(msg);
            }

            return response;
        }

        /// <summary>
        /// Return Operation Result from Execute SQL script in an item
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DbOpResult<T> ToSingleOp<T>(object inputData, string procedureName) where T : class, new()
        {
            var response = new DbOpResult<T>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using (var command = new DbCommand(Provider, Options))
                {
                    parameters = command.Prepare(procedureName, true, inputData);
                    var (data, columns) = command.Read(CommandBehavior.SingleRow);
                    command.RescueOutParamValues();
                    command.CloseConnetion();

                    command.SetValuesOutFields();

                    response.Result = command.TransformData<T>(data, columns).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<T>>(msg);
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
            using (var command = new DbCommand(Provider, Options))
            {
                command.Prepare(script, isStoreProcedure);
                var (data, columns) = command.Read(CommandBehavior.SingleRow);
                command.CloseConnetion();

                return command.TransformData<T>(data, columns).FirstOrDefault();
            }
        }

        /// <summary>
        /// Return item from Execute SQL script
        /// If you want get back value of out parameters consider to use ToSingleOp
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns></returns>
        public T? ToSingle<T>(object inputData, string procedureName) where T : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(procedureName, true, inputData);
            var (data, columns) = command.Read(CommandBehavior.SingleRow);
            command.RescueOutParamValues();
            command.CloseConnetion();

            command.SetValuesOutFields();

            return command.TransformData<T>(data, columns).FirstOrDefault();
        }

        public async Task<T?> ToSingleAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            using (var command = new DbCommand(Provider, Options))
            {
                await command.PrepareAsync(script, isStoreProcedure, cancellationToken);
                var (data, columns) = await command.ReadAsync(CommandBehavior.SingleRow, cancellationToken);
                await command.CloseConnetionAsync();

                return command.TransformData<T>(data, columns).FirstOrDefault();
            }
        }

        public async Task<T?> ToSingleAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            using (var command = new DbCommand(Provider, Options))
            {
                await command.PrepareAsync(procedureName, true, inputData, cancellationToken);
                var (data, columns) = await command.ReadAsync(CommandBehavior.SingleRow, cancellationToken);
                command.RescueOutParamValues();
                await command.CloseConnetionAsync();

                command.SetValuesOutFields();
                return command.TransformData<T>(data, columns).FirstOrDefault();
            }
        }

        public async Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<T>() { Success = true };

            try
            {
                response.Result = await ToSingleAsync<T>(script, isStoreProcedure, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<T>>(msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<T>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using (var command = new DbCommand(Provider, Options))
                {
                    parameters = await command.PrepareAsync(procedureName, true, inputData, cancellationToken);
                    var (data, columns) = await command.ReadAsync(CommandBehavior.SingleRow, cancellationToken);
                    command.RescueOutParamValues();
                    await command.CloseConnetionAsync();

                    command.SetValuesOutFields();

                    response.Result = command.TransformData<T>(data, columns).FirstOrDefault();
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<T>>(msg);
            }

            return response;
        }

        #endregion

        #region one result set

        /// <summary>
        /// Return Operation Result from execute SQL script in a list of T
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="script">Script text</param>
        /// <param name="isStoreProcedure">Flag when script is store procedure. Default : true</param>
        public DbOpResult<IEnumerable<T>> ToListOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var response = new DbOpResult<IEnumerable<T>>() { Success = true };

            try
            {
                response.Result = ToList<T>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<IEnumerable<T>>>(msg);
            }

            return response;
        }

        /// <summary>
        /// Return Operation Result from execute SQL script in a list of T
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        public DbOpResult<IEnumerable<T>> ToListOp<T>(object inputData, string procedureName) where T : class, new()
        {
            var response = new DbOpResult<IEnumerable<T>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data, columns) = command.Read(CommandBehavior.SingleResult);

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                response.Result = command.TransformData<T>(data, columns);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<IEnumerable<T>>>(msg);
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
        public IEnumerable<T> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);
            var (data, columns) = command.Read(CommandBehavior.SingleResult);

            command.CloseConnetion();

            return command.TransformData<T>(data, columns);
        }

        /// <summary>
        /// Return list of T from Execute SQL script
        /// If you want get back value of out parameters consider to use ToListOp
        /// </summary>
        /// <typeparam name="T">Typed class</typeparam>
        /// <param name="inputData">Matched fields from object names against store procedure parameters</param>
        /// <param name="procedureName">Store procedure name</param>
        /// <returns></returns>
        public IEnumerable<T> ToList<T>(object inputData, string script, bool isStoreProcedure = false) where T : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure, inputData);

            return command.ReadItems<T>(CommandBehavior.SingleResult);
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(script, isStoreProcedure, cancellationToken);
            var (data, columns) = await command.ReadAsync(CommandBehavior.SingleResult, cancellationToken);

            await command.CloseConnetionAsync();

            return command.TransformData<T>(data, columns);
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(procedureName, true, inputData, cancellationToken);

            var (data, columns) = await command.ReadAsync(CommandBehavior.SingleResult, cancellationToken);

            command.RescueOutParamValues();

            await command.CloseConnetionAsync();

            command.SetValuesOutFields();

            return command.TransformData<T>(data, columns);
        }

        public async Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<IEnumerable<T>>() { Success = true };

            try
            {
                response.Result = await ToListAsync<T>(script, isStoreProcedure, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<IEnumerable<T>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<IEnumerable<T>>>(msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<IEnumerable<T>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using (var command = new DbCommand(Provider, Options))
                {
                    parameters = await command.PrepareAsync(procedureName, true, inputData, cancellationToken);

                    var (data, columns) = await command.ReadAsync(CommandBehavior.SingleResult, cancellationToken);

                    command.RescueOutParamValues();

                    await command.CloseConnetionAsync();

                    command.SetValuesOutFields();

                    response.Result = command.TransformData<T>(data, columns);
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<IEnumerable<T>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<IEnumerable<T>>>(msg);
            }

            return response;
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(string script, bool isStoreProcedure = true)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>();

            try
            {
                response.Result = ToTuple<T1, T2>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1, t2);
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleAsync<T1, T2>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(script, isStoreProcedure, cancellationToken);

            var (data1, columns1) = await command.ReadAsync(CommandBehavior.Default, cancellationToken);
            var (data2, columns2) = await command.ReadNextAsync(cancellationToken);

            await command.CloseConnetionAsync();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1, t2);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>> ToTupleOpAsync<T1, T2>(string script, bool isStoreProcedure, CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2>(script, isStoreProcedure, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();
            var (data3, columns3) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data2, columns3);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>> ToTupleOp<T1, T2, T3>(string script, bool isStoreProcedure, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3>(script, isStoreProcedure, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>(msg);
            }

            return response;
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleAsync<T1, T2, T3>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            await command.PrepareAsync(script, isStoreProcedure, cancellationToken);

            var (data1, columns1) = await command.ReadAsync(CommandBehavior.Default, cancellationToken);
            var (data2, columns2) = await command.ReadNextAsync(cancellationToken);
            var (data3, columns3) = await command.ReadNextAsync(cancellationToken);

            await command.CloseConnetionAsync();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data2, columns3);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>();

            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();
            var (data3, columns3) = command.ReadNext();
            var (data4, columns4) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data3, columns3);
            var t4 = command.TransformData<T4>(data4, columns4);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(t1, t2, t3, t4);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();
            var (data3, columns3) = command.ReadNext();
            var (data4, columns4) = command.ReadNext();
            var (data5, columns5) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data3, columns3);
            var t4 = command.TransformData<T4>(data4, columns4);
            var t5 = command.TransformData<T5>(data5, columns5);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(t1, t2, t3, t4, t5);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();
            var (data3, columns3) = command.ReadNext();
            var (data4, columns4) = command.ReadNext();
            var (data5, columns5) = command.ReadNext();
            var (data6, columns6) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data3, columns3);
            var t4 = command.TransformData<T4>(data4, columns4);
            var t5 = command.TransformData<T5>(data5, columns5);
            var t6 = command.TransformData<T6>(data6, columns6);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(t1, t2, t3, t4, t5, t6);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, isStoreProcedure);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>(msg);
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
        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new()
        {
            using var command = new DbCommand(Provider, Options);
            command.Prepare(script, isStoreProcedure);

            var (data1, columns1) = command.Read(CommandBehavior.Default);
            var (data2, columns2) = command.ReadNext();
            var (data3, columns3) = command.ReadNext();
            var (data4, columns4) = command.ReadNext();
            var (data5, columns5) = command.ReadNext();
            var (data6, columns6) = command.ReadNext();
            var (data7, columns7) = command.ReadNext();

            command.CloseConnetion();

            var t1 = command.TransformData<T1>(data1, columns1);
            var t2 = command.TransformData<T2>(data2, columns2);
            var t3 = command.TransformData<T3>(data3, columns3);
            var t4 = command.TransformData<T4>(data4, columns4);
            var t5 = command.TransformData<T5>(data5, columns5);
            var t6 = command.TransformData<T6>(data6, columns6);
            var t7 = command.TransformData<T7>(data6, columns7);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        /// <summary>
        /// Return tuple with 2 Typed list from execute SQL script
        /// </summary>
        /// <typeparam name="T1">Typed class 1</typeparam>
        /// <typeparam name="T2">Typed class 2</typeparam>
        /// <param name="paramValues">Match parameters from object field names against store procedure</param>
        /// <param name="procedureName">Store procedure name</param>
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1, t2);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();
                var (data3, columns3) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);
                var t3 = command.TransformData<T3>(data3, columns3);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();
                var (data3, columns3) = command.ReadNext();
                var (data4, columns4) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);
                var t3 = command.TransformData<T3>(data3, columns3);
                var t4 = command.TransformData<T4>(data4, columns4);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(t1, t2, t3, t4);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();
                var (data3, columns3) = command.ReadNext();
                var (data4, columns4) = command.ReadNext();
                var (data5, columns5) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);
                var t3 = command.TransformData<T3>(data3, columns3);
                var t4 = command.TransformData<T4>(data4, columns4);
                var t5 = command.TransformData<T5>(data5, columns5);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(t1, t2, t3, t4, t5);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();
                var (data3, columns3) = command.ReadNext();
                var (data4, columns4) = command.ReadNext();
                var (data5, columns5) = command.ReadNext();
                var (data6, columns6) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);
                var t3 = command.TransformData<T3>(data3, columns3);
                var t4 = command.TransformData<T4>(data4, columns4);
                var t5 = command.TransformData<T5>(data5, columns5);
                var t6 = command.TransformData<T6>(data6, columns6);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(t1, t2, t3, t4, t5, t6);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>(msg);
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
        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(object inputData, string procedureName)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() { Success = true };

            IDataParameter[] parameters = Array.Empty<IDataParameter>();

            try
            {
                using var command = new DbCommand(Provider, Options);
                parameters = command.Prepare(procedureName, true, inputData);

                var (data1, columns1) = command.Read(CommandBehavior.Default);
                var (data2, columns2) = command.ReadNext();
                var (data3, columns3) = command.ReadNext();
                var (data4, columns4) = command.ReadNext();
                var (data5, columns5) = command.ReadNext();
                var (data6, columns6) = command.ReadNext();
                var (data7, columns7) = command.ReadNext();

                command.RescueOutParamValues();
                command.CloseConnetion();

                command.SetValuesOutFields();

                var t1 = command.TransformData<T1>(data1, columns1);
                var t2 = command.TransformData<T2>(data2, columns2);
                var t3 = command.TransformData<T3>(data3, columns3);
                var t4 = command.TransformData<T4>(data4, columns4);
                var t5 = command.TransformData<T5>(data5, columns5);
                var t6 = command.TransformData<T6>(data6, columns6);
                var t7 = command.TransformData<T7>(data7, columns7);

                response.Result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(t1, t2, t3, t4, t5, t6, t7);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(procedureName, parameters, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>(msg);
            }

            return response;
        }

        #endregion
    }
}