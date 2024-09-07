namespace Thomas.Database.Tests
{
    public interface IDatabaseProvider
    {
        string ConnectionString { get; }
    }
}
