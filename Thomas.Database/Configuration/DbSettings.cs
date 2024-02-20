using System.Globalization;
using Thomas.Database.Core.Provider;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database
{
    public sealed class DbSettings
    {
        public readonly string Signature;
        public readonly string StringConnection;
        public readonly CultureInfo CultureInfo;
        public readonly SqlProvider SqlProvider;
        /// <summary>
        /// Show detail error message in error message and log adding store procedure name and parameters
        /// </summary>
        public bool DetailErrorMessage { get; set; }

        /// <summary>
        /// Hide sensible data value in error message and log
        /// </summary>
        public bool HideSensibleDataValue { get; set; }
        public int ConnectionTimeout { get; set; } = 0;
        internal ISqlFormatter SQLValues
        {
            get
            {
                if (SqlProvider == SqlProvider.SqlServer)
                    return new SqlServerFormatter();
                if (SqlProvider == SqlProvider.Oracle)
                    return new OracleFormatter();

                return null;
            }
        }

        public DbSettings(string signature, SqlProvider provider, string stringConnection, string culture = "en-US")
        {
            Signature = signature;
            SqlProvider = provider;
            StringConnection = stringConnection;
            CultureInfo = string.IsNullOrEmpty(culture) ? CultureInfo.InvariantCulture : new CultureInfo(culture);
        }
    }

    public enum SqlProvider
    {
        SqlServer,
        MySql,
        PostgreSql,
        Oracle,
        Sqlite
    }
}
