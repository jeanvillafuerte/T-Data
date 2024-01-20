using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Thomas.Database.SqlServer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Thomas.Database.Cache.Metadata;

    internal sealed class SqlProvider : IDatabaseProvider
    {
        private static readonly ImmutableDictionary<string, SqlDbType> DbTypes = ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, SqlDbType>("String", SqlDbType.VarChar),
                new KeyValuePair<string, SqlDbType>("Int16", SqlDbType.SmallInt),
                new KeyValuePair<string, SqlDbType>("Int32", SqlDbType.Int),
                new KeyValuePair<string, SqlDbType>("Int64", SqlDbType.BigInt),
                new KeyValuePair<string, SqlDbType>("Byte", SqlDbType.TinyInt),
                new KeyValuePair<string, SqlDbType>("Decimal", SqlDbType.Decimal),
                new KeyValuePair<string, SqlDbType>("Boolean", SqlDbType.Bit),
                new KeyValuePair<string, SqlDbType>("Date", SqlDbType.Date),
                new KeyValuePair<string, SqlDbType>("DateTime", SqlDbType.DateTime),
                new KeyValuePair<string, SqlDbType>("Double", SqlDbType.Real),
                new KeyValuePair<string, SqlDbType>("Float", SqlDbType.Float),
                new KeyValuePair<string, SqlDbType>("Xml", SqlDbType.Xml),
                new KeyValuePair<string, SqlDbType>("Guid", SqlDbType.UniqueIdentifier)
            });

        public readonly DbSettings Options;

        static SqlProvider()
        {
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        }

        public SqlProvider(DbSettings options)
        {
            Options = options;
        }

        public DbCommand CreateCommand(in DbConnection connection, in string script, in bool isStoreProcedure)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = Options.ConnectionTimeout;

            if (isStoreProcedure)
                command.CommandType = CommandType.StoredProcedure;

            return command;
        }

        public DbConnection CreateConnection(in string connection)
        {
            return new SqlConnection(connection);
        }

        public IEnumerable<IDbDataParameter> GetParams(string metadataKey, object searchTerm)
        {
            MetadataParameters<SqlDbType>[] dataParameters = null;

            if (!CacheDbParameter<SqlDbType>.TryGet(in metadataKey, ref dataParameters))
            {
                var props = searchTerm.GetType().GetProperties();
                dataParameters = props.Select(y =>
                {
                    var name = $"@{y.Name.ToLower()}";
                    var dbType = GetSqlDbType(y.PropertyType.Name);
                    return new MetadataParameters<SqlDbType>(in y, in name, in dbType);
                }).ToArray();

                CacheDbParameter<SqlDbType>.Set(in metadataKey, in dataParameters);
            }

            foreach (var parameter in dataParameters)
            {
                yield return new SqlParameter()
                {
                    ParameterName = parameter.DbParameterName,
                    Value = parameter.GetValue(in searchTerm),
                    Direction = parameter.Direction,
                    Size = parameter.Size,
                    SqlDbType = parameter.DbType
                };
            }

        }

        public IEnumerable<dynamic> GetParams(string metadataKey)
        {
            MetadataParameters<SqlDbType>[] dataParameters = null;
            CacheDbParameter<SqlDbType>.TryGet(in metadataKey, ref dataParameters);
            return dataParameters.Select(x => x as dynamic).ToList();
        }

        private static SqlDbType GetSqlDbType(in string propertyName) => DbTypes[propertyName];

        public bool IsCancellatedOperationException(in Exception exception)
        {
            if (exception is SqlException ex)
                return ex.Errors.Cast<SqlError>().Any(x => x.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase));

            return false;
        }

        public void LoadParameterValues(IEnumerable<IDbDataParameter> parameters, in object searchTerm, in string metadataKey)
        {
            MetadataParameters<SqlDbType>[] dataParameters = null;

            CacheDbParameter<SqlDbType>.TryGet(in metadataKey, ref dataParameters);

            foreach (var item in dataParameters)
            {
                if (item.IsOutParameter)
                {
                    var parameter = parameters.First(x => x.ParameterName == item.DbParameterName);
                    item.SetValue(searchTerm, parameter.Value, Options.CultureInfo);
                }
            }
        }

    }
}
