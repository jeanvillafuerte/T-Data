﻿using System.Data;
using TData.Configuration;
using TData.Core.FluentApi;
using TData.Core.QueryGenerator;
using TData.Attributes;

namespace TData.Tests.Oracle
{
    public class OracleTests : IDatabaseProvider
    {
        public string ConnectionString => Environment.GetEnvironmentVariable("OracleCnx") ?? "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=FREEPDB1)));Persist Security Info=True;User ID=SYS;DBA Privilege=SYSDBA;Password=Oracle_Test;Statement Cache Size=10";

        //global setup
        [OneTimeSetUp]
        public void Initialize()
        {
            DbConfig.Clear();
            var tableBuilder = new TableBuilder();
            var table = tableBuilder.AddTable<User>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<User>().DbName("USERS");
            table.Column<User>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            var table2 = tableBuilder.AddTable<UserNullableRecord>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<UserNullableRecord>().DbName("USERS");
            table2.Column<UserNullableRecord>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            var table3 = tableBuilder.AddTable<UserNullableClass>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<UserNullableClass>().DbName("USERS");
            table3.Column<UserNullableClass>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            tableBuilder.AddTable<UserType>(x => x.Id, keyAutoGenerated: false).AddFieldsAsColumns<UserType>().DbName("USER_TYPE");
            DbHub.AddDbBuilder(tableBuilder);
            DbConfig.Register(new DbSettings("db1", SqlProvider.Oracle, ConnectionString));
        }

