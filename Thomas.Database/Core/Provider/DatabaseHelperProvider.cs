using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using Thomas.Database.Configuration;

namespace Thomas.Database.Core.Provider
{
    internal static partial class DatabaseHelperProvider
    {
        #region SqlServer

        internal static readonly Type SqlServerConnectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlServerCommandType = Type.GetType("Microsoft.Data.SqlClient.SqlCommand, Microsoft.Data.SqlClient")!;
        private static readonly Type SqlDbParameterCollectionType = Type.GetType("Microsoft.Data.SqlClient.SqlParameterCollection, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlDbParameterType = Type.GetType("Microsoft.Data.SqlClient.SqlParameter, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlDataReader = Type.GetType("Microsoft.Data.SqlClient.SqlDataReader, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlDbType = typeof(SqlDbType);
        private static readonly MethodInfo GetSqlParametersProperty = SqlServerCommandType?.GetProperty("Parameters", SqlDbParameterCollectionType)!.GetGetMethod()!;
        private static readonly MethodInfo AddSqlParameterMethod = SqlDbParameterCollectionType?.GetMethod("Add", new[] { SqlDbParameterType })!;
        internal static readonly ConstructorInfo SqlServerConnectionConstructor = SqlServerConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo SqlServerCommandConstructor = SqlServerCommandType?.GetConstructor(new Type[] { typeof(string), SqlServerConnectionType })!;
        private static readonly ConstructorInfo SqlParameterConstructor = SqlDbParameterType?.GetConstructor(new[] { typeof(string), SqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

        private static readonly IReadOnlyDictionary<Type, string> SqlTypes = new Dictionary<Type, string>
        {
                { typeof(string), "NVarChar"},
                { typeof(short), "SmallInt"},
                { typeof(int), "Int"},
                { typeof(long), "BigInt"},
                { typeof(byte), "TinyInt"},
                { typeof(decimal), "Decimal"},
                { typeof(double), "Float"},
                { typeof(float), "Float"},
                { typeof(bool), "Bit"},
                { typeof(ushort), "SmallInt"},
                { typeof(uint), "Int"},
                { typeof(ulong), "BigInt"},
                { typeof(DateTime), "DateTime"},
                { typeof(Guid), "UniqueIdentifier"},
                { typeof(SqlBinary), "Binary" },
#if NET6_0_OR_GREATER
                { typeof(DateOnly), "Date"},
                { typeof(TimeOnly), "Time"},
                { typeof(DateOnly?), "Date"},
                { typeof(TimeOnly?), "Time"},
#endif
                { typeof(sbyte), "TinyInt"},
                { typeof(byte[]), "Varbinary"},
                { typeof(TimeSpan), "Time"},
                { typeof(XmlDocument), "Xml"},
                { typeof(short?), "SmallInt"},
                { typeof(int?), "Int"},
                { typeof(long?), "BigInt"},
                { typeof(byte?), "TinyInt"},
                { typeof(decimal?), "Decimal"},
                { typeof(double?), "Float"},
                { typeof(float?), "Float"},
                { typeof(bool?), "Bit"},
                { typeof(DateTime?), "DateTime"},
                { typeof(Guid?), "UniqueIdentifier"},
                { typeof(SqlBinary?), "Binary"},
                { typeof(ushort?), "SmallInt"},
                { typeof(uint?), "Int"},
                { typeof(ulong?), "BigInt"},
                { typeof(sbyte?), "TinyInt"},
                { typeof(TimeSpan?), "Time"},
                { typeof(StringBuilder), "Text"}
          };

        #endregion SqlServer

        #region Oracle

        private static readonly Type OracleDbCommandType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleCommand, Oracle.ManagedDataAccess");
        internal static readonly Type OracleConnectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess");
        internal static readonly Type OracleDbParameterType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameter, Oracle.ManagedDataAccess");
        internal static readonly Type OracleDataReader = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDataReader, Oracle.ManagedDataAccess")!;
        internal static readonly PropertyInfo OracleValueParameterProperty = OracleDbParameterType?.GetProperty("Value");
        private static readonly Type OracleDbParameterCollectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameterCollection, Oracle.ManagedDataAccess");
        internal static readonly Type OracleDbType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDbType, Oracle.ManagedDataAccess");

        private static readonly MethodInfo GetOracleParametersProperty = OracleDbCommandType?.GetProperty("Parameters", OracleDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo AddOracleParameterMethod = OracleDbParameterCollectionType?.GetMethod("Add", new[] { OracleDbParameterType });
        internal static readonly ConstructorInfo OracleConnectionConstructor = OracleConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo OracleCommandConstructor = OracleDbCommandType?.GetConstructor(new Type[] { typeof(string), OracleConnectionType })!;
        internal static readonly ConstructorInfo OracleParameterConstructor = OracleDbParameterType?.GetConstructor(new[] { typeof(string), OracleDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;
        private static readonly MethodInfo OracleDbCommandBindByName = OracleDbCommandType?.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
        private static readonly MethodInfo OracleDbCommandInitialLONGFetchSize = OracleDbCommandType?.GetProperty("InitialLONGFetchSize", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
        private static readonly MethodInfo OracleDbCommandFetchSize = OracleDbCommandType?.GetProperty("FetchSize", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();

        private static readonly IReadOnlyDictionary<Type, string> OracleDbTypes = new Dictionary<Type, string>
        {
            { typeof(string), "VARCHAR2" },
            { typeof(short), "INT16" },
            { typeof(int), "INT32" },
            { typeof(long), "INT64" },
            { typeof(byte), "BYTE" },
            { typeof(decimal), "DECIMAL" },
            { typeof(bool), "INT32" },
            { typeof(double), "DOUBLE" },
            { typeof(float), "FLOAT" },
            { typeof(Guid), "RAW" },
            { typeof(ushort), "INT16" },
            { typeof(uint), "INT32" },
            { typeof(ulong), "INT64" },
            { typeof(sbyte), "BYTE" },
            { typeof(TimeSpan), "IntervalDS" },
            { typeof(short?), "INT16" },
            { typeof(int?), "INT32" },
            { typeof(long?), "INT64" },
            { typeof(byte?), "BYTE" },
            { typeof(decimal?), "DECIMAL" },
            { typeof(bool?), "INT32" },
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            { typeof(DateOnly), "DATE" },
            { typeof(DateOnly?), "DATE" },
#endif
            { typeof(DateTime), "DATE" },
            { typeof(DateTime?), "DATE" },
            { typeof(double?), "DOUBLE" },
            { typeof(float?), "FLOAT" },
            { typeof(Guid?), "RAW" },
            { typeof(ushort?), "INT16" },
            { typeof(uint?), "INT32" },
            { typeof(ulong?), "INT64" },
            { typeof(sbyte?), "BYTE" },
            { typeof(TimeSpan?), "IntervalDS" },
            { typeof(byte[]), "BLOB" },
            { typeof(XmlDocument), "XMLTYPE" },
            { typeof(StringBuilder), "CLOB" }
        };

        #endregion Oracle

        #region PostgreSql

        private static readonly Type PostgresDbCommandType = Type.GetType("Npgsql.NpgsqlCommand, Npgsql");
        internal static readonly Type PostgresConnectionType = Type.GetType("Npgsql.NpgsqlConnection, Npgsql");
        internal static readonly Type PostgresDbParameterType = Type.GetType("Npgsql.NpgsqlParameter, Npgsql");
        private static readonly Type PostgresDbParameterCollectionType = Type.GetType("Npgsql.NpgsqlParameterCollection, Npgsql");
        internal static readonly Type PostgresDataReader = Type.GetType("Npgsql.NpgsqlDataReader, Npgsql");
        private static readonly Type PostgresDbType = Type.GetType("NpgsqlTypes.NpgsqlDbType, Npgsql");

        private static readonly MethodInfo GetPostgresParametersProperty = PostgresDbCommandType?.GetProperty("Parameters", PostgresDbParameterCollectionType)?.GetGetMethod();
        private static readonly MethodInfo AddPostgresParameterMethod = PostgresDbParameterCollectionType?.GetMethod("Add", new[] { PostgresDbParameterType });
        internal static readonly ConstructorInfo PostgresConnectionConstructor = PostgresConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo PostgresCommandConstructor = PostgresDbCommandType?.GetConstructor(new Type[] { typeof(string), PostgresConnectionType })!;
        internal static readonly ConstructorInfo PostgresParameterConstructor = PostgresDbParameterType?.GetConstructor(new[] { typeof(string), PostgresDbType, typeof(int), typeof(string), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(DataRowVersion), typeof(object) });

        private static readonly IReadOnlyDictionary<Type, string> PostgresDbTypes = new Dictionary<Type, string>
        {
            { typeof(string), "VARCHAR" },
            { typeof(short), "SMALLINT" },
            { typeof(int), "INTEGER" },
            { typeof(long), "BIGINT" },
            { typeof(byte), "SMALLINT" },
            { typeof(decimal), "NUMERIC" },
            { typeof(bool), "BIT" },
            { typeof(DateTime), "TIMESTAMP" },
            { typeof(double), "DOUBLE" },
            { typeof(float), "REAL" },
            { typeof(Guid), "UUID" },
            { typeof(ushort), "SMALLINT" },
            { typeof(uint), "INTEGER" },
            { typeof(ulong), "BIGINT" },
            { typeof(sbyte), "SMALLINT" },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), "DATE" },
            { typeof(TimeOnly), "TIME" },
            { typeof(DateOnly?), "DATE" },
            { typeof(TimeOnly?), "TIME" },
#endif
            { typeof(short?), "SMALLINT" },
            { typeof(int?), "INT" },
            { typeof(long?), "BIGINT" },
            { typeof(byte?), "SMALLINT" },
            { typeof(decimal?), "NUMERIC" },
            { typeof(bool?), "BIT" },
            { typeof(DateTime?), "TIMESTAMP" },
            { typeof(double?), "DOUBLE" },
            { typeof(float?), "REAL" },
            { typeof(Guid?), "UUID" },
            { typeof(ushort?), "SMALLINT" },
            { typeof(uint?), "INT" },
            { typeof(ulong?), "BIGINT" },
            { typeof(sbyte?), "SMALLINT" },
            { typeof(XmlDocument), "XML" },
            { typeof(byte[]), "BYTEA" }
        };

        #endregion PostgreSql

        #region Mysql

        private static readonly Type MysqlDbCommandType = Type.GetType("MySql.Data.MySqlClient.MySqlCommand, MySql.Data");
        private static readonly Type MysqlDbType = Type.GetType("MySql.Data.MySqlClient.MySqlDbType, MySql.Data");
        internal static readonly Type MysqlConnectionType = Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data");
        internal static readonly Type MysqlDbParameterType = Type.GetType("MySql.Data.MySqlClient.MySqlParameter, MySql.Data");
        internal static readonly Type MysqlDataReader = Type.GetType("MySql.Data.MySqlClient.MySqlDataReader, MySql.Data");
        private static readonly Type MysqlDbParameterCollectionType = Type.GetType("MySql.Data.MySqlClient.MySqlParameterCollection, MySql.Data");

        private static readonly MethodInfo GetMysqlParametersProperty = MysqlDbCommandType?.GetProperty("Parameters", MysqlDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo AddMysqlParameterMethod = MysqlDbParameterCollectionType?.GetMethod("Add", new[] { MysqlDbParameterType });
        internal static readonly ConstructorInfo MysqlConnectionConstructor = MysqlConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo MysqlCommandConstructor = MysqlDbCommandType?.GetConstructor(new Type[] { typeof(string), MysqlConnectionType })!;
        internal static readonly ConstructorInfo MysqlParameterConstructor = MysqlDbParameterType?.GetConstructor(new[] { typeof(string), MysqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

        private readonly static IReadOnlyDictionary<Type, string> MySQLDbTypes = new Dictionary<Type, string>
        {
            { typeof(string), "VARCHAR" },
            { typeof(short), "INT16" },
            { typeof(int), "INT32" },
            { typeof(long), "INT64" },
            { typeof(byte), "BYTE" },
            { typeof(decimal), "DECIMAL" },
            { typeof(bool), "BIT" },
            { typeof(DateTime), "DATETIME" },
            { typeof(double), "DOUBLE" },
            { typeof(float), "FLOAT" },
            { typeof(Guid), "GUID" },
            { typeof(ushort), "INT16" },
            { typeof(uint), "INT32" },
            { typeof(ulong), "INT64" },
            { typeof(sbyte), "BYTE" },
            { typeof(short?), "INT16" },
            { typeof(int?), "INT32" },
            { typeof(long?), "INT64" },
            { typeof(byte?), "BYTE" },
            { typeof(decimal?), "DECIMAL" },
            { typeof(bool?), "BIT" },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), "DATE" },
            { typeof(TimeOnly), "TIME" },
            { typeof(DateOnly?), "DATE" },
            { typeof(TimeOnly?), "TIME" },
#endif
            { typeof(DateTime?), "DATETIME" },
            { typeof(double?), "DOUBLE" },
            { typeof(float?), "FLOAT" },
            { typeof(Guid?), "GUID" },
            { typeof(ushort?), "INT16" },
            { typeof(uint?), "INT32" },
            { typeof(ulong?), "INT64" },
            { typeof(sbyte?), "BYTE" },
            { typeof(byte[]), "MEDIUMBLOB" },
            { typeof(XmlDocument), "XML" }
        };

        #endregion Mysql

        #region Sqlite

        private static readonly Type SqliteDbCommandType = Type.GetType("Microsoft.Data.Sqlite.SqliteCommand, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteDbParameterType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameter, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteDataReader = Type.GetType("Microsoft.Data.Sqlite.SqliteDataReader, Microsoft.Data.Sqlite");
        private static readonly Type SqliteDbParameterCollectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameterCollection, Microsoft.Data.Sqlite");
        private static readonly MethodInfo GetSqliteParametersProperty = SqliteDbCommandType?.GetProperty("Parameters", SqliteDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo AddSqliteParameterMethod = SqliteDbParameterCollectionType?.GetMethod("Add", new[] { SqliteDbParameterType });
        internal static readonly ConstructorInfo SqliteConnectionConstructor = SqliteConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo SqliteCommandConstructor = SqliteDbCommandType?.GetConstructor(new Type[] { typeof(string), SqliteConnectionType })!;
        internal static readonly ConstructorInfo SqliteParameterConstructor = SqliteDbParameterType?.GetConstructor(new[] { typeof(string), typeof(object) })!;

        private static readonly MethodInfo GetParameterValue = typeof(DbParameter).GetProperty("Value").GetGetMethod()!;
        private static readonly MethodInfo GenericDbParametersCollection = typeof(DbCommand).GetProperty("Parameters").GetGetMethod()!;
        private static readonly MethodInfo GenericDbParameterCollectionIndex = typeof(DbParameterCollection).GetProperty("Item", new[] { typeof(string) }).GetGetMethod()!;

        #endregion Sqlite

        public static Action<object, DbCommand, DbDataReader> LoadOutParameterDelegate(in bool isExecuteNonQuery, in Type type, in DbParameterInfo[] parameters)
        {
            Type[] argTypes = { typeof(object), typeof(DbCommand), typeof(DbDataReader) };
            var method = new DynamicMethod(
                "LoadOutParameters" + InternalCounters.GetNextCommandHandlerCounter().ToString(),
                null,
                argTypes,
                type ?? typeof(DatabaseHelperProvider),
                true);

            var il = method.GetILGenerator();

            if (!isExecuteNonQuery)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("NextResult")!);
                il.Emit(OpCodes.Pop);
            }

            foreach (var parameter in parameters.Where(x => x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput))
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, GenericDbParametersCollection);
                il.Emit(OpCodes.Ldstr, parameter.Name);
                il.Emit(OpCodes.Callvirt, GenericDbParameterCollectionIndex);
                il.Emit(OpCodes.Callvirt, GetParameterValue);

                if (parameter.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, parameter.PropertyType);
                    il.Emit(OpCodes.Call, parameter.PropertyInfo.GetSetMethod(true)!);
                }
                else
                {
                    il.Emit(OpCodes.Callvirt, parameter.PropertyInfo.GetSetMethod(true)!);
                }
            }

            il.Emit(OpCodes.Ret);

            Type actionType = Expression.GetActionType(argTypes);

            return (Action<object, DbCommand, DbDataReader>)method.CreateDelegate(actionType);
        }

        /// <summary>
        /// Generate a delegate that return a command with values preloaded
        /// </summary>
        /// <param name="type">filter object</param>
        /// <param name="options">command configuration</param>
        /// <param name="hasOutputParameters">check if has output parameter the filter object</param>
        /// <param name="allParameters">list of parameter</param>
        /// <returns>instance of specific DbCommand provider with values preloaded</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Func<object, string, string, DbCommand, DbCommand> GetSetupCommandDelegate(in Type type, in LoaderConfiguration options, out bool hasOutputParameters, out DbParameterInfo[] allParameters)
        {
            hasOutputParameters = false;

            PropertyInfo[] properties = Array.Empty<PropertyInfo>();
            FluentApi.DbTable table = null;

            if (options.GenerateParameterWithKeys)
            {
                properties = new[] { DbConfigurationFactory.Tables[type.FullName].Key.Property };
            }
            else if (type != null)
            {
                if (DbConfigurationFactory.Tables.TryGetValue(type.FullName!, out table))
                {
                    if (options.KeyAsReturnValue)
                        properties = table.Columns.Where(x => !x.AutoGenerated).Select(x => x.Property).ToArray();
                    else
                        properties = table.Columns.Select(x => x.Property).ToArray();
                }
                else
                {
                    properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                }
            }

            int len = properties.Length + (options.KeyAsReturnValue ? 1 : 0) + (options.AdditionalOracleRefCursors?.Count ?? 0);

#if NETCOREAPP
            Span<DbParameterInfo> parameters = new DbParameterInfo[len];
#else
            var parameters = new DbParameterInfo[len];
#endif
            int counter = 0;
            foreach (var item in properties)
            {
                var attributes = GetDbParameterAttribute(in item);
                int enumValue = 0;

                if (options.Provider != SqlProvider.Sqlite)
                {
                    if (options.Provider == SqlProvider.Oracle && attributes.IsOracleCursor)
                    {
                        enumValue = 121; //OracleDbType.RefCursor
                        goto addParam;
                    }

                    enumValue = GetEnumValue(in options.Provider, item.PropertyType);
                }

            addParam:
                parameters[counter++] = new DbParameterInfo(
                    name: attributes.Name ?? item.Name,
                    bindName: null,
                    size: attributes.Size,
                    precision: attributes.Precision,
                    direction: attributes.Direction,
                    propertyInfo: item,
                    propertyType: null,
                    dbType: enumValue,
                    value: null);

                hasOutputParameters = attributes.Direction == ParameterDirection.Output || attributes.Direction == ParameterDirection.InputOutput || hasOutputParameters;
            }

            if (options.KeyAsReturnValue && table != null)
            {
                parameters[counter++] = new DbParameterInfo(
                    name: table.Key.Name,
                    bindName: null,
                    size: 0,
                    precision: 0,
                    direction: ParameterDirection.Output,
                    propertyInfo: table.Key.Property,
                    propertyType: table.Key.Property.PropertyType,
                    dbType: options.Provider == SqlProvider.Sqlite ? 0 : GetEnumValue(in options.Provider, table.Key.Property.PropertyType),
                    value: null);

                hasOutputParameters = true;
            }

            if (options.AdditionalOracleRefCursors?.Count > 0)
            {
                foreach (var item in options.AdditionalOracleRefCursors)
                {
                    parameters[counter++] = new DbParameterInfo(
                    name: item.Name,
                    bindName: null,
                    size: 0,
                    precision: 0,
                    direction: item.Direction,
                    propertyInfo: null,
                    propertyType: null,
                    dbType: item.DbType,
                    value: null);
                }
            }

            ConstructorInfo connectionConstructor = null;
            ConstructorInfo commandConstructor = null;
            Type dbconnectionType = null;
            Type dbCommandType = null;

            switch (options.Provider)
            {
                case SqlProvider.SqlServer:
                    commandConstructor = SqlServerCommandConstructor;
                    connectionConstructor = SqlServerConnectionConstructor;
                    dbconnectionType = SqlServerConnectionType;
                    dbCommandType = SqlServerCommandType;
                    break;
                case SqlProvider.MySql:
                    commandConstructor = MysqlCommandConstructor;
                    connectionConstructor = MysqlConnectionConstructor;
                    dbconnectionType = MysqlConnectionType;
                    dbCommandType = MysqlDbCommandType;
                    break;
                case SqlProvider.PostgreSql:
                    commandConstructor = PostgresCommandConstructor;
                    connectionConstructor = PostgresConnectionConstructor;
                    dbconnectionType = PostgresConnectionType;
                    dbCommandType = PostgresDbCommandType;
                    break;
                case SqlProvider.Oracle:
                    commandConstructor = OracleCommandConstructor;
                    connectionConstructor = OracleConnectionConstructor;
                    dbconnectionType = OracleConnectionType;
                    dbCommandType = OracleDbCommandType;
                    break;
                case SqlProvider.Sqlite:
                    commandConstructor = SqliteCommandConstructor;
                    connectionConstructor = SqliteConnectionConstructor;
                    dbconnectionType = SqliteConnectionType;
                    dbCommandType = SqliteDbCommandType;
                    break;
            }

            if (connectionConstructor == null)
                throw new NotImplementedException($"The provider {options.Provider} library was not found");

            var method = new DynamicMethod(
                "SetupCommand" + InternalCounters.GetNextCommandHandlerCounter().ToString(),
                typeof(DbCommand),
                new[] { typeof(object), typeof(string), typeof(string), typeof(DbCommand) },
                type ?? typeof(DatabaseHelperProvider),
                true);

            var il = method.GetILGenerator();

            LocalBuilder commandInstance = null;

            if (!options.IsTransactionOperation)
                SetupCommandBase(in il, in options, out commandInstance);

            SetupLoadParameter(in dbCommandType, in commandInstance, in il, parameters, in options);

            //set prepare statement
            if (options.ShouldPrepareStatement())
            {
                if (options.IsTransactionOperation)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Callvirt, typeof(DbCommand).GetMethod("Prepare"));
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, commandInstance);
                    il.Emit(OpCodes.Call, dbCommandType.GetMethod("Prepare"));
                }
            }

            if (options.IsTransactionOperation)
            {
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, commandInstance);
                il.Emit(OpCodes.Castclass, typeof(DbCommand));
                il.Emit(OpCodes.Ret);
            }

            Type funcType = Expression.GetFuncType(new [] { typeof(object), typeof(string), typeof(string), typeof(DbCommand), typeof(DbCommand) } );

#if NETFRAMEWORK
            allParameters = parameters;
#else
            allParameters = parameters.ToArray();
#endif
            return (Func<object, string, string, DbCommand, DbCommand>)method.CreateDelegate(funcType);
        }

        private const int DataRowVersionDefault = (int)DataRowVersion.Default;

        private static int GetEnumValue(in SqlProvider provider, in Type type)
        {
            var dbType = GetDbType(in provider);
            var dbTypes = DbTypes(in provider);

            if (!dbTypes.ContainsKey(type))
                throw new KeyNotFoundException($"{type.Name} key was no found on {provider} mapping");

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (Enum.TryParse(dbType, dbTypes[type], true, out var enumVal))
#else
            if (EnumExtensions.TryParse(dbType, dbTypes[type], true, out var enumVal))
#endif
                return (int)enumVal!;

           throw new NotSupportedException($"Conversion not supported from {type.Name} to Enum {dbTypes[type]} in {provider} mapping");
        }

        private static void SetupCommandBase(in ILGenerator il, in LoaderConfiguration options, out LocalBuilder commandInstance)
        {
            ConstructorInfo connectionConstructor = null;
            ConstructorInfo commandConstructor = null;
            Type dbconnectionType = null;
            Type dbCommandType = null;

            switch (options.Provider)
            {
                case SqlProvider.SqlServer:
                    commandConstructor = SqlServerCommandConstructor;
                    connectionConstructor = SqlServerConnectionConstructor;
                    dbconnectionType = SqlServerConnectionType;
                    dbCommandType = SqlServerCommandType;
                    break;
                case SqlProvider.MySql:
                    commandConstructor = MysqlCommandConstructor;
                    connectionConstructor = MysqlConnectionConstructor;
                    dbconnectionType = MysqlConnectionType;
                    dbCommandType = MysqlDbCommandType;
                    break;
                case SqlProvider.PostgreSql:
                    commandConstructor = PostgresCommandConstructor;
                    connectionConstructor = PostgresConnectionConstructor;
                    dbconnectionType = PostgresConnectionType;
                    dbCommandType = PostgresDbCommandType;
                    break;
                case SqlProvider.Oracle:
                    commandConstructor = OracleCommandConstructor;
                    connectionConstructor = OracleConnectionConstructor;
                    dbconnectionType = OracleConnectionType;
                    dbCommandType = OracleDbCommandType;
                    break;
                case SqlProvider.Sqlite:
                    commandConstructor = SqliteCommandConstructor;
                    connectionConstructor = SqliteConnectionConstructor;
                    dbconnectionType = SqliteConnectionType;
                    dbCommandType = SqliteDbCommandType;
                    break;
            }

            if (connectionConstructor == null)
                throw new NotImplementedException($"The provider {options.Provider} library was not found");

            //declare connection
            var connectionInstance = il.DeclareLocal(dbconnectionType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Newobj, connectionConstructor);
            il.Emit(OpCodes.Stloc, connectionInstance);

            commandInstance = il.DeclareLocal(dbCommandType);

            //instance command
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldloc, connectionInstance);
            il.Emit(OpCodes.Newobj, commandConstructor);
            il.Emit(OpCodes.Stloc, commandInstance);

            //set command type
            if (options.CommandType != CommandType.Text)
            {
                il.Emit(OpCodes.Ldloc, commandInstance);
                il.Emit(OpCodes.Ldc_I4, (int)options.CommandType);
                il.Emit(OpCodes.Callvirt, dbCommandType.GetProperty("CommandType").GetSetMethod(true));
            }

            //set timeout
            if (options.Timeout > 0)
            {
                il.Emit(OpCodes.Ldloc, commandInstance);
                il.Emit(OpCodes.Ldc_I4, options.Timeout);
                il.Emit(OpCodes.Callvirt, dbCommandType.GetProperty("CommandTimeout").GetSetMethod(true));
            }

            //useful values for oracle command
            if (options.Provider == SqlProvider.Oracle)
            {
                if (options.AdditionalOracleRefCursors == null)
                {
                    il.Emit(OpCodes.Ldloc, commandInstance);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.EmitCall(OpCodes.Callvirt, OracleDbCommandBindByName, null);
                }

                il.Emit(OpCodes.Ldloc, commandInstance);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.EmitCall(OpCodes.Callvirt, OracleDbCommandInitialLONGFetchSize, null);

                if (options.FetchSize >= 0)
                {
                    il.Emit(OpCodes.Ldloc, commandInstance);
                    il.Emit(OpCodes.Ldc_I8, options.FetchSize);
                    il.EmitCall(OpCodes.Callvirt, OracleDbCommandFetchSize, null);
                }
            }
        }

