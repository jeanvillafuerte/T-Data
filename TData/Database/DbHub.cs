using TData.Configuration;
using TData.Core.FluentApi;

namespace TData
{
    /// <summary>
    /// Provides methods to interact with the database configurations and instances.
    /// </summary>
    public static class DbHub
    {
        /// <summary>
        /// Gets the default database instance.
        /// </summary>
        /// <param name="buffered">Indicates whether the database operations should be buffered.</param>
        /// <returns>An instance of <see cref="IDatabase"/> representing the default database.</returns>
        public static IDatabase GetDefaultDb(bool buffered = true)
        {
            var config = DbConfig.Get();
            return new DbBase(in config, in buffered);
        }

        /// <summary>
        /// Uses the specified database configuration.
        /// </summary>
        /// <param name="signature">The signature of the database configuration to use.</param>
        /// <param name="buffered">Indicates whether the database operations should be buffered.</param>
        /// <returns>An instance of <see cref="IDatabase"/> representing the specified database.</returns>
        /// <exception cref="SignatureNotFoundException">Thrown when the specified database configuration is not found.</exception>
        public static IDatabase Use(in string signature, in bool buffered = true)
        {
            var config = DbConfig.Get(in signature);
            if (config == null)
                throw new SignatureNotFoundException();

            return new DbBase(in config, in buffered);
        }

        /// <summary>
        /// Adds a table builder to the database configuration.
        /// </summary>
        /// <param name="builder">The table builder to add.</param>
        public static void AddTableBuilder(TableBuilder builder) => DbConfig.AddTableBuilder(in builder);
    }
}
