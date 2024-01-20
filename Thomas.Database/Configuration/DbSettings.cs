using System.Globalization;

namespace Thomas.Database
{
    public sealed class DbSettings
    {
        public readonly string Signature;
        public readonly string StringConnection;
        public readonly CultureInfo CultureInfo;

        /// <summary>
        /// Show detail error message in error message and log adding store procedure name and parameters
        /// </summary>
        public bool DetailErrorMessage { get; set; }

        /// <summary>
        /// Hide sensible data value in error message and log
        /// </summary>
        public bool HideSensibleDataValue { get; set; }
        public int ConnectionTimeout { get; set; } = 0;

        public DbSettings(string signature, string stringConnection, string culture = "en-US")
        {
            Signature = signature;
            StringConnection = stringConnection;
            CultureInfo = string.IsNullOrEmpty(culture) ? CultureInfo.InvariantCulture : new CultureInfo(culture);
        }
    }
}
