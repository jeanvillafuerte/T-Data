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
using TData.Configuration;
using TData.Core.Converters;
using TData.Helpers;
using static TData.Core.Provider.DatabaseProvider;
using Column = TData.Core.FluentApi.DbColumn;

namespace TData.Core.Provider
{
    internal static partial class DatabaseHelperProvider
    {
        internal static string OFFSET_PARAMETER = "offSet_tdata";
        internal static string PAGESIZE_PARAMETER = "pageSize_tdata";

        #region SqlServer

        internal static readonly Type SqlServerConnectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlServerCommandType = Type.GetType("Microsoft.Data.SqlClient.SqlCommand, Microsoft.Data.SqlClient")!;
        private static readonly Type SqlDbParameterCollectionType = Type.GetType("Microsoft.Data.SqlClient.SqlParameterCollection, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlParameterType = Type.GetType("Microsoft.Data.SqlClient.SqlParameter, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlDataReader = Type.GetType("Microsoft.Data.SqlClient.SqlDataReader, Microsoft.Data.SqlClient")!;
        internal static readonly Type SqlDbType = typeof(SqlDbType);
        private static readonly MethodInfo GetSqlParametersProperty = SqlServerCommandType?.GetProperty("Parameters", SqlDbParameterCollectionType)!.GetGetMethod()!;
        private static readonly MethodInfo AddSqlParameterMethod = SqlDbParameterCollectionType?.GetMethod("Add", new[] { SqlParameterType })!;
        internal static readonly ConstructorInfo SqlServerConnectionConstructor = SqlServerConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo SqlServerCommandConstructor = SqlServerCommandType?.GetConstructor(new Type[] { typeof(string), SqlServerConnectionType })!;
        private static readonly ConstructorInfo SqlParameterConstructor = SqlParameterType?.GetConstructor(new[] { typeof(string), SqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

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
#endif
                { typeof(sbyte), "TinyInt"},
                { typeof(byte[]), "Varbinary"},
                { typeof(TimeSpan), "Time"},
                { typeof(XmlDocument), "Xml"},
                { typeof(StringBuilder), "Text"}
          };

        #endregion SqlServer

        #region Oracle

        private static readonly Type OracleDbCommandType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleCommand, Oracle.ManagedDataAccess");
        internal static readonly Type OracleConnectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess");
        internal static readonly Type OracleParameterType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameter, Oracle.ManagedDataAccess");
        internal static readonly Type OracleDataReader = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDataReader, Oracle.ManagedDataAccess")!;
        internal static readonly PropertyInfo OracleValueParameterProperty = OracleParameterType?.GetProperty("Value");
        private static readonly Type OracleDbParameterCollectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameterCollection, Oracle.ManagedDataAccess");
        internal static readonly Type OracleDbType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDbType, Oracle.ManagedDataAccess");

        private static readonly MethodInfo GetOracleParametersProperty = OracleDbCommandType?.GetProperty("Parameters", OracleDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo GetOracleParameterCollectionIndex = OracleDbParameterCollectionType?.GetProperty("Item", new[] { typeof(string) }).GetGetMethod()!;
        private static readonly MethodInfo GetOracleParameterValue = OracleParameterType?.GetProperty("Value").GetGetMethod()!;
        private static readonly MethodInfo AddOracleParameterMethod = OracleDbParameterCollectionType?.GetMethod("Add", new[] { OracleParameterType });
        internal static readonly ConstructorInfo OracleConnectionConstructor = OracleConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo OracleCommandConstructor = OracleDbCommandType?.GetConstructor(new Type[] { typeof(string), OracleConnectionType })!;
        internal static readonly ConstructorInfo OracleParameterConstructor = OracleParameterType?.GetConstructor(new[] { typeof(string), OracleDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;
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
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            { typeof(DateOnly), "DATE" },
#endif
            { typeof(DateTime), "DATE" },
            { typeof(byte[]), "BLOB" },
            { typeof(XmlDocument), "XMLTYPE" },
            { typeof(StringBuilder), "CLOB" }
        };

        #endregion Oracle

        #region PostgreSql

        private static readonly Type PostgresCommandType = Type.GetType("Npgsql.NpgsqlCommand, Npgsql");
        internal static readonly Type PostgresConnectionType = Type.GetType("Npgsql.NpgsqlConnection, Npgsql");
        internal static readonly Type PostgresParameterType = Type.GetType("Npgsql.NpgsqlParameter, Npgsql");
        private static readonly Type PostgresDbParameterCollectionType = Type.GetType("Npgsql.NpgsqlParameterCollection, Npgsql");
        internal static readonly Type PostgresDataReader = Type.GetType("Npgsql.NpgsqlDataReader, Npgsql");
        private static readonly Type PostgresDbType = Type.GetType("NpgsqlTypes.NpgsqlDbType, Npgsql");

        private static readonly MethodInfo GetPostgresParametersProperty = PostgresCommandType?.GetProperty("Parameters", PostgresDbParameterCollectionType)?.GetGetMethod();
        private static readonly MethodInfo AddPostgresParameterMethod = PostgresDbParameterCollectionType?.GetMethod("Add", new[] { PostgresParameterType });
        internal static readonly ConstructorInfo PostgresConnectionConstructor = PostgresConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo PostgresCommandConstructor = PostgresCommandType?.GetConstructor(new Type[] { typeof(string), PostgresConnectionType })!;
        internal static readonly ConstructorInfo PostgresParameterConstructor = PostgresParameterType?.GetConstructor(new[] { typeof(string), PostgresDbType, typeof(int), typeof(string), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(DataRowVersion), typeof(object) });

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
            { typeof(TimeSpan), "INTERVAL" },
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
#endif
            { typeof(XmlDocument), "XML" },
            { typeof(byte[]), "BYTEA" },
            { typeof(StringBuilder), "TEXT" }
        };

        #endregion PostgreSql

        #region Mysql

        private static readonly Type MysqlDbCommandType = Type.GetType("MySql.Data.MySqlClient.MySqlCommand, MySql.Data");
        private static readonly Type MysqlDbType = Type.GetType("MySql.Data.MySqlClient.MySqlDbType, MySql.Data");
        internal static readonly Type MysqlConnectionType = Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data");
        internal static readonly Type MysqlParameterType = Type.GetType("MySql.Data.MySqlClient.MySqlParameter, MySql.Data");
        internal static readonly Type MysqlDataReader = Type.GetType("MySql.Data.MySqlClient.MySqlDataReader, MySql.Data");
        private static readonly Type MysqlDbParameterCollectionType = Type.GetType("MySql.Data.MySqlClient.MySqlParameterCollection, MySql.Data");

        private static readonly MethodInfo GetMysqlParametersProperty = MysqlDbCommandType?.GetProperty("Parameters", MysqlDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo AddMysqlParameterMethod = MysqlDbParameterCollectionType?.GetMethod("Add", new[] { MysqlParameterType });
        internal static readonly ConstructorInfo MysqlConnectionConstructor = MysqlConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo MysqlCommandConstructor = MysqlDbCommandType?.GetConstructor(new Type[] { typeof(string), MysqlConnectionType })!;
        internal static readonly ConstructorInfo MysqlParameterConstructor = MysqlParameterType?.GetConstructor(new[] { typeof(string), MysqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

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
            { typeof(TimeSpan), "TIME" },
            { typeof(double), "DOUBLE" },
            { typeof(float), "FLOAT" },
            { typeof(Guid), "GUID" },
            { typeof(ushort), "INT16" },
            { typeof(uint), "INT32" },
            { typeof(ulong), "INT64" },
            { typeof(sbyte), "BYTE" },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), "DATE" },
            { typeof(TimeOnly), "TIME" },
#endif
            { typeof(byte[]), "MEDIUMBLOB" },
            { typeof(XmlDocument), "XML" }
        };

        #endregion Mysql

        #region Sqlite

        private static readonly Type SqliteDbCommandType = Type.GetType("Microsoft.Data.Sqlite.SqliteCommand, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteParameterType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameter, Microsoft.Data.Sqlite");
        internal static readonly Type SqliteDataReader = Type.GetType("Microsoft.Data.Sqlite.SqliteDataReader, Microsoft.Data.Sqlite");
        private static readonly Type SqliteDbParameterCollectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameterCollection, Microsoft.Data.Sqlite");
        private static readonly MethodInfo GetSqliteParametersProperty = SqliteDbCommandType?.GetProperty("Parameters", SqliteDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo AddSqliteParameterMethod = SqliteDbParameterCollectionType?.GetMethod("Add", new[] { SqliteParameterType });
        internal static readonly ConstructorInfo SqliteConnectionConstructor = SqliteConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo SqliteCommandConstructor = SqliteDbCommandType?.GetConstructor(new Type[] { typeof(string), SqliteConnectionType })!;
        internal static readonly ConstructorInfo SqliteParameterConstructor = SqliteParameterType?.GetConstructor(new[] { typeof(string), typeof(object) })!;

        private static readonly MethodInfo GetParameterValue = typeof(DbParameter).GetProperty("Value").GetGetMethod()!;
        private static readonly MethodInfo GenericDbParametersCollection = typeof(DbCommand).GetProperty("Parameters").GetGetMethod()!;
        private static readonly MethodInfo GenericDbParameterCollectionIndex = typeof(DbParameterCollection).GetProperty("Item", new[] { typeof(string) }).GetGetMethod()!;

        #endregion Sqlite

        internal static Action<object, DbCommand, DbDataReader> LoadOutParameterDelegate(in bool isExecuteNonQuery, in Type type, in DbParameterInfo[] parameters, in SqlProvider provider)
        {
            Type[] argTypes = { typeof(object), typeof(DbCommand), typeof(DbDataReader) };
            var method = new DynamicMethod(
                "LoadOutParameters" + InternalCounters.GetNextCommandHandlerCounter().ToString(),
                null,
                argTypes,
                type ?? typeof(DatabaseHelperProvider),
                true);

            var il = method.GetILGenerator(64);

            if (!isExecuteNonQuery)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("NextResult")!);
                il.Emit(OpCodes.Pop);
            }

            foreach (var parameter in parameters.Where(x => x.IsOutput))
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);

                if (provider == SqlProvider.Oracle)
                {
                    il.Emit(OpCodes.Castclass, OracleDbCommandType);
                    il.Emit(OpCodes.Call, GetOracleParametersProperty);
                    il.Emit(OpCodes.Ldstr, parameter.Name);
                    il.Emit(OpCodes.Call, GetOracleParameterCollectionIndex);

                    //pass the type to convert the value
                    il.Emit(OpCodes.Ldtoken, parameter.PropertyType);
                    il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

                    il.Emit(OpCodes.Call, typeof(DatabaseProvider).GetMethod(nameof(DatabaseProvider.GetValueFromOracleParameter), BindingFlags.Static | BindingFlags.NonPublic));
                }
                else
                {
                    il.Emit(OpCodes.Callvirt, GenericDbParametersCollection);
                    il.Emit(OpCodes.Ldstr, parameter.Name);
                    il.Emit(OpCodes.Callvirt, GenericDbParameterCollectionIndex);
                    il.Emit(OpCodes.Callvirt, GetParameterValue);
                }

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
        internal static Delegate GetSetupCommandDelegate(in bool isSingleType, in Type type, in LoaderConfiguration options, in bool addPagingParameters, out bool hasOutputParameters, ref DbParameterInfo[] allParameters)
        {
            hasOutputParameters = false;

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
                    dbCommandType = PostgresCommandType;
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
                throw new DBProviderNotFoundException($"The provider {options.Provider} library was not found");

            Type mainInputType = isSingleType ? typeof(object) : typeof(object[]);

            var method = new DynamicMethod(
                "SetupCommand" + InternalCounters.GetNextCommandHandlerCounter().ToString(),
                typeof(DbCommand),
                new[] { mainInputType.MakeByRefType(), 
                        typeof(string).MakeByRefType(),
                        typeof(string).MakeByRefType(), 
                        typeof(DbCommand).MakeByRefType() },
                type ?? typeof(DatabaseHelperProvider),
                true);

            var il = method.GetILGenerator(64);

            LocalBuilder commandInstance = null;
            LocalBuilder connectionInstance = null;

            if (!options.IsTransactionOperation)
                SetupCommandBase(in il, in options, out connectionInstance, out commandInstance);

            if (allParameters != null && allParameters.Length > 0)
            {
                SetupLoadParameter(in dbCommandType, in commandInstance, in il, allParameters, in options, true);
            }
            else
            {
                var parameters = new LinkedList<DbParameterInfo>();

                if (type != null)
                {
                    Column[] properties = Array.Empty<Column>();
                    FluentApi.DbTable table = null;

                    if (options.GenerateParameterWithKeys)
                    {
                        properties = new[] { DbConfig.Tables[type.FullName].Key };
                    }
                    else if (type != null)
                    {
                        if (DbConfig.Tables.TryGetValue(type.FullName!, out table))
                        {
                            if (options.SkipAutoGeneratedColumn)
                                properties = table.Columns.Where(x => !x.AutoGenerated).Select(x => x).ToArray();
                            else
                                properties = table.Columns.Select(x => x).ToArray();
                        }
                        else
                        {
                            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => new Column() { Property = x }).ToArray();
                        }
                    }

                    foreach (var item in properties)
                    {
                        int enumValue = 0;
                        Type effectiveType = Nullable.GetUnderlyingType(item.Property.PropertyType) ?? item.Property.PropertyType;
                        Attributes.DbParameterAttribute attributes = null;
                        IInParameterValueConverter typeConverter = null;

                        if (item.Configurated)
                        {
                            var dbType = GetDbType(in options.Provider);

                            if (!string.IsNullOrEmpty(item.DbType))
                            {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                                if (Enum.TryParse(dbType, item.DbType, true, out var enumVal))
#else
                            if (EnumExtensions.TryParse(dbType, item.DbType, true, out var enumVal))
#endif
                                    enumValue = (int)enumVal!;
                                else
                                    throw new NotSupportedException($"Configured column {table.Name}.{item.Name} has an invalid DbType {item.DbType}");

                            }

                            var precision = item.Precision == 0 && effectiveType == typeof(decimal) ? 18 : item.Precision;
                            var scale = item.Scale == 0 && effectiveType == typeof(decimal) ? 6 : item.Scale;
                            attributes = new Attributes.DbParameterAttribute
                            {
                                Name = item.Name,
                                Precision = (byte)precision,
                                Scale = (byte)scale,
                                Size = (byte)item.Size,
                            };
                        }
                        else
                        {
                            attributes = GetDbParameterAttribute(item.Property);
                        }

                        if (!attributes.IsOracleCursor)
                            hasOutputParameters = attributes.IsOutput || hasOutputParameters;

                        if (enumValue == 0 && options.Provider != SqlProvider.Sqlite)
                        {
                            if (options.Provider == SqlProvider.Oracle && attributes.IsOracleCursor)
                            {
                                enumValue = 121; //OracleDbType.RefCursor
                                goto addParam;
                            }

                            enumValue = GetEnumValue(in options.Provider, effectiveType);
                        }

                        if (TypeConversionRegistry.TryGetInParameterConverter(options.Provider, effectiveType, out typeConverter))
                        {
                            if (options.Provider != SqlProvider.Sqlite && !(options.Provider == SqlProvider.Oracle && effectiveType == typeof(Guid)))
                                enumValue = GetEnumValue(in options.Provider, typeConverter.TargetType);
                        }

                    addParam:
                        parameters.AddLast(new DbParameterInfo(
                            name: attributes.Name ?? item.Property.Name,
                            bindName: null,
                            size: attributes.Size,
                            precision: attributes.Precision,
                            scale: attributes.Scale,
                            direction: attributes.Direction,
                            propertyInfo: item.Property,
                            propertyType: null,
                            dbType: enumValue,
                            value: null,
                            converter: typeConverter));
                    }

                    if (options.KeyAsReturnValue && options.Provider == SqlProvider.Oracle && table != null)
                    {
                        parameters.AddLast(new DbParameterInfo(
                            name: table.Key.Name,
                            bindName: null,
                            size: 0,
                            precision: 0,
                            scale: 0,
                            direction: ParameterDirection.Output,
                            propertyInfo: table.Key.Property,
                            propertyType: table.Key.Property.PropertyType,
                            dbType: options.Provider == SqlProvider.Sqlite ? 0 : GetEnumValue(in options.Provider, table.Key.Property.PropertyType),
                            value: null,
                            converter: null));

                        hasOutputParameters = true;
                    }
                }

                if (options.AdditionalOracleRefCursors > 0)
                {
                    for (int i = 1; i <= options.AdditionalOracleRefCursors; i++)
                    {
                        parameters.AddLast(new DbParameterInfo(
                            name: $"C_CURSOR{i}",
                            bindName: null,
                            size: 0,
                            precision: 0,
                            scale: 0,
                            direction: ParameterDirection.Output,
                            propertyInfo: null,
                            propertyType: null,
                            dbType: 121,
                            value: null,
                            converter: null));
                    }
                }

                if (addPagingParameters)
                {
                    var integerType = GetEnumValue(in options.Provider, typeof(int));

                    parameters.AddLast(new DbParameterInfo(
                       name: OFFSET_PARAMETER,
                       bindName: null,
                       size: 0,
                       precision: 0,
                       scale: 0,
                       direction: ParameterDirection.Input,
                       propertyInfo: null,
                       propertyType: null,
                       dbType: integerType,
                       value: null,
                       converter: null,
                       pagingParameter: true));

                    parameters.AddLast(new DbParameterInfo(
                       name: PAGESIZE_PARAMETER,
                       bindName: null,
                       size: 0,
                       precision: 0,
                       scale: 0,
                       direction: ParameterDirection.Input,
                       propertyInfo: null,
                       propertyType: null,
                       dbType: integerType,
                       value: null,
                       converter: null,
                       pagingParameter: true));
                }

                allParameters = parameters.Count == 0 ? Array.Empty<DbParameterInfo>() : parameters.ToArray();

                if (allParameters.Length > 0)
                    SetupLoadParameter(in dbCommandType, in commandInstance, in il, allParameters, in options, false);
            }

            PrepareStatement(in options, in il, in connectionInstance, in commandInstance, in dbCommandType, in dbconnectionType);

            //return command
            if (options.IsTransactionOperation)
            {
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldind_Ref);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldloc_1, commandInstance);
                il.Emit(OpCodes.Castclass, typeof(DbCommand));
                il.Emit(OpCodes.Ret);
            }

