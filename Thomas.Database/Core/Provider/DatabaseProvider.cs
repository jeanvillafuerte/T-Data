using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Database.Cache;
using Thomas.Database.Core.Provider.Clients;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider
{
    internal class DatabaseProvider
    {
        private static readonly ImmutableDictionary<SqlProvider, IProviderConfig> Providers = ImmutableDictionary.CreateRange(new[]
        {
                new KeyValuePair<SqlProvider, IProviderConfig>(SqlProvider.SqlServer, new SqlProviderConfig()),
                new KeyValuePair<SqlProvider, IProviderConfig>(SqlProvider.MySql, new SqlProviderConfig()),
                new KeyValuePair<SqlProvider, IProviderConfig>(SqlProvider.PostgreSql, new SqlProviderConfig()),
                new KeyValuePair<SqlProvider, IProviderConfig>(SqlProvider.Oracle, new OracleProviderConfig()),
                new KeyValuePair<SqlProvider, IProviderConfig>(SqlProvider.Sqlite, new SqlProviderConfig())
        });

        internal SqlProvider Provider
        {
            get
            {
                return _options.SqlProvider;
            }
        }

        private readonly DbSettings _options;
        private readonly DbProviderFactory _dbProviderFactory;
        public DatabaseProvider(DbSettings options)
        {
            _options = options;

            foreach (var item in Providers[_options.SqlProvider].SupportedAssemblies)
            {
                try
                {
                    _dbProviderFactory = DbProviderFactories.GetFactory(item);
                    break;
                }
                catch
                {

                }
            }

            if (_dbProviderFactory == null)
            {
                throw new Exception("No provider found");
            }
        }

        public DbCommand CreateCommand(in DbConnection connection, in string script, in bool isStoreProcedure)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = _options.ConnectionTimeout;

            if (isStoreProcedure)
                command.CommandType = CommandType.StoredProcedure;

            return command;
        }

        public DbConnection CreateConnection(in string connection)
        {
            var cnx = _dbProviderFactory.CreateConnection();
            cnx.ConnectionString = connection;
            return cnx;
        }

        public DbParameter CreateParameter(string key, QueryParameter parameter)
        {
            var dbParameter = _dbProviderFactory.CreateParameter();
            dbParameter.ParameterName = key;
            dbParameter.Value = parameter.Value;
            dbParameter.Direction = parameter.IsOutParam ? ParameterDirection.Output : ParameterDirection.Input;

            if (parameter.SourceType != null)
            {
                var parameterTypeName = GetParameterTypeName(parameter.SourceType);
                SetDbTypeParameter(dbParameter, parameterTypeName);
            }

            return dbParameter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetParameterTypeName(Type type)
        {
            string parameterType = "";
            switch (_options.SqlProvider)
            {
                case SqlProvider.SqlServer:
                    parameterType = SqlServerDbTypes[type.Name];
                    break;
                case SqlProvider.Oracle:
                    parameterType = OracleDbTypes[type.Name];
                    break;
                case SqlProvider.PostgreSql:
                    parameterType = PostgreSQLDbTypes[type.Name];
                    break;
                case SqlProvider.MySql:
                    parameterType = MySQLDbTypes[type.Name];
                    break;
                case SqlProvider.Sqlite:
                    parameterType = SQLiteDbTypes[type.Name];
                    break;
            }

            return parameterType;
        }

        public IEnumerable<IDbDataParameter> GetParams(string metadataKey, object searchTerm)
        {
            MetadataParameters[] dataParameters = null;

            if (!CacheDbParameterArray.TryGet(in metadataKey, ref dataParameters))
            {
                var props = searchTerm.GetType().GetProperties();
                dataParameters = props.Select(y =>
                {
                    var name = $"{_options.SQLValues.BindVariable}{y.Name.ToLower()}";
                    var dbTypeName = GetParameterTypeName(y.PropertyType);
                    return new MetadataParameters(in y, in name, in dbTypeName);
                }).ToArray();

                CacheDbParameterArray.Set(in metadataKey, in dataParameters);
            }

            foreach (var parameter in dataParameters)
            {
                var dbParameter = _dbProviderFactory.CreateParameter();
                dbParameter.ParameterName = parameter.DbParameterName;
                dbParameter.Value = parameter.GetValue(in searchTerm);
                dbParameter.Direction = parameter.Direction;
                dbParameter.Size = parameter.Size;
                SetDbTypeParameter(dbParameter, parameter.DbTypeName);
                yield return dbParameter;
            }
        }

        public IEnumerable<dynamic> GetParams(string metadataKey)
        {
            MetadataParameters[] dataParameters = null;
            CacheDbParameterArray.TryGet(in metadataKey, ref dataParameters);
            return dataParameters.Select(x => x as dynamic).ToList();
        }

        public IEnumerable<IDbDataParameter> GetRawParams(object searchTerm)
        {
            var props = searchTerm.GetType().GetProperties();
            var dataParameters = props.Select(y =>
            {
                var name = $"{_options.SQLValues.BindVariable}{y.Name.ToLower()}";
                var dbTypeName = GetParameterTypeName(y.PropertyType);
                return new MetadataParameters(in y, in name, in dbTypeName);
            }).ToArray();

            foreach (var parameter in dataParameters)
            {
                var dbParameter = _dbProviderFactory.CreateParameter();
                dbParameter.ParameterName = parameter.DbParameterName;
                dbParameter.Value = parameter.GetValue(in searchTerm);
                dbParameter.Direction = parameter.Direction;
                dbParameter.Size = parameter.Size;
                SetDbTypeParameter(dbParameter, parameter.DbTypeName);
                yield return dbParameter;
            }
        }

        public bool IsCancellatedOperationException(in Exception? exception) => exception.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LoadParameterValues(IEnumerable<IDbDataParameter> parameters, in object searchTerm, in string metadataKey)
        {
            MetadataParameters[] dataParameters = null;

            CacheDbParameterArray.TryGet(in metadataKey, ref dataParameters);

            foreach (var item in dataParameters)
            {
                if (item.IsOutParameter)
                {
                    var parameter = parameters.First(x => x.ParameterName == item.DbParameterName);
                    item.SetValue(searchTerm, parameter.Value, _options.CultureInfo);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDbTypeParameter(in DbParameter parameter, string dbTypeEnumValue)
        {
            var key = HashHelper.GenerateHash($"{_options.SqlProvider}").ToString();

            if (CachePropertyDbType.TryGetValue(key, out var paramInfo))
            {
                var setter = paramInfo.PropertyInfo.SetMethod!.CreateDelegate(typeof(Action<,>).MakeGenericType(paramInfo.PropertyInfo.DeclaringType!, paramInfo.PropertyInfo.PropertyType));
                object dbTypeValue = Enum.Parse(paramInfo.Type, dbTypeEnumValue, true);
                setter.DynamicInvoke(parameter, dbTypeValue);
            }
            else
            {
                var provider = Providers[_options.SqlProvider];
                Assembly providerAssembly = Assembly.Load(provider.SupportedAssemblies[0]);
                Type parameterType = providerAssembly.GetType(provider.FullDbParameterType);

                if (parameterType != null && parameter.GetType().Equals(parameterType))
                {
                    PropertyInfo bbTypeProperty = parameterType.GetProperty(provider.DbTypeProperty);
                    Type dbTypeEnum = providerAssembly.GetType(provider.FullDbType);
                    object dbTypeValue = Enum.Parse(dbTypeEnum, dbTypeEnumValue, true);
                    var setter = bbTypeProperty.SetMethod!.CreateDelegate(typeof(Action<,>).MakeGenericType(bbTypeProperty.DeclaringType, bbTypeProperty.PropertyType));
                    setter.DynamicInvoke(parameter, dbTypeValue);

                    CachePropertyDbType.TryAdd(key, new ParameterDbInfo { PropertyInfo = bbTypeProperty, Type = dbTypeEnum });
                }
            }
        }

        internal object GetValueFromParameter(in IDbDataParameter parameter)
        {
            var provider = Providers[_options.SqlProvider];
            Assembly providerAssembly = Assembly.Load(provider.SupportedAssemblies[0]);
            Type parameterType = providerAssembly.GetType(provider.FullDbParameterType);

            if (parameter.GetType().Equals(parameterType))
            {
                PropertyInfo bbTypeProperty = parameterType.GetProperty("Value");
                var valueObject = bbTypeProperty.GetValue(parameter);

                var valueType = valueObject.GetType();
                if (valueType.Name.Contains("Oracle"))
                {
                    PropertyInfo valueProperty = valueType.GetProperty("Value");
                    return valueProperty.GetValue(valueObject);
                }
            }

            return null;
        }

        private static readonly ConcurrentDictionary<string, ParameterDbInfo> CachePropertyDbType = new ConcurrentDictionary<string, ParameterDbInfo>(Environment.ProcessorCount * 2, 5);

        class ParameterDbInfo
        {
            public PropertyInfo PropertyInfo { get; set; }
            public Type Type { get; set; }
        }

        private ImmutableDictionary<string, string> SqlServerDbTypes = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("String", "NVarChar"),
            new KeyValuePair<string, string>("Int16", "SmallInt"),
            new KeyValuePair<string, string>("Int32", "Int"),
            new KeyValuePair<string, string>("Int64", "BigInt"),
            new KeyValuePair<string, string>("Byte", "TinyInt"),
            new KeyValuePair<string, string>("Decimal", "Decimal"),
            new KeyValuePair<string, string>("Boolean", "Bit"),
            new KeyValuePair<string, string>("Date", "Date"),
            new KeyValuePair<string, string>("DateTime", "DateTime"),
            new KeyValuePair<string, string>("Double", "Float"),
            new KeyValuePair<string, string>("Float", "Float"),
            new KeyValuePair<string, string>("Xml", "Xml"),
            new KeyValuePair<string, string>("Guid", "UniqueIdentifier"),
            new KeyValuePair<string, string>("Binary", "Binary"),
            new KeyValuePair<string, string>("Time", "Time"),
            new KeyValuePair<string, string>("UInt16", "SmallInt"),
            new KeyValuePair<string, string>("UInt32", "Int"),
            new KeyValuePair<string, string>("UInt64", "BigInt"),
            new KeyValuePair<string, string>("SByte", "TinyInt")
        });

        private ImmutableDictionary<string, string> OracleDbTypes = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("String", "VARCHAR2"),
            new KeyValuePair<string, string>("Int16", "INT16"),
            new KeyValuePair<string, string>("Int32", "INT32"),
            new KeyValuePair<string, string>("Int64", "INT64"),
            new KeyValuePair<string, string>("Byte", "BYTE"),
            new KeyValuePair<string, string>("Decimal", "DECIMAL"),
            new KeyValuePair<string, string>("Boolean", "INT32"),
            new KeyValuePair<string, string>("Date", "DATE"),
            new KeyValuePair<string, string>("DateTime", "DATE"),
            new KeyValuePair<string, string>("Double", "DOUBLE"),
            new KeyValuePair<string, string>("Float", "FLOAT"),
            new KeyValuePair<string, string>("Xml", "XMLTYPE"),
            new KeyValuePair<string, string>("Guid", "RAW"),
            new KeyValuePair<string, string>("Binary", "RAW"),
            new KeyValuePair<string, string>("Time", "TIMESTAMP"),
            new KeyValuePair<string, string>("UInt16", "INT16"),
            new KeyValuePair<string, string>("UInt32", "INT32"),
            new KeyValuePair<string, string>("UInt64", "INT64"),
            new KeyValuePair<string, string>("SByte", "BYTE")
        });

        private ImmutableDictionary<string, string> PostgreSQLDbTypes = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("String", "VARCHAR"),
            new KeyValuePair<string, string>("Int16", "SMALLINT"),
            new KeyValuePair<string, string>("Int32", "INT"),
            new KeyValuePair<string, string>("Int64", "BIGINT"),
            new KeyValuePair<string, string>("Byte", "SMALLINT"),
            new KeyValuePair<string, string>("Decimal", "DECIMAL"),
            new KeyValuePair<string, string>("Boolean", "BOOLEAN"),
            new KeyValuePair<string, string>("Date", "DATE"),
            new KeyValuePair<string, string>("DateTime", "TIMESTAMP"),
            new KeyValuePair<string, string>("Double", "DOUBLE PRECISION"),
            new KeyValuePair<string, string>("Float", "REAL"),
            new KeyValuePair<string, string>("Xml", "XML"),
            new KeyValuePair<string, string>("Guid", "UUID"),
            new KeyValuePair<string, string>("Binary", "BYTEA"),
            new KeyValuePair<string, string>("Time", "TIME"),
            new KeyValuePair<string, string>("UInt16", "SMALLINT"),
            new KeyValuePair<string, string>("UInt32", "INT"),
            new KeyValuePair<string, string>("UInt64", "BIGINT"),
            new KeyValuePair<string, string>("SByte", "SMALLINT")
        });

        private ImmutableDictionary<string, string> MySQLDbTypes = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("String", "VARCHAR"),
            new KeyValuePair<string, string>("Int16", "SMALLINT"),
            new KeyValuePair<string, string>("Int32", "INT"),
            new KeyValuePair<string, string>("Int64", "BIGINT"),
            new KeyValuePair<string, string>("Byte", "TINYINT"),
            new KeyValuePair<string, string>("Decimal", "DECIMAL"),
            new KeyValuePair<string, string>("Boolean", "BOOLEAN"),
            new KeyValuePair<string, string>("Date", "DATE"),
            new KeyValuePair<string, string>("DateTime", "DATETIME"),
            new KeyValuePair<string, string>("Double", "DOUBLE"),
            new KeyValuePair<string, string>("Float", "FLOAT"),
            new KeyValuePair<string, string>("Xml", "XML"),
            new KeyValuePair<string, string>("Guid", "BINARY"),
            new KeyValuePair<string, string>("Binary", "BINARY"),
            new KeyValuePair<string, string>("Time", "TIME"),
            new KeyValuePair<string, string>("UInt16", "SMALLINT"),
            new KeyValuePair<string, string>("UInt32", "INT"),
            new KeyValuePair<string, string>("UInt64", "BIGINT"),
            new KeyValuePair<string, string>("SByte", "TINYINT")
        });

        private ImmutableDictionary<string, string> SQLiteDbTypes = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("String", "TEXT"),
            new KeyValuePair<string, string>("Int16", "INTEGER"),
            new KeyValuePair<string, string>("Int32", "INTEGER"),
            new KeyValuePair<string, string>("Int64", "INTEGER"),
            new KeyValuePair<string, string>("Byte", "INTEGER"),
            new KeyValuePair<string, string>("Decimal", "REAL"),
            new KeyValuePair<string, string>("Boolean", "INTEGER"),
            new KeyValuePair<string, string>("Date", "TEXT"),
            new KeyValuePair<string, string>("DateTime", "TEXT"),
            new KeyValuePair<string, string>("Double", "REAL"),
            new KeyValuePair<string, string>("Float", "REAL"),
            new KeyValuePair<string, string>("Xml", "TEXT"),
            new KeyValuePair<string, string>("Guid", "TEXT"),
            new KeyValuePair<string, string>("Binary", "BLOB"),
            new KeyValuePair<string, string>("Time", "TEXT"),
            new KeyValuePair<string, string>("UInt16", "INTEGER"),
            new KeyValuePair<string, string>("UInt32", "INTEGER"),
            new KeyValuePair<string, string>("UInt64", "INTEGER"),
            new KeyValuePair<string, string>("SByte", "INTEGER")
        });
    }
}
