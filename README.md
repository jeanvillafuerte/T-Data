# ![](./TDataIco.png "T-Data") _**T-Data**_

## Simplified setup for robust database operations by relief having to manage connection, commands or transactions

### 🌟 Join Us in Shaping the Future of T-Data! 🌟
Join us in refining a library designed to streamline database interactions effortlessly. Your insights, tests, and small enhancements can make a big difference. Feel free to fix bugs and suggest features.

### 🎯 Features:

- Seamless compatibility with both .NET Core and .NET Framework.
- Type-safe data results for enhanced reliability.
- Streamlined query integration with full support for both typed and anonymous parameters.
- Flexible transaction handling with lambda-based expressions.
- Customizable DbParameter attributes, including direction, size, and precision.
- Unique database signature for instant static context creation.
- Cache data management to boost performance with dynamic cache refresh capabilities.
- Full compatibility with record types.
- Zero dependencies.

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
DbConfig.Register(new DbSettings(
    signature: "db1",
    provider: SqlProvider.SqlServer,
    stringConnection: $"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}"
));

DbConfig.Register(new DbSettings(
    signature: "db2",
    provider: SqlProvider.Sqlite,
    stringConnection: $"Data Source={filePath}"
));
```

### Custom configuration

```c#
DbConfig.Register(new DbSettings { 
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
   builder.AddTable<User>(key: x => x.Id).AddFieldsAsColumns<User>().DbName("system_user");
   DbConfig.AddDbBuilder(builder);
```

### Expressions calls
```c#
public class UserRepository
{
    public List<User> GetUsers() => DbHub.Use("db1").FetchList<User>();
    public List<User> GetInvalidUsers() => DbHub.Use("db1").FetchList<User>(
        user => 
        user.LastName == null &&
        user.Age <= 0 && user.Age > 100 &&
        user.Salary <= 0
    );

    public int Add(User user) => DbHub.Use("db2").Add<User, int>(user);
    public void Update(User user) => DbHub.Use("db2").Update(user);
    public void Delete(User user) => DbHub.Use("db2").Delete(user);
}
```
### Direct calls
```c#
    public void DisableUser(int id) => DbHub.Use("db2").ExecuteOp("disable_user", new { id });
    public Tuple<List<User>, List<Office>> ProcessData() {
        return DbHub.Use("db1").ToTuple<User, Office>("sp_process");
    }

    public bool ActiveOffice(decimal officeId)
    {
        return DbHub.Use("db2").ExecuteTransaction((db) =>
        {
            db.Execute("UPDATE User SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            db.Execute("UPDATE Office SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            return db.Commit();
        });
    }

    //batching
    public void Export()
    {
        var (dispose, data) = DbHub.Use("db1").FetchData<Order>(script: "SELECT * FROM Order", parameters: null, batchSize: 10000);
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
        return DbHub.Use("db1").FetchList<User>("SELECT * FROM User"); 
    }
```

### Perfomance

Experience really good performance without the hassle of managing DbConnection. Here's a fair test where we consistently dispose of connections after each use.

Note: Tested with Docker MSSQL locally

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

| Detailed Runtime           | Type             | Method                   | Mean     | StdDev   | Error    | Op/s    | Gen0   | Allocated |
|--------------------------- |------------------|------------------------- |---------:|---------:|---------:|--------:|-------:|----------:|
| .NET 8.0.8 (8.0.824.36612) | TDataBenchmark   | FetchOne<>               | 398.2 us |  4.01 us |  5.13 us | 2,511.3 | 0.4883 |   8.09 KB |
| .NET 8.0.8 (8.0.824.36612) | DapperBenckmark  | QuerySingle<T>           | 406.7 us |  6.78 us |  7.25 us | 2,459.0 | 0.4883 |   7.43 KB |
| .NET 8.0.8 (8.0.824.36612) | TDataBenchmark   | FetchListRecord<>        | 447.2 us |  9.42 us |  8.80 us | 2,236.3 | 1.9531 |  25.02 KB |
| .NET 8.0.8 (8.0.824.36612) | TDataBenchmark   | FetchList<>              | 465.0 us |  7.68 us |  9.20 us | 2,150.3 | 1.9531 |  25.53 KB |
| .NET 8.0.8 (8.0.824.36612) | DapperBenckmark  | Query<T>                 | 513.8 us | 49.20 us | 16.87 us | 1,946.4 | 1.9531 |   30.3 KB |
| .NET 8.0.8 (8.0.824.36612) | TDataBenchmark   | 'FetchList<> Expression' | 529.4 us | 60.12 us | 20.50 us | 1,889.0 | 1.9531 |  27.65 KB |
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