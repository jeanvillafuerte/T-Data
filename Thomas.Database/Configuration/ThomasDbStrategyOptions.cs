using System.Security;

namespace Thomas.Database
{
    public sealed class ThomasDbStrategyOptions
    {
        public string Culture { get; set; } = "en-US";
        public string User { get; set; }
        public SecureString Password { get; set; }
        public string StringConnection { get; set; }
        public TypeMatchConvention TypeMatchConvention { get; set; }

        public bool DetailErrorMessage { get; set; }
        public bool SensitiveDataLog { get; set; }
        public bool StrictMode { get; set; }
        public int MaxDegreeOfParallelism { get; set; } = 1;
        public int ConnectionTimeout { get; set; } = 0;
    }
}
