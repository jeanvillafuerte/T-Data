using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using Sigil;
using Thomas.Database.Cache;
using Thomas.Database.Core.Converters;
using Thomas.Database.Core.Provider;
using Thomas.Database.Configuration;
using Thomas.Database.Core.FluentApi;
using Label = Sigil.Label;
using System.Buffers;

namespace Thomas.Database
{
    internal partial class DatabaseCommand
    {
        #region delegate builder

        private static string GetMethodInfo(Type type, in SqlProvider provider)
        {
            return type.Name switch
            {
                "Int16" => "GetInt16",
                "Int32" => "GetInt32",
                "Int64" => "GetInt64",
                "UInt16" => "GetInt16",
                "UInt32" => "GetInt32",
                "UInt64" => "GetInt64",
                "Byte" => "GetByte",
                "SByte" => "GetByte",
                "Float" => "GetFloat",
                "Double" => "GetDouble",
                "Decimal" => "GetDecimal",
                "Boolean" => "GetBoolean",
                "String" => "GetString",
                "DateTime" => "GetDateTime",
                "TimeSpan" => "GetTimeSpan",
                "Guid" => "GetGuid",
                "Char" => "GetChar",
                "Byte[]" => "GetBytes",

                //specific handlers
                "BitArray" when provider == SqlProvider.PostgreSql => "GetBoolean",
                "Object" when provider == SqlProvider.PostgreSql => "GetBoolean",
                _ => throw new NotSupportedException($"Unsupported type {type.Name}")
            };
        }

        private static readonly Type ParserType = Type.GetType("Thomas.Database.DatabaseCommand, Thomas.Database")!;
        private static readonly MethodInfo ReadStreamMethod = ParserType.GetMethod(nameof(ReadStream), BindingFlags.NonPublic | BindingFlags.Static)!;

        internal static readonly Type ConvertType = typeof(Convert);
        private static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new Type[] { typeof(Guid) })!;
        private static readonly ConstructorInfo GuidConstructorByteArray = typeof(Guid).GetConstructor(new Type[] { typeof(byte[]) })!;
        private static readonly ConstructorInfo GuidConstructorString = typeof(Guid).GetConstructor(new Type[] { typeof(string) })!;
        private static readonly ConstructorInfo TimeSpanConstructorTicks = typeof(TimeSpan).GetConstructor(new Type[] { typeof(long) })!;

        private static Func<DbDataReader, T> GetParserTypeDelegate<T>(in DbDataReader reader, in int preparationQueryKey, in SqlProvider provider, in int bufferSize = 0, in int batchSize = 0)
        {
            var typeResult = typeof(T);

            var key = (preparationQueryKey * 23) + typeResult.GetHashCode();

            if (CacheTypeParser<T>.TryGet(in key, out Func<DbDataReader, T> parser))
                return parser;

            var columnInfoCollection = GetColumnMap(in typeResult, in reader, in provider);
            //if no large object then remove sequential access for next executions

            if (batchSize == 0)
            {
#if NETFRAMEWORK
                if (!columnInfoCollection.Any(x => x.IsLargeObject))
#else
            if (!columnInfoCollection.ToArray().Any(x => x.IsLargeObject))
#endif
                    DatabaseProvider.RemoveSequentialAccess(in preparationQueryKey);
            }

            var emitter = Emit<Func<DbDataReader, T>>.NewDynamicMethod(typeResult, $"ParseDataRowTo{typeResult.FullName!.Replace('.', '_')}", true, true);

            Func<DbDataReader, T> @delegate = null;

            if (IsReadonlyRecordType(in typeResult))
            {
                @delegate = GenerateEmitterForReadonlyRecord<T>(emitter, in typeResult, in provider, in bufferSize, in columnInfoCollection);
            }
            else
            {
                @delegate = GenerateEmitterBySetters<T>(emitter, in typeResult, in provider, in bufferSize, in columnInfoCollection);
            }

            CacheTypeParser<T>.Set(key, @delegate);
            return @delegate;
        }