        /*Considerations:
            - Oracle require honor parameter order
         */
#if NETCOREAPP
        private static void SetupLoadParameter(in Type dbCommandType, in LocalBuilder commandInstance, in ILGenerator il, ReadOnlySpan<DbParameterInfo> parameters, in LoaderConfiguration options)
#else
        private static void SetupLoadParameter(in Type dbCommandType, in LocalBuilder commandInstance, in ILGenerator il, DbParameterInfo[] parameters, in LoaderConfiguration options)
#endif
        {
            if (parameters.Length == 0)
                return;

            MethodInfo parametersProperty = null;
            ConstructorInfo parametersConstructor = null;
            MethodInfo addParameterMethod = null;

            switch (options.Provider)
            {
                case SqlProvider.SqlServer:
                    parametersProperty = GetSqlParametersProperty;
                    parametersConstructor = SqlParameterConstructor;
                    addParameterMethod = AddSqlParameterMethod;
                    break;
                case SqlProvider.MySql:
                    parametersProperty = GetMysqlParametersProperty;
                    parametersConstructor = MysqlParameterConstructor;
                    addParameterMethod = AddMysqlParameterMethod;
                    break;
                case SqlProvider.PostgreSql:
                    parametersProperty = GetPostgresParametersProperty;
                    parametersConstructor = PostgresParameterConstructor;
                    addParameterMethod = AddPostgresParameterMethod;
                    break;
                case SqlProvider.Oracle:
                    parametersProperty = GetOracleParametersProperty;
                    parametersConstructor = OracleParameterConstructor;
                    addParameterMethod = AddOracleParameterMethod;
                    break;
                case SqlProvider.Sqlite:
                    parametersProperty = GetSqliteParametersProperty;
                    parametersConstructor = SqliteParameterConstructor;
                    addParameterMethod = AddSqliteParameterMethod;
                    break;
            }

            foreach (var parameter in parameters)
            {
                if (options.IsTransactionOperation)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Castclass, dbCommandType);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, commandInstance);
                }

