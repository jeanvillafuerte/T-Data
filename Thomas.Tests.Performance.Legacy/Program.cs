using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thomas.Database;
using Thomas.Database.SqlServer;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy
{
    class Program
    {
        public static string TableName { get; set; }
        public static bool CleanData { get; set; }
        public static IThomasDb Service { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Start Setup...");

            Setup();

            Console.WriteLine("End Setup...");

            var stopWatch = new Stopwatch();

            Console.WriteLine("");
            Console.WriteLine("Secuencial calls.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("Method ToList<>");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = Service.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName};", false);

                Console.WriteLine($"Iteration {i + 1}, Rows processed : {data.Count}");
                Console.WriteLine($"Elapse milliseconds: { stopWatch.ElapsedMilliseconds}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Method ToListOp<>");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = Service.ToListOp<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName};", false);

                Console.WriteLine($"Iteration {i + 1}, Rows processed : {data.Result.Count}");
                Console.WriteLine($"Elapse milliseconds: { stopWatch.ElapsedMilliseconds}");

                stopWatch.Reset();
            }


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("Method ToList<> from store procedure");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = Service.ToList<Person>(new { age = 5 }, $@"get_{TableName}");

                Console.WriteLine($"Iteration {i + 1}, Rows processed : {data.Count}");
                Console.WriteLine($"Elapse milliseconds: { stopWatch.ElapsedMilliseconds}");

                stopWatch.Reset();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("Method ToListOp<> from store procedure");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = Service.ToListOp<Person>(new { age = 5 }, $@"get_{TableName}");

                Console.WriteLine($"Iteration {i + 1}, Rows processed : {data.Result?.Count}");
                Console.WriteLine($"Elapse milliseconds: { stopWatch.ElapsedMilliseconds}");

                stopWatch.Reset();
            }

            Console.WriteLine("Start Cleaning...");

            Clean();

            Console.WriteLine("End Cleaning...");

            Console.ReadKey();
        }

        static void Setup()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection().AddJsonFile("dbsettings.json", true);

            var configuration = builder.Build();

            var str = configuration["connection"];
            var len = configuration["rows"];

            CleanData = bool.Parse(configuration["cleanData"]);

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddThomasSqlDatabase((options) => new ThomasDbStrategyOptions()
            {
                StringConnection = str,
                MaxDegreeOfParallelism = 1,
                ConnectionTimeout = 0
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            Service = serviceProvider.GetService<IThomasDb>();

            SetDataBase(Service, int.Parse(len));
        }

        static void SetDataBase(IThomasDb service, int length)
        {
            TableName = $"Person_{DateTime.Now.ToString("yyyyMMddhhmmss")}";

            string tableScriptDefinition = $@"IF (OBJECT_ID('{TableName}') IS NULL)
                                                BEGIN
																	
	                                                CREATE TABLE {TableName}
													(
		                                                Id			INT PRIMARY KEY IDENTITY(1,1),
		                                                UserName	VARCHAR(25),
		                                                FirstName	VARCHAR(500),
		                                                LastName	VARCHAR(500),
		                                                BirthDate	DATE,
		                                                Age			SMALLINT,
		                                                Occupation	VARCHAR(300),
		                                                Country		VARCHAR(240),
		                                                Salary		DECIMAL(20,2),
		                                                UniqueId	UNIQUEIDENTIFIER,
		                                                [State]		BIT,
		                                                LastUpdate	DATETIME
	                                                )

                                                END";

            var result = service.ExecuteOp(tableScriptDefinition, false);

            if (!result.Success)
            {
                throw new Exception(result.ErrorMessage);
            }

            var createSp = $"CREATE PROCEDURE get_{TableName} (@age SMALLINT) AS SELECT * FROM {TableName} WHERE Age = @age";

            result = service.ExecuteOp(createSp, false);

            if (!result.Success)
            {
                throw new Exception(result.ErrorMessage);
            }

            string data = $@"SET NOCOUNT ON
							DECLARE @IDX INT = 0
							WHILE @IDX < {length}
							BEGIN
								INSERT INTO {TableName} (UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate)
								VALUES ( REPLICATE('A',25), REPLICATE('A',500), REPLICATE('A',500), '1988-01-01', ROUND(RAND() * 100, 0), REPLICATE('A',300), REPLICATE('A',240), ROUND(RAND() * 10000, 2), NEWID(), ROUND(RAND(), 0), DATEADD(DAY, ROUND(RAND() * -12, 0), GETDATE()))
								SET @IDX = @IDX + 1;
							END";

            var dataResult = service.ExecuteOp(data, false);

            if (!dataResult.Success)
            {
                throw new Exception(dataResult.ErrorMessage);
            }


            var createIndexByAge = $"CREATE NONCLUSTERED INDEX IDX_{TableName}_01 on {TableName} (Age)";

            result = service.ExecuteOp(createIndexByAge, false);

            if (!result.Success)
            {
                throw new Exception(result.ErrorMessage);
            }

        }

        static void Clean()
        {
            if (CleanData)
            {
                Service.Execute($"DROP TABLE {TableName}", false);
            }
        }
    }
}