        //support only class with public setters and parameterless constructor 
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Func<DbDataReader, T> GenerateEmitterBySetters<T>(Emit<Func<DbDataReader, T>> emitter,in Type typeResult, in SqlProvider provider, in int bufferSize, in Span<PropertyTypeInfo> columnInfoCollection)
#else
        private static Func<DbDataReader, T> GenerateEmitterBySetters<T>(Emit<Func<DbDataReader, T>> emitter, in Type typeResult, in SqlProvider provider, in int bufferSize, in PropertyTypeInfo[] columnInfoCollection)
#endif
        {
            if (typeResult.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Cannot find parameterless constructor for {typeResult.Name}");
            
            using Local instance = emitter.DeclareLocal<T>("objectValue", false); // Declare local variable to hold the new instance of T

            emitter.NewObject<T>().StoreLocal(instance); // Store the new instance in the local variable

            Type dbDataReader = null;

            switch (provider)
            {
                case SqlProvider.SqlServer:
                    dbDataReader = DatabaseHelperProvider.SqlDataReader;
                    break;
                case SqlProvider.MySql:
                    dbDataReader = DatabaseHelperProvider.MysqlDataReader;
                    break;
                case SqlProvider.PostgreSql:
                    dbDataReader = DatabaseHelperProvider.PostgresDataReader;
                    break;
                case SqlProvider.Oracle:
                    dbDataReader = DatabaseHelperProvider.OracleDataReader;
                    break;
                case SqlProvider.Sqlite:
                    dbDataReader = DatabaseHelperProvider.SqliteDataReader;
                    break;
            }

            int index = 0;

            foreach (var column in columnInfoCollection)
            {
                if (column == null)
                    break;

                Label notNullLabel = null;
                Label endLabel = null;

                if (column.AllowNull)
                {
                    emitter.LoadArgument(0)
                                  .CastClass(dbDataReader)
                                  .LoadConstant(index)
                                  .CallVirtual(dbDataReader.GetMethod("IsDBNull"));

                    //check if not null
                    notNullLabel = emitter.DefineLabel("notNull" + index);
                    endLabel = emitter.DefineLabel("end" + index);

                    emitter.BranchIfFalse(notNullLabel);

                    // If !IsDBNull returned false
                    emitter.Branch(endLabel);

                    //If !IsDBNull returned true
                    emitter.MarkLabel(notNullLabel);
                }

                emitter.LoadLocal(instance);

                var getMethodName = GetMethodInfo(column.Source, in provider) ?? throw new NotSupportedException($"Unsupported type {column.Source.Name}");
                Type underlyingType = Nullable.GetUnderlyingType(column.PropertyInfo.PropertyType);

                //byte[] To Guid - Oracle
                if (getMethodName.Equals("GetBytes", StringComparison.Ordinal) && (column.PropertyInfo.PropertyType == typeof(Guid) || underlyingType == typeof(Guid)))
                {
                    // Define a local for the byte array
                    using var bufferLocal = emitter.DeclareLocal<byte[]>();

                    // Create a new byte array to hold the GUID
                    emitter.LoadConstant(16) // GUID size
                                        .NewArray<byte>()
                                        .StoreLocal(bufferLocal);

                    // Assume the data is already in the correct position in the reader, and fill the byte array
                    emitter.LoadArgument(0)
                                        .CastClass(dbDataReader)
                                        .LoadConstant(index)
                                        .LoadConstant(0L)
                                        .LoadLocal(bufferLocal)
                                        .LoadConstant(0)
                                        .LoadConstant(16)
                                        .CallVirtual(dbDataReader.GetMethod("GetBytes"))
                                        .Pop();

                    emitter.LoadLocal(instance)
                            .LoadLocal(bufferLocal)
                            .NewObject(GuidConstructorByteArray);

                    if (underlyingType != null)
                        emitter.NewObject(NullableGuidConstructor);

                    emitter.Call(column.PropertyInfo!.GetSetMethod(true))
                            .Pop();

                    if (column.AllowNull)
                    {
                        emitter.MarkLabel(endLabel);
                    }

                    index++;
                    continue;
                }
                else if (getMethodName.Equals("GetBytes", StringComparison.Ordinal))
                {
                    emitter.LoadArgument(0)
                        .LoadConstant(index)
                        .LoadConstant(bufferSize > 0 ? bufferSize : 8192)
                        .Call(ReadStreamMethod);
                }
                else
                {
                    emitter.LoadArgument(0)
                                        .CastClass(dbDataReader)
                                        .LoadConstant(index)
                                        .Call(dbDataReader.GetMethod(getMethodName, new[] { typeof(int)} ));
                }

                if (column.RequiredConversion)
                    EmitValueConversion(in emitter, in column, in underlyingType, in provider);

                if (column.PropertyInfo.PropertyType.IsValueType)
                {
                    emitter.Call(column.PropertyInfo!.GetSetMethod(true));
                }
                else
                {
                    emitter.CallVirtual(column.PropertyInfo!.GetSetMethod(true));
                }

                if (column.AllowNull)
                {
                    emitter.MarkLabel(endLabel);
                }

                index++;
            }

            return emitter.LoadLocal(instance).Return().CreateDelegate();
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Func<DbDataReader, T> GenerateEmitterForReadonlyRecord<T>(Emit<Func<DbDataReader, T>> emitter, in Type type, in SqlProvider provider, in int bufferSize, in Span<PropertyTypeInfo> columnInfoCollection)
#else
        private static Func<DbDataReader, T> GenerateEmitterForReadonlyRecord<T>(Emit<Func<DbDataReader, T>> emitter, in Type type, in SqlProvider provider, in int bufferSize, in PropertyTypeInfo[] columnInfoCollection)
#endif
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToArray();
            var constructor = type.GetConstructor(fields.Select(x => x.FieldType).ToArray()) ?? throw new InvalidOperationException($"Cannot find readonly record constructor for {type.Name}");

            var columns = columnInfoCollection.ToArray();
            var locals = fields.Select(f => emitter.DeclareLocal(f.FieldType)).ToList();

            Type dbDataReader = null;

            switch (provider)
            {
                case SqlProvider.SqlServer:
                    dbDataReader = DatabaseHelperProvider.SqlDataReader;
                    break;
                case SqlProvider.MySql:
                    dbDataReader = DatabaseHelperProvider.MysqlDataReader;
                    break;
                case SqlProvider.PostgreSql:
                    dbDataReader = DatabaseHelperProvider.PostgresDataReader;
                    break;
                case SqlProvider.Oracle:
                    dbDataReader = DatabaseHelperProvider.OracleDataReader;
                    break;
                case SqlProvider.Sqlite:
                    dbDataReader = DatabaseHelperProvider.SqliteDataReader;
                    break;
            }

            for (int index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                var local = locals[index];

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                var column = columns.FirstOrDefault(x => field.Name.Contains($"<{x.Name}>", StringComparison.InvariantCultureIgnoreCase));
#else
                var column = columns.FirstOrDefault(x => field.Name.Contains($"<{x.Name}>"));
#endif
                if (column == null)
                    continue;

                //when column not found we need to set default value
                Label notNullLabel = emitter.DefineLabel("notNull" + index);
                Label endLabel = emitter.DefineLabel("end" + index);

                Type underlyingType = Nullable.GetUnderlyingType(field.FieldType);

                if (column.AllowNull)
                {
                    emitter.LoadArgument(0)
                                    .CastClass(dbDataReader)
                                    .LoadConstant(index)
                                    .CallVirtual(dbDataReader.GetMethod("IsDBNull"));

                    //check if not null
                    emitter.BranchIfFalse(notNullLabel);

                    //ELSE - handling default value and avoid store default value on null-able types
                    if (underlyingType == null)
                    {
                        EmitDefaultValue(emitter, field);
                        emitter.StoreLocal(local);
                    }

                    //if column is null then skip to end
                    emitter.Branch(endLabel);
                    emitter.MarkLabel(notNullLabel);
                }

                var getMethodName = GetMethodInfo(column.Source, in provider);

                //byte[] To Guid - Oracle
                if (getMethodName.Equals("GetBytes", StringComparison.Ordinal) && (column.PropertyInfo.PropertyType == typeof(Guid) || underlyingType == typeof(Guid)))
                {
                    // Define a local for the byte array
                    using var bufferLocal = emitter.DeclareLocal<byte[]>();

                    // Create a new byte array to hold the GUID
                    emitter.LoadConstant(16) // GUID size
                                        .NewArray<byte>()
                                        .StoreLocal(bufferLocal);

                    // Assume the data is already in the correct position in the reader, and fill the byte array
                    emitter.LoadArgument(0)
                                        .CastClass(dbDataReader)
                                        .LoadConstant(index)
                                        .LoadConstant(0L)
                                        .LoadLocal(bufferLocal)
                                        .LoadConstant(0)
                                        .LoadConstant(16)
                                        .Call(dbDataReader.GetMethod("GetBytes"))
                                        .Pop();

                    emitter.LoadLocal(bufferLocal);
                    emitter.NewObject(GuidConstructorByteArray);

                    if (underlyingType != null)
                    {
                        emitter.NewObject(NullableGuidConstructor);
                    }

                    emitter.StoreLocal(local);

                    if (column.AllowNull)
                    {
                        emitter.MarkLabel(endLabel);
                    }

                    continue;
                }
                else if (getMethodName.Equals("GetBytes", StringComparison.Ordinal))
                {
                    emitter.LoadArgument(0)
                        .LoadConstant(index)
                        .LoadConstant(bufferSize > 0 ? bufferSize : 8192)
                        .Call(ReadStreamMethod);
                }
                else
                {
                    emitter.LoadArgument(0)
                            .CastClass(dbDataReader)
                            .LoadConstant(index)
                            .Call(dbDataReader.GetMethod(getMethodName, new[] { typeof(int) }));
                }

                if (column.RequiredConversion)
                    EmitValueConversion(in emitter, in column, in underlyingType, in provider);

                emitter.StoreLocal(local);

                if (column.AllowNull)
                {
                    emitter.MarkLabel(endLabel);
                }
            }

            foreach (var local in locals)
                emitter.LoadLocal(local);

            return emitter.NewObject(constructor)
                    .Return()
                    .CreateDelegate();
        }


        private static void EmitValueConversion<T>(in Emit<Func<DbDataReader, T>> emitter, in PropertyTypeInfo column, in Type underlyingType, in SqlProvider provider)
        {
            Type propertyType = column.PropertyInfo.PropertyType;
            Type sourceType = column.Source;

            if (IsPrimitiveTypeConversion(propertyType, sourceType))
            {
                emitter.Convert(propertyType);
            }
            else if (underlyingType == sourceType)
            {
                emitter.NewObject(typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));
            }
            else if (propertyType == typeof(Guid) && sourceType == typeof(string))
            {
                emitter.NewObject(GuidConstructorString);
            }
            else if (underlyingType == typeof(Guid) && sourceType == typeof(string))
            {
                emitter.NewObject(GuidConstructorString)
                    .NewObject(NullableGuidConstructor);
            }
            else if ((propertyType == typeof(TimeSpan) || underlyingType == typeof(TimeSpan)) && sourceType == typeof(long))
            {
                emitter.NewObject(TimeSpanConstructorTicks);
            }
            else if ((propertyType == typeof(TimeSpan) || underlyingType == typeof(TimeSpan)) && sourceType == typeof(string))
            {
                emitter.Call(typeof(CommonConversion).GetMethod(nameof(CommonConversion.SafeConversionStringToTimeSpan), BindingFlags.NonPublic | BindingFlags.Static)!);
            }
            else if (provider.Equals(SqlProvider.PostgreSql) && column.DataTypeName == "bit" && underlyingType == typeof(bool))
            {
                emitter.NewObject(typeof(Nullable<>).MakeGenericType(typeof(bool)).GetConstructor(new[] { typeof(bool) }));
            }
            else if (provider.Equals(SqlProvider.PostgreSql) && column.DataTypeName == "bit" && propertyType == typeof(bool))
            {
            }
            else
            {
                var effectiveType = underlyingType ?? propertyType;
                var converterMethod = ConvertType.GetMethod($"To{effectiveType.Name}", new[] { sourceType });

                if (converterMethod != null)
                {
                    emitter.Call(converterMethod);

                    if (underlyingType != null)
                        emitter.NewObject(typeof(Nullable<>).MakeGenericType(effectiveType).GetConstructor(new[] { effectiveType }));
                }
                else
                {
                    throw new InvalidCastException($"Cannot convert field {column.PropertyInfo.DeclaringType.Name}.{column.Name}, {sourceType.Name} to {effectiveType.Name}");
                }
            }
        }

