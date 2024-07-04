using Thomas.Database.Core.Provider.Formatter;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Configuration
{
    public sealed class DbSettings
    {
        /// <summary>
        /// Buffer size for data BLOB or similar data types
        /// </summary>
        public int BufferSize { get; set; }

        public readonly string Signature;
        public readonly string StringConnection;

        public readonly SqlProvider SqlProvider;
        /// <summary>
        /// Show detail error message in error message and log adding store procedure name and parameters
        /// </summary>
        public bool DetailErrorMessage { get; set; }

        /// <summary>
        /// Hide sensible data value in error message and log
        /// </summary>
        public bool HideSensibleDataValue { get; set; }

        /// <summary>
        /// Associate with the size of the data to be fetched for OracleCommand
        /// </summary>
        public int FetchSize { get; set; } = -1;

        /// <summary>
        /// Prepare statements, this configuration will work only for direct SQL statements not for store procedures
        /// Default is false
        /// Consider enable this option for:
        /// Oracle
        /// PostgreSql https://www.npgsql.org/doc/prepare.html
        /// MySql
        /// </summary>
        public bool PrepareStatements { get; set; }

        public int ConnectionTimeout { get; set; }

        internal ISqlFormatter SQLValues
        {
            get
            {
                return SqlProvider switch
                {
                    SqlProvider.SqlServer => new SqlServerFormatter(),
                    SqlProvider.MySql => new MySqlFormatter(),
                    SqlProvider.PostgreSql => new PostgreSqlFormatter(),
                    SqlProvider.Oracle => new OracleFormatter(),
                    SqlProvider.Sqlite => new SqliteFormatter(),
                    _ => throw new System.NotSupportedException(),
                };
            }
        }

        public DbSettings(string signature, SqlProvider provider, string stringConnection)
        {
            Signature = signature;
            SqlProvider = provider;
            StringConnection = stringConnection;
        }
    }
}
