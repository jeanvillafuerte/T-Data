using System;
using Microsoft.Extensions.Configuration;
using BenchmarkDotNet.Attributes;
using TData;
using TData.Configuration;
using TData.Tests.Performance.Entities;
#if NETCOREAPP
using Microsoft.EntityFrameworkCore;
#endif
using TData.Core.FluentApi;

namespace TData.Tests.Performance.Benchmark
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

            DbConfig.Register(new DbSettings("db", SqlProvider.SqlServer, cnx));
            SetDataBase(int.Parse(len), out var tableName);

            var tableBuilder = new TableBuilder();
            tableBuilder.AddTable<Person>(x => x.Id).AddFieldsAsColumns<Person>().DbName(tableName);
#if NETCOREAPP
            tableBuilder.AddTable<PersonReadonlyRecord>(x => x.Id).AddFieldsAsColumns<PersonReadonlyRecord>().DbName(tableName);
#endif
            DbHub.AddDbBuilder(tableBuilder);
        }

        void SetDataBase(int length, out string tableName)
        {
            tableName = $"Person_{DateTime.Now:yyyyMMddhhmmss}";
            TableName = tableName;

            DbHub.Use("db", buffered: false).ExecuteBlock((service) =>
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

                var result = service.TryExecute(tableScriptDefinition, null);

                if (!result.Success)
                {
                    throw new Exception(result.ErrorMessage);
                }

                var checkSp1 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_persons]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_persons] AS' END ";

                result = service.TryExecute(checkSp1, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var checkSp2 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_byId]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_byId] AS' END ";

                result = service.TryExecute(checkSp2, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp1 = $"ALTER PROCEDURE get_persons(@age SMALLINT) AS SELECT * FROM {TableName} WHERE Age = @age";

                result = service.TryExecute(createSp1, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp2 = $"ALTER PROCEDURE get_byId(@id INT, @username VARCHAR(25) OUTPUT) AS SELECT @username = UserName FROM {TableName} WHERE Id = @id";

                result = service.TryExecute(createSp2, null);

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

                var dataResult = service.TryExecute(data, null);

                if (!dataResult.Success)
                {
                    throw new Exception(dataResult.ErrorMessage);
                }

                var createIndexByAge = $"CREATE NONCLUSTERED INDEX IDX_{TableName}_01 on {TableName} (Age)";

                result = service.TryExecute(createIndexByAge, null);

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
                DbHub.Use("db", buffered: false).Execute($"DROP TABLE {TableName}", null);
            }
        }
    }

#if NETCOREAPP
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

#endif
}
