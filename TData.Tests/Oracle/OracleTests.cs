namespace TData.Tests.Oracle
{
    public class OracleTests : DbProviderTestsBase
    {
        protected override string ConnectionString => Environment.GetEnvironmentVariable("OracleCnx") ?? "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=FREEPDB1)));Persist Security Info=True;User ID=SYS;DBA Privilege=SYSDBA;Password=Oracle_Test;Statement Cache Size=10";
        protected override string BindVariable => ":";
        protected override bool SkipStoreProcedures => false;
        protected override DbProvider DbProvider => DbProvider.Oracle;

        [Test, Order(1)]
        public void DropIfExistsTable()
        {
            var dbContext = DbHub.Use(DbSignature);
            dbContext.Execute(@"BEGIN
                                    EXECUTE IMMEDIATE 'DROP TABLE BOOK';
                                    EXECUTE IMMEDIATE 'DROP TABLE APP_USER';
                                    EXECUTE IMMEDIATE 'DROP TABLE USER_TYPE';
                                EXCEPTION WHEN OTHERS THEN NULL;
                                END;");
            Assert.Pass();
        }

        [Test, Order(2)]
        public void CreateTable()
        {
            var dbContext = DbHub.Use(DbSignature);
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE TABLE BOOK(ID NUMBER(*,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 99999 INCREMENT BY 1 START WITH 1 NOT NULL PRIMARY KEY, CONTENT NCLOB)");
                db.Execute(@"CREATE TABLE APP_USER(
                                ID NUMBER(*,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 99999 INCREMENT BY 1 START WITH 1 NOT NULL PRIMARY KEY,
                                USER_TYPE_ID NUMBER(*,0) NOT NULL,
                                NAME VARCHAR2(50), 
                                STATE NUMBER(1), 
                                SALARY NUMBER(15,4), 
                                BIRTHDAY DATE, 
                                USERCODE RAW(16), 
                                ICON BLOB)");
                db.Execute(@"CREATE TABLE USER_TYPE(
                                ID NUMBER(*,0) NOT NULL PRIMARY KEY,
                                NAME VARCHAR2(50))");
            });
            Assert.Pass();
        }

        [Test, Order(3)]
        public void CreateStoreProcedures()
        {
            var dbContext = DbHub.Use(DbSignature);

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE OR REPLACE PROCEDURE GET_ALL(rCursor OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor FOR SELECT * FROM APP_USER; END;");
                db.Execute("CREATE OR REPLACE PROCEDURE GET_USER(user_id INTEGER, rCursor OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor FOR SELECT * FROM APP_USER WHERE ID = user_id; END;");
                db.Execute(@"CREATE OR REPLACE PROCEDURE GET_TOTALUSER(total OUT INTEGER, totalSalary OUT NUMBER) IS BEGIN SELECT COUNT(*), SUM(NVL(Salary, 0)) INTO total, totalSalary FROM APP_USER; END;");
                db.Execute("CREATE OR REPLACE PROCEDURE GET_DATA(rCursor1 OUT SYS_REFCURSOR, rCursor2 OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor1 FOR SELECT * FROM APP_USER; OPEN rCursor2 FOR SELECT * FROM USER_TYPE; END;");
                db.Execute(@"CREATE OR REPLACE PROCEDURE BULK_INSERT_USER_DATA(total INTEGER)
                                AS
                                BEGIN
                                    FOR i IN 1..total LOOP
                                        INSERT INTO APP_USER (
                                            USER_TYPE_ID, 
                                            NAME, 
                                            STATE, 
                                            SALARY, 
                                            BIRTHDAY, 
                                            USERCODE, 
                                            ICON
                                        ) 
                                        VALUES (
                                            TRUNC(DBMS_RANDOM.VALUE(1, 11)),
                                            CASE TRUNC(DBMS_RANDOM.VALUE(1, 6)) 
                                                WHEN 1 THEN 'John Doe'
                                                WHEN 2 THEN 'Jane Smith'
                                                WHEN 3 THEN 'Alice Johnson'
                                                WHEN 4 THEN 'Bob Brown'
                                                WHEN 5 THEN 'Charlie Davis'
                                            END,
                                            TRUNC(DBMS_RANDOM.VALUE(0, 2)),
                                            ROUND(DBMS_RANDOM.VALUE(30000, 100000), 4),
                                            TRUNC(SYSDATE - DBMS_RANDOM.VALUE(1, 50 * 365)),
                                            SYS_GUID(),
                                            NULL);
                                    END LOOP;
                                    COMMIT;
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
