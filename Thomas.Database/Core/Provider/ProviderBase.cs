using System;
using System.Data.Common;
using System.Data;
using System.Reflection;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Thomas.Database.Core.Provider
{
    internal static partial class DatabaseHelperProvider
    {
        internal static readonly ConcurrentDictionary<ulong, CommandMetadata> CommandMetadata = new ConcurrentDictionary<ulong, CommandMetadata>(Environment.ProcessorCount * 2, 50);
        internal static readonly ConcurrentDictionary<SqlProvider, Func<string, DbConnection>> ConnectionCache = new ConcurrentDictionary<SqlProvider, Func<string, DbConnection>>(Environment.ProcessorCount * 2, 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ParameterDirection GetParameterDireccion(PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
                if (attribute is ParameterDirectionAttribute attr)
                    return GetDirection(attr.Direction);

            return ParameterDirection.Input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ParameterDirection GetDirection(ParamDirection direction) => direction switch
        {
            ParamDirection.Input => ParameterDirection.Input,
            ParamDirection.InputOutput => ParameterDirection.InputOutput,
            ParamDirection.Output => ParameterDirection.Output,
            _ => throw new UnknownParameterDirectionException()
        };

        static int? GetParameterSize(PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
                if (attribute is ParameterSizeAttribute attr)
                    return attr.Size;

            return null;
        }

        static int? GetParameterPrecision(PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
                if (attribute is ParameterPrecisionAttribute attr)
                    return attr.Precision;

            return null;
        }

        internal static void LoadConnectionDelegate(SqlProvider provider)
        {
            if (!ConnectionCache.TryGetValue(provider, out var connection))
            {
                ConstructorInfo constructorInfo = null;

                switch (provider)
                {
                    case SqlProvider.SqlServer:
                        constructorInfo = SqlServerConnectionConstructor;
                        break;
                    case SqlProvider.MySql:
                        constructorInfo = MysqlConnectionConstructor;
                        break;
                    case SqlProvider.PostgreSql:
                        constructorInfo = PostgresConnectionConstructor;
                        break;
                    case SqlProvider.Oracle:
                        constructorInfo = OracleConnectionConstructor;
                        break;
                    case SqlProvider.Sqlite:
                        constructorInfo = SqliteConnectionConstructor;
                        break;
                }

                if (constructorInfo == null)
                    throw new NotImplementedException($"The provider {provider} library was not found");

                Type[] argTypes = { typeof(string) };

                var method = new DynamicMethod(
                    $"Get{provider}Connection",
                    typeof(DbConnection),
                    argTypes,
                    typeof(DbConnection).Module);

                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(string).GetMethod("Intern", BindingFlags.Static | BindingFlags.Public));
                il.Emit(OpCodes.Newobj, constructorInfo);
                il.Emit(OpCodes.Ret);

                Type actionType = Expression.GetFuncType(new[] { typeof(string), typeof(DbConnection) });
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
        public readonly ParameterDirection Direction;
        public readonly PropertyInfo PropertyInfo;
        public readonly Type PropertyType;
        public readonly int DbType;
        public readonly object? Value;

        public DbParameterInfo(in string name, in string bindName, in int size, in int precision, in ParameterDirection direction, in PropertyInfo propertyInfo, in Type propertyType, in int dbType, in object? value)
        {
            Name = name;
            BindName = bindName;
            Size = size;
            Precision = precision;
            Direction = direction;
            PropertyInfo = propertyInfo;
            PropertyType = propertyType;
            DbType = dbType;
            Value = value;
        }
    }
}
