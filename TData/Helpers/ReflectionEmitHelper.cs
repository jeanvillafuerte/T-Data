using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static TData.DatabaseCommand;
using TData.Core.Converters;

namespace TData.Helpers
{
    internal static class ReflectionEmitHelper
    {
        internal static readonly ConstructorInfo NullableGuidConstructor = typeof(Guid?).GetConstructor(new Type[] { typeof(Guid) })!;
        internal static readonly ConstructorInfo GuidConstructorString = typeof(Guid).GetConstructor(new Type[] { typeof(string) })!;
        internal static readonly ConstructorInfo TimeSpanConstructorTicks = typeof(TimeSpan).GetConstructor(new Type[] { typeof(long) })!;

        private static bool IsPrimitiveTypeConversion(Type propertyType, Type sourceType)
        {
            return propertyType.IsValueType && propertyType.IsPrimitive &&
                   sourceType.IsValueType && sourceType.IsPrimitive;
        }

        internal static OpCode GetStoreLocalValueOpCode(in int index) => index switch
        {
            1 => OpCodes.Stloc_1,
            2 => OpCodes.Stloc_2,
            3 => OpCodes.Stloc_3,
            _ => (index <= 255) ? OpCodes.Stloc_S :  OpCodes.Stloc
        };

        internal static OpCode GetLoadLocalValueOpCode(in int index) => index switch
        {
            1 => OpCodes.Ldloc_1,
            2 => OpCodes.Ldloc_2,
            3 => OpCodes.Ldloc_3,
            _ => (index <= 255) ? OpCodes.Ldloc_S : OpCodes.Ldloc
        };

        internal static void EmitInt32Value(in ILGenerator emitter, in int value)
        {
            switch (value)
            {
                case 0:
                    emitter.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    emitter.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    emitter.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    emitter.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    emitter.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    emitter.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    emitter.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    emitter.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    emitter.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        emitter.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        emitter.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;

            }
        }

        internal static void EmitConvert(in ILGenerator emitter, in Type primitiveType)
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

        internal static void EmitValueConversion(in ILGenerator emitter, in PropertyTypeInfo column, in Type underlyingType, in DbProvider provider)
        {
            Type propertyType = column.PropertyInfo.PropertyType;
            Type sourceType = column.Source;

            if (column.ForceCast)
            {
                emitter.Emit(OpCodes.Castclass, propertyType);
            }
            else if (IsPrimitiveTypeConversion(propertyType, sourceType))
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
            else if (provider.Equals(DbProvider.PostgreSql) && column.DataTypeName == "bit" && underlyingType == typeof(bool))
            {
                emitter.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(typeof(bool)).GetConstructor(new[] { typeof(bool) }));
            }
            else if (provider.Equals(DbProvider.PostgreSql) && column.DataTypeName == "bit" && propertyType == typeof(bool))
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

        internal static void EmitDefaultValue(in ILGenerator emitter, in FieldInfo field)
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
    }
}