            Type delegateType = isSingleType ? typeof(ConfigureCommandDelegate) : typeof(ConfigureCommandDelegate2);

            return method.CreateDelegate(delegateType);
        }

        private const int DataRowVersionDefault = (int)DataRowVersion.Default;

        private static void PrepareStatement(in LoaderConfiguration options, in ILGenerator il, in LocalBuilder connectionInstance, in LocalBuilder commandInstance, in Type dbCommandType, in Type dbConnectionType)
        {
            if (options.ShouldPrepareStatement())
            {
                OpenConnection(in options, in il, in connectionInstance, in dbConnectionType);
                PrepareStatementInternal(in options, in il, in commandInstance, in dbCommandType);
            }
            else
            {
                OpenConnection(in options, in il, in connectionInstance, in dbConnectionType);
            }

            static void PrepareStatementInternal(in LoaderConfiguration options, in ILGenerator il, in LocalBuilder commandInstance, in Type dbCommandType)
            {

               if (options.IsTransactionOperation)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Callvirt, typeof(DbCommand).GetMethod("Prepare"));
                }
                else
                {
                    il.Emit(OpCodes.Ldloc_1, commandInstance);
                    il.Emit(OpCodes.Call, dbCommandType.GetMethod("Prepare"));
                }
            }

            static void OpenConnection(in LoaderConfiguration options, in ILGenerator il, in LocalBuilder connectionInstance, in Type dbConnectionType)
            {
                if (!options.IsTransactionOperation)
                {
                    il.Emit(OpCodes.Ldloc_0, connectionInstance);
                    il.Emit(OpCodes.Call, dbConnectionType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));
                }
            }
        }

        internal static int GetEnumValue(in SqlProvider provider, in Type type)
        {
            if (provider == SqlProvider.Sqlite)
                return 0;

            Type underlyingType = Nullable.GetUnderlyingType(type);

            var dbType = GetDbType(in provider);
            var dbTypes = DbTypes(in provider);

            if (!dbTypes.ContainsKey(underlyingType ?? type))
                throw new KeyNotFoundException($"The type '{type.Name}' is not supported by the '{provider}' provider in this library. " +
                                                "Please check the documentation for supported types or ensure that the type is correctly mapped.");

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (Enum.TryParse(dbType, dbTypes[underlyingType ?? type], true, out var enumVal))
#else
            if (EnumExtensions.TryParse(dbType, dbTypes[type], true, out var enumVal))
#endif
                return (int)enumVal!;

           throw new NotSupportedException($"Conversion not supported from {type.Name} to Enum {dbTypes[type]} in {provider} mapping");
        }

        private static void SetupCommandBase(in ILGenerator il, in LoaderConfiguration options, out LocalBuilder connectionInstance, out LocalBuilder commandInstance)
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
                    dbCommandType = PostgresCommandType;
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
            connectionInstance = il.DeclareLocal(dbconnectionType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Newobj, connectionConstructor);
            il.Emit(OpCodes.Stloc_0, connectionInstance);

            commandInstance = il.DeclareLocal(dbCommandType);

            //instance command
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Ldloc_0, connectionInstance);
            il.Emit(OpCodes.Newobj, commandConstructor);
            il.Emit(OpCodes.Stloc_1, commandInstance);

            //set command type
            if (options.CommandType != CommandType.Text)
            {
                il.Emit(OpCodes.Ldloc_1, commandInstance);
                ReflectionEmitHelper.EmitInt32Value(in il, (int)options.CommandType);
                il.Emit(OpCodes.Callvirt, dbCommandType.GetProperty("CommandType").GetSetMethod(true));
            }

            //set timeout
            if (options.Timeout > 0)
            {
                il.Emit(OpCodes.Ldloc_1, commandInstance);
                ReflectionEmitHelper.EmitInt32Value(in il, in options.Timeout);
                il.Emit(OpCodes.Callvirt, dbCommandType.GetProperty("CommandTimeout").GetSetMethod(true));
            }

            //useful values for oracle command
            if (options.Provider == SqlProvider.Oracle)
            {
                if (options.AdditionalOracleRefCursors == 0)
                {
                    il.Emit(OpCodes.Ldloc_1, commandInstance);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.EmitCall(OpCodes.Callvirt, OracleDbCommandBindByName, null);
                }

                il.Emit(OpCodes.Ldloc_1, commandInstance);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.EmitCall(OpCodes.Callvirt, OracleDbCommandInitialLONGFetchSize, null);

                if (options.FetchSize > 0)
                {
                    il.Emit(OpCodes.Ldloc_1, commandInstance);
                    il.Emit(OpCodes.Ldc_I8, (long)options.FetchSize);
                    il.EmitCall(OpCodes.Call, OracleDbCommandFetchSize, null);
                }
            }
        }

        /*Considerations:
            - Oracle require honor parameter order
         */
