using System;

namespace TData.Tests.Performance.Legacy.Setup
{
    public static class DataBaseManager
    {
        public static void LoadDatabases(int rows, string tableName)
        {
            SeedDataBase("db1", rows, tableName);
            SeedDataBase("db2", rows, tableName);
        }

        static void SeedDataBase(string db, int rows, string tableName)
        {
            DbHub.Use(db, buffered: false).ExecuteBlock((service) =>
            {
                string tableScriptDefinition = $@"IF (OBJECT_ID('{tableName}') IS NULL)
                                                BEGIN

	                                                CREATE TABLE {tableName}
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

                var checkSp1 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_{tableName}]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_{tableName}] AS' END ";

                result = service.TryExecute(checkSp1, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var checkSp2 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_byId]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_byId] AS' END ";

                result = service.TryExecute(checkSp2, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var checkSp3 = $"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[get_byAge]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [get_byAge] AS' END ";

                result = service.TryExecute(checkSp3, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp = $"ALTER PROCEDURE get_{tableName}(@age SMALLINT) AS SELECT * FROM {tableName} WHERE Age = @age";

                result = service.TryExecute(createSp, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp2 = $"ALTER PROCEDURE get_byId(@id INT, @username VARCHAR(25) OUTPUT) AS SELECT @username = UserName FROM {tableName} WHERE Id = @id";

                result = service.TryExecute(createSp2, null);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var createSp3 = $"ALTER PROCEDURE get_byAge(@age INT, @total INT OUTPUT) AS SELECT * FROM {tableName} WHERE Age = @age SELECT @total = COUNT(1) FROM {tableName} WHERE Age = @age";

                result = service.TryExecute(createSp3);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                string data = $@"SET NOCOUNT ON
							DECLARE @IDX INT = 0
							WHILE @IDX < {rows}
							BEGIN
								INSERT INTO {tableName} (UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate)
								VALUES ( REPLICATE('A', ROUND(RAND() * 25, 0)), REPLICATE('A', ROUND(RAND() * 500, 0)), REPLICATE('A', ROUND(RAND() * 500, 0)), '1988-01-01', ROUND(RAND() * 100, 0), REPLICATE('A', ROUND(RAND() * 300, 0)), REPLICATE('A', ROUND(RAND() * 240, 0)), ROUND(RAND() * 10000, 2), NEWID(), ROUND(RAND(), 0), DATEADD(DAY, ROUND(RAND() * -12, 0), GETDATE()))
								SET @IDX = @IDX + 1;
							END";

                var dataResult = service.TryExecute(data, null);

                if (!dataResult.Success)
                {
                    throw new Exception(dataResult.ErrorMessage);
                }

                var createIndexByAge = $"CREATE NONCLUSTERED INDEX IDX_{tableName}_01 on {tableName} (Age)";

                result = service.TryExecute(createIndexByAge, null);

                if (!result.Success)
                {
                    Console.WriteLine($"Error : {result.ErrorMessage}");
                }
            });
        }

        public static void DropTable(IDatabase service, bool clean, string tableName)
        {
            if (clean)
            {
                service.Execute($"DROP TABLE {tableName}", null);
            }
        }
    }
}
