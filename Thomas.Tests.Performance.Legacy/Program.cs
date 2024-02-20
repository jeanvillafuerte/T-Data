using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thomas.Cache;
using Thomas.Cache.Factory;
using Thomas.Database;
using Thomas.Database.Configuration;

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
            Setup(out var rows);
            WriteStep("Completed Setup...", true);

            WriteStep("Starting tests database1...");
            RunTestsDatabase(Database1, "db1", rows);
            WriteStep("Completed tests database1...", true);

            WriteStep("Starting tests database2...");
            RunTestsDatabase(Database2, "db2", rows);
            WriteStep("Completed tests database2...", true);

            WriteStep("Starting tests database2 (result cached)...");
            RunTestsCachedDatabase(CachedResultDatabase, "db2 (cached)", rows);
            WriteStep("Completed tests database2 (result cached)...", true);

            WriteStep("Starting tests database2 (async)...");
            RunTestsDatabaseAsync(Database2, "db2 (async)", rows);
            WriteStep("Completed tests database2 (async)...", true);

            WriteStep("Dropping tables...");
            DropTables();
            WriteStep("Dropped tables.");

            Console.ReadKey();
        }

        static void Setup(out int rowsGenerated)
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

            serviceCollection.AddScoped<IDataBaseManager, DataBaseManager>();

            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

            DbConfigurationFactory.Register(new DbSettings("db1", SqlProvider.SqlServer, cnx1));
            DbConfigurationFactory.Register(new DbSettings("db2", SqlProvider.SqlServer, cnx2));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loadDataManager = serviceProvider.GetService<IDataBaseManager>();

            rowsGenerated = int.Parse(len);
            loadDataManager.LoadDatabases(rowsGenerated, TableName);

            Database1 = DbFactory.CreateDbContext("db1");
            Database2 = DbFactory.CreateDbContext("db2");
            CachedResultDatabase = CachedDbFactory.CreateContext("db2");
        }

        static void RunTestsDatabase(IDatabase database, string databaseName, int rows)
        {
            Task.WaitAll(
                Task.Run(() => new Tests.Expression(databaseName).Execute(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Single(databaseName).Execute(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.List(databaseName).Execute(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Tuple(databaseName).Execute(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Procedures(databaseName).Execute(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Error(databaseName).Execute(database, databaseName, TableName, rows))
                );
        }

        static void RunTestsCachedDatabase(ICachedDatabase database, string databaseName, int rows)
        {
            Task.WaitAll(
                Task.Run(() => new Tests.Expression(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Single(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.List(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Tuple(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Procedures(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows)),
                Task.Run(() => new Tests.Error(databaseName).ExecuteCachedDatabase(database, databaseName, TableName, rows))
                );

            database.Release();
        }

        static void RunTestsDatabaseAsync(IDatabase database, string databaseName, int rows)
        {
            Task.WaitAll(
                Task.Run(async () => await new Tests.Expression(databaseName).ExecuteAsync(database, databaseName, TableName, rows)),
                Task.Run(async () => await new Tests.List(databaseName).ExecuteAsync(database, databaseName, TableName, rows)),
                Task.Run(async () => await new Tests.Tuple(databaseName).ExecuteAsync(database, databaseName, TableName, rows)),
                Task.Run(async () => await new Tests.Procedures(databaseName).ExecuteAsync(database, databaseName, TableName, rows)),
                Task.Run(async () => await new Tests.Error(databaseName).ExecuteAsync(database, databaseName, TableName, rows))
                );
        }

        static void DropTables()
        {
            DataBaseManager.DropTable(Database1, true, TableName);
            DataBaseManager.DropTable(Database2, true, TableName);
            CachedResultDatabase.Release();
        }

        static void WriteStep(string message, bool includeBlankLine = false)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            if (includeBlankLine)
                Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
