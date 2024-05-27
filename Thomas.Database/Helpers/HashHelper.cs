using System;

namespace Thomas.Database
{
    public static class HashHelper
    {

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static int GenerateHash(in ReadOnlySpan<char> input)
#else
        public static int GenerateHash(string input)
#endif
        {
            unchecked
            {
                int hash = 27;

                foreach (char c in input)
                {
                    hash = (hash * 13) + c;
                }

                return hash;
            }
        }
    }
}