        [Test, Order(1)]
        public void DropIfExistsTable()
        {
            var dbContext = DbHub.Use("db1");
            dbContext.Execute(@"BEGIN
                                    EXECUTE IMMEDIATE 'DROP TABLE USERS';
                                    EXECUTE IMMEDIATE 'DROP TABLE USER_TYPE';
                                EXCEPTION WHEN OTHERS THEN NULL;
                                END;");
            Assert.Pass();
        }

        [Test, Order(2)]
        public void CreateTable()
        {
            var dbContext = DbHub.Use("db1");
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute(@"CREATE TABLE USERS(
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
            var dbContext = DbHub.Use("db1");

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("CREATE OR REPLACE PROCEDURE GET_ALL(rCursor OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor FOR SELECT * FROM USERS; END;");
                db.Execute("CREATE OR REPLACE PROCEDURE GET_USER(id_param INTEGER, rCursor OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor FOR SELECT * FROM USERS WHERE ID = id_param; END;");
                db.Execute(@"CREATE OR REPLACE PROCEDURE GET_TOTALUSER(total OUT INTEGER, totalSalary OUT NUMBER) IS BEGIN SELECT COUNT(*), SUM(NVL(Salary, 0)) INTO total, totalSalary FROM USERS; END;");
                db.Execute("CREATE OR REPLACE PROCEDURE GET_DATA(rCursor1 OUT SYS_REFCURSOR, rCursor2 OUT SYS_REFCURSOR) IS BEGIN OPEN rCursor1 FOR SELECT * FROM USERS; OPEN rCursor2 FOR SELECT * FROM USER_TYPE; END;");
                db.Execute(@"CREATE OR REPLACE PROCEDURE BULK_INSERT_USER_DATA(total INTEGER)
                                AS
                                BEGIN
                                    FOR i IN 1..total LOOP
                                        INSERT INTO USERS (
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
            var dbContext = DbHub.Use("db1");
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
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 1, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            Assert.That(Convert.ToInt32(id), Is.GreaterThan(0));
        }

        [Test, Order(6)]
        public void InsertUser()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "Peter", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 2, "Jean", true, 1346.23m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 1, "John", true, 6344.98m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(7)]
        public void UpdateUser()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "rocket.png"));
            dbContext.Update(new User(1, 3, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(8)]
        public void Query()
        {
            var dbContext = DbHub.Use("db1");
            var users = dbContext.FetchList<User>();
            Assert.That(users, Is.Not.Null);
        }

        [Test, Order(9)]
        public void InsertDummyRecords()
        {
            var dbContext = DbHub.Use("db1");
            dbContext.Execute("BULK_INSERT_USER_DATA", new { total = 5000 });
            Assert.Pass();
        }

        [Test, Order(10)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public void FetchPageList(int pageSize)
        {
            var dbContext = DbHub.Use("db1");
            foreach (var items in dbContext.FetchPagedList<User>("SELECT * FROM USERS", offset: 0, pageSize, null))
            {
                Assert.That(items.Count, Is.GreaterThan(0));
            }
        }

        [Test, Order(10)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public void FetchPageRows(int pageSize)
        {
            var dbContext = DbHub.Use("db1");
            foreach (var items in dbContext.FetchPagedRows("SELECT * FROM USERS", offset: 0, pageSize, null))
            {
                Assert.That(items.Count, Is.GreaterThan(0));
            }
        }

        [Test, Order(10)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task FetchPageListAsync(int pageSize)
        {
            var dbContext = DbHub.Use("db1");
            await foreach (var items in dbContext.FetchPagedListAsync<User>("SELECT * FROM USERS", offset: 0, pageSize, null, CancellationToken.None))
            {
                Assert.That(items.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void UpdateIfSingleColumn()
        {
            var data = new[] { (11, 7777.6666m) };
            var context = DbHub.GetDefaultDb();
            context.UpdateIf<User>(x => x.Id == 1, (f => f.Salary, 7777.6666m));
            var user = context.FetchOne<User>(x => x.Id == 1);
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Salary, Is.EqualTo(7777.6666m));
        }

        [Test]
        public void UpdateIfMultipleColumns()
        {
            var context = DbHub.GetDefaultDb();
            context.UpdateIf<User>(x => x.Id == 1,
                (f => f.Salary, 7777.6666m),
                (f => f.Name, "TData 2")
            );

            var user = context.FetchOne<User>(x => x.Id == 1);
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Salary, Is.EqualTo(7777.6666m));
            Assert.That(user.Name, Is.EqualTo("TData 2"));
        }

        [Test]
        public void DeleteUser()
        {
            var dbContext = DbHub.GetDefaultDb();
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 1, "TData", true, 6666.888m, new DateTime(1984, 4, 8), Guid.NewGuid(), icon));
            Assert.That(id, Is.GreaterThan(0));
            var user = dbContext.FetchOne<User>(x => x.Id == id);
            Assert.That(user, Is.Not.Null);
            dbContext.Delete(user);
            Assert.Pass();
        }

        [Test]
        public void DeleteIf()
        {
            var dbContext = DbHub.GetDefaultDb();
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            int[] ids = new int[2];
            dbContext.ExecuteBlock((db) =>
            {
                ids[0] = db.Insert<User, int>(new User(0, 1, "Ron", true, 3323.45m, new DateTime(1984, 4, 8), Guid.NewGuid(), icon));
                ids[1] = db.Insert<User, int>(new User(0, 1, "Harry", true, 6534.32m, new DateTime(1984, 4, 8), Guid.NewGuid(), icon));
            });

            dbContext.DeleteIf<User>(x => ids.Contains(x.Id));
        }

        #region data types
        [Test]
        public void GuidTest()
        {
            var dbContext = DbHub.Use("db1");
            var param = new { Value = Guid.NewGuid() };
            var data = dbContext.ExecuteScalar<Guid>($"SELECT :Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data));

            var data2 = dbContext.FetchOne<SimpleGuidRecord>($"SELECT :Value as Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data2.Value));
        }

        [Test]
        public void TimeSpanTest()
        {
            var dbContext = DbHub.Use("db1");
            var param = new { Value = TimeSpan.FromSeconds(100) };
            var data = dbContext.ExecuteScalar<TimeSpan>($"SELECT :Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data));

            var data2 = dbContext.FetchOne<SimpleTimeSpanRecord>($"SELECT :Value as Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data2.Value));

            var param2 = new { Value = "00:10:00" };
            var data3 = dbContext.FetchOne<SimpleTimeSpanRecord>($"SELECT :Value as Value FROM DUAL", param2);
            Assert.That(new TimeSpan(0, 10, 0), Is.EqualTo(data3.Value));

            Assert.Throws<TimeSpanConversionException>(() => dbContext.FetchOne<SimpleTimeSpanRecord>($"SELECT 'some string value' as Value FROM DUAL"));
        }

        [Test]
        public void NullableValueTypeTest()
        {
            var dbContext = DbHub.Use("db1");
            var param = new { Value = (int?)null };
            var data = dbContext.ExecuteScalar<int?>($"SELECT :Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data));
            Assert.Throws<DbNullToValueTypeException>(() => dbContext.ExecuteScalar<int>($"SELECT :Value FROM DUAL", param));
        }

        [Test]
        public void NullableNonValueTypeTest()
        {
            var dbContext = DbHub.Use("db1");
            var param = new { Value = (string?)null };
            var data = dbContext.ExecuteScalar<string>($"SELECT :Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data));

            data = dbContext.ExecuteScalar<string?>($"SELECT :Value FROM DUAL", param);
            Assert.That(param.Value, Is.EqualTo(data));
        }

        [Test]
        public void NullableRecordFieldsTest()
        {
            var dbContext = DbHub.Use("db1");
            var user = new UserNullableRecord(0, 2, "Sample", null, null, null, null, null);
            var id = dbContext.Insert<UserNullableRecord, int>(user);
            var output = dbContext.FetchOne<UserNullableRecord>(x => x.Id == id);
            Assert.That(user.State, Is.EqualTo(output.State));
            Assert.That(user.Salary, Is.EqualTo(output.Salary));
            Assert.That(user.Birthday, Is.EqualTo(output.Birthday));
            Assert.That(user.UserCode, Is.EqualTo(output.UserCode));
        }

        [Test]
        public void NullableClassFieldsTest()
        {
            var dbContext = DbHub.Use("db1");
            var user = new UserNullableClass { Name = "Sample 2" };
            var id = dbContext.Insert<UserNullableClass, int>(user);
            var output = dbContext.FetchOne<UserNullableClass>(x => x.Id == id);
            Assert.That(user.State, Is.EqualTo(output.State));
            Assert.That(user.Salary, Is.EqualTo(output.Salary));
            Assert.That(user.Birthday, Is.EqualTo(output.Birthday));
            Assert.That(user.UserCode, Is.EqualTo(output.UserCode));
            Assert.That(user.Icon, Is.EqualTo(output.Icon));
        }

        #endregion

        #region Queries

        //WARNING: EMPTY STRING IS NULL IN ORACLE
        [Test]
        [TestCase("some value")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio. Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at nibh elementum imperdiet.")]
        [TestCase("")]
        public void SingleValue(string stringValue)
        {
            var dbContext = DbHub.Use("db1");
            var value = dbContext.ExecuteScalar<string>($"SELECT '{stringValue}' FROM DUAL");
            Assert.That(value, Is.EqualTo(stringValue == "" ? null : stringValue));
        }

        [Test]
        [TestCase("some value")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio. Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at nibh elementum imperdiet.")]
        [TestCase("")]
        [TestCase(null)]
        public void SingleByParamValue(string stringValue)
        {
            var dbContext = DbHub.Use("db1");
            var value = dbContext.ExecuteScalar<string>($"SELECT :Value FROM DUAL", new { Value = stringValue });
            Assert.That(value, Is.EqualTo(stringValue == "" ? null : stringValue));
        }

        [Test]
        [TestCase("E")]
        [TestCase("A")]
        public void ComplexQuery(string filter)
        {
            var dbContext = DbHub.Use("db1");
            var result = dbContext.FetchList<User>(x => (x.State &&
                                         x.Name.Contains(filter) &&
                                         x.Birthday < DateTime.Now) ||
                                         SqlExpression.Between<User>(x => x.Birthday, new DateTime(1950, 1, 1), DateTime.MaxValue) &&
                                         (x.Salary % 2) > 0);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 500)]
        public void QueryWithArraysParameters(int start, int count)
        {
            var list = Enumerable.Range(start, count).ToArray();
            var dbContext = DbHub.Use("db1");
            var people = dbContext.FetchList<User>(x => list.Contains(x.Id));
            Assert.That(people, Is.Not.Null);
        }

        [Test]
        public void QueryWithExists()
        {
            var dbContext = DbHub.Use("db1");
            var result = dbContext.FetchList<User>(x => SqlExpression.Exists<User, UserType>((user, userType) => user.UserTypeId == userType.Id));
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void QueryWithLike()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "John", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            User result = dbContext.FetchOne<User>(x => x.Name.Contains('o') && x.Name.EndsWith("n") && x.Name.StartsWith("J"));
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void QueryNullAndNotNull()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var user = new UserNullableRecord(0, 2, "John", null, 351.94m, new DateTime(1996, 7, 28), Guid.NewGuid(), icon);
            dbContext.Insert(user);
            var result = dbContext.FetchOne<UserNullableRecord>(x => x.State == null && x.Name != null && x.Name == "John");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(user.Name));
            Assert.That(result.State, Is.Null);
            Assert.That(result.Salary, Is.EqualTo(user.Salary));
            Assert.That(result.Birthday, Is.EqualTo(user.Birthday));
        }

        [Test]
        public async Task ComplexQueryAsync()
        {
            var dbContext = DbHub.Use("db1");
            string filterName = "A";
            var result = await dbContext.FetchListAsync<User>(x => (x.State &&
                                         x.Name.Contains(filterName) &&
                                         x.Birthday < DateTime.Now) ||
                                         SqlExpression.Between<User>(x => x.Birthday, new DateTime(1950, 1, 1), DateTime.MaxValue) &&
                                         (x.Salary % 2) > 0);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 500)]
        public async Task QueryWithArraysParametersAsync(int start, int count)
        {
            var list = Enumerable.Range(start, count).ToArray();
            var dbContext = DbHub.Use("db1");
            var people = await dbContext.FetchListAsync<User>(x => list.Contains(x.Id));
            Assert.That(people, Is.Not.Null);
        }

        [Test]
        public async Task QueryWithExistsAsync()
        {
            var dbContext = DbHub.Use("db1");
            var result = await dbContext.FetchListAsync<User>(x => SqlExpression.Exists<User, UserType>((user, userType) => user.UserTypeId == userType.Id));
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void QueryWithSelectors()
        {
            var dbContext = DbHub.Use("db1");
            var result = dbContext.FetchList<User>(x => x.Id > 0, x => new { x.Id, x.Name });
            Assert.That(result, Is.Not.Empty);
            Assert.That(result[0].Id, Is.GreaterThan(0));
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].State, Is.EqualTo(default(bool)));
            Assert.That(result[0].Salary, Is.EqualTo(default(decimal)));
            Assert.That(result[0].Birthday, Is.EqualTo(default(DateTime)));
            Assert.That(result[0].UserCode, Is.EqualTo(default(Guid)));
        }

        [Test]
        public void ToListByQueryTextTest()
        {
            var dbContext = DbHub.Use("db1");
            var users = dbContext.FetchList<User>("SELECT * FROM USERS");
            Assert.That(users, Is.Not.Empty);
        }

        [Test]
        public void ToSingleByQueryTextTest()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var user = dbContext.FetchOne<User>("SELECT * FROM USERS WHERE ID = :Id", new { Id = id });
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public void ToListByExpressionTest()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var users = dbContext.FetchList<User>();
            Assert.That(users, Is.Not.Empty);
        }

        [Test]
        public void ToSingleByExpressionTest()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var user = dbContext.FetchOne<User>(x => x.Id == id);
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public void TryFetchListByTextTest()
        {
            var dbContext = DbHub.Use("db1");
            var result = dbContext.TryFetchList<User>("SELECT * FROM USERS");
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Empty);
        }

        [Test]
        public void TryFetchOneByQueryTextTest()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var result = dbContext.TryFetchOne<User>("SELECT * FROM USERS WHERE ID = :Id", new { Id = id });
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Null);
        }

        [Test]
        public async Task TryFetchListAsync()
        {
            var dbContext = DbHub.GetDefaultDb();
            var result = await dbContext.TryFetchListAsync<User>("SELECT * FROM USERS");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Empty);
        }

        [Test]
        public async Task TryFetchListAsyncCancel()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(new TimeSpan(0, 0, 0, 0, 0, 1));

            var dbContext = DbHub.GetDefaultDb();
            var result = await dbContext.TryFetchListAsync<User>("SELECT * FROM USERS", null, source.Token);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False, "Success");
            Assert.That(result.Canceled, Is.True, "Canceled");
        }


        [Test]
        public async Task TryFetchOneAsync()
        {
            var dbContext = DbHub.GetDefaultDb();
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var id = dbContext.Insert<User, int>(new User(0, 2, "Tom", true, 4400.555m, new DateTime(1995, 4, 20), Guid.NewGuid(), icon));
            var entity = await dbContext.TryFetchOneAsync<User>("SELECT * FROM USERS WHERE Id = :id", new { id });
            Assert.That(entity.Success, Is.True);
            Assert.That(entity.Result, Is.Not.Null);
            Assert.That(entity.Result.Name, Is.EqualTo("Tom"));
            Assert.That(entity.Result.State, Is.EqualTo(true));
            Assert.That(entity.Result.Salary, Is.EqualTo(4400.555m));
            Assert.That(entity.Result.Birthday, Is.EqualTo(new DateTime(1995, 4, 20)));
        }

        class TupleParam
        {
            [DbParameter(direction: ParameterDirection.Output, isOracleCursor: true)]
            public string p_cursor1 { get; set; }

            [DbParameter(direction: ParameterDirection.Output, isOracleCursor: true)]
            public string p_cursor2 { get; set; }
        }

        [Test]
        public async Task TryFetchTupleAsync()
        {
            var dbContext = DbHub.GetDefaultDb();
            var tuple = await dbContext.TryFetchTupleAsync<User, UserType>("BEGIN OPEN :p_cursor1 FOR SELECT * FROM USERS; OPEN :p_cursor2 FOR SELECT * FROM USER_TYPE; END;", new TupleParam());
            Assert.That(tuple, Is.Not.Null);
            Assert.That(tuple.Success, Is.True);
            Assert.That(tuple.Result.Item1, Is.Not.Empty);
            Assert.That(tuple.Result.Item2, Is.Not.Empty);
        }

        [Test]
        public async Task TryFetchTupleAsyncCancel()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(new TimeSpan(0, 0, 0, 0, 0, 1));

            var dbContext = DbHub.GetDefaultDb();
            var tuple = await dbContext.TryFetchTupleAsync<User, UserType>("BEGIN OPEN :p_cursor1 FOR SELECT * FROM USERS; OPEN :p_cursor2 FOR SELECT * FROM USER_TYPE; END;", new TupleParam(), source.Token);
            Assert.That(tuple, Is.Not.Null);
            Assert.That(tuple.Success, Is.False, "Success");
            Assert.That(tuple.Canceled, Is.True, "Canceled");
        }

