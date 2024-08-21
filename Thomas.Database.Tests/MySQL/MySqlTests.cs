﻿using NUnit.Framework.Internal;
using Thomas.Database.Configuration;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;
using Thomas.Database.Attributes;
using System.Data;

namespace Thomas.Database.Tests.MySQL
{
    public class MySqlTests : IDatabaseProvider
    {
        public string ConnectionString => Environment.GetEnvironmentVariable("MySqlCnx") ?? "Server=localhost;Database=test;Uid=root;Pwd=Mysql_Test;";

        [OneTimeSetUp]
        public void Initialize()
        {
            DbConfigurationFactory.Clear();
            var tableBuilder = new TableBuilder();
            var table = tableBuilder.AddTable<User>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<User>().DbName("USERS");
            table.Column<User>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            var table2 = tableBuilder.AddTable<UserNullableRecord>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<UserNullableRecord>().DbName("USERS");
            table2.Column<UserNullableRecord>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            var table3 = tableBuilder.AddTable<UserNullableClass>(x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<UserNullableClass>().DbName("USERS");
            table3.Column<UserNullableClass>(x => x.UserTypeId).DbName("USER_TYPE_ID");
            tableBuilder.AddTable<UserType>(x => x.Id, keyAutoGenerated: false).AddFieldsAsColumns<UserType>().DbName("USER_TYPE");
            DbFactory.AddDbBuilder(tableBuilder);
            DbConfigurationFactory.Register(new DbSettings("db1", SqlProvider.MySql, ConnectionString));
        }

        [Test, Order(1)]
        public void DropIfExistsTable()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("DROP TABLE IF EXISTS USERS");
                db.Execute("DROP TABLE IF EXISTS USER_TYPE");
            });
            Assert.Pass();
        }

        [Test, Order(2)]
        public void CreateTable()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            dbContext.ExecuteBlock((db) =>
            {
                db.Execute(@"CREATE TABLE USERS (
                                    ID INT PRIMARY KEY AUTO_INCREMENT,
                                    USER_TYPE_ID INT NOT NULL,
                                    NAME VARCHAR(50),
                                    STATE BOOLEAN,
                                    SALARY DECIMAL(15,2),
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
            var dbContext = DbFactory.GetDbContext("db1");

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("DROP PROCEDURE IF EXISTS GET_ALL");
                db.Execute("CREATE PROCEDURE GET_ALL() BEGIN SELECT * FROM USERS; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_USER");
                db.Execute(@"CREATE PROCEDURE GET_USER(IN user_id INT) BEGIN SELECT * FROM USERS WHERE ID = user_id; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_TOTALUSER");
                db.Execute("CREATE PROCEDURE GET_TOTALUSER(OUT total INTEGER, OUT totalSalary DECIMAL(15,2)) BEGIN SELECT COUNT(*), SUM(COALESCE(Salary, 0)) INTO total, totalSalary FROM USERS; END;");
                db.Execute("DROP PROCEDURE IF EXISTS GET_DATA");
                db.Execute("CREATE PROCEDURE GET_DATA() BEGIN SELECT * FROM USERS; SELECT * FROM USER_TYPE; END;");
            });

            Assert.Pass();
        }

        [Test, Order(4)]
        public void InsertUserType()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            dbContext.ExecuteBlock((db) =>
            {
                db.Add(new UserType(1, "Administrator"));
                db.Add(new UserType(2, "Operator"));
                db.Add(new UserType(3, "Regular"));
            });
        }

        [Test, Order(4)]
        public void InsertDataAndReturnNewId()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var id = dbContext.Add<User, int>(new User(0, 1, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            Assert.Greater(Convert.ToInt32(id), 0);
        }

        [Test, Order(5)]
        public void InsertData()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            dbContext.Add(new User(0, 2, "Peter", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Add(new User(0, 2, "Jean", true, 1346.23m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            dbContext.Add(new User(0, 1, "John", true, 6344.98m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(6)]
        public void UpdateData()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "rocket.png"));
            dbContext.Update(new User(1, 2, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(7)]
        public void Query()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var users = dbContext.ToList<User>();
            Assert.IsNotEmpty(users);
        }

        [Test, Order(8)]
        public void DeleteData()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var user = dbContext.ToSingle<User>(x => x.Id == 1);
            if (user == null)
                Assert.Fail("User not found");
            else
            {
                dbContext.Delete(user);
                Assert.Pass();
            }
        }

        [Test]
        public void GetAllStoreProcedureTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var user = dbContext.ToList<User>("GET_ALL");
            Assert.IsNotEmpty(user);
        }

        [Test]
        public void GetUserStoreProcedureTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var user = new User(0, 3, "Carlos", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon);
            var id = dbContext.Add<User, int>(user);
            var userFromDb = dbContext.ToSingle<User>("GET_USER", new { user_id = id });
            var userRecordFromDb = dbContext.ToSingle<UserNullableRecord>("GET_USER", new { user_id = id });
            var userClassFromDb = dbContext.ToSingle<UserNullableClass>("GET_USER", new { user_id = id });

            Assert.IsNotNull(userFromDb);
            Assert.That(userFromDb.Id, Is.EqualTo(id));
            Assert.That(userFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userFromDb.State, Is.EqualTo(user.State));
            Assert.That(userFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userRecordFromDb);
            Assert.That(userRecordFromDb.Id, Is.EqualTo(id));
            Assert.That(userRecordFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userRecordFromDb.State, Is.EqualTo(user.State));
            Assert.That(userRecordFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userRecordFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userRecordFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userRecordFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userClassFromDb);
            Assert.That(userClassFromDb.Id, Is.EqualTo(id));
            Assert.That(userClassFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userClassFromDb.State, Is.EqualTo(user.State));
            Assert.That(userClassFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userClassFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userClassFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userClassFromDb.Icon, Is.EqualTo(user.Icon));
        }

        #region data types
        [Test]
        public void GuidTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var param = new { Value = Guid.NewGuid() };
            var data = dbContext.ExecuteScalar<Guid>($"SELECT @Value", param);
            Assert.That(param.Value, Is.EqualTo(data));

            var data2 = dbContext.ToSingle<SimpleGuidRecord>($"SELECT @Value as Value", param);
            Assert.That(param.Value, Is.EqualTo(data2.Value));
        }

        [Test]
        public void TimeSpanTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var param = new { Value = TimeSpan.FromSeconds(100) };
            var data = dbContext.ExecuteScalar<TimeSpan>($"SELECT @Value", param);
            Assert.That(param.Value, Is.EqualTo(data));

            var data2 = dbContext.ToSingle<SimpleTimeSpanRecord>($"SELECT @Value as Value", param);
            Assert.That(param.Value, Is.EqualTo(data2.Value));

            var data3 = dbContext.ToSingle<SimpleTimeSpanRecord>($"SELECT @Value as Value", new { Value = "00:10:00" });
            Assert.That(new TimeSpan(0, 10, 0), Is.EqualTo(data3.Value));

            Assert.Throws<TimeSpanConversionException>(() => dbContext.ToSingle<SimpleTimeSpanRecord>($"SELECT 'some string value' as Value"));
        }

        [Test]
        public void NullableValueTypeTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var param = new { Value = (int?)null };
            var data = dbContext.ExecuteScalar<int?>($"SELECT @Value", param);
            Assert.That(param.Value, Is.EqualTo(data));
            Assert.Throws<DbNullToValueTypeException>(() => dbContext.ExecuteScalar<int>($"SELECT @Value", param));
        }

        [Test]
        public void NullableNonValueTypeTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var param = new { Value = (string?)null };
            var data = dbContext.ExecuteScalar<string>($"SELECT @Value", param);
            Assert.That(param.Value, Is.EqualTo(data));

            data = dbContext.ExecuteScalar<string?>($"SELECT @Value", param);
            Assert.That(param.Value, Is.EqualTo(data));
        }

        [Test]
        public void NullableRecordFieldsTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var user = new UserNullableRecord(0, 3, "Sample", null, null, null, null, null);
            var id = dbContext.Add<UserNullableRecord, int>(user);
            var output = dbContext.ToSingle<UserNullableRecord>(x => x.Id == id);
            Assert.That(user.State, Is.EqualTo(output.State));
            Assert.That(user.Salary, Is.EqualTo(output.Salary));
            Assert.That(user.Birthday, Is.EqualTo(output.Birthday));
            Assert.That(user.UserCode, Is.EqualTo(output.UserCode));
        }

        [Test]
        public void NullableClassFieldsTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var user = new UserNullableClass { Name = "Sample 2" };
            var id = dbContext.Add<UserNullableClass, int>(user);
            var output = dbContext.ToSingle<UserNullableClass>(x => x.Id == id);
            Assert.That(user.State, Is.EqualTo(output.State));
            Assert.That(user.Salary, Is.EqualTo(output.Salary));
            Assert.That(user.Birthday, Is.EqualTo(output.Birthday));
            Assert.That(user.UserCode, Is.EqualTo(output.UserCode));
            Assert.That(user.Icon, Is.EqualTo(output.Icon));
        }

        #endregion

        #region Queries

        [Test]
        [TestCase("some value")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio. Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at nibh elementum imperdiet.")]
        [TestCase("")]
        public void SingleValue(string stringValue)
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var value = dbContext.ExecuteScalar<string>($"SELECT '{stringValue}'");
            Assert.That(value, Is.EqualTo(stringValue));
        }

        [Test]
        [TestCase("some value")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
        [TestCase("lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio. Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at nibh elementum imperdiet.")]
        [TestCase("")]
        [TestCase(null)]
        public void SingleByParamValue(string stringValue)
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var value = dbContext.ExecuteScalar<string>($"SELECT @Value", new { Value = stringValue });
            Assert.That(value, Is.EqualTo(stringValue));
        }

        [Test]
        [TestCase("E")]
        [TestCase("A")]
        public void ComplexQuery(string filter)
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var result = dbContext.ToList<User>(x => (x.State &&
                                         x.Name.Contains(filter) &&
                                         x.Birthday < DateTime.Now) ||
                                         SqlExpression.Between<User>(x => x.Birthday, new DateTime(1950, 1, 1), DateTime.MaxValue) &&
                                         (x.Salary % 2) > 0);
            Assert.IsNotEmpty(result);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 500)]
        public void QueryWithArraysParameters(int start, int count)
        {
            var list = Enumerable.Range(start, count).ToArray();
            var dbContext = DbFactory.GetDbContext("db1");
            var people = dbContext.ToList<User>(x => list.Contains(x.Id));
            Assert.IsNotEmpty(people);
        }

        [Test]
        public void QueryWithExists()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var result = dbContext.ToList<User>(x => SqlExpression.Exists<User, UserType>((user, userType) => user.UserTypeId == userType.Id));
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void QueryWithLike()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            dbContext.Add(new User(0, 2, "John", false, 3350.99m, new DateTime(1989, 5, 17), Guid.NewGuid(), icon));
            User result = dbContext.ToSingle<User>(x => x.Name.Contains('o') && x.Name.EndsWith("n") && x.Name.StartsWith("J"));
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void QueryNullAndNotNull()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var user = new UserNullableRecord(0, 2, "John", null, 351.94m, new DateTime(1996, 7, 28), Guid.NewGuid(), icon);
            dbContext.Add(user);
            var result = dbContext.ToSingle<UserNullableRecord>(x => x.State == null && x.Name != null && x.Name == "John");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(user.Name));
            Assert.That(result.State, Is.Null);
            Assert.That(result.Salary, Is.EqualTo(user.Salary));
            Assert.That(result.Birthday, Is.EqualTo(user.Birthday));
        }

        [Test]
        public async Task ComplexQueryAsync()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            string filterName = "A";
            var result = await dbContext.ToListAsync<User>(x => (x.State &&
                                         x.Name.Contains(filterName) &&
                                         x.Birthday < DateTime.Now) ||
                                         SqlExpression.Between<User>(x => x.Birthday, new DateTime(1950, 1, 1), DateTime.MaxValue) &&
                                         (x.Salary % 2) > 0);

            Assert.IsNotEmpty(result);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 500)]
        public async Task QueryWithArraysParametersAsync(int start, int count)
        {
            var list = Enumerable.Range(start, count).ToArray();
            var dbContext = DbFactory.GetDbContext("db1");
            var people = await dbContext.ToListAsync<User>(x => list.Contains(x.Id));
            Assert.IsNotEmpty(people);
        }

        [Test]
        public async Task QueryWithExistsAsync()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var result = await dbContext.ToListAsync<User>(x => SqlExpression.Exists<User, UserType>((user, userType) => user.UserTypeId == userType.Id));
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ToListByQueryTextTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var users = dbContext.ToList<User>("SELECT * FROM USERS");
            Assert.That(users, Is.Not.Empty);
        }

        [Test]
        public void ToSingleByQueryTextTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var id = dbContext.Add<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var user = dbContext.ToSingle<User>("SELECT * FROM USERS WHERE ID = @Id", new { Id = id });
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public void ToListByExpressionTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            dbContext.Add(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var users = dbContext.ToList<User>();
            Assert.That(users, Is.Not.Empty);
        }


        [Test]
        public void ToSingleByExpressionTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var id = dbContext.Add<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var user = dbContext.ToSingle<User>(x => x.Id == id);
            Assert.That(user, Is.Not.Null);
        }

        [Test]
        public void ToListOpByTextTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var result = dbContext.ToListOp<User>("SELECT * FROM USERS");
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Empty);
        }

        [Test]
        public void ToSingleOpByQueryTextTest()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var id = dbContext.Add<User, int>(new User(0, 2, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon));
            var result = dbContext.ToSingleOp<User>("SELECT * FROM USERS WHERE ID = @Id", new { Id = id });
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.Not.Null);
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
            var dbContext = DbFactory.GetDbContext("db1");
            var user = dbContext.ToList<User>("GET_ALL");
            Assert.IsNotEmpty(user);
        }

        [Test]
        public void GetUserStoreProcedure()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var user = new User(0, 3, "Carlos", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon);
            var user_id = dbContext.Add<User, int>(user);
            var userFromDb = dbContext.ToSingle<User>("GET_USER", new { user_id });
            var userRecordFromDb = dbContext.ToSingle<UserNullableRecord>("GET_USER", new { user_id });
            var userClassFromDb = dbContext.ToSingle<UserNullableClass>("GET_USER", new { user_id });

            Assert.IsNotNull(userFromDb);
            Assert.That(userFromDb.Id, Is.EqualTo(user_id));
            Assert.That(userFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userFromDb.State, Is.EqualTo(user.State));
            Assert.That(userFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userRecordFromDb);
            Assert.That(userRecordFromDb.Id, Is.EqualTo(user_id));
            Assert.That(userRecordFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userRecordFromDb.State, Is.EqualTo(user.State));
            Assert.That(userRecordFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userRecordFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userRecordFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userRecordFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userClassFromDb);
            Assert.That(userClassFromDb.Id, Is.EqualTo(user_id));
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
            var dbContext = DbFactory.GetDbContext("db1");
            var filter = new UserTotal();
            dbContext.Execute("GET_TOTALUSER", filter);
            Assert.That(filter.Total, Is.GreaterThan(0));
            Assert.That(filter.TotalSalary, Is.GreaterThan(0));
        }

        [Test]
        public void SPTuple()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var result = dbContext.ToTuple<User, UserType>("GET_DATA");
            Assert.That(result.Item1, Is.Not.Empty);
            Assert.That(result.Item2, Is.Not.Empty);
        }

        [Test]
        public async Task SPOutParameterAsync()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var filter = new UserTotal();
            await dbContext.ExecuteAsync("GET_TOTALUSER", filter);
            Assert.That(filter.Total, Is.GreaterThan(0));
            Assert.That(filter.TotalSalary, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetAllStoreProcedureAsync()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var user = await dbContext.ToListAsync<User>("GET_ALL");
            Assert.IsNotEmpty(user);
        }

        [Test]
        public async Task GetUserStoreProcedureAsync()
        {
            var dbContext = DbFactory.GetDbContext("db1");
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "ThomasIco.png"));
            var user = new User(0, 3, "Carlos", true, 1340.5m, new DateTime(1997, 3, 21), Guid.NewGuid(), icon);
            var user_id = dbContext.Add<User, int>(user);
            var userFromDb = await dbContext.ToSingleAsync<User>("GET_USER", new { user_id });
            var userRecordFromDb = await dbContext.ToSingleAsync<UserNullableRecord>("GET_USER", new { user_id });
            var userClassFromDb = await dbContext.ToSingleAsync<UserNullableClass>("GET_USER", new { user_id });

            Assert.IsNotNull(userFromDb);
            Assert.That(userFromDb.Id, Is.EqualTo(user_id));
            Assert.That(userFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userFromDb.State, Is.EqualTo(user.State));
            Assert.That(userFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userRecordFromDb);
            Assert.That(userRecordFromDb.Id, Is.EqualTo(user_id));
            Assert.That(userRecordFromDb.Name, Is.EqualTo(user.Name));
            Assert.That(userRecordFromDb.State, Is.EqualTo(user.State));
            Assert.That(userRecordFromDb.Salary, Is.EqualTo(user.Salary));
            Assert.That(userRecordFromDb.Birthday, Is.EqualTo(user.Birthday));
            Assert.That(userRecordFromDb.UserCode, Is.EqualTo(user.UserCode));
            Assert.That(userRecordFromDb.Icon, Is.EqualTo(user.Icon));

            Assert.IsNotNull(userClassFromDb);
            Assert.That(userClassFromDb.Id, Is.EqualTo(user_id));
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
