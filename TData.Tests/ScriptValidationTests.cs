namespace TData.Tests
{
    public class ScriptValidationTests
    {
        [Test]
        [TestCase("GET_ALL", true)]
        [TestCase(" GET_ALL", true)]
        [TestCase("SELECT * FROM USERS", false)]
        public void IsStoreProcedureTest(string query, bool expected)
        {
            var result = QueryValidators.IsStoredProcedure(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"BEGIN
                    EXECUTE IMMEDIATE 'DROP TABLE BOOK';
                    EXCEPTION WHEN OTHERS THEN NULL;
                    END;", true)]
        [TestCase(@"
                DECLARE @DepartmentName NVARCHAR(100)
                SET @DepartmentName = 'Engineering'
                SELECT FirstName, LastName, Department
                FROM Employees
                WHERE Department = @DepartmentName;", true)]
        
        [TestCase(@"IF NOT EXISTS (SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[GET_ALL]') AND type in (N'P', N'PC')) 
                    BEGIN 
                        EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [GET_ALL] AS' 
                    END", true)]
        [TestCase("SELECT GETDATE()", false)]
        public void IsAnonymousBlockTest(string query, bool expected)
        {
            var result = QueryValidators.IsAnonymousBlock(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void PostgreSQLFunctionDefinitionTest()
        {
            var query = @"CREATE FUNCTION GET_TOTALUSER(OUT total INTEGER,
                                                           OUT totalSalary DECIMAL)
                            AS
                            $$
                            BEGIN
                                SELECT COUNT(*), SUM(COALESCE(Salary, 0)) INTO total, totalSalary FROM APP_USER;
                            END;
                            $$ LANGUAGE plpgsql;";

            var  result = QueryValidators.ScriptExpectParameterMatch(query);
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(@"SELECT '123'::INTEGER AS cast_to_integer", false)]
        [TestCase(@"SELECT '123'::INTEGER + @value", true)]
        public void PostgreSQLCastTest(string query, bool expected)
        {
            var result = QueryValidators.ScriptExpectParameterMatch(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"SELECT @@VERSION", false )]
        [TestCase(@"SELECT @@VERSION + @value", true)]
        public void SQLServerSystemVariablesTest(string query, bool expected)
        {
            var result = QueryValidators.ScriptExpectParameterMatch(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"SELECT * FROM APP_USER WHERE ID = @id", false)]
        [TestCase("GRANT SELECT, INSERT ON Employees TO USER_1;", true)]
        [TestCase("REVOKE INSERT ON mydb.Employees FROM 'someuser'@'localhost';", true)]
        public void IsDCLTest(string query, bool expected)
        {
            var result = QueryValidators.IsDCL(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"SELECT * FROM APP_USER WHERE ID = @id", true)]
        [TestCase("GRANT SELECT, INSERT ON Employees TO USER_1", false)]
        [TestCase("DELETE FROM APP_USER WHERE ID = @id", true)]
        public void IsDMLTest(string query, bool expected)
        {
            var result = QueryValidators.IsDML(query);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(@"SELECT * FROM APP_USER WHERE ID = @id", false)]
        [TestCase("CREATE TABLE BOOK(ID NUMBER(*,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 99999 INCREMENT BY 1 START WITH 1 NOT NULL PRIMARY KEY, CONTENT NCLOB)", true)]
        [TestCase("CREATE VIEW view_name_2020 AS SELECT column1, column2, column3 FROM table_name WHERE YEAR = 2020;", true)]
        public void IsDDLTest(string query, bool expected)
        {
            var result = QueryValidators.IsDDL(query);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