                il.Emit(OpCodes.Callvirt, parametersProperty);

                if (options.Provider == SqlProvider.Sqlite)
                {
                    BuildSqliteParameter(in il, in parameter);
                }
                else if (options.Provider == SqlProvider.PostgreSql)
                {
                    BuildPostgresParameter(in il, in parameter);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, parameter.Name);
                    il.Emit(OpCodes.Ldc_I4, parameter.DbType);
                    il.Emit(OpCodes.Ldc_I4, parameter.Size);
                    il.Emit(OpCodes.Ldc_I4, (int)parameter.Direction);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ldc_I4, parameter.Precision);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldc_I4, DataRowVersionDefault);
                    il.Emit(OpCodes.Ldarg_0);

                    if (parameter.Direction == ParameterDirection.Output)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        EmitValue(in il, in parameter);
                    }

                    il.Emit(OpCodes.Newobj, parametersConstructor);
                    il.Emit(OpCodes.Callvirt, addParameterMethod);
                    il.Emit(OpCodes.Pop);
                }
            }
        }

        #region value getters

        private static bool ShouldValidateValue(string typeName) => typeName switch
        {
            "Nullable`1" => true,
            "String" => true,
            "DateTime" => true,
            "TimeSpan" => true,
            "Guid" => true,
            "StringBuilder" => true,
            "XmlDocument" => true,
            "Byte[]" => true,
            _ => false,
        };

        /*
        * handle default values as null (default) or compatible with DbType that way load parameters could be more fluent
        * recommended use null-able properties for a natural interpretation of null values
        */
        private static void EmitValue(in ILGenerator il, in DbParameterInfo parameter)
        {
            il.Emit(OpCodes.Call, parameter.PropertyInfo.GetGetMethod()!);

            if (ShouldValidateValue(parameter.PropertyType.Name))
            {
                var underlyingType = Nullable.GetUnderlyingType(parameter.PropertyType);

                MethodInfo valueGetter;
                if (underlyingType != null)
                {
                    valueGetter = typeof(DatabaseHelperProvider).GetMethod("GetValueGeneric", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(new[] { underlyingType });
                }
                else
                {
                    valueGetter = typeof(DatabaseHelperProvider).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { parameter.PropertyType }, null);
                }

                il.Emit(OpCodes.Call, valueGetter);
            }
            else if (parameter.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, parameter.PropertyType);
            }
        }

        private static object GetValue(DateTime value)
        {
            return DateTime.MinValue.Equals(value) ? (object)DBNull.Value : value;

        }

        private static object GetValue(TimeSpan value)
        {
            return TimeSpan.MinValue.Equals(value) ? (object)DBNull.Value : value;

        }

        private static object GetValue(byte[] value)
        {
            return value == null || value.Length == 0 ? (object)DBNull.Value : value;

        }

        private static object GetValueGeneric<T>(T? value) where T : struct
        {
            return value.HasValue ? value : (object)DBNull.Value;
        }

        private static object GetValue(Guid value)
        {
            return Guid.Empty.Equals(value) ? (object)DBNull.Value : value;
        }

        private static object GetValue(string value)
        {
            return value == null ? (object)DBNull.Value : value;
        }

        private static object GetValue(StringBuilder value)
        {
            return value == null || value.Length == 0 ? (object)DBNull.Value : value;
        }

        private static object GetValue(XmlDocument value)
        {
            return value == null ? (object)DBNull.Value : value;
        }

        #endregion

        private static void BuildPostgresParameter(in ILGenerator il, in DbParameterInfo parameter)
        {
            //parameterName: string
            //parameterType: NpgsqlTypes
            //size: Int32,
            //sourceColumn: string
            //direction: ParameterDirection
            //isNullable: Boolean
            //precision: Int32
            //scale: Int32
            //dataRowVersion: DataRowVersion
            //value: object;

            il.Emit(OpCodes.Ldstr, parameter.Name);
            il.Emit(OpCodes.Ldc_I4, parameter.DbType);
            il.Emit(OpCodes.Ldc_I4, parameter.Size);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldc_I4, (int)parameter.Direction);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldc_I4, parameter.Precision);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4, DataRowVersionDefault);
            il.Emit(OpCodes.Ldarg_0);

            if (parameter.Direction == ParameterDirection.Output)
            {
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                EmitValue(in il, in parameter);
            }

            il.Emit(OpCodes.Newobj, PostgresParameterConstructor);
            il.Emit(OpCodes.Callvirt, AddPostgresParameterMethod);
            il.Emit(OpCodes.Pop);
        }

        private static void BuildSqliteParameter(in ILGenerator il, in DbParameterInfo parameter)
        {
            //parameterName: string
            //value: object

            il.Emit(OpCodes.Ldstr, parameter.Name);
            il.Emit(OpCodes.Ldarg_0);

            if (parameter.Direction == ParameterDirection.Output)
            {
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                EmitValue(in il, in parameter);
            }

            il.Emit(OpCodes.Newobj, SqliteParameterConstructor);
            il.Emit(OpCodes.Callvirt, AddSqliteParameterMethod);
            il.Emit(OpCodes.Pop);
        }

        private static Type GetDbType(in SqlProvider provider) => provider switch
        {
            SqlProvider.SqlServer => SqlDbType,
            SqlProvider.Oracle => OracleDbType,
            SqlProvider.MySql => MysqlDbType,
            SqlProvider.Sqlite => null,
            SqlProvider.PostgreSql => PostgresDbType,
            _ => throw new NotImplementedException()
        };

        internal static IReadOnlyDictionary<Type, string> DbTypes(in SqlProvider provider)
        {
            return provider switch
            {
                SqlProvider.SqlServer => SqlTypes,
                SqlProvider.Oracle => OracleDbTypes,
                SqlProvider.MySql => MySQLDbTypes,
                SqlProvider.Sqlite => null,
                SqlProvider.PostgreSql => PostgresDbTypes,
                _ => throw new NotImplementedException()
            };
        }

        internal readonly struct LoaderConfiguration
        {
            public readonly bool PrepareStatements;
            public readonly bool IsTransactionOperation;
            public readonly bool IsExecuteNonQuery;
            public readonly bool KeyAsReturnValue;
            public readonly bool GenerateParameterWithKeys;
            public readonly SqlProvider Provider;
            public readonly int FetchSize;
            public readonly List<DbParameterInfo> AdditionalOracleRefCursors;

            public readonly string Query;
            public readonly int Timeout;
            public readonly CommandType CommandType;

            public LoaderConfiguration(in bool keyAsReturnValue, in bool generateParameterWithKeys, in List<DbParameterInfo> additionalOracleRefCursors, in SqlProvider provider, in int fetchSize, in bool isExecuteNonQuery, in string query, in int timeout, in CommandType commandType, in bool isTransactionOperation, in bool prepareStatements)
            {
                KeyAsReturnValue = keyAsReturnValue;
                GenerateParameterWithKeys = generateParameterWithKeys;
                AdditionalOracleRefCursors = additionalOracleRefCursors;
                IsTransactionOperation = isTransactionOperation;
                Provider = provider;
                FetchSize = fetchSize;
                IsExecuteNonQuery = isExecuteNonQuery;
                Query = query;
                Timeout = timeout;
                CommandType = commandType;
                PrepareStatements = prepareStatements;
            }

            internal readonly bool ShouldPrepareStatement()
            {
                return PrepareStatements && CommandType == CommandType.Text;
            }
        }
    }
}
