using Thomas.Database.Core.Provider.Formatter;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Configuration
{
    /// <summary>
    /// Represents the settings for database configuration.
    /// </summary>
    public sealed class DbSettings
    {
        /// <summary>
        /// Buffer size for data BLOB or similar data types.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets the signature for the database settings.
        /// </summary>
        public readonly string Signature;

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        public readonly string StringConnection;

        /// <summary>
        /// Gets the SQL provider for the database.
        /// </summary>
        public readonly SqlProvider SqlProvider;

        /// <summary>
        /// Show detailed error message in error message and log, adding stored procedure name and parameters.
        /// </summary>
        public bool DetailErrorMessage { internal get; set; }

        /// <summary>
        /// Hide sensitive data values in error messages and logs.
        /// </summary>
        public bool HideSensibleDataValue { internal get; set; }

        /// <summary>
        /// Associate with the size of the data to be fetched for OracleCommand.
        /// </summary>
        public int FetchSize { internal get; set; } = -1;

        /// <summary>
        /// Prepare statements. This configuration will work only for direct SQL statements, not for stored procedures.
        /// Default is false. Consider enabling this option for:
        /// Oracle, PostgreSql, MySql.
        /// </summary>
        public bool PrepareStatements { internal get; set; }

        /// <summary>
        /// Sets the connection timeout.
        /// </summary>
        public int ConnectionTimeout { internal get; set; }

        /// <summary>
        /// Gets the SQL formatter based on the SQL provider.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DbSettings"/> class.
        /// </summary>
        /// <param name="signature">The signature for the database settings.</param>
        /// <param name="provider">The SQL provider for the database.</param>
        /// <param name="stringConnection">The connection string for the database.</param>
        public DbSettings(string signature, SqlProvider provider, string stringConnection)
        {
            Signature = signature;
            SqlProvider = provider;
            StringConnection = stringConnection;
        }
    }
}
