namespace TData.Tests.SQLite
{
    public class SQLiteTests : DbProviderTestsBase
    {
        protected override string ConnectionString => "Data Source=.\\database_test.db";
        protected override string BindVariable => "@";
        protected override bool SkipStoreProcedures => true;
        protected override DbProvider DbProvider => DbProvider.Sqlite;

        [Test, Order(2)]
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

        [Test, Order(3)]
        public void CreateTable()
        {
            var dbContext = DbHub.Use(DbSignature, buffered: false);
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE TABLE BOOK(ID INTEGER PRIMARY KEY, CONTENT TEXT)");
                db.Execute("CREATE TABLE APP_USER(ID INTEGER PRIMARY KEY, USER_TYPE_ID INTEGER NOT NULL, NAME TEXT, STATE INTEGER, SALARY REAL, BIRTHDAY TEXT, USERCODE TEXT, ICON BLOB)");
                db.Execute("CREATE TABLE USER_TYPE(ID INTEGER NOT NULL, NAME TEXT)");
            });
            Assert.Pass();
        }

        [Test, Order(4)]
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
            var id = dbContext.Insert<User, int>(new User(0, 3, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            Assert.That(Convert.ToInt32(id), Is.GreaterThan(0));
        }

        [Test, Order(6)]
        public void InsertData()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "Peter", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 2, "Jean", true, 1346.23m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 1, "John", true, 6344.98m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(7)]
        public void UpdateData()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "rocket.png"));
            dbContext.Update(new User(1, 3, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            var user = dbContext.FetchOne<User>(x => x.Id == 1);
            Assert.That(user.Name, Is.EqualTo("Paul"));
            Assert.That(icon.Length, Is.EqualTo(user.Icon.Length));
        }

        [Test, Order(8)]
        public void InsertDummyRecords()
        {
            var dbContext = DbHub.Use(DbSignature);
            dbContext.Execute(@"WITH RECURSIVE
                                    generate_numbers AS (
                                        SELECT 1 AS n
                                        UNION ALL
                                        SELECT n + 1
                                        FROM generate_numbers
                                        WHERE n < :total
                                    )
                                INSERT INTO APP_USER (
                                    USER_TYPE_ID, 
                                    NAME, 
                                    STATE, 
                                    SALARY, 
                                    BIRTHDAY, 
                                    USERCODE, 
                                    ICON
                                )
                                SELECT 
                                    ABS(RANDOM() % 10) + 1 AS USER_TYPE_ID,
                                    CASE ABS(RANDOM() % 5) 
                                        WHEN 0 THEN 'John Doe'
                                        WHEN 1 THEN 'Jane Smith'
                                        WHEN 2 THEN 'Alice Johnson'
                                        WHEN 3 THEN 'Bob Brown'
                                        ELSE 'Charlie Davis'
                                    END AS NAME,
                                    ABS(RANDOM() % 2) AS STATE,
                                    ROUND((30000 + (RANDOM() % 70001)) / 1.0, 4) AS SALARY,
                                    DATE('now', '-' || ABS(RANDOM() % (50 * 365)) || ' days') AS BIRTHDAY,
                                    LOWER(HEX(RANDOMBLOB(16))) AS USERCODE,
                                    NULL AS ICON
                                FROM generate_numbers", new { total = 5000 });
            Assert.Pass();
        }
    }
}
