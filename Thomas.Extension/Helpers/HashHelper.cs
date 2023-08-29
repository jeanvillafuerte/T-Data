using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace Thomas.Cache.Helpers
{
    internal enum TypeCacheObject {
        ScriptDefinition,
        Input
    }

    internal static class HashHelper
    {
        internal static uint GenerateUniqueHash(string script, string signature, object? inputData = null, TypeCacheObject type = TypeCacheObject.ScriptDefinition)
        {
            var options = new JsonSerializerOptions()
            {
                DefaultBufferSize = 1024,
                PropertyNamingPolicy = null,
                WriteIndented = false,
                IgnoreNullValues = true,
                MaxDepth = 3,
                ReadCommentHandling = JsonCommentHandling.Disallow
            };

            var json = JsonSerializer.Serialize(new { signature, script, value = inputData ?? "", type }, options);
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
