using System;
using System.Reflection.Emit;
using System.Reflection;
using Thomas.Database.Core.Provider;

namespace Thomas.Database.Core.QueryGenerator
{
    //Reduce memory footprint by using a single type for all generated types
    internal partial class SqlGenerator<T> : IParameterHandler where T : class, new()
    {
        internal static Type BuildType(ReadOnlySpan<DbParameterInfo> dbParametersToBind)
        {
            var assemblyName = new AssemblyName("ThomasInternalAssembly");
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(assemblyName.Name);
            TypeBuilder tb = mb.DefineType($"DynamicType{InternalCounters.GetNextTypeCounter()}", TypeAttributes.Public);

            var types = new Type[dbParametersToBind.Length];

            for (int i = 0; i < dbParametersToBind.Length; i++)
                types[i] = dbParametersToBind[i].PropertyType;

            ConstructorBuilder constructor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, types);
            ILGenerator ctorIL = constructor.GetILGenerator();

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            for (int i = 0; i < dbParametersToBind.Length; i++)
            {
                var dbParameter = dbParametersToBind[i];
                FieldBuilder field = tb.DefineField(dbParameter.Name, dbParameter.PropertyType, FieldAttributes.Public | FieldAttributes.InitOnly);

                // Generate IL for constructor to set readonly fields
                ctorIL.Emit(OpCodes.Ldarg_0); // Load the instance (this)
                EmitLoadArgument(ctorIL, i); // Load the object[index] argument
                if (!dbParameter.PropertyType.IsValueType)
                    ctorIL.Emit(OpCodes.Castclass, dbParameter.PropertyType); // Cast it if it's a reference type

                ctorIL.Emit(OpCodes.Stfld, field); // Set the field
                PropertyBuilder property = tb.DefineProperty(dbParameter.Name, PropertyAttributes.HasDefault, dbParameter.PropertyType, null);
                MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + dbParameter.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    dbParameter.PropertyType, Type.EmptyTypes);

                ILGenerator getIl = getPropMthdBldr.GetILGenerator();
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, field);
                getIl.Emit(OpCodes.Ret);
                property.SetGetMethod(getPropMthdBldr);
            }

            // Finish the constructor
            ctorIL.Emit(OpCodes.Ret);

            return tb.CreateTypeInfo().AsType();
        }

        static void EmitLoadArgument(ILGenerator il, int i)
        {
            if (i == 0) il.Emit(OpCodes.Ldarg_1);
            else if (i == 1) il.Emit(OpCodes.Ldarg_2);
            else if (i == 2) il.Emit(OpCodes.Ldarg_3);
            else if (i >= 3) il.Emit(OpCodes.Ldarg_S, i + 1);
        }
    }
}
