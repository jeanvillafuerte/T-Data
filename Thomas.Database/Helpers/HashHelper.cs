using System;
using System.Text.Json;

namespace Thomas.Database
{
    public static class HashHelper
    {
        static JsonSerializerOptions _options = new JsonSerializerOptions() { DefaultBufferSize = 1024, PropertyNamingPolicy = null, WriteIndented = false, MaxDepth = 3, ReadCommentHandling = JsonCommentHandling.Disallow };

        public static ulong GenerateHash(string query, in object parameters)
        {
            string json = string.Empty;

            if (parameters != null)
                JsonSerializer.Serialize(parameters, _options);

            return GenerateUniqueHash($"{query}{json}");
        }

        public static ulong GenerateUniqueHash(in ReadOnlySpan<char> span)
        {
            return GenerateHash(span);
        }

        public static ulong GenerateHash(in ReadOnlySpan<char> span)
        {
            ulong hash = 5381;

            foreach (char c in span)
            {
                hash = (hash << 5) + hash + c;
            }

            return hash;
        }

    }
}
