using System;

namespace Thomas.Database
{
    internal static class QueryValidators
    {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsStoredProcedure(ReadOnlySpan<char> input)
#else
        internal static bool IsStoredProcedure(string input)
#endif
        {
            var asd = new[] { ' ', '\t', '\n', '\r' };
            var trimmed = input.Trim();
            return trimmed.IndexOfAny(asd) == -1;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsAnonymousBlock(ReadOnlySpan<char> input)
#else
        internal static bool IsAnonymousBlock(string input)
#endif
        {
            return input.IndexOf("DECLARE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("BEGIN", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("EXEC", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("$$", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("SET", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("IF", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DO", StringComparison.OrdinalIgnoreCase) == 0;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsDML(ReadOnlySpan<char> input)
#else
        internal static bool IsDML(string input)
#endif
        {
            return input.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) == 0;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsDDL(ReadOnlySpan<char> input)
#else
        internal static bool IsDDL(string input)
#endif
        {
            return input.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("ALTER", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DROP", StringComparison.OrdinalIgnoreCase) == 0;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsDCL(ReadOnlySpan<char> input)
#else
        internal static bool IsDCL(string input)
#endif
        {
            return input.IndexOf("GRANT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("REVOKE", StringComparison.OrdinalIgnoreCase) == 0;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool ScriptExpectParameterMatch(ReadOnlySpan<char> input)
#else
        internal static bool ScriptExpectParameterMatch(string input)
#endif
        {
            var bindSymbols = new[] { '@', ':', '$' };
            return input.IndexOfAny(bindSymbols) > 0;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsInsert(ReadOnlySpan<char> input)
#else
        internal static bool IsSelect(string input)
#endif
        {
            return input.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
