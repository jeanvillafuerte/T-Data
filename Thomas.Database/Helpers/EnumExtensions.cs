#if !(NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System;

namespace Thomas.Database.Core.Provider
{
    internal static class EnumExtensions
    {
        public static bool TryParse(Type enumType, string value, bool ignoreCase, out object result)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("enumType parameter is not an Enum type.", nameof(enumType));

            result = null;

            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                result = Enum.Parse(enumType, value, ignoreCase);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
#endif