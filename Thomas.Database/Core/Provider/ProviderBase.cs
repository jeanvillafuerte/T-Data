using System;
using System.Data.Common;
using System.Data;
using System.Reflection;
using Thomas.Database.Attributes;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Linq.Expressions;
using Thomas.Database.Core.Converters;

namespace Thomas.Database.Core.Provider
{
    internal static partial class DatabaseHelperProvider
    {
        internal static readonly ConcurrentDictionary<int, CommandMetaData> CommandMetadata = new ConcurrentDictionary<int, CommandMetaData>(Environment.ProcessorCount * 2, 50);
        internal static readonly ConcurrentDictionary<SqlProvider, Func<string, DbConnection>> ConnectionCache = new ConcurrentDictionary<SqlProvider, Func<string, DbConnection>>(Environment.ProcessorCount * 2, 10);

        static DbParameterAttribute GetDbParameterAttribute(in PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
                if (attribute is DbParameterAttribute attr)
                {
                    attr.Precision = (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?)) && attr.Precision == 0  ? (byte)6 : attr.Precision;
                    attr.Scale = (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?)) && attr.Scale == 0 ? (byte)6 : attr.Scale;
                    return attr;
                }

            return new DbParameterAttribute {
                Direction = ParameterDirection.Input,
                Precision = (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?)) ? (byte)18 : (byte)0,
                Scale = (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?)) ? (byte)6 : (byte)0,
                Name = property.Name
            };
        }

        internal static void LoadConnectionDelegate(SqlProvider provider)
        {
            if (!ConnectionCache.TryGetValue(provider, out var connection))
            {
                ConstructorInfo constructorInfo = null;
                Type connectionType = null;
                switch (provider)
                {
                    case SqlProvider.SqlServer:
                        constructorInfo = SqlServerConnectionConstructor;
                        connectionType = SqlServerConnectionType;
                        break;
                    case SqlProvider.MySql:
                        constructorInfo = MysqlConnectionConstructor;
                        connectionType = MysqlConnectionType;
                        break;
                    case SqlProvider.PostgreSql:
                        constructorInfo = PostgresConnectionConstructor;
                        connectionType = PostgresConnectionType;
                        break;
                    case SqlProvider.Oracle:
                        constructorInfo = OracleConnectionConstructor;
                        connectionType = OracleConnectionType;
                        break;
                    case SqlProvider.Sqlite:
                        constructorInfo = SqliteConnectionConstructor;
                        connectionType = SqliteConnectionType;
                        break;
                }

                if (constructorInfo == null)
                    throw new NotImplementedException($"The provider {provider} library was not found");

                Type[] argTypes = { typeof(string) };

                var method = new DynamicMethod(
                    $"Get{provider}Connection",
                    connectionType,
                    argTypes,
                    connectionType.Module);

                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Newobj, constructorInfo);
                il.Emit(OpCodes.Ret);

                Type actionType = Expression.GetFuncType(new[] { typeof(string), connectionType });
                ConnectionCache.TryAdd(provider, (Func<string, DbConnection>)method.CreateDelegate(actionType)!);
            }
        }
    }

    internal sealed class DbParameterInfo
    {
        public readonly string Name;
        public readonly string BindName;
        public readonly int Size;
        public readonly int Precision;
        public readonly int Scale;
        public readonly ParameterDirection Direction;
        public readonly PropertyInfo PropertyInfo;
        public readonly Type PropertyType;
        public readonly int DbType;
        public readonly object Value;
        public readonly IInParameterValueConverter Converter;

        public bool IsInput => Direction == ParameterDirection.Input || Direction == ParameterDirection.InputOutput;
        public bool IsOutput => Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput || Direction == ParameterDirection.ReturnValue;

        public DbParameterInfo(in string name, in string bindName, in int size, in int precision, in int scale, in ParameterDirection direction, in PropertyInfo propertyInfo, in Type propertyType, in int dbType, in object value, in IInParameterValueConverter converter)
        {
            Name = name;
            BindName = bindName;
            Size = size;
            Precision = precision;
            Scale = scale;
            Direction = direction;
            PropertyInfo = propertyInfo;
            PropertyType = propertyType != null ? propertyType : propertyInfo != null ? propertyInfo.PropertyType : null;
            DbType = dbType;
            Value = value;
            Converter = converter;
        }
    }
}
