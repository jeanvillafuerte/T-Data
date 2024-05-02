using System;

namespace Thomas.Database
{
    internal static class QueryValidators
    {
        internal static bool IsStoredProcedure(ReadOnlySpan<char> input)
        {
            var asd = new[] { ' ', '\t', '\n', '\r' };
            var trimmed = input.Trim();
            return trimmed.IndexOfAny(asd) == -1;
        }

        internal static bool IsAnonymousBlock(ReadOnlySpan<char> input)
        {
            return input.IndexOf("DECLARE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("BEGIN", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("EXEC", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("$$", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("SET", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("IF", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DO", StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static bool IsDML(ReadOnlySpan<char> input)
        {
            return input.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static bool IsDDL(ReadOnlySpan<char> input)
        {
            return input.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("ALTER", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("DROP", StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static bool IsDCL(ReadOnlySpan<char> input)
        {
            return input.IndexOf("GRANT", StringComparison.OrdinalIgnoreCase) == 0 ||
                   input.IndexOf("REVOKE", StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static bool ScriptExpectParameterMatch(ReadOnlySpan<char> input)
        {
            var bindSymbols = new[] { '@', ':' };
            return input.IndexOfAny(bindSymbols) > -1;
        }

        internal static bool IsInsert(ReadOnlySpan<char> input)
        {
            return input.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
