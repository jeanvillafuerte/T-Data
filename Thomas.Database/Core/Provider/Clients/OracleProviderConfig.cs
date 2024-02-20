namespace Thomas.Database.Core.Provider.Clients
{
    internal class OracleProviderConfig : IProviderConfig
    {
        public string[] SupportedAssemblies => new[] { "Oracle.ManagedDataAccess" };
        public string FullDbParameterType => "Oracle.ManagedDataAccess.Client.OracleParameter";
        public string DbTypeProperty => "OracleDbType";
        public string FullDbType => "Oracle.ManagedDataAccess.Client.OracleDbType";
    }
}
