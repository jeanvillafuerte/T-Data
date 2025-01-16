namespace TData.Tests.PostgreSQL
{
    internal class PostgresSQLTests : DbProviderTestsBase
    {
        protected override string ConnectionString => Environment.GetEnvironmentVariable("PostgresCnx") ?? "Server=localhost;Port=5432;Database=test;User ID=postgres;Password=postgres";
        protected override string BindVariable => "@";
        protected override bool SkipStoreProcedures => false;
        protected override DbProvider DbProvider => DbProvider.PostgreSql;

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
                db.Execute("CREATE TABLE BOOK(ID SERIAL PRIMARY KEY, CONTENT TEXT)");
                db.Execute(@"CREATE TABLE APP_USER (
                                    ID SERIAL PRIMARY KEY,
                                    USER_TYPE_ID INTEGER NOT NULL,
                                    NAME CHARACTER VARYING(50),
                                    STATE BIT(1),
                                    SALARY NUMERIC(15,4),
                                    BIRTHDAY DATE,
                                    USERCODE CHAR(36),
                                    ICON BYTEA
                                )");
                db.Execute("CREATE TABLE USER_TYPE(ID INTEGER PRIMARY KEY, NAME CHARACTER VARYING(50))");
            });
            Assert.Pass();
        }

        [Test, Order(3)]
        public void CreateStoreProcedures()
        {
            var dbContext = DbHub.Use(DbSignature, buffered: false);

            dbContext.ExecuteBlock((db) =>
            {
                db.Execute("DROP FUNCTION IF EXISTS GET_ALL()");
                db.Execute(@"CREATE FUNCTION GET_ALL()
                                RETURNS TABLE (
                                    id INT,
                                    user_type_id INT,
                                    name CHARACTER VARYING(50),
                                    state BIT(1),
                                    salary DECIMAL,
                                    birthday DATE,
                                    userCode CHAR(36),
                                    icon BYTEA
                                ) AS $$
                                BEGIN
                                    RETURN QUERY SELECT * FROM APP_USER;
                                END;
                                $$ LANGUAGE plpgsql;
                            ");
                db.Execute("DROP FUNCTION IF EXISTS GET_USER(INTEGER)");
                db.Execute(@"CREATE FUNCTION GET_USER(user_id INTEGER)
                            RETURNS TABLE (
                                id INT,
                                user_type_id INT,
                                name CHARACTER VARYING(50),
                                state BIT(1),
                                salary DECIMAL,
                                birthday DATE,
                                userCode CHAR(36),
                                icon BYTEA
                            ) AS $$
                            BEGIN
                                RETURN QUERY SELECT * FROM APP_USER AS U WHERE U.ID = user_id;
                            END;
                            $$ LANGUAGE plpgsql;
                        ");
                db.Execute("DROP FUNCTION IF EXISTS GET_TOTALUSER(OUT INTEGER, OUT DECIMAL)");
                db.Execute(@"CREATE FUNCTION GET_TOTALUSER(OUT total INTEGER,
                                                           OUT totalSalary DECIMAL)
                            AS
                            $$
                            BEGIN
                                SELECT COUNT(*), SUM(COALESCE(Salary, 0)) INTO total, totalSalary FROM APP_USER;
                            END;
                            $$ LANGUAGE plpgsql;");
                db.Execute("DROP FUNCTION IF EXISTS GET_DATA();");
                db.Execute(@"CREATE FUNCTION GET_DATA()
                            RETURNS SETOF REFCURSOR 
                            AS $$
                            DECLARE
                            c1 REFCURSOR;
                            c2 REFCURSOR;
                            BEGIN
                                OPEN c1 FOR SELECT * FROM APP_USER;
                                RETURN NEXT c1;
                                OPEN c2 FOR SELECT * FROM USER_TYPE;
                                RETURN NEXT c2;

                                RETURN;
                            END;
                            $$ LANGUAGE plpgsql;
                        ");
                db.Execute("DROP FUNCTION IF EXISTS GET_OUTPUT_PARAMS(IN INTEGER, OUT INTEGER, OUT TEXT)");
                db.Execute(@"CREATE FUNCTION GET_OUTPUT_PARAMS(
                                IN input_param INT,
                                OUT output_param1 INT,
                                OUT output_param2 TEXT
                            )
                            AS $$
                            BEGIN
                                output_param1 := input_param * 10;
                                output_param2 := 'Output for ' || input_param;
                            END;
                            $$ LANGUAGE plpgsql;");
                db.Execute("DROP PROCEDURE IF EXISTS CLONE_USER(IN INTEGER)");
                db.Execute("CREATE PROCEDURE CLONE_USER(IN user_id INTEGER) AS $$ BEGIN INSERT INTO APP_USER (USER_TYPE_ID, NAME, STATE, SALARY, BIRTHDAY, USERCODE, ICON) SELECT USER_TYPE_ID, NAME, STATE, SALARY, BIRTHDAY, USERCODE, ICON FROM APP_USER WHERE ID = user_id; END; $$ LANGUAGE plpgsql;");
                db.Execute("DROP PROCEDURE IF EXISTS BULK_INSERT_USER_DATA(IN INTEGER)");
                db.Execute(@"CREATE PROCEDURE BULK_INSERT_USER_DATA(IN total INTEGER)
                            AS $$
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
                                        FLOOR(RANDOM() * 10 + 1)::INTEGER,
                                        SUBSTRING(MD5(RANDOM()::TEXT), 1, 16),
                                        CASE WHEN RANDOM() > 0.5 THEN B'1' ELSE B'0' END,
                                        ROUND((RANDOM() * 70000 + 30000)::NUMERIC, 4),
                                        CURRENT_DATE - (RANDOM() * 50 * 365)::INTEGER,
                                        GEN_RANDOM_UUID(),
                                        NULL
                                    );
                                END LOOP;
                            END;
                            $$ LANGUAGE plpgsql;");
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
            var id = dbContext.Insert<User, int>(new User(0, 1, "Jean", true, 1340.5m, new DateTime(1997, 3, 21), System.Guid.NewGuid(), icon));
            Assert.That(Convert.ToInt32(id), Is.GreaterThan(0));
        }

        [Test, Order(6)]
        public void InsertData()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "TDataIco.png"));
            dbContext.Insert(new User(0, 2, "Peter", false, 3350.99m, new DateTime(1989, 5, 17), System.Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 2, "Jean", true, 1346.23m, new DateTime(1989, 5, 17), System.Guid.NewGuid(), icon));
            dbContext.Insert(new User(0, 1, "John", true, 6344.98m, new DateTime(1989, 5, 17), System.Guid.NewGuid(), icon));
            Assert.Pass();
        }

        [Test, Order(7)]
        public void UpdateData()
        {
            var dbContext = DbHub.Use(DbSignature);
            var icon = File.ReadAllBytes(Path.Combine(".", "Content", "rocket.png"));
            dbContext.Update(new User(1, 2, "Paul", false, 3350.99m, new DateTime(1989, 5, 17), System.Guid.NewGuid(), icon));
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

        [Test]
        public void CastingWithParameters()
        {
            var dbContext = DbHub.Use(DbSignature);
            var result = dbContext.ExecuteScalar<int>("SELECT '100'::INTEGER + @value", new { value = 100 });
            Assert.That(result, Is.EqualTo(200));
        }
    }
}