        [Test]
        public void FetchData()
        {
            var dbContext = DbHub.GetDefaultDb();
            var (dispose, iterator) = dbContext.FetchData<User>("SELECT * FROM USERS", null, 5);

            foreach (var list in iterator)
            {
                Assert.That(list.Count, Is.LessThanOrEqualTo(5));
            }

            dispose();
            Assert.Pass();
        }
        #endregion


        #region Store Procedures

        class UserTotal
        {

            [DbParameter(direction: ParameterDirection.Output)]
            public int Total { get; set; }

            //match store procedure definition precision and scale
            [DbParameter(direction: ParameterDirection.Output, precision: 15, scale: 2)]
            public decimal TotalSalary { get; set; }
        }

        [Test]
        public void GetAllStoreProcedure()
        {
            var dbContext = DbHub.Use("db1");
            var user = dbContext.FetchList<User>("GET_ALL");
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public void GetUserStoreProcedure()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var user = new User(0, 3, "Carlos", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon);
            var id = dbContext.Insert<User, int>(user);
            var userFromDb = dbContext.FetchOne<User>("GET_USER", new { id });
            var userRecordFromDb = dbContext.FetchOne<UserNullableRecord>("GET_USER", new { id });
            var userClassFromDb = dbContext.FetchOne<UserNullableClass>("GET_USER", new { id });

            Assert.That(userFromDb, Is.Not.Null);
            Assert.That(userFromDb.Id, Is.EqualTo(id));
            Assert.That(userFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userFromDb.State, Is.EqualTo(user.State));
            Assert.That(userFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.That(userRecordFromDb, Is.Not.Null);
            Assert.That(userRecordFromDb.Id, Is.EqualTo(id));
            Assert.That(userRecordFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userRecordFromDb.State, Is.EqualTo(user.State));
            Assert.That(userRecordFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userRecordFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userRecordFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userRecordFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.That(userClassFromDb, Is.Not.Null);
            Assert.That(userClassFromDb.Id, Is.EqualTo(id));
            Assert.That(userClassFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userClassFromDb.State, Is.EqualTo(user.State));
            Assert.That(userClassFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userClassFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userClassFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userClassFromDb.Icon, Is.EqualTo(user.Icon));
        }

        [Test]
        public void SPOutParameter()
        {
            var dbContext = DbHub.Use("db1");
            var filter = new UserTotal();
            dbContext.Execute("GET_TOTALUSER", filter);
            Assert.That(filter.Total, Is.GreaterThan(0));
            Assert.That(filter.TotalSalary, Is.GreaterThan(0));
        }

        [Test]
        public void SPTuple()
        {
            var dbContext = DbHub.Use("db1");
            var result = dbContext.FetchTuple<User, UserType>("GET_DATA");
            Assert.That(result.Item1, Is.Not.Empty);
            Assert.That(result.Item2, Is.Not.Empty);
        }

        [Test]
        public async Task SPOutParameterAsync()
        {
            var dbContext = DbHub.Use("db1");
            var filter = new UserTotal();
            await dbContext.ExecuteAsync("GET_TOTALUSER", filter);
            Assert.That(filter.Total, Is.GreaterThan(0));
            Assert.That(filter.TotalSalary, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetAllStoreProcedureAsync()
        {
            var dbContext = DbHub.Use("db1");
            var user = await dbContext.FetchListAsync<User>("GET_ALL");
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public async Task GetUserStoreProcedureAsync()
        {
            var dbContext = DbHub.Use("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            var user = new User(0, 3, "Carlos", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon);
            var id = dbContext.Insert<User, int>(user);
            var userFromDb = await dbContext.FetchOneAsync<User>("GET_USER", new { id });
            var userRecordFromDb = await dbContext.FetchOneAsync<UserNullableRecord>("GET_USER", new { id });
            var userClassFromDb = await dbContext.FetchOneAsync<UserNullableClass>("GET_USER", new { id });

            Assert.That(userFromDb, Is.Not.Null);
            Assert.That(userFromDb.Id, Is.EqualTo(id));
            Assert.That(userFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userFromDb.State, Is.EqualTo(user.State));
            Assert.That(userFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.That(userRecordFromDb, Is.Not.Null);
            Assert.That(userRecordFromDb.Id, Is.EqualTo(id));
            Assert.That(userRecordFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userRecordFromDb.State, Is.EqualTo(user.State));
            Assert.That(userRecordFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userRecordFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userRecordFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userRecordFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.That(userClassFromDb, Is.Not.Null);
            Assert.That(userClassFromDb.Id, Is.EqualTo(id));
            Assert.That(userClassFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userClassFromDb.State, Is.EqualTo(user.State));
            Assert.That(userClassFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userClassFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userClassFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userClassFromDb.Icon, Is.EqualTo(user.Icon));
        }



        #endregion
    }
}
