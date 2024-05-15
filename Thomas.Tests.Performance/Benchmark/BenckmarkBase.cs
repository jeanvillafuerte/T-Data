using System;
using Microsoft.Extensions.Configuration;
using BenchmarkDotNet.Attributes;
using Thomas.Cache;
using Thomas.Cache.Factory;
using Thomas.Database;
using Thomas.Database.Configuration;
using Thomas.Tests.Performance.Entities;
using Microsoft.EntityFrameworkCore;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Tests.Performance.Benchmark
{
    [BenchmarkCategory("ORM")]
    public class BenckmarkBase
    {
        protected string TableName;
        protected bool CleanData;
        protected string StringConnection;

        public void Start()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection().AddJsonFile("dbsettings.json", true);

            var configuration = builder.Build();

            var cnx = configuration["connection"];
            var len = configuration["rows"];

            StringConnection = cnx;
            CleanData = bool.Parse(configuration["cleanData"]);

            DbConfigurationFactory.Register(new DbSettings("db", SqlProvider.SqlServer, cnx));
            SetDataBase(int.Parse(len), out var tableName);

            var tableBuilder = new TableBuilder();
            tableBuilder.Configure<Person>(x => x.Id).AddFieldsAsColumns<Person>().DbName(tableName);
            tableBuilder.Configure<PersonReadonlyRecord>(x => x.Id).AddFieldsAsColumns<PersonReadonlyRecord>().DbName(tableName);
            DbFactory.AddDbBuilder(tableBuilder);
        }

        void SetDataBase(int length, out string tableName)
        {
            tableName = $"Person_{DateTime.Now:yyyyMMddhhmmss}";
            TableName = tableName;

            DbFactory.GetDbContext("db").ExecuteBlock((service) =>
            {
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

                var result = service.ExecuteOp(tableScriptDefinition, null, noCacheMetadata: true);

                if (!result.Success)
                {
                    throw new Exception(result.ErrorMessage);
                }

                var checkSp1 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_persons]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_persons] AS' END ";

                result = service.ExecuteOp(checkSp1, null, noCacheMetadata: true);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var checkSp2 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_byId]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_byId] AS' END ";

                result = service.ExecuteOp(checkSp2, null, noCacheMetadata: true);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp1 = $"ALTER PROCEDURE get_persons(@age SMALLINT) AS SELECT * FROM {TableName} WHERE Age = @age";

                result = service.ExecuteOp(createSp1, null, noCacheMetadata: true);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp2 = $"ALTER PROCEDURE get_byId(@id INT, @username VARCHAR(25) OUTPUT) AS SELECT @username = UserName FROM {TableName} WHERE Id = @id";

                result = service.ExecuteOp(createSp2, null, noCacheMetadata: true);

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

                var dataResult = service.ExecuteOp(data, null, noCacheMetadata: true);

                if (!dataResult.Success)
                {
                    throw new Exception(dataResult.ErrorMessage);
                }

                var createIndexByAge = $"CREATE NONCLUSTERED INDEX IDX_{TableName}_01 on {TableName} (Age)";

                result = service.ExecuteOp(createIndexByAge, null, noCacheMetadata: true);

                if (!result.Success)
                {
                    Console.WriteLine(result.ErrorMessage);
                }
            });
        }

        protected void Clean()
        {
            if (CleanData)
            {
                DbFactory.GetDbContext("db").Execute($"DROP TABLE {TableName}", null, noCacheMetadata: true);
            }
        }
    }

    public class PersonContext : DbContext
    {
        private readonly string _stringConnection;

        public PersonContext(string stringConnection)
        {
            _stringConnection = stringConnection;
        }
        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_stringConnection);
        }

        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().ToTable("Person");
        }
    }
}
