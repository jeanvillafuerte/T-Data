namespace TData.Tests.MySQL
{
    public class MySQLTests : DbProviderTestsBase
    {
        protected override string ConnectionString => Environment.GetEnvironmentVariable("MySqlCnx") ?? "Server=localhost;Database=test;Uid=root;Pwd=Mysql_Test;";
        protected override string BindVariable => "@";
        protected override bool SkipStoreProcedures => false;
        protected override DbProvider DbProvider => DbProvider.MySql;

        [Test, Order(1)]
        public void DropIfExistsTable()
        {
            var dbContext = DbHub.Use(DbSignature);
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
            var dbContext = DbHub.Use(DbSignature);
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE TABLE BOOK(ID INT PRIMARY KEY AUTO_INCREMENT, CONTENT LONGTEXT)");
                db.Execute(@"CREATE TABLE APP_USER (
                                    ID INT PRIMARY KEY AUTO_INCREMENT,
                                    USER_TYPE_ID INT NOT NULL,
                                    NAME VARCHAR(50),
                                    STATE BOOLEAN,
                                    SALARY DECIMAL(15,4),
                                    BIRTHDAY DATE,
                                    USERCODE CHAR(36),
                                    ICON MEDIUMBLOB
                                )");
                db.Execute("CREATE TABLE USER_TYPE(ID INT PRIMARY KEY, NAME VARCHAR(50))");
            });
            Assert.Pass();
        }

        [Test, Order(3)]
        public void CreateStoreProcedures()
        {
            var dbContext = DbHub.Use(DbSignature);

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("DROP PROCEDURE IF EXISTS GET_ALL");
                db.Execute("CREATE PROCEDURE GET_ALL() BEGIN SELECT * FROM APP_USER; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_USER");
                db.Execute(@"CREATE PROCEDURE GET_USER(IN user_id INT) BEGIN SELECT * FROM APP_USER WHERE ID = user_id; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_TOTALUSER");
                db.Execute("CREATE PROCEDURE GET_TOTALUSER(OUT total INTEGER, OUT totalSalary DECIMAL(15,2)) BEGIN SELECT COUNT(*), SUM(COALESCE(Salary, 0)) INTO total, totalSalary FROM APP_USER; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_DATA");
                db.Execute("CREATE PROCEDURE GET_DATA() BEGIN SELECT * FROM APP_USER; SELECT * FROM USER_TYPE; END;");
                db.Execute("DROP PROCEDURE IF EXISTS BULK_INSERT_USER_DATA");
                db.Execute(@"CREATE PROCEDURE BULK_INSERT_USER_DATA(IN total INT)
                            BEGIN
                                DECLARE i INT DEFAULT 1;
                                WHILE i <= total DO
                                    INSERT INTO APP_USER (
                                        USER_TYPE_ID,
                                        NAME,
                                        STATE,
                                        SALARY,
                                        BIRTHDAY,
                                        USERCODE,
                                        ICON
                                    ) VALUES (
                                        FLOOR(1 + (RAND() * 10)),
                                        CONCAT('User_', FLOOR(1 + (RAND() * 10000))),
                                        RAND() < 0.5,
                                        ROUND(RAND() * 100000, 4),
                                        DATE_ADD('1950-01-01', INTERVAL FLOOR(RAND() * 25550) DAY),
                                        UUID(),
                                        NULL
                                    );
                                    SET i = i + 1;
                                END WHILE;
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
            dbContext.Update(new User(1, 2, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(8)]
        public void Query()
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
    }
}
