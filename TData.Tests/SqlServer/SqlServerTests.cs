namespace TData.Tests.SQLServer
{
    public class SQLServerTests : DbProviderTestsBase
    {
        protected override string ConnectionString => Environment.GetEnvironmentVariable("SqlServerCnx") ?? "Data Source=localhost;Initial Catalog=tempdb;Persist Security Info=True;User ID=sa;Password=Mssql_Test;TrustServerCertificate=true;packet size=2048;ApplicationIntent=ReadOnly;Min Pool Size=32;Max Pool Size=64;Pooling=true";
        protected override bool SkipStoreProcedures => false;
        protected override string BindVariable => "@";
        protected override DbProvider DbProvider => DbProvider.SqlServer;

        [Test, Order(1)]
        public void DropIfExistsTable()
        {
            var dbContext = DbHub.Use(DbSignature, buffered: false);

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("DROP TABLE IF EXISTS BOOK");
                db.Execute("DROP TABLE IF EXISTS APP_USER");
                db.Execute("DROP TABLE IF EXISTS USER_TYPE");
            });

            Assert.Pass();
        }

        [Test, Order(2)]
        public void CreateTable()
        {
            var dbContext = DbHub.Use(DbSignature, buffered: false);
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE TABLE BOOK(ID INT PRIMARY KEY IDENTITY(1,1), CONTENT NTEXT)");
                db.Execute("CREATE TABLE APP_USER(ID INT PRIMARY KEY IDENTITY(1,1), USER_TYPE_ID INT NOT NULL, NAME VARCHAR(50), STATE BIT, SALARY DECIMAL(15,4), BIRTHDAY DATE, USERCODE UniqueIdentifier, ICON VARBINARY(MAX))");
                db.Execute("CREATE TABLE USER_TYPE(ID INT PRIMARY KEY, NAME VARCHAR(50))");
            });

            Assert.Pass();
        }

        [Test, Order(3)]
        public void CreateStoreProcedures()
        {
            var dbContext = DbHub.Use(DbSignature, buffered: false);

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[GET_ALL]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [GET_ALL] AS' END");
                db.Execute("ALTER PROCEDURE GET_ALL AS SELECT * FROM APP_USER");
                db.Execute("IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[GET_USER]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [GET_USER] AS' END");
                db.Execute("ALTER PROCEDURE GET_USER(@user_id INT) AS SELECT * FROM APP_USER WHERE ID = @user_id");
                db.Execute("IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[GET_TOTALUSER]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [GET_TOTALUSER] AS' END");
                db.Execute("ALTER PROCEDURE GET_TOTALUSER(@total INT OUTPUT, @totalSalary DECIMAL(15,2) OUTPUT) AS SELECT @total = COUNT(*),  @totalSalary = SUM(ISNULL(Salary, 0)) FROM APP_USER");
                db.Execute("IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[GET_DATA]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [GET_DATA] AS' END");
                db.Execute("ALTER PROCEDURE GET_DATA AS SELECT * FROM APP_USER; SELECT * FROM USER_TYPE");
                db.Execute("IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[BULK_INSERT_USER_DATA]') AND type in (N'P', N'PC')) BEGIN EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [BULK_INSERT_USER_DATA] AS' END");
                db.Execute(@"ALTER PROCEDURE BULK_INSERT_USER_DATA(@total INT) AS 
                            DECLARE @IDX INT = 0
							WHILE @IDX < @total
							BEGIN
								INSERT INTO APP_USER (
                                        USER_TYPE_ID,
                                        NAME,
                                        STATE,
                                        SALARY,
                                        BIRTHDAY,
                                        USERCODE,
                                        ICON)
								VALUES 
                                     (
                                        FLOOR(1 + (RAND() * 10)),
                                        CONCAT('User_', FLOOR(1 + (RAND() * 10000))),
                                        CASE WHEN RAND() < 0.5 THEN 1 ELSE 0 END,
                                        ROUND(RAND() * 100000, 4),
                                        DATEADD(DAY, ROUND(RAND() * -12, 0), GETDATE()),
                                        NEWID(),
                                        NULL)
								SET @IDX = @IDX + 1;
							END;");
            });
            
            Assert.Pass();
        }

        [Test, Order(5)]
        public void InsertUserType()
        {
            var dbContext = DbHub.Use(DbSignature);
            dbContext.ExecuteBlock((db) =>
            {
                db.Insert(new UserType(1, "Administrator"));
                db.Insert(new UserType(2, "Operator"));
                db.Insert(new UserType(3, "Regular"));
            });
        }

        [Test, Order(5)]
        public void InsertDataAndReturnNewId()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 1, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            Assert.That(Convert.ToInt32(id), Is.GreaterThan(0));
        }

        [Test, Order(6)]
        public void InsertUser()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "Peter", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 2, "Jean", true, 1346.23m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 1, "John", true, 6344.98m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(7)]
        public void UpdateUser()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "rocket.png"));
            dbContext.Update(new User(1, 3, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(8)]
        public void SimpleQueryUser()
        {
            var dbContext = DbHub.Use(DbSignature);
            var users = dbContext.FetchList<User>();
            Assert.That(users, Is.Not.Null);
        }

        [Test, Order(9)]
        public void InsertDummyRecords()
        {
            var dbContext = DbHub.Use(DbSignature);
            dbContext.Execute("BULK_INSERT_USER_DATA", new { total = 5000 });
            Assert.Pass();
        }

        [Test]
        public void SystemVariables()
        {
            var dbContext = DbHub.Use(DbSignature);
            var version = dbContext.ExecuteScalar<string>("SELECT @@VERSION");
            Assert.That(version, Is.Not.Null);
        }
    }
}