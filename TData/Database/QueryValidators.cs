using System;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TData.Tests")]
namespace TData
{
    internal static class QueryValidators
    {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsStoredProcedure(ReadOnlySpan<char> input)
#else
        internal static bool IsStoredProcedure(in string input)
#endif
        {
            var invalidCharacters = new[] { ' ', '\t', '\n', '\r' };
            var trimmed = input.Trim();
            return trimmed.IndexOfAny(invalidCharacters) == -1;
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static bool IsAnonymousBlock(ReadOnlySpan<char> input)
#else
        internal static bool IsAnonymousBlock(string input)
#endif
        {
            var trimmed = input.Trim();
            return trimmed.IndexOf("DECLARE", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("BEGIN", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("EXEC", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("$$", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("SET", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("IF", StringComparison.OrdinalIgnoreCase) == 0 ||
                   trimmed.IndexOf("DO", StringComparison.OrdinalIgnoreCase) == 0;
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

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Skip SQL Server system variables prefixed with @@
                if (c == '@' && i + 1 < input.Length && input[i + 1] == '@')
                {
                    i++; // Skip next character '@'
                    continue;
                }

                // Skip PostgreSQL type casting (::)
                if (c == ':' && i + 1 < input.Length && input[i + 1] == ':')
                {
                    i++; // Skip next character ':'
                    continue;
                }

                // Skip PostgreSQL dollar-quoting scope ($$)
                if (c == '$' && i + 1 < input.Length && input[i + 1] == '$')
                {
                    i++; // Skip next character '$'
                    continue;
                }

                if (bindSymbols.Contains(c))
                {
                    return true; // Bind symbol found
                }
            }

            return false;
        }
    }
}