        private static void EmitDefaultValue<T>(Emit<Func<DbDataReader, T>> emitter, FieldInfo field)
        {
            if (field.FieldType.IsValueType)
            {
                if (field.FieldType == typeof(int) || field.FieldType == typeof(double) ||
                    field.FieldType == typeof(float) || field.FieldType == typeof(long) ||
                    field.FieldType == typeof(short) || field.FieldType == typeof(byte) ||
                    field.FieldType == typeof(sbyte) ||
                    field.FieldType == typeof(uint) || field.FieldType == typeof(ushort) ||
                    field.FieldType == typeof(ulong))
                {
                    emitter.LoadConstant(0);
                }
                else if (field.FieldType == typeof(bool))
                {
                    emitter.LoadConstant(false);
                }
                else if (field.FieldType == typeof(decimal))
                {
                    // Decimal constants are created by loading the value onto the stack and then calling the constructor
                    emitter.LoadConstant(0);     // lo
                    emitter.LoadConstant(0);     // mid
                    emitter.LoadConstant(0);     // hi
                    emitter.LoadConstant(false); // isNegative
                    emitter.LoadConstant(0);     // scale
                    emitter.NewObject<decimal, int, int, int, bool, byte>();
                }
                else if (field.FieldType == typeof(Guid))
                {
                    emitter.LoadField(typeof(Guid).GetField("Empty"));
                }
                else if (field.FieldType == typeof(DateTime))
                {
                    emitter.LoadField(typeof(DateTime).GetField("MinValue"));
                }
                else if (field.FieldType == typeof(TimeSpan))
                {
                    emitter.LoadField(typeof(TimeSpan).GetField("Zero"));
                }
#if NET6_0_OR_GREATER
                else if (field.FieldType == typeof(DateOnly))
                {
                    emitter.LoadField(typeof(DateOnly).GetField("MinValue"));
                }
                else if (field.FieldType == typeof(TimeOnly))
                {
                    emitter.LoadField(typeof(TimeOnly).GetField("Midnight"));
                }
#endif
                else
                {
                    throw new NotSupportedException($"Unsupported type {field.FieldType.FullName}");
                }
            }
            else
            {
                emitter.LoadNull();
            }
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Span<PropertyTypeInfo> GetColumnMap(in Type type, in DbDataReader reader, in SqlProvider provider)
#else
        private static PropertyTypeInfo[] GetColumnMap(in Type type, in DbDataReader reader, in SqlProvider provider)
#endif
        {
            var table = reader.GetSchemaTable();
            var columnSchema = new LinkedList<PropertyTypeInfo>();

            if (!DbConfigurationFactory.Tables.TryGetValue(type.FullName!, out var configuratedTable))
            {
                var dbTable = new DbTable { Name = type.Name!, Columns = new LinkedList<Thomas.Database.Core.FluentApi.DbColumn>() };
                dbTable.AddFieldsAsColumns(type);
                DbConfigurationFactory.Tables.TryAdd(type.FullName!, dbTable);
                configuratedTable = dbTable;
            }
            
            foreach (DataRow row in table.Rows)
            {
                var columnName = row["ColumnName"].ToString();
                var property = configuratedTable.Columns.Where(x => x.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase) ||
                                                                    x.DbName?.Equals(columnName, StringComparison.InvariantCultureIgnoreCase) == true).Select(x => x.Property).FirstOrDefault();

                if (property == null)
                    continue;

                var dataType = (Type)row["DataType"];
                var isLob = provider == SqlProvider.Oracle && bool.TryParse(row["IsValueLob"].ToString(), out var isValueLob) && isValueLob;
                var dataTypeName = provider == SqlProvider.MySql || provider == SqlProvider.Oracle ? null : row["DataTypeName"].ToString();


                columnSchema.AddLast(new PropertyTypeInfo
                {
                    DataTypeName = dataTypeName,
                    Source = dataType,
                    PropertyInfo = property,
                    Name = columnName,
                    RequiredConversion = !dataType.Equals(property.PropertyType),
                    IsLargeObject = isLob || (bool.TryParse(row["IsLong"].ToString(), out var isLong) && isLong) || dataTypeName == "BLOB",
                    AllowNull = !bool.TryParse(row["AllowDBNull"].ToString(), out var allowNull) || allowNull
                });
            }
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return columnSchema.ToArray().AsSpan();
#else
            return columnSchema.ToArray();
#endif
        }

