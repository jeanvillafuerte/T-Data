using System;
using System.Security;

namespace Thomas.Database
{
    public sealed class ThomasDbStrategyOptions
    {
        public string Signature { get; set; }
        public string Culture { get; set; } = "en-US";
        public string User { get; set; }
        public SecureString Password { get; set; }
        public string StringConnection { get; set; }

        public bool DetailErrorMessage { get; set; }
        public bool SensitiveDataLog { get; set; }
        public bool StrictMode { get; set; }
        public uint ThresholdParallelism { get; set; } = 1000000;
        public int ConnectionTimeout { get; set; } = 0;
        public string ApplicationName { get; set; }
        public bool UseCache { get; set; } = true;
        public bool UseCompressedCacheStrategy { get; set; }
    }
}
