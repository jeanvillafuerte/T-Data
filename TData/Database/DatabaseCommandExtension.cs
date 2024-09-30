using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using TData.InternalCache;
using TData.Core.Converters;
using TData.Core.Provider;
using TData.Configuration;
using TData.Core.FluentApi;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace TData
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

        private static readonly Type ParserType = Type.GetType("TData.DatabaseCommand, TData")!;
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

            var dynamicMethod = new DynamicMethod($"ParseDataRowTo{typeResult.FullName!.Replace('.', '_')}", typeof(T), new[] { typeof(DbDataReader) }, typeResult.Module, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

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

            if (IsReadonlyRecordType(in typeResult))
            {
                GenerateEmitterForReadonlyRecord<T>(ilGenerator, in typeResult, in provider, in bufferSize, in columnInfoCollection, in dbDataReader);
            }
            else
            {
                GenerateEmitterBySetters<T>(ilGenerator, in typeResult, in provider, in bufferSize, in columnInfoCollection, in dbDataReader);
            }

            Type funcType = Expression.GetFuncType(new[] { typeof(DbDataReader), typeof(T) });
            Func<DbDataReader, T> @delegate = (Func<DbDataReader, T>)dynamicMethod.CreateDelegate(funcType);
            CacheTypeParser<T>.Set(key, @delegate);
            return @delegate;
        }

        //support only class with public setters and parameterless constructor 
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static void GenerateEmitterBySetters<T>(ILGenerator emitter, in Type typeResult, in SqlProvider provider, in int bufferSize, in Span<PropertyTypeInfo> columnInfoCollection, in Type dbDataReader)
#else
        private static void GenerateEmitterBySetters<T>(ILGenerator emitter, in Type typeResult, in SqlProvider provider, in int bufferSize, in PropertyTypeInfo[] columnInfoCollection, in Type dbDataReader)
#endif
        {
            if (typeResult.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Cannot find parameterless constructor for {typeResult.Name}");

            LocalBuilder instance = emitter.DeclareLocal(typeof(T)); // Declare local variable to hold the new instance of T

            emitter.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes)); // Create a new instance of T
            emitter.Emit(OpCodes.Stloc, instance); // Store the new instance in the local variable

            int index = 0;

            foreach (var column in columnInfoCollection)
            {
                if (column == null)
                    break;

                Label notNullLabel = default;
                Label endLabel = default;

                if (column.AllowNull)
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, index);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod("IsDBNull"));

                    //check if not null
                    notNullLabel = emitter.DefineLabel();
                    endLabel = emitter.DefineLabel();

                    emitter.Emit(OpCodes.Brfalse_S, notNullLabel);

                    // If !IsDBNull returned false
                    emitter.Emit(OpCodes.Br_S, endLabel);

                    //If !IsDBNull returned true
                    emitter.MarkLabel(notNullLabel);
                }

                emitter.Emit(OpCodes.Ldloc, instance);

                var getMethodName = GetMethodInfo(column.Source, in provider) ?? throw new NotSupportedException($"Unsupported type {column.Source.Name}");
                Type underlyingType = Nullable.GetUnderlyingType(column.PropertyInfo.PropertyType);

                //byte[] To Guid - Oracle
                if (getMethodName.Equals("GetBytes", StringComparison.Ordinal) && (column.PropertyInfo.PropertyType == typeof(Guid) || underlyingType == typeof(Guid)))
                {
                    // Define a local for the byte array
                    LocalBuilder bufferLocal = emitter.DeclareLocal(typeof(byte[]));

                    // Create a new byte array to hold the GUID
                    emitter.Emit(OpCodes.Ldc_I4, 16); // GUID size
                    emitter.Emit(OpCodes.Newarr, typeof(byte));
                    emitter.Emit(OpCodes.Stloc, bufferLocal);

                    // Assume the data is already in the correct position in the reader, and fill the byte array
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, index);
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    emitter.Emit(OpCodes.Ldloc, bufferLocal);
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    emitter.Emit(OpCodes.Ldc_I4, 16);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod("GetBytes"));
                    emitter.Emit(OpCodes.Pop);

                    emitter.Emit(OpCodes.Ldloc, instance);
                    emitter.Emit(OpCodes.Ldloc, bufferLocal);
                    emitter.Emit(OpCodes.Newobj, GuidConstructorByteArray);

                    if (underlyingType != null)
                        emitter.Emit(OpCodes.Newobj, NullableGuidConstructor);

                    emitter.Emit(OpCodes.Callvirt, column.PropertyInfo!.GetSetMethod(true));
                    emitter.Emit(OpCodes.Pop);

                    if (column.AllowNull)
                    {
                        emitter.MarkLabel(endLabel);
                    }

                    index++;
                    continue;
                }
                else if (getMethodName.Equals("GetBytes", StringComparison.Ordinal))
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldc_I4, index);
                    emitter.Emit(OpCodes.Ldc_I4, bufferSize > 0 ? bufferSize : 8192);
                    emitter.Emit(OpCodes.Call, ReadStreamMethod);
                }
                else
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, index);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod(getMethodName, new[] { typeof(int) }));
                }

                if (column.RequiredConversion)
                    EmitValueConversion(in emitter, in column, in underlyingType, in provider);

                if (column.PropertyInfo.PropertyType.IsValueType)
                {
                    emitter.Emit(OpCodes.Call, column.PropertyInfo!.GetSetMethod(true));
                }
                else
                {
                    emitter.Emit(OpCodes.Callvirt, column.PropertyInfo!.GetSetMethod(true));
                }

                if (column.AllowNull)
                {
                    emitter.MarkLabel(endLabel);
                }

                index++;
            }

            emitter.Emit(OpCodes.Ldloc, instance);
            emitter.Emit(OpCodes.Ret);
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
private static void GenerateEmitterForReadonlyRecord<T>(ILGenerator emitter, in Type type, in SqlProvider provider, in int bufferSize, in Span<PropertyTypeInfo> columnInfoCollection, in Type dbDataReader)
#else
        private static void GenerateEmitterForReadonlyRecord<T>(ILGenerator emitter, in Type type, in SqlProvider provider, in int bufferSize, in PropertyTypeInfo[] columnInfoCollection, in Type dbDataReader)