#if NETCOREAPP
        private static void SetupLoadParameter(in Type dbCommandType, in LocalBuilder commandInstance, in ILGenerator il, ReadOnlySpan<DbParameterInfo> parameters, in LoaderConfiguration options, bool isExpressionRequest)
#else
        private static void SetupLoadParameter(in Type dbCommandType, in LocalBuilder commandInstance, in ILGenerator il, DbParameterInfo[] parameters, in LoaderConfiguration options, bool isExpressionRequest)
#endif
        {
            MethodInfo parametersProperty = null;
            ConstructorInfo parameterConstructor = null;
            MethodInfo addParameterMethod = null;
            Type dbParameterType = null;

            switch (options.Provider)
            {
                case SqlProvider.SqlServer:
                    dbParameterType = SqlParameterType;
                    parametersProperty = GetSqlParametersProperty;
                    parameterConstructor = SqlParameterConstructor;
                    addParameterMethod = AddSqlParameterMethod;
                    break;
                case SqlProvider.MySql:
                    dbParameterType = MysqlParameterType;
                    parametersProperty = GetMysqlParametersProperty;
                    parameterConstructor = MysqlParameterConstructor;
                    addParameterMethod = AddMysqlParameterMethod;
                    break;
                case SqlProvider.PostgreSql:
                    dbParameterType = PostgresParameterType;
                    parametersProperty = GetPostgresParametersProperty;
                    parameterConstructor = PostgresParameterConstructor;
                    addParameterMethod = AddPostgresParameterMethod;
                    break;
                case SqlProvider.Oracle:
                    dbParameterType = OracleParameterType;
                    parametersProperty = GetOracleParametersProperty;
                    parameterConstructor = OracleParameterConstructor;
                    addParameterMethod = AddOracleParameterMethod;
                    break;
                case SqlProvider.Sqlite:
                    dbParameterType = SqliteParameterType;
                    parametersProperty = GetSqliteParametersProperty;
                    parameterConstructor = SqliteParameterConstructor;
                    addParameterMethod = AddSqliteParameterMethod;
                    break;
            }

            int index = 0;
            int offsetIndex = options.IsTransactionOperation ? 0 : 1; //skip the connection and command parameters index
            foreach (var parameter in parameters)
            {
                LocalBuilder localValue = null;
                LocalBuilder convertedValue = null;

                int valueLocalIndex = 0;
                int convertedValueLocalIndex = 0;

                if (parameter.IsInput)
                {
                    valueLocalIndex = index == 0 && !options.IsTransactionOperation ? offsetIndex + 1 : offsetIndex;
                    localValue = il.DeclareLocal(typeof(object));

                    if (parameter.PagingParameter)
                    {
                        //emit zero
                        il.Emit(OpCodes.Ldc_I4_0);
                    }
                    else
                    {
                        EmitValue(in il, in parameter, in isExpressionRequest, in index);
                    }

                    il.Emit(ReflectionEmitHelper.GetStoreLocalValueOpCode(in valueLocalIndex), localValue);

                    if (parameter.Converter != null)
                    {
                        var isNullableType = Nullable.GetUnderlyingType(parameter.PropertyType) != null;
                        var convertNotNull = il.DefineLabel();

                        if (isNullableType)
                        {
                           il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in valueLocalIndex), localValue);
                           il.Emit(OpCodes.Ldnull);
                           il.Emit(OpCodes.Ceq);
                           il.Emit(OpCodes.Brfalse_S, convertNotNull);
                        }

                        var converterType = parameter.Converter.GetType();
                        var localConverter = il.DeclareLocal(converterType);
                        var constructorInfo = converterType.GetConstructor(Type.EmptyTypes);
                        il.Emit(OpCodes.Newobj, constructorInfo);
                        var convertedIndex = valueLocalIndex + 1;
                        il.Emit(ReflectionEmitHelper.GetStoreLocalValueOpCode(in convertedIndex), localConverter);

                        il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in convertedIndex), localConverter);
                        il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in valueLocalIndex), localValue);
                        il.Emit(OpCodes.Callvirt, GetMethodInfoConverterClass(in converterType));

                        //declare convertedValue variable
                        convertedValueLocalIndex = convertedIndex + 1;
                        convertedValue = il.DeclareLocal(typeof(object));
                        il.Emit(ReflectionEmitHelper.GetStoreLocalValueOpCode(in convertedValueLocalIndex), convertedValue);

                        //close if
                        if (isNullableType)
                        {
                            il.MarkLabel(convertNotNull);

                            //if false load DbNull.Value into the convertedValue
                            il.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value"));
                            il.Emit(ReflectionEmitHelper.GetStoreLocalValueOpCode(in convertedValueLocalIndex), convertedValue);
                        }
                    }
                }

                var effectiveLocalIndex = convertedValueLocalIndex > 0 ? convertedValueLocalIndex : valueLocalIndex;
                var effectiveLocal = convertedValue ?? localValue;
                if (options.Provider == SqlProvider.Sqlite)
                {
                    BuildSqliteParameter(in il, in parameter, in effectiveLocal, in effectiveLocalIndex);
                }
                else if (options.Provider == SqlProvider.PostgreSql)
                {
                    BuildPostgresParameter(in il, in parameter, in effectiveLocal, in effectiveLocalIndex);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, parameter.Name);
                    ReflectionEmitHelper.EmitInt32Value(in il, in parameter.DbType);
                    ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Size);
                    ReflectionEmitHelper.EmitInt32Value(in il, (int)parameter.Direction);
                    il.Emit(OpCodes.Ldc_I4_1);
                    ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Precision);
                    ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Scale);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldc_I4, DataRowVersionDefault);

                    if (parameter.Direction == ParameterDirection.Output ||parameter.Direction == ParameterDirection.ReturnValue)
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in effectiveLocalIndex), effectiveLocal);
                    }
                }

                il.Emit(OpCodes.Newobj, parameterConstructor);

                var dbParameterInstance = il.DeclareLocal(dbParameterType);
                int parameterLocalIndex = (effectiveLocalIndex == 0 ? offsetIndex : effectiveLocalIndex) + 1;
                il.Emit(ReflectionEmitHelper.GetStoreLocalValueOpCode(in parameterLocalIndex), dbParameterInstance);
                

                if (options.IsTransactionOperation)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Castclass, dbCommandType);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc_1, commandInstance);
                }

                il.Emit(OpCodes.Callvirt, parametersProperty);
                il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in parameterLocalIndex), dbParameterInstance);
                il.Emit(OpCodes.Callvirt, addParameterMethod);
                il.Emit(OpCodes.Pop);
                offsetIndex = parameterLocalIndex + 1;
                index++;
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
        private static void EmitValue(in ILGenerator il, in DbParameterInfo parameter, in bool isExpressionRequest, in int localIndex)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_Ref);

            if (isExpressionRequest)
            {
                ReflectionEmitHelper.EmitInt32Value(in il, in localIndex);
                il.Emit(OpCodes.Ldelem_Ref);

                //given is an object[] need to cast values
                if (parameter.PropertyType.IsValueType)
                {
                    var underlyingType = Nullable.GetUnderlyingType(parameter.PropertyType);
                    if (underlyingType != null)
                        il.Emit(OpCodes.Unbox_Any, underlyingType);
                    else
                        il.Emit(OpCodes.Unbox_Any, parameter.PropertyType);
                }
            }
            else
                il.Emit(OpCodes.Call, parameter.PropertyInfo.GetGetMethod()!);

            if (ShouldValidateValue(parameter.PropertyType.Name))
            {
                var underlyingType = Nullable.GetUnderlyingType(parameter.PropertyType);

                MethodInfo valueGetter;
                if (underlyingType != null)
                {
                    valueGetter = typeof(DatabaseHelperProvider).GetMethod(nameof(GetValueGeneric), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(new[] { underlyingType });
                }
                else
                {
                    valueGetter = typeof(DatabaseHelperProvider).GetMethod(nameof(GetValue), BindingFlags.NonPublic | BindingFlags.Static, null, new[] { parameter.PropertyType }, null);
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
            return value.HasValue ? value.Value : (object)DBNull.Value;
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

        private static void BuildPostgresParameter(in ILGenerator il, in DbParameterInfo parameter, in LocalBuilder value, in int effectiveLocalIndex)
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

            //ignore output parameters
            if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.ReturnValue)
                return;

            il.Emit(OpCodes.Ldstr, $"@{parameter.Name.ToLower()}");
            ReflectionEmitHelper.EmitInt32Value(in il, in parameter.DbType);
            ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Size);
            il.Emit(OpCodes.Ldnull);
            ReflectionEmitHelper.EmitInt32Value(in il, (int)parameter.Direction);
            il.Emit(OpCodes.Ldc_I4_1);
            ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Precision);
            ReflectionEmitHelper.EmitInt32Value(in il, in parameter.Scale);
            il.Emit(OpCodes.Ldc_I4, DataRowVersionDefault);
            il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in effectiveLocalIndex), value);
        }

        private static void BuildSqliteParameter(in ILGenerator il, in DbParameterInfo parameter, in LocalBuilder value, in int effectiveLocalIndex)
        {
            //parameterName: string
            //value: object

            il.Emit(OpCodes.Ldstr, parameter.Name);

            if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.ReturnValue)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
               il.Emit(ReflectionEmitHelper.GetLoadLocalValueOpCode(in effectiveLocalIndex), value);
            }
        }

        public static MethodInfo GetMethodInfoConverterClass(in Type type)
        {
            if (!typeof(IInParameterValueConverter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.Name} does not implement {typeof(IInParameterValueConverter).Name}");
            }

            InterfaceMapping interfaceMapping = type.GetInterfaceMap(typeof(IInParameterValueConverter));

            for (int i = 0; i < interfaceMapping.InterfaceMethods.Length; i++)
            {
                if (interfaceMapping.InterfaceMethods[i].Name.Equals("ConvertInValue"))
                {
                    return interfaceMapping.TargetMethods[i];
                }
            }

            throw new NotImplementedException();
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

    }

    internal readonly struct LoaderConfiguration
    {
        public readonly bool CanPrepareStatement;
        public readonly bool PrepareStatements;
        public readonly bool IsTransactionOperation;
        public readonly bool KeyAsReturnValue;
        public readonly bool SkipAutoGeneratedColumn;
        public readonly bool GenerateParameterWithKeys;
        public readonly SqlProvider Provider;
        public readonly int FetchSize;
        public readonly int AdditionalOracleRefCursors;
        public readonly bool ShouldIncludeSequentialBehavior;
        public readonly int Timeout;
        public readonly CommandType CommandType;

        public LoaderConfiguration(in bool keyAsReturnValue, in bool skipAutoGeneratedColumn, in bool generateParameterWithKeys, in int additionalOracleRefCursors, in SqlProvider provider, in int fetchSize, in int timeout, in CommandType commandType, in bool isTransactionOperation, in bool prepareStatements, in bool canPrepareStatement, in bool shouldIncludeSequentialBehavior)
        {
            KeyAsReturnValue = keyAsReturnValue;
            SkipAutoGeneratedColumn = skipAutoGeneratedColumn;
            GenerateParameterWithKeys = generateParameterWithKeys;
            AdditionalOracleRefCursors = additionalOracleRefCursors;
            IsTransactionOperation = isTransactionOperation;
            Provider = provider;
            FetchSize = fetchSize;
            Timeout = timeout;
            CommandType = commandType;
            PrepareStatements = prepareStatements;
            CanPrepareStatement = canPrepareStatement;
            ShouldIncludeSequentialBehavior = shouldIncludeSequentialBehavior;
        }

        internal readonly bool ShouldPrepareStatement()
        {
            return CanPrepareStatement && PrepareStatements && CommandType == CommandType.Text;
        }
    }
}
