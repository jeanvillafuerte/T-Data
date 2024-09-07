# ![](./ThomasIco.png "ThomasDataAdapter") _**ThomasDataAdapter**_

## Simplified setup for robust database operations by relief having to manage connection, commands or transactions

### 🌟 Join Us in Shaping the Future of ThomasDataAdapter! 🌟
Join us in refining a library designed to streamline database interactions effortlessly. Your insights, tests, and small enhancements can make a big difference. Feel free to fix bugs and suggest features.

### 🎯 Features:

- Support for NET CORE and NET FRAMEWORK
- Enhanced integration of both typed and anonymous types with query parameters for a more streamlined process.
- Expanded transaction support through the use of lambda expressions.
- Attribute configuration for DbParameter, including parameter direction, size, and precision.
- Unique signature creation for database settings, enabling static instantiation of database contexts on demand.
- Advanced optional caching layer designed to improve application performance by managing storage, updates, and deletions of cache, including the ability to refresh cache dynamically through a key identifier based on specific queries, expressions, and parameters.
- Full support for record types.

### Supported database providers
- Microsoft.Data.SqlClient
- Oracle.ManagedDataAccess.Core
- Npgsql
- Mysql.Data
- Microsoft.Data.Sqlite

## 🚀 Quick start

### Basic configuration in startup.cs

Benefits of persisting the configuration with its string connection you may have different versions of it for different purposes like pooling or Packet Size:

```c#
DbConfigurationFactory.Register(new DbSettings(
    signature: "db1",
    provider: SqlProvider.SqlServer,
    stringConnection: $"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}"
));

DbConfigurationFactory.Register(new DbSettings(
    signature: "db2",
    provider: SqlProvider.Sqlite,
    stringConnection: $"Data Source={filePath}"
));
```

### Custom configuration

```c#
DbConfigurationFactory.Register(new DbSettings { 
    Signature = "db1",
    StringConnection = $"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}",
    DetailErrorMessage = true,
    ConnectionTimeout = 300,
    SensitiveDataLog = true
 });
```
### Expression configuration
```c#
   var builder = new TableBuilder();
   builder.Configure<User>(key: x => x.Id).AddFieldsAsColumns<User>().DbName("system_user");
   DbFactory.AddDbBuilder(builder);
```

### Expressions calls
```c#
public class UserRepository
{
    public List<User> GetUsers() => DbFactory.GetDbContext("db1").ToList<User>();
    public List<User> GetInvalidUsers() => DbFactory.GetDbContext("db1").ToList<User>(
        user => 
        SqlExpression.IsNull<Person>(x => x.LastName) &&
        x.Age <= 0 && x.Age > 100 &&
        x.Salary <= 0
    );

    public int Add(User user) => DbFactory.GetDbContext("db2").Add<User, int>(user);
    public void Update(User user) => DbFactory.GetDbContext("db2").Update(user);
    public void Delete(User user) => DbFactory.GetDbContext("db2").Delete(user);
}
```
### Direct calls
```c#
    public void DisableUser(int id) => DbFactory.GetDbContext("db2").ExecuteOp(new { id }, "disable_user");
    public Tuple<IEnumerable<User>, IEnumerable<Office>> ProcessData() {
        return DbFactory.GetDbContext("db1").ToTupleOp<User, Office>("sp_process");
    }

    public bool ActiveOffice(decimal officeId)
    {
        return DbFactory.GetDbContext("db2").ExecuteTransaction((db) =>
        {
            db.Execute("UPDATE User SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            db.Execute("UPDATE Office SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            return db.Commit();
        });
    }

    //batching
    public void Export()
    {
        var (dispose, data) = sqlServerContext.FetchData<Order>(script: "SELECT * FROM Order", parameters: null, batchSize: 10000);
        foreach (var batch in data)
        {
            //process batch
        }
        dispose();
    }

    //records
    public record User(int Id, string Name, byte[] photo);
    public List<User> Export()
    {
        return sqlServerContext.ToList<User>("SELECT * FROM User"); 
    }
```

### Perfomance

Experience really good performance without the hassle of managing DbConnection. Here's a fair test where we consistently dispose of connections after each use.

Note: Tested with Docker MSSQL locally

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4112/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.304
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


