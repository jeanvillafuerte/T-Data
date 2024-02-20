namespace Thomas.Database.Core.Provider.Clients
{
    internal interface IProviderConfig
    {
        string[] SupportedAssemblies { get; }
        string FullDbParameterType { get; }
        string DbTypeProperty { get; }
        string FullDbType { get; }
    }
}
