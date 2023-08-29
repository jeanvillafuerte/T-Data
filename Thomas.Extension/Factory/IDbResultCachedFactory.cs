namespace Thomas.Cache.Factory
{
    public interface IDbResultCachedFactory
    {
        ICachedDatabase CreateDbContext(string signature);
    }
}
