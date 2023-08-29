using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Thomas.Database;
using Thomas.Database.SqlServer;
using Thomas.Database.Strategy.Factory;

namespace Thomas.Tests.Performance.Benchmark
{
    [BenchmarkCategory("ORM")]
    public class Setup
    {
        protected IDatabase service;

        protected string TableName;

        protected bool CleanData;

        public void Start()
        {

            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection().AddJsonFile("dbsettings.json", true);

            var configuration = builder.Build();

            var cnx = configuration["connection"];
            var len = configuration["rows"];

            CleanData = bool.Parse(configuration["cleanData"]);

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddDbFactory();
            serviceCollection.AddSqlDatabase(new ThomasDbStrategyOptions { Signature = "db", StringConnection = cnx, MaxDegreeOfParallelism = 1, ConnectionTimeout = 0 });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            service = serviceProvider.GetRequiredService<IDbFactory>().CreateDbContext("db");

            SetDataBase(service, int.Parse(len));
        }

        void SetDataBase(IDatabase service, int length)
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

            var checkSp1 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_persons]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_persons] AS' END ";

            result = service.ExecuteOp(checkSp1, false);

            if (!result.Success)
                throw new Exception(result.ErrorMessage);

            var checkSp2 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_byId]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_byId] AS' END ";

            result = service.ExecuteOp(checkSp2, false);

            if (!result.Success)
                throw new Exception(result.ErrorMessage);

            var createSp1 = $"ALTER PROCEDURE get_persons(@age SMALLINT) AS SELECT * FROM {TableName} WHERE Age = @age";

            result = service.ExecuteOp(createSp1, false);

            if (!result.Success)
                throw new Exception(result.ErrorMessage);

            var createSp2 = $"ALTER PROCEDURE get_byId(@id INT, @username VARCHAR(25) OUTPUT) AS SELECT @username = UserName FROM {TableName} WHERE Id = @id";

            result = service.ExecuteOp(createSp2, false);

            if (!result.Success)
                throw new Exception(result.ErrorMessage);

            string data = $@"SET NOCOUNT ON
							DECLARE @IDX INT = 0
							WHILE @IDX <= {length}
							BEGIN
								INSERT INTO {TableName} (UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate)
                                VALUES ( REPLICATE('A', ROUND(RAND() * 25, 0)), REPLICATE('A', ROUND(RAND() * 500, 0)), REPLICATE('A', ROUND(RAND() * 500, 0)), '1988-01-01', ROUND(RAND() * 100, 0), REPLICATE('A', ROUND(RAND() * 300, 0)), REPLICATE('A', ROUND(RAND() * 240, 0)), ROUND(RAND() * 10000, 2), NEWID(), ROUND(RAND(), 0), DATEADD(DAY, ROUND(RAND() * -12, 0), GETDATE()))
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

        protected void Clean()
        {
            if (CleanData)
            {
                service.Execute($"DROP TABLE {TableName}", false);
            }
        }
    }
}
