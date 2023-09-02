using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Thomas.Database.SqlServer
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
            DbTypes.TryAdd("String", SqlDbType.VarChar);
            DbTypes.TryAdd("Int16", SqlDbType.SmallInt);
            DbTypes.TryAdd("Int32", SqlDbType.Int);
            DbTypes.TryAdd("Int64", SqlDbType.BigInt);
            DbTypes.TryAdd("Byte", SqlDbType.TinyInt);
            DbTypes.TryAdd("Decimal", SqlDbType.Decimal);
            DbTypes.TryAdd("Boolean", SqlDbType.Bit);
            DbTypes.TryAdd("Date", SqlDbType.Date);
            DbTypes.TryAdd("DateTime", SqlDbType.DateTime);
            DbTypes.TryAdd("Double", SqlDbType.Real);
            DbTypes.TryAdd("Float", SqlDbType.Float);
            DbTypes.TryAdd("Xml", SqlDbType.Xml);
            DbTypes.TryAdd("Guid", SqlDbType.UniqueIdentifier);
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

        public IEnumerable<IDataParameter> ExtractValuesFromSearchTerm(object searchTerm, string metadataKey)
        {
            if (!MetadataCacheManager.Instance.TryGet(metadataKey, out MetadataPropertyInfo[] dataParameters))
            {
                dataParameters = GetPropertiesCached(searchTerm);
                MetadataCacheManager.Instance.Set(metadataKey, dataParameters);
            }

            for (int i = 0; i < dataParameters.Length; i++)
            {
                yield return new SqlParameter()
                {
                    ParameterName = dataParameters[i].ParameterName,
                    SqlDbType = (SqlDbType)dataParameters[i].DbType,
                    Direction = dataParameters[i].Direction,
                    Value = dataParameters[i].GetDbParameterValue(searchTerm),
                    Size = dataParameters[i].Size
                };
            }

        }

        MetadataPropertyInfo[] GetPropertiesCached(object searchTerm)
        {
            var props = searchTerm.GetType().GetProperties();
            return props.Select(
                y => new MetadataPropertyInfo(y, GetParameter(y, searchTerm), (int)GetSqlDbType(y.PropertyType.Name))).ToArray();
        }

        static DbParameter GetParameter(PropertyInfo info, object value)
        {
            return new SqlParameter()
                {
                    ParameterName = $"@{info.Name.ToLower()}",
                    Value = info.GetValue(value) ?? DBNull.Value
                };
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
