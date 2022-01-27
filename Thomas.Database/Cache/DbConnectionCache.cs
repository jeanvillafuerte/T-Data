using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace Thomas.Database.Cache
{
    internal sealed class DbConnectionCache
    {
        private static DbConnectionCache instance;

        internal static DbConnectionCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbConnectionCache();
                }
                return instance;
            }
        }

        private static readonly ConcurrentDictionary<string, DbConnection> Data = new ConcurrentDictionary<string, DbConnection>();

        internal void Set(string key, DbConnection value)
        {
            Data[SecureStringConnection.ComputeHash(key)] = value;
        }

        internal bool TryGet(string key, out DbConnection types)
        {
            return Data.TryGetValue(SecureStringConnection.ComputeHash(key), out types);
        }

        static class SecureStringConnection
        {
            public static string b;

            static SecureStringConnection()
            {
                string GuidString = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                b = GuidString.Replace("=", "").Replace("+", "");
            }

            public static string ComputeHash(string plainText)
            {
                byte[] saltBytes = Encoding.UTF8.GetBytes(b);

                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

                for (int i = 0; i < plainTextBytes.Length; i++)
                {
                    plainTextWithSaltBytes[i] = plainTextBytes[i];
                }

                for (int i = 0; i < saltBytes.Length; i++)
                {
                    plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
                }

                var hash = new SHA512Managed();

                byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

                byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashWithSaltBytes[i] = hashBytes[i];
                }

                for (int i = 0; i < saltBytes.Length; i++)
                {
                    hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
                }

                return Convert.ToBase64String(hashWithSaltBytes);
            }
        }
    }
}
