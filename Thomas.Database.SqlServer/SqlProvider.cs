using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Thomas.Database.SqlServer
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Thomas.Database.Cache.Metadata;

    public class SqlProvider : IDatabaseProvider
    {
        private static ConcurrentDictionary<string, SqlDbType> DbTypes;

        public ThomasDbStrategyOptions Options { get; }

        static SqlProvider()
        {
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
            LoadDbTypes();
        }

        public SqlProvider(ThomasDbStrategyOptions options)
        {
            Options = options;
        }

        private static void LoadDbTypes()
        {
            DbTypes = new ConcurrentDictionary<string, SqlDbType>(Environment.ProcessorCount, 13);
            DbTypes.TryAdd("string", SqlDbType.VarChar);
            DbTypes.TryAdd("int16", SqlDbType.SmallInt);
            DbTypes.TryAdd("int32", SqlDbType.Int);
            DbTypes.TryAdd("int64", SqlDbType.BigInt);
            DbTypes.TryAdd("byte", SqlDbType.TinyInt);
            DbTypes.TryAdd("decimal", SqlDbType.Decimal);
            DbTypes.TryAdd("boolean", SqlDbType.Bit);
            DbTypes.TryAdd("date", SqlDbType.Date);
            DbTypes.TryAdd("datetime", SqlDbType.DateTime);
            DbTypes.TryAdd("double", SqlDbType.Real);
            DbTypes.TryAdd("float", SqlDbType.Float);
            DbTypes.TryAdd("xml", SqlDbType.Xml);
            DbTypes.TryAdd("guid", SqlDbType.UniqueIdentifier);
        }

        public DbCommand CreateCommand(DbConnection connection, string script, bool isStoreProcedure)
        {
            var command = connection.CreateCommand();
            command.UpdatedRowSource = UpdateRowSource.None;
            command.CommandText = script;
            command.CommandTimeout = Options.ConnectionTimeout;
            command.CommandType = isStoreProcedure ? CommandType.StoredProcedure : CommandType.Text;
            command.Prepare();
            return command;
        }

        public async Task<DbCommand> CreateCommandAsync(DbConnection connection, string script, bool isStoreProcedure, CancellationToken cancellationToken)
        {
            var command = connection.CreateCommand();
            command.UpdatedRowSource = UpdateRowSource.None;
            command.CommandText = script;
            command.CommandTimeout = Options.ConnectionTimeout;
            command.CommandType = isStoreProcedure ? CommandType.StoredProcedure : CommandType.Text;
            await command.PrepareAsync(cancellationToken);

            return command;
        }

        public DbConnection CreateConnection(string connection)
        {
            return new SqlConnection(connection);
        }

        public (IDataParameter[], string) ExtractValuesFromSearchTerm(object searchTerm)
        {
            var tp = searchTerm.GetType();
            var key = tp.FullName;

            if (!MetadataCacheManager.Instance.TryGet(key, out Dictionary<string, MetadataPropertyInfo> propsCached))
            {
                var props = tp.GetProperties();
                propsCached = props.ToDictionary(x => x.Name, y => y.PropertyType.IsGenericType ? new MetadataPropertyInfo(y, Nullable.GetUnderlyingType(y.PropertyType)) : new MetadataPropertyInfo(y, y.PropertyType));
                MetadataCacheManager.Instance.Set(key, propsCached);
            }

            var parameters = new List<IDataParameter>();

            foreach (var keyValuePair in propsCached)
            {
                var value = keyValuePair.Value.GetValue(searchTerm);

                ParameterDirection direction = keyValuePair.Value.GetParameterDireccion();
                int size = keyValuePair.Value.GetParameterSize();

                parameters.Add(new SqlParameter()
                {
                    ParameterName = $"@{keyValuePair.Value.ParameterName}",
                    Value = direction == ParameterDirection.Input ? value : DBNull.Value,
                    SqlDbType = SqlProvider.GetSqlDbType(keyValuePair.Value.PropertyName),
                    Direction = direction,
                    Size = size
                });
            }

            return (parameters.ToArray(), key);
        }

        private static SqlDbType GetSqlDbType(string propertyName) => DbTypes[propertyName];

        public bool IsCancellatedOperationException(Exception exception)
        {
            if (exception is SqlException ex)
                return ex.Errors.Cast<SqlError>().Any(x => x.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase));

            return false;
        }
    }
}
