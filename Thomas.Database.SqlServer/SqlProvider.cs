using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace Thomas.Database.SqlServer
{
    public class SqlProvider : IDatabaseProvider
    {
        private Dictionary<string, SqlDbType> DbTypes;

        public ThomasDbStrategyOptions Options { get; }

        static SqlProvider()
        {
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        }

        public SqlProvider(ThomasDbStrategyOptions options)
        {
            Options = options;
            LoadDbTypes();
        }

        private void LoadDbTypes()
        {
            DbTypes = new Dictionary<string, SqlDbType>();
            DbTypes.Add("String", SqlDbType.VarChar);
            DbTypes.Add("Int16", SqlDbType.SmallInt);
            DbTypes.Add("Int32", SqlDbType.Int);
            DbTypes.Add("Int64", SqlDbType.BigInt);
            DbTypes.Add("Decimal", SqlDbType.Decimal);
            DbTypes.Add("Boolean", SqlDbType.Bit);
            DbTypes.Add("Date", SqlDbType.Date);
            DbTypes.Add("DateTime", SqlDbType.DateTime);
            DbTypes.Add("Double", SqlDbType.Real);
            DbTypes.Add("Xml", SqlDbType.Xml);
            DbTypes.Add("Guid", SqlDbType.UniqueIdentifier);
        }

        public DbCommand CreateCommand(string connection)
        {
            var cnx = new SqlConnection(connection);
            var cmd = new SqlCommand();
            cmd.Connection = cnx;
            cmd.CommandTimeout = Options.ConnectionTimeout;
            cnx.Open();
            return cmd;
        }

        public DbCommand CreateCommand(string connection, string user, SecureString password)
        {
            var credential = new SqlCredential(user, password);
            var cnx = new SqlConnection(connection, credential);
            var cmd = new SqlCommand();
            cmd.Connection = cnx;
            cmd.CommandTimeout = Options.ConnectionTimeout;
            cnx.Open();
            return cmd;
        }

        public DbCommand CreateCommand()
        {
            var cmd = new SqlCommand();
            cmd.CommandTimeout = Options.ConnectionTimeout;
            return cmd;
        }

        public DbConnection CreateConnection(string connection)
        {
            var cnx = new SqlConnection(connection);
            return cnx;
        }

        public DbConnection CreateConnection(string connection, string user, SecureString password)
        {
            var credential = new SqlCredential(user, password);
            var cnx = new SqlConnection(connection, credential);
            return cnx;
        }

        public DbParameter CreateParameter(string parameterName, object value, DbType type)
        {
            return new SqlParameter() { ParameterName = $"@{parameterName}", Value = value ?? DBNull.Value, SqlDbType = GetType(type) };
        }

        public DbTransaction CreateTransacion(string stringConnection)
        {
            var cnx = CreateConnection(stringConnection);

            cnx.Open();

            return cnx.BeginTransaction();
        }

        public DbTransaction CreateTransacion(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDataParameter[] ExtractValuesFromSearchTerm(object searchTerm)
        {
            var properties = searchTerm.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var parameters = new List<IDataParameter>();

            foreach (var property in properties)
            {
                var value = property.GetValue(searchTerm);

                int direccion = 1;

                foreach (var attribute in property.GetCustomAttributes(true))
                {
                    DirectionAttribute attr = attribute as DirectionAttribute;
                    if (attr != null)
                    {
                        direccion = (int)attr.Direction;
                        break;
                    }
                }

                parameters.Add(new SqlParameter()
                {
                    ParameterName = $"@{property.Name.ToLower()}",
                    Value = value ?? DBNull.Value,
                    SqlDbType = GetSqlDbType(property),
                    Direction = (ParameterDirection)direccion
                });
            }

            return parameters.ToArray();
        }

        private SqlDbType GetSqlDbType(PropertyInfo propertyInfo)
        {
            return DbTypes[propertyInfo.PropertyType.Name];
        }

        private SqlDbType GetType(DbType type)
        {
            switch (type)
            {
                case DbType.AnsiString:
                    return SqlDbType.NVarChar;
                case DbType.Binary:
                    return SqlDbType.VarBinary;
                case DbType.Byte:
                    return SqlDbType.TinyInt;
                case DbType.Boolean:
                    return SqlDbType.Bit;
                case DbType.Currency:
                    return SqlDbType.Money;
                case DbType.Date:
                case DbType.DateTime:
                    return SqlDbType.DateTime;
                case DbType.DateTime2:
                    return SqlDbType.DateTime2;
                case DbType.DateTimeOffset:
                    return SqlDbType.DateTimeOffset;
                case DbType.Decimal:
                    return SqlDbType.Decimal;
                case DbType.Double:
                    return SqlDbType.Float;
                case DbType.Guid:
                    return SqlDbType.UniqueIdentifier;
                case DbType.Int16:
                    return SqlDbType.SmallInt;
                case DbType.Int32:
                    return SqlDbType.Int;
                case DbType.Int64:
                    return SqlDbType.BigInt;
                case DbType.Object:
                    return SqlDbType.Variant;
                case DbType.SByte:
                    return SqlDbType.TinyInt;
                case DbType.Single:
                    return SqlDbType.Int;
                case DbType.String:
                    return SqlDbType.VarChar;
                case DbType.Time:
                    return SqlDbType.Time;
                case DbType.UInt16:
                    return SqlDbType.SmallInt;
                case DbType.UInt32:
                    return SqlDbType.Int;
                case DbType.UInt64:
                    return SqlDbType.BigInt;
                case DbType.VarNumeric:
                    return SqlDbType.BigInt;
                case DbType.AnsiStringFixedLength:
                    return SqlDbType.NChar;
                case DbType.StringFixedLength:
                    return SqlDbType.Char;
                case DbType.Xml:
                    return SqlDbType.Xml;
                default:
                    return SqlDbType.VarChar;
            }
        }
    }
}