#endif
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToArray();
            var constructor = type.GetConstructor(fields.Select(x => x.FieldType).ToArray()) ?? throw new InvalidOperationException($"Cannot find readonly record constructor for {type.Name}");

            var columns = columnInfoCollection.ToArray();
            var locals = fields.Select(f => emitter.DeclareLocal(f.FieldType)).ToList();

            int columnIndex = 0;
            for (int index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                var local = locals[index];

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        var column = columns.FirstOrDefault(x => field.Name.Contains($"<{x.PropertyInfo.Name}>", StringComparison.InvariantCultureIgnoreCase));
#else
                var column = columns.FirstOrDefault(x => field.Name.Contains($"<{x.PropertyInfo.Name}>"));
#endif
                if (column == null)
                    continue;

                //when column not found we need to set default value
                Label notNullLabel = emitter.DefineLabel();
                Label endLabel = emitter.DefineLabel();

                Type underlyingType = Nullable.GetUnderlyingType(field.FieldType);

                if (column.AllowNull)
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, columnIndex);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod("IsDBNull"));

                    //check if not null
                    emitter.Emit(OpCodes.Brfalse_S, notNullLabel);

                    //ELSE - handling default value and avoid store default value on null-able types
                    if (underlyingType == null)
                    {
                        EmitDefaultValue(emitter, field);
                        emitter.Emit(OpCodes.Stloc, local);
                    }

                    //if column is null then skip to end
                    emitter.Emit(OpCodes.Br_S, endLabel);
                    emitter.MarkLabel(notNullLabel);
                }

                var getMethodName = GetMethodInfo(column.Source, in provider);

                //byte[] To Guid - Oracle
                if (getMethodName.Equals("GetBytes", StringComparison.Ordinal) && (column.PropertyInfo.PropertyType == typeof(Guid) || underlyingType == typeof(Guid)))
                {
                    // Define a local for the byte array
                    var bufferLocal = emitter.DeclareLocal(typeof(byte[]));

                    // Create a new byte array to hold the GUID
                    emitter.Emit(OpCodes.Ldc_I4, 16); // GUID size
                    emitter.Emit(OpCodes.Newarr, typeof(byte));
                    emitter.Emit(OpCodes.Stloc, bufferLocal);

                    // Assume the data is already in the correct position in the reader, and fill the byte array
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, columnIndex);
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    emitter.Emit(OpCodes.Ldloc, bufferLocal);
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    emitter.Emit(OpCodes.Ldc_I4, 16);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod("GetBytes"));
                    emitter.Emit(OpCodes.Pop);

                    emitter.Emit(OpCodes.Ldloc, bufferLocal);
                    emitter.Emit(OpCodes.Newobj, GuidConstructorByteArray);

                    if (underlyingType != null)
                    {
                        emitter.Emit(OpCodes.Newobj, NullableGuidConstructor);
                    }

                    emitter.Emit(OpCodes.Stloc, local);

                    if (column.AllowNull)
                    {
                        emitter.MarkLabel(endLabel);
                    }

                    columnIndex++;
                    continue;
                }
                else if (getMethodName.Equals("GetBytes", StringComparison.Ordinal))
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Ldc_I4, columnIndex);
                    emitter.Emit(OpCodes.Ldc_I4, bufferSize > 0 ? bufferSize : 8192);
                    emitter.Emit(OpCodes.Call, ReadStreamMethod);
                }
                else
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Castclass, dbDataReader);
                    emitter.Emit(OpCodes.Ldc_I4, columnIndex);
                    emitter.Emit(OpCodes.Callvirt, dbDataReader.GetMethod(getMethodName, new[] { typeof(int) }));
                }

                if (column.RequiredConversion)
                    EmitValueConversion(in emitter, in column, in underlyingType, in provider);

                emitter.Emit(OpCodes.Stloc, local);

                if (column.AllowNull)
                {
                    emitter.MarkLabel(endLabel);
                }

                columnIndex++;
            }

            foreach (var local in locals)
                emitter.Emit(OpCodes.Ldloc, local);

            emitter.Emit(OpCodes.Newobj, constructor);
            emitter.Emit(OpCodes.Ret);
        }

        private static void EmitConvert(in ILGenerator emitter, in Type primitiveType)
        {
            OpCode conversionOpCode;

            switch (Type.GetTypeCode(primitiveType))
            {
                case TypeCode.Byte:
                    conversionOpCode = OpCodes.Conv_U1;
                    break;
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    conversionOpCode = OpCodes.Conv_I1;
                    break;
                case TypeCode.Int16:
                    conversionOpCode = OpCodes.Conv_I2;
                    break;
                case TypeCode.UInt16:
                    conversionOpCode = OpCodes.Conv_U2;
                    break;
                case TypeCode.Int32:
                    conversionOpCode = OpCodes.Conv_I4;
                    break;
                case TypeCode.UInt32:
                    conversionOpCode = OpCodes.Conv_U4;
                    break;
                case TypeCode.Int64:
                    conversionOpCode = OpCodes.Conv_I8;
                    break;
                case TypeCode.UInt64:
                    conversionOpCode = OpCodes.Conv_U8;
                    break;
                case TypeCode.Single:
                    conversionOpCode = OpCodes.Conv_R4;
                    break;
                case TypeCode.Double:
                    conversionOpCode = OpCodes.Conv_R8;
                    break;
                default:
                    if (primitiveType == typeof(IntPtr))
                    {
                        conversionOpCode = OpCodes.Conv_I;
                    }
                    else if (primitiveType == typeof(UIntPtr))
                    {
                        conversionOpCode = OpCodes.Conv_U;
                    }
                    else
                    {
                        throw new NotSupportedException($"Conversion for type {primitiveType} is not supported.");
                    }
                    break;
            }

            emitter.Emit(conversionOpCode);
        }

        private static void EmitValueConversion(in ILGenerator emitter, in PropertyTypeInfo column, in Type underlyingType, in SqlProvider provider)
        {
            Type propertyType = column.PropertyInfo.PropertyType;
            Type sourceType = column.Source;

            if (IsPrimitiveTypeConversion(propertyType, sourceType))
            {
                EmitConvert(emitter, propertyType);
            }
            else if (underlyingType == sourceType)
            {
                emitter.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(underlyingType).GetConstructor(new[] { underlyingType }));
            }
            else if (propertyType == typeof(Guid) && sourceType == typeof(string))
            {
                emitter.Emit(OpCodes.Newobj, GuidConstructorString);
            }
            else if (underlyingType == typeof(Guid) && sourceType == typeof(string))
            {
                emitter.Emit(OpCodes.Newobj, GuidConstructorString);
                emitter.Emit(OpCodes.Newobj, NullableGuidConstructor);
            }
            else if ((propertyType == typeof(TimeSpan) || underlyingType == typeof(TimeSpan)) && sourceType == typeof(long))
            {
                emitter.Emit(OpCodes.Newobj, TimeSpanConstructorTicks);
            }
            else if ((propertyType == typeof(TimeSpan) || underlyingType == typeof(TimeSpan)) && sourceType == typeof(string))
            {
                emitter.Emit(OpCodes.Call, typeof(CommonConversion).GetMethod(nameof(CommonConversion.SafeConversionStringToTimeSpan), BindingFlags.NonPublic | BindingFlags.Static));
            }
            else if (provider.Equals(SqlProvider.PostgreSql) && column.DataTypeName == "bit" && underlyingType == typeof(bool))
            {
                emitter.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(typeof(bool)).GetConstructor(new[] { typeof(bool) }));
            }
            else if (provider.Equals(SqlProvider.PostgreSql) && column.DataTypeName == "bit" && propertyType == typeof(bool))
            {
                // No conversion needed
            }
            else
            {
                var effectiveType = underlyingType ?? propertyType;
                var converterMethod = ConvertType.GetMethod($"To{effectiveType.Name}", new[] { sourceType });

                if (converterMethod != null)
                {
                    emitter.Emit(OpCodes.Call, converterMethod);

                    if (underlyingType != null)
                        emitter.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(effectiveType).GetConstructor(new[] { effectiveType }));
                }
                else
                {
                    throw new InvalidCastException($"Cannot convert field {column.PropertyInfo.DeclaringType.Name}.{column.Name}, {sourceType.Name} to {effectiveType.Name}");
                }
            }
        }

        private static void EmitDefaultValue(ILGenerator emitter, FieldInfo field)
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
                    emitter.Emit(OpCodes.Ldc_I4_0);
                }
                else if (field.FieldType == typeof(bool))
                {
                    emitter.Emit(OpCodes.Ldc_I4_0);
                }
                else if (field.FieldType == typeof(decimal))
                {
                    // Decimal constants are created by loading the value onto the stack and then calling the constructor
                    emitter.Emit(OpCodes.Ldc_I4_0); // lo
                    emitter.Emit(OpCodes.Ldc_I4_0); // mid
                    emitter.Emit(OpCodes.Ldc_I4_0); // hi
                    emitter.Emit(OpCodes.Ldc_I4_0); // isNegative
                    emitter.Emit(OpCodes.Ldc_I4_0); // scale
                    emitter.Emit(OpCodes.Newobj, typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }));
                }
                else if (field.FieldType == typeof(Guid))
                {
                    emitter.Emit(OpCodes.Ldsfld, typeof(Guid).GetField("Empty"));
                }
                else if (field.FieldType == typeof(DateTime))
                {
                    emitter.Emit(OpCodes.Ldsfld, typeof(DateTime).GetField("MinValue"));
                }
                else if (field.FieldType == typeof(TimeSpan))
                {
                    emitter.Emit(OpCodes.Ldsfld, typeof(TimeSpan).GetField("Zero"));
                }
