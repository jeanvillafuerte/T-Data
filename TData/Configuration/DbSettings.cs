using System.Text;
using TData.Core.Provider.Formatter;
using TData.Core.QueryGenerator;

namespace TData.Configuration
{
    /// <summary>
    /// Represents the settings for database configuration.
    /// </summary>
    public sealed class DbSettings
    {
        public bool DefaultDb { get; set; }

        /// <summary>
        /// Buffer size for data BLOB or similar data types, default value: 8KB
        /// </summary>
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// Buffer size to handle long text data, default value: 1MB
        /// </summary>
        public int TextBufferSize { get; set; } = 1048576;

        /// <summary>
        /// Text treatment for long text data, default value: UTF8
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets the signature for the database settings.
        /// </summary>
        public readonly string Signature;

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        public string StringConnection { internal get; set; }

        /// <summary>
        /// Gets the SQL provider for the database.
        /// </summary>
        public readonly DbProvider SqlProvider;

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
                    DbProvider.SqlServer => new SqlServerFormatter(),
                    DbProvider.MySql => new MySqlFormatter(),
                    DbProvider.PostgreSql => new PostgreSqlFormatter(),
                    DbProvider.Oracle => new OracleFormatter(),
                    DbProvider.Sqlite => new SqliteFormatter(),
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
        public DbSettings(string signature, DbProvider provider, string stringConnection)
        {
            Signature = signature;
            SqlProvider = provider;
            StringConnection = stringConnection;
        }
    }
}
