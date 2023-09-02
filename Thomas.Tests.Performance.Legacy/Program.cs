using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Cache.Factory;
using Thomas.Database;
using Thomas.Database.SqlServer;
using Thomas.Database.Strategy.Factory;
using Thomas.Tests.Performance.Legacy.Setup;

namespace Thomas.Tests.Performance.Legacy
{
    class Program
    {
        public static string TableName { get; set; }
        public static bool CleanData { get; set; }
        public static IDatabase Database1 { get; set; }
        public static IDatabase Database2 { get; set; }
        public static ICachedDatabase CachedResultDatabase { get; set; }

        static void Main(string[] args)
        {
            WriteStep("Starting setup...");
            Setup();
            WriteStep("Completed Setup...", true);

            WriteStep("Starting tests database1...");
            RunTestsDatabase(Database1, "db1");
            WriteStep("Completed tests database1...", true);

            WriteStep("Starting tests database2...");
            RunTestsDatabase(Database2, "db2");
            WriteStep("Completed tests database2...", true);

            //WriteStep("Starting tests database2 (result cached)...");
            //RunTestsCachedDatabase(CachedResultDatabase, "db2 (cached)");
            //WriteStep("Completed tests database2 (result cached)...", true);

            WriteStep("Starting tests database2 (async)...");
            RunTestsDatabaseAsync(Database2, "db2 (async)");
            WriteStep("Completed tests database2 (async)...", true);

            WriteStep("Dropping tables...");
            DropTables();
            WriteStep("Dropped tables.");

            Console.ReadKey();
        }

        static void WriteStep(string message, bool includeBlankLine = false)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            if (includeBlankLine)
                Console.WriteLine();
        }

        static void RunTestsDatabase(IDatabase database, string databaseName)
        {
            Task.WaitAll(
                Task.Run(() => new Tests.Single().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.List().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.Tuple().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.Procedures().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.Error().Execute(database, databaseName, TableName))
                );
        }

        static void RunTestsCachedDatabase(ICachedDatabase database, string databaseName)
        {
            Task.WaitAll(
                Task.Run(() => new Tests.Single().ExecuteCachedDatabase(database, databaseName, TableName)),
                Task.Run(() => new Tests.List().ExecuteCachedDatabase(database, databaseName, TableName)),
                //Task.Run(() => new Tests.Tuple().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.Procedures().ExecuteCachedDatabase(database, databaseName, TableName)),
                Task.Run(() => new Tests.Error().ExecuteCachedDatabase(database, databaseName, TableName))
                );

            database.ReleaseCache();
        }

        static void RunTestsDatabaseAsync(IDatabase database, string databaseName)
        {
            Task.WaitAll(
                Task.Run(async () => await new Tests.List().ExecuteAsync(database, databaseName, TableName)),
                Task.Run(() => new Tests.Tuple().Execute(database, databaseName, TableName)),
                Task.Run(() => new Tests.Procedures().ExecuteAsync(database, databaseName, TableName)),
                Task.Run(() => new Tests.Error().Execute(database, databaseName, TableName))
                );
        }

        static void Setup()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection().AddJsonFile("dbsettings.json", true);

            var configuration = builder.Build();

            var cnx1 = configuration["connection1"];
            var cnx2 = configuration["connection2"];
            var len = configuration["rows"];
            TableName = $"Person_{DateTime.Now.ToString("yyyyMMddhhmmss")}";
            CleanData = bool.Parse(configuration["cleanData"]);

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddDbFactory();
            serviceCollection.AddScoped<IDataBaseManager, DataBaseManager>();
            serviceCollection.AddSqlDatabase(new ThomasDbStrategyOptions { Signature = "db1", StringConnection = cnx1, ConnectionTimeout = 0, UseCache = false });
            serviceCollection.AddSqlDatabase(new ThomasDbStrategyOptions { Signature = "db2", StringConnection = cnx2, ConnectionTimeout = 0, UseCache = true });
            serviceCollection.AddDbResultCached();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var loadDataManager = serviceProvider.GetService<IDataBaseManager>();

            loadDataManager.LoadDatabases(int.Parse(len), TableName);

            Database1 = serviceProvider.GetRequiredService<IDbFactory>().CreateDbContext("db1");
            Database2 = serviceProvider.GetRequiredService<IDbFactory>().CreateDbContext("db2");
            CachedResultDatabase = serviceProvider.GetRequiredService<IDbResultCachedFactory>().CreateDbContext("db2");
        }

        static void DropTables()
        {
            DataBaseManager.DropTable(Database1, true, TableName);
            DataBaseManager.DropTable(Database2, true, TableName);
        }
    }
}
