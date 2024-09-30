using TData.Configuration;

namespace TData.Tests
{
    public class DatabaseCommandTests
    {
        
        DbCommandConfiguration commmandConfig = new DbCommandConfiguration(System.Data.CommandBehavior.SingleResult, MethodHandled.FetchListQueryString, false, false, false);

        [Test, Ignore("db provider library is within the project")]
        public void ThrowDbProviderNotFound()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.PostgreSql, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<DBProviderNotFoundException>(() => new DatabaseCommand(options, "SELECT * FROM USERS", null, commmandConfig, true, null, null));
        }

        [Test]
        public void InstanceForFirstTimeRequest()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            var command = new DatabaseCommand(options, "SELECT * FROM USERS", null, commmandConfig, true, null, null);
            Assert.That(command, Is.Not.Null);
        }

        [Test]
        public void DDLUnsupportedParameters()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<UnsupportedParametersException>(() => new DatabaseCommand(options, "CREATE TABLE USERS (@Name INT)", new { Name = "Users" }, commmandConfig, true, null, null));
        }

        [Test]
        public void DCLUnsupportedParameters()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<UnsupportedParametersException>(() => new DatabaseCommand(options, "GRANT SELECT, INSERT ON USERS TO ADMIN", new { Name = "Users" }, commmandConfig, true, null, null));
        }

        [Test]
        public void DMLNotAllowParameters()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<NotAllowParametersException>(() => new DatabaseCommand(options, "SELECT * FROM USERS", new { Name = "Users" }, commmandConfig, true, null, null));
        }

        [Test]
        public void DMLMissingParameters()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<MissingParametersException>(() => new DatabaseCommand(options, "SELECT * FROM USERS WHERE Id = @Id", null, commmandConfig, true, null, null));
        }

        [Test]
        public void AnonymousBlockUnsupportedParameters()
        {
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            const string script = @"BEGIN
                                        DECLARE @Name INT
                                        SET @Name = 1
                                        SELECT @Name
                                    END";
            Assert.Throws<UnsupportedParametersException>(() => new DatabaseCommand(options, script, new { Name = "Users" }, commmandConfig, true, null, null));
        }

        [Test]
        //avoid to use in methods like ToSingle, ToList or ToTuple
        public void NotSupportedCommandType()
        {
            DbCommandConfiguration commmandConfig = new DbCommandConfiguration(System.Data.CommandBehavior.SingleResult, MethodHandled.FetchListQueryString, false, false, false);
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            Assert.Throws<NotSupportedCommandTypeException>(() => new DatabaseCommand(options, "GRANT SELECT, INSERT ON USERS TO ADMIN", null, commmandConfig, true, null, null));
        }

        [Test]
        public void AllowDCLQuery()
        {
            DbCommandConfiguration commmandConfig = new DbCommandConfiguration(System.Data.CommandBehavior.Default, MethodHandled.Execute, false, false, false);
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            var command = new DatabaseCommand(options, "GRANT SELECT, INSERT ON USERS TO ADMIN", null, commmandConfig, true, null, null);
            Assert.That(command, Is.Not.Null);
        }

        [Test]
        public void AllowDDLQuery()
        {
            DbCommandConfiguration commmandConfig = new DbCommandConfiguration(System.Data.CommandBehavior.Default, MethodHandled.Execute, false, false, false);
            DbSettings options = new DbSettings("Db1", SqlProvider.SqlServer, "Data Source=localhost;Initial Catalog=db;Persist Security Info=True;User ID=sa;Password=YourPassword;") { ConnectionTimeout = 30 };
            var command = new DatabaseCommand(options, "CREATE TABLE USERS (Name VARCHAR(25))", null, commmandConfig, true, null, null);
            Assert.That(command, Is.Not.Null);
        }

    }
}