| Detailed Runtime           | Type                       | Method                      | Mean     | StdDev   | Error    | Op/s    | Gen0   | Allocated |
|--------------------------- |--------------------------- |---------------------------- |---------:|---------:|---------:|--------:|-------:|----------:|
| .NET 8.0.8 (8.0.824.36612) | ThomasDataAdapterBenchmark | Single<>                    | 464.3 us | 51.84 us | 18.82 us | 2,153.8 | 0.4883 |   7.81 KB |
| .NET 8.0.8 (8.0.824.36612) | DapperBenckmark            | QuerySingle<T>              | 482.0 us | 68.35 us | 23.82 us | 2,074.7 | 0.4883 |   8.26 KB |
| .NET 8.0.8 (8.0.824.36612) | ThomasDataAdapterBenchmark | ToList<>                    | 516.8 us | 24.02 us | 10.28 us | 1,935.0 | 1.9531 |  25.12 KB |
| .NET 8.0.8 (8.0.824.36612) | DapperBenckmark            | Query<T>                    | 536.9 us | 63.32 us | 22.45 us | 1,862.5 | 0.9766 |  23.63 KB |
| .NET 8.0.8 (8.0.824.36612) | ThomasDataAdapterBenchmark | 'ToListRecord<> Expression' | 588.5 us | 66.56 us | 23.20 us | 1,699.2 | 1.9531 |  26.02 KB |
```

### Best practices

- For fastest data retrieve prefer use Store procedure over Sql text queries
- Prefer use Records over Anemic class to retrieve data
- Try different bufferSize values when need read streams for optimal performance 
- Columns defined as NOT NULL will enhance the generate algorithm because will avoid IsDBNull branching
- Use latest database provider library version as much as possible for security and performance concerns
- Use nullable Datatype for classes used to write operations or store procedures object filter for more natural interpretation of DBNull when is possible, here are more DbNull implicit values:


| Type        | implicit DbNull           | 
| ------------- |:-------------:| 
| String      | null |
| Datetime      | MinValue      |
| TimeSpan | MinValue      |
| Guid | default      |
| StringBuilder | null      |
| XmlDocument | null      |
| Byte[] | null or empty array     |
| Nullable<> | not(HasValue)      |

### Considerations
Library's purpose is to make easy DB interactions with a simple configuration. Obviously it doesn't attempt to solve every problem.
There are some considerations at development time:

- Ensure that the specific database provider library is installed in your project, as it will be accessed via reflection by this library.
- Configuration for write operations (insert, update and delete) requires TableBuilder preferring at application startup.
 
### Db Types

 - Nullable versions has the same corresponding DbType
 - DateOnly and TimeOnly available in NET 6 or greater
 - C# types will transform to their specific dbTypes to setup parameters command
 - SQLite dbtypes won't setup so when create dynamically parameters the library itself to infer their type

| C# Type        | SQL Server           | Oracle           | PostgreSQL           | MySQL
| ------------- | ------------- | ------------- | ------------- |:-------------:|
| string      | NVARCHAR | VARCHAR2 | VARCHAR | VARCHAR
| short / ushort    | SMALLINT | INT16 | SMALLINT | INT16
| int / uint      | INT | INT32 | INTEGER | INT32
| long / ulong    | BIGINT | INT64 | BIGINT | INT64
| byte / sbyte    | TINYINT | BYTE | SMALLINT | BYTE
| decimal      | DECIMAL | DECIMAL | NUMERIC | DECIMAL
| double      | FLOAT | DOUBLE | DOUBLE | DOUBLE
| float      | FLOAT | FLOAT | REAL | FLOAT
| bool      | BIT | INT32 (0 or 1) | BIT | BIT
| DateTime      | DATETIME | DATE | TIMESTAMP | DATETIME
| TimeSpan      | TIME | IntervalDS | INTERVAL | -
| Guid      | UNIQUEIDENTIFIER | RAW | UUID | GUID
| byte[]      | VARBINARY | BLOB | BYTEA | MEDIUMBLOB
| SqlBinary      | BINARY | - | - | -
| XmlDocument      | XML | XMLTYPE | XML | XML
| StringBuilder      | TEXT | CLOB | TEXT | -
| DateOnly      | DATE | DATE | DATE | DATE
| TimeOnly      | TIME | - | TIME | TIME

### Limitations
Be aware of the following limitations:

- Currently limited to specific DB library providers, with plans to expand support in the future.
- Depend on [Sigil](https://github.com/kevin-montrose/Sigil) a powerful reflection.emit ILGenerator. However, I consider remove it.