using System.Data;

namespace Thomas.Database.Database
{
    internal readonly struct DbCommandConfiguration
    {
        public readonly CommandBehavior CommandBehavior;
        public readonly MethodHandled MethodHandled;
        public readonly bool KeyAsReturnValue;
        public readonly bool GenerateParameterWithKeys;
        public readonly bool NoCacheMetaData;

        public DbCommandConfiguration(CommandBehavior commandBehavior, MethodHandled methodHandled, bool keyAsReturnValue, bool generateParameterWithKeys, bool noCacheMetadata)
        {
            CommandBehavior = commandBehavior;
            MethodHandled = methodHandled;
            KeyAsReturnValue = keyAsReturnValue;
            GenerateParameterWithKeys = generateParameterWithKeys;
            NoCacheMetaData = noCacheMetadata;
        }

        internal readonly bool IsTuple()
        {
            return MethodHandled == MethodHandled.ToTupleQueryString_2 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_3 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_4 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_5 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_6 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_7;
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = (hash * 23) + CommandBehavior.GetHashCode();
            hash = (hash * 23) + MethodHandled.GetHashCode();
            hash = (hash * 23) + KeyAsReturnValue.GetHashCode();
            hash = (hash * 23) + GenerateParameterWithKeys.GetHashCode();
            hash = (hash * 23) + NoCacheMetaData.GetHashCode();
            return hash;
        }
    }
}
