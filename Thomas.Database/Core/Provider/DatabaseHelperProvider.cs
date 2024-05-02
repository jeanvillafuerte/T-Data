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

        private static readonly Type? SqlServerConnectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient")!;
        private static readonly Type? SqlDbCommandType = Type.GetType("Microsoft.Data.SqlClient.SqlCommand, Microsoft.Data.SqlClient")!;
        internal static readonly Type? SqlDbParameterType = Type.GetType("Microsoft.Data.SqlClient.SqlParameter, Microsoft.Data.SqlClient")!;
        private static readonly Type? SqlDbParameterCollectionType = Type.GetType("Microsoft.Data.SqlClient.SqlParameterCollection, Microsoft.Data.SqlClient")!;
        internal static readonly Type? SqlDbType = Type.GetType("System.Data.SqlDbType, System.Data")!;
        private static readonly MethodInfo? GetSqlParametersProperty = SqlDbCommandType.GetProperty("Parameters", SqlDbParameterCollectionType)!.GetGetMethod()!;
        private static readonly MethodInfo? AddSqlParameterMethod = SqlDbParameterCollectionType.GetMethod("Add", new[] { SqlDbParameterType })!;
        internal static readonly ConstructorInfo? SqlServerConnectionConstructor = SqlServerConnectionType.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo? SqlServerCommandConstructor = SqlDbCommandType.GetConstructor(new Type[] { typeof(string) })!;
        private static readonly ConstructorInfo? SqlParameterConstructor = SqlDbParameterType?.GetConstructor(new[] { typeof(string), SqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

        private static readonly IReadOnlyDictionary<Type, string> SqlTypes = new Dictionary<Type, string>(new[]
        {
                new KeyValuePair<Type, string>(typeof(string), "NVarChar"),
                new KeyValuePair<Type, string>(typeof(short), "SmallInt"),
                new KeyValuePair<Type, string>(typeof(int), "Int"),
                new KeyValuePair<Type, string>(typeof(long), "BigInt"),
                new KeyValuePair<Type, string>(typeof(byte), "TinyInt"),
                new KeyValuePair<Type, string>(typeof(decimal), "Decimal"),
                new KeyValuePair<Type, string>(typeof(double), "Float"),
                new KeyValuePair<Type, string>(typeof(float), "Float"),
                new KeyValuePair<Type, string>(typeof(bool), "Bit"),
                new KeyValuePair<Type, string>(typeof(ushort), "SmallInt"),
                new KeyValuePair<Type, string>(typeof(uint), "Int"),
                new KeyValuePair<Type, string>(typeof(ulong), "BigInt"),
                new KeyValuePair<Type, string>(typeof(DateOnly), "Date"),
                new KeyValuePair<Type, string>(typeof(DateTime), "DateTime"),
                new KeyValuePair<Type, string>(typeof(Guid), "UniqueIdentifier"),
                new KeyValuePair<Type, string>(typeof(SqlBinary), "Binary"),
                new KeyValuePair<Type, string>(typeof(TimeOnly), "Time"),
                new KeyValuePair<Type, string>(typeof(sbyte), "TinyInt"),
                new KeyValuePair<Type, string>(typeof(byte[]), "Varbinary"),
                new KeyValuePair<Type, string>(typeof(TimeSpan), "Time"),
                new KeyValuePair<Type, string>(typeof(XmlDocument), "Xml"),
                new KeyValuePair<Type, string>(typeof(short?), "SmallInt"),
                new KeyValuePair<Type, string>(typeof(int?), "Int"),
                new KeyValuePair<Type, string>(typeof(long?), "BigInt"),
                new KeyValuePair<Type, string>(typeof(byte?), "TinyInt"),
                new KeyValuePair<Type, string>(typeof(decimal?), "Decimal"),
                new KeyValuePair<Type, string>(typeof(double?), "Float"),
                new KeyValuePair<Type, string>(typeof(float?), "Float"),
                new KeyValuePair<Type, string>(typeof(bool?), "Bit"),
                new KeyValuePair<Type, string>(typeof(DateOnly?), "Date"),
                new KeyValuePair<Type, string>(typeof(DateTime?), "DateTime"),
                new KeyValuePair<Type, string>(typeof(Guid?), "UniqueIdentifier"),
                new KeyValuePair<Type, string>(typeof(SqlBinary?), "Binary"),
                new KeyValuePair<Type, string>(typeof(TimeOnly?), "Time"),
                new KeyValuePair<Type, string>(typeof(ushort?), "SmallInt"),
                new KeyValuePair<Type, string>(typeof(uint?), "Int"),
                new KeyValuePair<Type, string>(typeof(ulong?), "BigInt"),
                new KeyValuePair<Type, string>(typeof(sbyte?), "TinyInt"),
                new KeyValuePair<Type, string>(typeof(TimeSpan?), "Time"),
                new KeyValuePair<Type, string>(typeof(StringBuilder), "Text"),
          });

        #endregion SqlServer

        #region Oracle

        internal static readonly Type? OracleConnectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess");
        private static readonly Type? OracleDbCommandType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleCommand, Oracle.ManagedDataAccess");
        internal static readonly Type? OracleDbParameterType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameter, Oracle.ManagedDataAccess");
        internal static readonly PropertyInfo? OracleValueParameterProperty = OracleDbParameterType?.GetProperty("Value");
        private static readonly Type? OracleDbParameterCollectionType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleParameterCollection, Oracle.ManagedDataAccess");
        internal static readonly Type? OracleDbType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleDbType, Oracle.ManagedDataAccess");

        private static readonly MethodInfo? GetOracleParametersProperty = OracleDbCommandType?.GetProperty("Parameters", OracleDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo? AddOracleParameterMethod = OracleDbParameterCollectionType?.GetMethod("Add", new[] { OracleDbParameterType });
        internal static readonly ConstructorInfo? OracleConnectionConstructor = OracleConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo? OracleParameterConstructor = OracleDbParameterType?.GetConstructor(new[] { typeof(string), OracleDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;
        private static readonly MethodInfo? OracleDbCommandBindByName = OracleDbCommandType?.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
        private static readonly MethodInfo? OracleDbCommandInitialLONGFetchSize = OracleDbCommandType?.GetProperty("InitialLONGFetchSize", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
        private static readonly MethodInfo? OracleDbCommandFetchSize = OracleDbCommandType?.GetProperty("FetchSize", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();

        private static readonly IReadOnlyDictionary<Type, string> OracleDbTypes = new Dictionary<Type, string>(new[]
        {
            new KeyValuePair<Type, string>(typeof(string), "VARCHAR2"),
            new KeyValuePair<Type, string>(typeof(short), "INT16"),
            new KeyValuePair<Type, string>(typeof(int), "INT32"),
            new KeyValuePair<Type, string>(typeof(long), "INT64"),
            new KeyValuePair<Type, string>(typeof(byte), "BYTE"),
            new KeyValuePair<Type, string>(typeof(decimal), "DECIMAL"),
            new KeyValuePair<Type, string>(typeof(bool), "INT32"),
            new KeyValuePair<Type, string>(typeof(DateOnly), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime), "DATE"),
            new KeyValuePair<Type, string>(typeof(double), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float), "FLOAT"),
            new KeyValuePair<Type, string>(typeof(Guid), "RAW"),
            new KeyValuePair<Type, string>(typeof(ushort), "INT16"),
            new KeyValuePair<Type, string>(typeof(uint), "INT32"),
            new KeyValuePair<Type, string>(typeof(ulong), "INT64"),
            new KeyValuePair<Type, string>(typeof(sbyte), "BYTE"),
            new KeyValuePair<Type, string>(typeof(TimeSpan), "IntervalDS"),
            new KeyValuePair<Type, string>(typeof(short?), "INT16"),
            new KeyValuePair<Type, string>(typeof(int?), "INT32"),
            new KeyValuePair<Type, string>(typeof(long?), "INT64"),
            new KeyValuePair<Type, string>(typeof(byte?), "BYTE"),
            new KeyValuePair<Type, string>(typeof(decimal?), "DECIMAL"),
            new KeyValuePair<Type, string>(typeof(bool?), "INT32"),
            new KeyValuePair<Type, string>(typeof(DateOnly?), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime?), "DATE"),
            new KeyValuePair<Type, string>(typeof(double?), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float?), "FLOAT"),
            new KeyValuePair<Type, string>(typeof(Guid?), "RAW"),
            new KeyValuePair<Type, string>(typeof(ushort?), "INT16"),
            new KeyValuePair<Type, string>(typeof(uint?), "INT32"),
            new KeyValuePair<Type, string>(typeof(ulong?), "INT64"),
            new KeyValuePair<Type, string>(typeof(sbyte?), "BYTE"),
            new KeyValuePair<Type, string>(typeof(TimeSpan?), "IntervalDS"),
            new KeyValuePair<Type, string>(typeof(byte[]), "BLOB"),
            new KeyValuePair<Type, string>(typeof(XmlDocument), "XMLTYPE"),
            new KeyValuePair<Type, string>(typeof(StringBuilder), "CLOB"),
        });

        #endregion Oracle

        #region PostgreSql

        internal static readonly Type? PostgresConnectionType = Type.GetType("Npgsql.NpgsqlConnection, Npgsql");
        private static readonly Type? PostgresDbCommandType = Type.GetType("Npgsql.NpgsqlCommand, Npgsql");
        internal static readonly Type? PostgresDbParameterType = Type.GetType("Npgsql.NpgsqlParameter, Npgsql");
        private static readonly Type? PostgresDbParameterCollectionType = Type.GetType("Npgsql.NpgsqlParameterCollection, Npgsql");
        private static readonly Type? PostgresDbType = Type.GetType("NpgsqlTypes.NpgsqlDbType, Npgsql");

        private static readonly MethodInfo? GetPostgresParametersProperty = PostgresDbCommandType?.GetProperty("Parameters", PostgresDbParameterCollectionType)?.GetGetMethod();
        private static readonly MethodInfo? AddPostgresParameterMethod = PostgresDbParameterCollectionType?.GetMethod("Add", new[] { PostgresDbParameterType });
        internal static readonly ConstructorInfo? PostgresConnectionConstructor = PostgresConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo? PostgresParameterConstructor = PostgresDbParameterType?.GetConstructor(new[] { typeof(string), PostgresDbType, typeof(int), typeof(string), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(DataRowVersion), typeof(object) });

        private static readonly IReadOnlyDictionary<Type, string> PostgresDbTypes = new Dictionary<Type, string>(new[]
        {
            new KeyValuePair<Type, string>(typeof(string), "VARCHAR"),
            new KeyValuePair<Type, string>(typeof(short), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(int), "INTEGER"),
            new KeyValuePair<Type, string>(typeof(long), "BIGINT"),
            new KeyValuePair<Type, string>(typeof(byte), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(decimal), "NUMERIC"),
            new KeyValuePair<Type, string>(typeof(bool), "BIT"),
            new KeyValuePair<Type, string>(typeof(DateOnly), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime), "TIMESTAMP"),
            new KeyValuePair<Type, string>(typeof(double), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float), "REAL"),
            new KeyValuePair<Type, string>(typeof(Guid), "UUID"),
            new KeyValuePair<Type, string>(typeof(TimeOnly), "TIME"),
            new KeyValuePair<Type, string>(typeof(ushort), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(uint), "INTEGER"),
            new KeyValuePair<Type, string>(typeof(ulong), "BIGINT"),
            new KeyValuePair<Type, string>(typeof(sbyte), "SMALLINT"),

            new KeyValuePair<Type, string>(typeof(short?), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(int?), "INT"),
            new KeyValuePair<Type, string>(typeof(long?), "BIGINT"),
            new KeyValuePair<Type, string>(typeof(byte?), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(decimal?), "NUMERIC"),
            new KeyValuePair<Type, string>(typeof(bool?), "BIT"),
            new KeyValuePair<Type, string>(typeof(DateOnly?), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime?), "TIMESTAMP"),
            new KeyValuePair<Type, string>(typeof(double?), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float?), "REAL"),
            new KeyValuePair<Type, string>(typeof(Guid?), "UUID"),
            new KeyValuePair<Type, string>(typeof(TimeOnly?), "TIME"),
            new KeyValuePair<Type, string>(typeof(ushort?), "SMALLINT"),
            new KeyValuePair<Type, string>(typeof(uint?), "INT"),
            new KeyValuePair<Type, string>(typeof(ulong?), "BIGINT"),
            new KeyValuePair<Type, string>(typeof(sbyte?), "SMALLINT"),

            new KeyValuePair<Type, string>(typeof(XmlDocument), "XML"),
            new KeyValuePair<Type, string>(typeof(byte[]), "BYTEA"),
        });

        #endregion PostgreSql

        #region Mysql

        internal static readonly Type? MysqlConnectionType = Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data");
        private static readonly Type? MysqlDbCommandType = Type.GetType("MySql.Data.MySqlClient.MySqlCommand, MySql.Data");
        internal static readonly Type? MysqlDbParameterType = Type.GetType("MySql.Data.MySqlClient.MySqlParameter, MySql.Data");
        private static readonly Type? MysqlDbParameterCollectionType = Type.GetType("MySql.Data.MySqlClient.MySqlParameterCollection, MySql.Data");
        private static readonly Type? MysqlDbType = Type.GetType("MySql.Data.MySqlClient.MySqlDbType, MySql.Data");

        private static readonly MethodInfo? GetMysqlParametersProperty = MysqlDbCommandType?.GetProperty("Parameters", MysqlDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo? AddMysqlParameterMethod = MysqlDbParameterCollectionType?.GetMethod("Add", new[] { MysqlDbParameterType });
        internal static readonly ConstructorInfo? MysqlConnectionConstructor = MysqlConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo? MysqlParameterConstructor = MysqlDbParameterType?.GetConstructor(new[] { typeof(string), MysqlDbType, typeof(int), typeof(ParameterDirection), typeof(bool), typeof(byte), typeof(byte), typeof(string), typeof(DataRowVersion), typeof(object) })!;

        private readonly static IReadOnlyDictionary<Type, string> MySQLDbTypes = new Dictionary<Type, string>(new[]
        {
            new KeyValuePair<Type, string>(typeof(string), "VARCHAR"),
            new KeyValuePair<Type, string>(typeof(short), "INT16"),
            new KeyValuePair<Type, string>(typeof(int), "INT32"),
            new KeyValuePair<Type, string>(typeof(long), "INT64"),
            new KeyValuePair<Type, string>(typeof(byte), "BYTE"),
            new KeyValuePair<Type, string>(typeof(decimal), "DECIMAL"),
            new KeyValuePair<Type, string>(typeof(bool), "BIT"),
            new KeyValuePair<Type, string>(typeof(DateOnly), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime), "DATETIME"),
            new KeyValuePair<Type, string>(typeof(double), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float), "FLOAT"),
            new KeyValuePair<Type, string>(typeof(Guid), "GUID"),
            new KeyValuePair<Type, string>(typeof(TimeOnly), "TIME"),
            new KeyValuePair<Type, string>(typeof(ushort), "INT16"),
            new KeyValuePair<Type, string>(typeof(uint), "INT32"),
            new KeyValuePair<Type, string>(typeof(ulong), "INT64"),
            new KeyValuePair<Type, string>(typeof(sbyte), "BYTE"),

            new KeyValuePair<Type, string>(typeof(short?), "INT16"),
            new KeyValuePair<Type, string>(typeof(int?), "INT32"),
            new KeyValuePair<Type, string>(typeof(long?), "INT64"),
            new KeyValuePair<Type, string>(typeof(byte?), "BYTE"),
            new KeyValuePair<Type, string>(typeof(decimal?), "DECIMAL"),
            new KeyValuePair<Type, string>(typeof(bool?), "BIT"),
            new KeyValuePair<Type, string>(typeof(DateOnly?), "DATE"),
            new KeyValuePair<Type, string>(typeof(DateTime?), "DATETIME"),
            new KeyValuePair<Type, string>(typeof(double?), "DOUBLE"),
            new KeyValuePair<Type, string>(typeof(float?), "FLOAT"),
            new KeyValuePair<Type, string>(typeof(Guid?), "GUID"),
            new KeyValuePair<Type, string>(typeof(TimeOnly?), "TIME"),
            new KeyValuePair<Type, string>(typeof(ushort?), "INT16"),
            new KeyValuePair<Type, string>(typeof(uint?), "INT32"),
            new KeyValuePair<Type, string>(typeof(ulong?), "INT64"),
            new KeyValuePair<Type, string>(typeof(sbyte?), "BYTE"),

            new KeyValuePair<Type, string>(typeof(byte[]), "MEDIUMBLOB"),
            new KeyValuePair<Type, string>(typeof(XmlDocument), "XML"),
        });

        #endregion Mysql

        #region Sqlite

        internal static readonly Type? SqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
        private static readonly Type? SqliteDbCommandType = Type.GetType("Microsoft.Data.Sqlite.SqliteCommand, Microsoft.Data.Sqlite");
        internal static readonly Type? SqliteDbParameterType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameter, Microsoft.Data.Sqlite");
        private static readonly Type? SqliteDbParameterCollectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteParameterCollection, Microsoft.Data.Sqlite");
        private static readonly MethodInfo? GetSqliteParametersProperty = SqliteDbCommandType?.GetProperty("Parameters", SqliteDbParameterCollectionType)!.GetGetMethod();
        private static readonly MethodInfo? AddSqliteParameterMethod = SqliteDbParameterCollectionType?.GetMethod("Add", new[] { SqliteDbParameterType });
        internal static readonly ConstructorInfo? SqliteConnectionConstructor = SqliteConnectionType?.GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo? SqliteParameterConstructor = SqliteDbParameterType?.GetConstructor(new[] { typeof(string), typeof(object) })!;

        #endregion Sqlite

        public static Action<object, DbCommand> GetLoadCommandParametersDelegate(in Type type, bool excludeAutogenerateColumns, in DbSettings options, ref bool hasOutputParameters, ref string[] filters)
        {
            PropertyInfo[] properties;

            if (DbConfigurationFactory.Tables.TryGetValue(type.FullName!, out FluentApi.DbTable? value))
                properties = value.Columns.Where(x => x.Autogenerated != excludeAutogenerateColumns).Select(x => x.Property).ToArray();
            else
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            //discover dynamic type generated by the library, the exclude auto generate columns is used to exclude the properties of the parent class
            if (excludeAutogenerateColumns && type.BaseType != null && type.AssemblyQualifiedName.Contains("ThomasInternalAssembly"))
            {
                var parentColumns = DbConfigurationFactory.Tables[type.BaseType.FullName!].Columns.Where(x => x.Autogenerated != excludeAutogenerateColumns).Select(x => x.Property).ToArray();
                properties = parentColumns.Union(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
            }

            filters = properties.Select(x => x.Name).ToArray();

            Span<DbParameterInfo> parameters = new DbParameterInfo[properties.Length];

            var dbType = GetDbType(options.SqlProvider);
            var dbTypes = DbTypes(options.SqlProvider);
            byte counter = 0;
            foreach (var item in properties)
            {
                var direction = GetParameterDireccion(item);
                var size = GetParameterSize(item);
                var precision = GetParameterPrecision(item);

                if (size == null && item.PropertyType == typeof(decimal))
                    size = 18;
                else
                    size = 0;

                if (precision == null && item.PropertyType == typeof(decimal))
                    precision = 6;
                else
                    precision = 0;

                int enumIntVal = 0;

                if (options.SqlProvider != SqlProvider.Sqlite)
                {
                    if (!dbTypes.ContainsKey(item.PropertyType))
                        throw new KeyNotFoundException($"{item.PropertyType.Name} key was no found on {options.SqlProvider.ToString()} mapping");

                    if (Enum.TryParse(dbType, dbTypes[item.PropertyType], true, out var enumVal))
                    {
                        enumIntVal = (int)enumVal!;
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion not supported from {item.PropertyType.Name} to Enum {dbTypes[item.PropertyType]} in {options.SqlProvider.ToString()} mapping");
                    }
                }

                parameters[counter++] = new DbParameterInfo(
                    item.Name,
                    null,
                    size!.Value,
                    precision!.Value,
                    direction,
                    item,
                    null,
                    enumIntVal,
                    null);

                hasOutputParameters = direction == ParameterDirection.Output || direction == ParameterDirection.InputOutput || hasOutputParameters;
            }

            Type[] argTypes = { typeof(object), typeof(DbCommand) };
            var method = new DynamicMethod(
                "LoadParameters" + InternalCounters.GetNextCommandHandlerCounter().ToString(),
                null,
                argTypes,
                type,
                true);

            var il = method.GetILGenerator();

            GenerateParameters(il, parameters, options.SqlProvider);

            //useful values for oracle command
            if (options.SqlProvider == SqlProvider.Oracle)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, OracleDbCommandType);
                il.Emit(OpCodes.Ldc_I4_1);
                il.EmitCall(OpCodes.Callvirt, OracleDbCommandBindByName, null);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, OracleDbCommandType);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.EmitCall(OpCodes.Callvirt, OracleDbCommandInitialLONGFetchSize, null);

                if (options.FetchSize >= 0)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, OracleDbCommandType);
                    il.Emit(OpCodes.Ldc_I8, options.FetchSize);
                    il.EmitCall(OpCodes.Callvirt, OracleDbCommandFetchSize, null);
                }
            }

            il.Emit(OpCodes.Ret);

            Type actionType = Expression.GetActionType(argTypes);

            return (Action<object, DbCommand>)method.CreateDelegate(actionType);
        }

        private const int DataRowVersionDefault = (int)DataRowVersion.Default;

        /*Considerations:
            - Oracle require honor parameter order
         */
        private static void GenerateParameters(ILGenerator il, ReadOnlySpan<DbParameterInfo> parameters, SqlProvider provider)
        {
            Type? commandType = null;
            MethodInfo? parametersProperty = null;
            ConstructorInfo? parametersConstructor = null;
            MethodInfo? addParameterMethod = null;

            switch (provider)
            {
                case SqlProvider.SqlServer:
                    commandType = SqlDbCommandType;
                    parametersProperty = GetSqlParametersProperty;
                    parametersConstructor = SqlParameterConstructor;
                    addParameterMethod = AddSqlParameterMethod;
                    break;
                case SqlProvider.MySql:
                    commandType = MysqlDbCommandType;
                    parametersProperty = GetMysqlParametersProperty;
                    parametersConstructor = MysqlParameterConstructor;
                    addParameterMethod = AddMysqlParameterMethod;
                    break;
                case SqlProvider.PostgreSql:
                    commandType = PostgresDbCommandType;
                    parametersProperty = GetPostgresParametersProperty;
                    parametersConstructor = PostgresParameterConstructor;
                    addParameterMethod = AddPostgresParameterMethod;
                    break;
                case SqlProvider.Oracle:
                    commandType = OracleDbCommandType;
                    parametersProperty = GetOracleParametersProperty;
                    parametersConstructor = OracleParameterConstructor;
                    addParameterMethod = AddOracleParameterMethod;
                    break;
                case SqlProvider.Sqlite:
                    commandType = SqliteDbCommandType;
                    parametersProperty = GetSqliteParametersProperty;
                    parametersConstructor = SqliteParameterConstructor;
                    addParameterMethod = AddSqliteParameterMethod;
                    break;
            }

            foreach (var parameter in parameters)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, commandType);
                il.Emit(OpCodes.Callvirt, parametersProperty);

                if (provider == SqlProvider.Sqlite)
                {
                    BuildSqliteParameter(in il, in parameter);
                }
                else if (provider == SqlProvider.PostgreSql)
                {
                    BuildPostgresParameter(il, parameter);
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
                    il.Emit(OpCodes.Call, parameter.PropertyInfo.GetGetMethod()!);

                    if (parameter.PropertyInfo.PropertyType.IsValueType)
                        il.Emit(OpCodes.Box, parameter.PropertyInfo.PropertyType);

                    il.Emit(OpCodes.Newobj, parametersConstructor);
                    il.Emit(OpCodes.Callvirt, addParameterMethod);
                    il.Emit(OpCodes.Pop);
                }
            }
        }

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
            il.Emit(OpCodes.Call, parameter.PropertyInfo.GetGetMethod()!);

            if (parameter.PropertyInfo.PropertyType.IsValueType)
                il.Emit(OpCodes.Box, parameter.PropertyInfo.PropertyType);

            il.Emit(OpCodes.Newobj, PostgresParameterConstructor);
            il.Emit(OpCodes.Callvirt, AddPostgresParameterMethod);
            il.Emit(OpCodes.Pop);
        }

        private static void BuildSqliteParameter(in ILGenerator il, in DbParameterInfo parameter)
        {
            //parameterName: string
            //value: object

            il.Emit(OpCodes.Ldstr, $"${parameter.Name}");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, parameter.PropertyInfo.GetGetMethod());
            if (parameter.PropertyInfo.PropertyType.IsValueType)
                il.Emit(OpCodes.Box, parameter.PropertyInfo.PropertyType);
            il.Emit(OpCodes.Newobj, SqliteParameterConstructor);

            il.Emit(OpCodes.Callvirt, AddSqliteParameterMethod);
            il.Emit(OpCodes.Pop);
        }

        private static Type GetDbType(SqlProvider provider) => provider switch
        {
            SqlProvider.SqlServer => SqlDbType,
            SqlProvider.Oracle => OracleDbType,
            SqlProvider.MySql => MysqlDbType,
            SqlProvider.Sqlite => null,
            SqlProvider.PostgreSql => PostgresDbType
        };

        internal static IReadOnlyDictionary<Type, string> DbTypes(SqlProvider provider)
        {
            return provider switch
            {
                SqlProvider.SqlServer => SqlTypes,
                SqlProvider.Oracle => OracleDbTypes,
                SqlProvider.MySql => MySQLDbTypes,
                SqlProvider.Sqlite => null,
                SqlProvider.PostgreSql => PostgresDbTypes
            };
        }
    }
}
