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
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3527/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
  Job-VNBJGA : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


| Detailed Runtime           | Type                       | Method                      | Mean     | StdDev   | Error    | Op/s    | Gen0   | Allocated |
|--------------------------- |--------------------------- |---------------------------- |---------:|---------:|---------:|--------:|-------:|----------:|
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | Single<>                    | 432.8 us | 22.58 us |  8.15 us | 2,310.6 |      - |   8.04 KB |
| .NET 8.0.1 (8.0.123.58001) | DapperBenckmark            | QuerySingle<T>              | 500.0 us | 25.82 us |  9.93 us | 2,000.1 |      - |  11.54 KB |
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | 'ToListRecord<> Expression' | 563.7 us | 44.90 us | 15.23 us | 1,774.0 | 1.9531 |  26.76 KB |
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | ToList<>                    | 572.7 us | 12.89 us | 10.83 us | 1,746.2 | 1.9531 |  26.47 KB |
| .NET 8.0.1 (8.0.123.58001) | DapperBenckmark            | 'Query<T> (buffered)'       | 602.0 us | 38.75 us | 13.36 us | 1,661.1 | 1.9531 |  26.45 KB |
```

### Best practices

- For fastest data retrieve prefer use Store procedure over Sql text queries
- Prefer use Records over Anemic class to retrieve data
- Try different bufferSize values when need read streams for optimal performance 
- Columns defined as NOT NULL will enhance the generate algorithm because will avoid IsDBNull branching
- Use latest database provider library version as much as possible for security and performance concerns
- Use nullable Datatype for classes used to write operations or store procedures object filter for more natural interpretation of DBNull when is possible, here are more DbNull implicit values from other datatypes:

### Considerations
Library's purpose is to make easy DB interactions with a simple configuration. Obviously it doesn't attempt to solve every problem.
There are some considerations at development time:

- Ensure that the specific database provider library is installed in your project, as it will be accessed via reflection by this library.
- Configuration for write operations (insert, update and delete) requires TableBuilder preferring at application startup.
 
### Limitations
Be aware of the following limitations:

- Currently limited to specific DB library providers, with plans to expand support in the future.
- Depend on [Sigil](https://github.com/kevin-montrose/Sigil) a powerful reflection.emit ILGenerator. However, I consider remove it.