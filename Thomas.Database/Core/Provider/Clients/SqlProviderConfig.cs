namespace Thomas.Database.Core.Provider.Clients
{
    internal class SqlProviderConfig : IProviderConfig
    {
        public string[] SupportedAssemblies => new[] { "Microsoft.Data.SqlClient" };
        public string FullDbParameterType => "Microsoft.Data.SqlClient.SqlParameter";
        public string DbTypeProperty => "SqlDbType";
        public string FullDbType => "System.Data.SqlDbType";
    }
}
