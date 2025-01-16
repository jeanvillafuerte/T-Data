using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BenchmarkDotNet.Attributes;
using TData.Configuration;
using TData.Tests.Performance.Entities;
using TData.Cache;


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

        #region JSON handlers
        class CacheItemConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    return JArray.Load(reader).ToObject(objectType);
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    return JObject.Load(reader).ToObject(objectType);
                }
                else
                {
                    throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing {objectType}.");
                }
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        JsonSerializerSettings JSONSettings = new JsonSerializerSettings()
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ss",
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
        };

        object JSONDeserialize(in object rawData, in Type type, in bool treatAsList) =>
                            JsonConvert.DeserializeObject((string)rawData, type, settings: new JsonSerializerSettings { Converters = new List<JsonConverter> { new CacheItemConverter() } });

        object JSONSerialize(in object data) =>
                        JsonConvert.SerializeObject(data, settings: JSONSettings);

        #endregion

        public void Start()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection().AddJsonFile("dbsettings.json", true);

            var configuration = builder.Build();

            var cnx = configuration["connection"];
            var len = configuration["rows"];

            StringConnection = cnx;
            CleanData = bool.Parse(configuration["cleanData"]);

            DbConfig.Register(new DbSettings("db", DbProvider.SqlServer, cnx));
            DbCacheConfig.Register(new DbSettings("dbCached_inmemory", DbProvider.SqlServer, cnx) { BufferSize = 4096 }, new CacheSettings(DbCacheProvider.InMemory) {  TTL = TimeSpan.FromSeconds(100) });
            DbCacheConfig.Register(new DbSettings("dbCached_sqlite", DbProvider.SqlServer, cnx) { BufferSize = 4096 }, new CacheSettings(DbCacheProvider.Sqlite, isTextFormat: true, JSONSerialize, JSONDeserialize) { TTL = TimeSpan.FromSeconds(100) });

            SetDataBase(int.Parse(len), out var tableName);

            var tableBuilder = new TableBuilder();
            tableBuilder.AddTable<Person>(x => x.Id).AddFieldsAsColumns<Person>().DbName(tableName);
#if NETCOREAPP
            tableBuilder.AddTable<PersonReadonlyRecord>(x => x.Id).AddFieldsAsColumns<PersonReadonlyRecord>().DbName(tableName);
#endif
            DbHub.AddTableBuilder(tableBuilder);
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