#if NET6_0_OR_GREATER
                else if (field.FieldType == typeof(DateOnly))
                {
                    emitter.Emit(OpCodes.Ldsfld, typeof(DateOnly).GetField("MinValue"));
                }
                else if (field.FieldType == typeof(TimeOnly))
                {
                    emitter.Emit(OpCodes.Ldsfld, typeof(TimeOnly).GetField("Midnight"));
                }
#endif
                else
                {
                    throw new NotSupportedException($"Unsupported type {field.FieldType.FullName}");
                }
            }
            else
            {
                emitter.Emit(OpCodes.Ldnull);
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

            if (!DbConfig.Tables.TryGetValue(type.FullName!, out var configuratedTable))
            {
                var dbTable = new DbTable { Name = type.Name!, Columns = new LinkedList<TData.Core.FluentApi.DbColumn>() };
                dbTable.AddFieldsAsColumns(type);
                DbConfig.Tables.TryAdd(type.FullName!, dbTable);
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
            byte[] buffer = new byte[bufferSize];
            long dataIndex = 0;
            using MemoryStream memoryStream = new MemoryStream();
            long bytesRead = reader.GetBytes(index, dataIndex, buffer, 0, bufferSize);

            while (bytesRead == bufferSize)
            {
                memoryStream.Write(buffer, 0, (int)bytesRead);
                dataIndex += bytesRead;
                bytesRead = reader.GetBytes(index, dataIndex, buffer, 0, bufferSize);
            }

            memoryStream.Write(buffer, 0, (int)bytesRead);

            return memoryStream.ToArray();
            
        }

    #endregion specific readers

    }
}
