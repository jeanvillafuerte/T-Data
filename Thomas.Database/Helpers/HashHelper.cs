using System.Text;
using System.Text.Json;

namespace Thomas.Database
{
    public enum TypeCacheObject
    {
        ScriptDefinition,
        Input
    }

    public static class HashHelper
    {
        static JsonSerializerOptions _options = new JsonSerializerOptions() { DefaultBufferSize = 1024, PropertyNamingPolicy = null, WriteIndented = false, MaxDepth = 3, ReadCommentHandling = JsonCommentHandling.Disallow };

        public static string GenerateUniqueStringHash(string script)
        {
            ulong hash = 5381;
            foreach (char c in script)
            {
                hash = ((hash << 5) + hash) + c;
            }

            return hash.ToString("X");
        }

        public static uint GenerateUniqueHash(string script, string signature, object? inputData = null, TypeCacheObject type = TypeCacheObject.ScriptDefinition)
        {
            var json = JsonSerializer.Serialize(new { signature, script, value = inputData ?? "", type }, _options);
            return Fnv1aHash(Encoding.UTF8.GetBytes(json));
        }

        private static uint Fnv1aHash(byte[] data)
        {
            const int fnvPrime = 16777619;
            uint hash = 2166136261;

            foreach (byte byteVal in data)
            {
                hash ^= byteVal;
                hash *= fnvPrime;
            }

            return hash;
        }
    }
}