        internal static bool IsReadonlyRecordType(in Type type)
        {
            var allFieldsInitOnly = type.GetRuntimeFields().All(x => x.Attributes.HasFlag(FieldAttributes.InitOnly));
            Type equatableType = typeof(IEquatable<>).MakeGenericType(type);
            return allFieldsInitOnly && equatableType.IsAssignableFrom(type) && (type.GetMethods().Any(x => x.Name == "<Clone>$") || type.IsValueType);
        }

        private static bool IsPrimitiveTypeConversion(Type propertyType, Type sourceType)
        {
            return propertyType.IsValueType && propertyType.IsPrimitive &&
                   sourceType.IsValueType && sourceType.IsPrimitive;
        }

        private class PropertyTypeInfo
        {
            public Type Source { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public bool RequiredConversion { get; set; }
            public string Name { get; set; }
            public bool IsLargeObject { get; set; }
            public bool AllowNull { get; set; }
            public string DataTypeName { get; set; }

            public override int GetHashCode()
            {
                return Source.GetHashCode() * 23 + PropertyInfo.PropertyType.GetHashCode();
            }
        }

#endregion delegate builder

        #region specific readers
        private static byte[] ReadStream(DbDataReader reader, int index, int bufferSize) //default 8KB
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            long dataIndex = 0;
            long bytesRead;

            using MemoryStream memoryStream = new MemoryStream();

            try
            {
                bytesRead = reader.GetBytes(index, dataIndex, buffer, 0, bufferSize);

                while (bytesRead == bufferSize)
                {
                    memoryStream.Write(buffer, 0, (int)bytesRead);
                    dataIndex += bytesRead;
                    bytesRead = reader.GetBytes(index, dataIndex, buffer, 0, bufferSize);
                }

                memoryStream.Write(buffer, 0, (int)bytesRead);

                return memoryStream.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

    #endregion specific readers

}
}
