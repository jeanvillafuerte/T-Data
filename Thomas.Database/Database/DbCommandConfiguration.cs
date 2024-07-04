using System.Data;

namespace Thomas.Database.Database
{
    internal readonly struct DbCommandConfiguration
    {
        public readonly CommandBehavior CommandBehavior;
        public readonly MethodHandled MethodHandled;
        public readonly bool KeyAsReturnValue;
        public readonly bool GenerateParameterWithKeys;

        public DbCommandConfiguration(CommandBehavior commandBehavior, MethodHandled methodHandled, bool keyAsReturnValue, bool generateParameterWithKeys)
        {
            CommandBehavior = commandBehavior;
            MethodHandled = methodHandled;
            KeyAsReturnValue = keyAsReturnValue;
            GenerateParameterWithKeys = generateParameterWithKeys;
        }

        readonly internal bool IsTuple()
        {
            return MethodHandled == MethodHandled.ToTupleQueryString_2 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_3 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_4 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_5 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_6 ||
                    MethodHandled == MethodHandled.ToTupleQueryString_7;
        }

        readonly internal bool EligibleForAddOracleCursors() => MethodHandled == MethodHandled.ToListQueryString || MethodHandled == MethodHandled.ToSingleQueryString || IsTuple();

        public override int GetHashCode()
        {
            var hash = 17;
            hash = (hash * 23) + CommandBehavior.GetHashCode();
            hash = (hash * 23) + MethodHandled.GetHashCode();
            hash = (hash * 23) + KeyAsReturnValue.GetHashCode();
            hash = (hash * 23) + GenerateParameterWithKeys.GetHashCode();
            return hash;
        }
    }
}
