# ![](./TDataIco.png "T-Data") _**T-Data**_

## Simplified setup for robust database operations by relief having to manage connection, commands or transactions

### 🌟 Join Us in Shaping the Future of T-Data! 🌟
Join us in refining a library designed to streamline database interactions effortlessly. Your insights, tests, and small enhancements can make a big difference. Feel free to fix bugs and suggest features.

### 🎯 Features:

- Easily register multiple database contexts with a unique signature for instant static context creation and seamless reuse.
- Seamless compatibility with both .NET Core and .NET Framework.
- Type-safe data results for enhanced reliability.
- Streamlined query integration with full support for both typed and anonymous parameters.
- Flexible transaction handling with lambda-based expressions.
- Customizable DbParameter attributes, including direction, size, and precision.
- Cache data management to boost performance with dynamic cache refresh capabilities.
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
### Use with text queries and store procedures:

This feature will permit perform text queries calls 

```c#
  var dbContext = DbHub.Use(DbSignature);
  var users = dbContext.FetchList<User>("SELECT * FROM APP_USER");
  var user = dbContext.FetchOne<User>($"SELECT * FROM APP_USER WHERE ID = @Id", new { Id = id });
```
here resilient way to perform them:

```c#
  var dbContext = DbHub.Use(DbSignature);
  var resultList = dbContext.TryFetchList<User>("SELECT * FROM APP_USER");
  
  if (resultList.Success) {
    var users = resultList.Result;
  }

  var ResultUser = dbContext.TryFetchOne<User>($"SELECT * FROM APP_USER WHERE ID = @Id", new { Id = id });
```

Using Store procedures

```c#
    dbContext.FetchOne<User>("GET_USER", new { user_id }); //considering store procedure parameter is named user_id

    //retrieving a multiple result sets
    dbContext.FetchTuple<User, UserType>("GET_DATA");
```

By using output parameters, the library dynamically populates the corresponding fields with the retrieved values.

```c#

    //define class that hold store procedure parameters
    public class UserTotal
    {
        public int Total { get; set; }
        public decimal TotalSalary { get; set; }
    }

    var filter = new UserTotal();
    await dbContext.ExecuteAsync("GET_TOTALUSER", filter);
```

### Scalar values

```c#
    var data = dbContext.ExecuteScalar<int>($"SELECT @@MAX_CONNECTIONS - @value", new { value = 1 });
    var date = dbContext.ExecuteScalar<string>($"SELECT @@VERSION");
```

### Block Execution

This feature allows you to reuse the same DbConnection for multiple operations, improving performance by reducing connection overhead. It also lets you define custom execution rules, such as handling errors gracefully or applying specific logic during execution.

```c#
   DbHub.Use(DbSignature, buffered: false).ExecuteBlock((db) =>
   {
       db.Execute("DROP TABLE IF EXISTS BOOK");
       db.TryExecute("DROP TABLE IF EXISTS APP_USER");
       db.TryExecute("DROP TABLE IF EXISTS USER_TYPE");
   });
```

### Fetch Paged List

Using the base query, this feature automatically handles pagination for you by generating the appropriate paginated queries and managing the retrieval process seamlessly.

```c#
    foreach (var users in dbContext.FetchPagedList<User>("SELECT * FROM APP_USER ORDER BY Name", offset: 0, pageSize, 1000))
    {
        ...
    }
```
### Cache feature
The caching layer helps improve performance by temporarily storing data with a configurable TTL (Time to Live). Once the TTL expires, the database is queried again to fetch fresh data, which is then re-cached for the specified duration.

### Key Behaviors:
**Query Flagging**: Assign a unique string key to specific queries, enabling manual data refresh from anywhere in the application.

**Cache Types**: Supports both In-Memory and SQLite caching strategies.

**Cache Management**: 
- Clear a specific cached value using its unique key.
- Clear all cached values tied to a particular database signature.

**Flexible Data Caching**:
Cache various data types, including:
- Single values
- Lists
- Tuples (multiple result sets)

**Stored Procedure Support**: In addition to caching result sets, output parameters from stored procedures are also cached.

**Default TTL**: The default cache duration is 1 day, but it can be configured based on your requirements.

### InMemory cache configuration:

```c#
DbCacheConfig.Register(
    new DbSettings("db_main_source", DbProvider.SqlServer, stringConnection),
    new CacheSettings(DbCacheProvider.InMemory) { TTL = TimeSpan.FromHours(8) });
```
### Sqlite cache configuration
Require a delegates to serialize and deserialize the result query to save in the sqlite cache, depending the format you choose is might be a Text format to save JSON format or XML or use a binary format to save BSON, Apache Avro or a format you see convenient

```c#
DbCacheConfig.Register(
    new DbSettings("db_source", DbProvider.PostgreSql, stringConnection),
    new CacheSettings(DbCacheProvider.Sqlite, isTextFormat: false, serializer_bson, deserializer_bson) { TTL = TimeSpan.FromHours(1) });
```

Additionally, this configuration offer way to retrieve the raw data from the cache layer where you can retrieve it using **TryGetStringValue** if you chose a text serialization or **TryGetBytesValue** for binary formats, if data is expired will return false.

### Expression Configuration
Configure records or classes to receive and map data from database result sets dynamically. This allows you to define how each field from the result set corresponds to properties in your objects, ensuring flexibility and reducing boilerplate code when working with different data models. No more manual queries!

```c#
   var builder = new TableBuilder();
   builder.AddTable<User>(key: x => x.Id).AddFieldsAsColumns<User>().DbName("system_user");
   DbConfig.AddTableBuilder(builder);

   //retrieve data direcly
   var dbContext = DbHub.Use("db1");
   var users = dbContext.FetchList<User>();
   var invalidUsers = dbContext.FetchList<User>(
        user => 
        user.LastName == null &&
        user.Age <= 0 && user.Age > 100 &&
        user.Salary <= 0
    );

```

Options to configure per column when needed, here some examples:
```c#
var tableBuilder = new TableBuilder();
DbTable dbTable = tableBuilder.AddTable<Order>(key: x => x.Id, keyAutoGenerated: true).AddFieldsAsColumns<Order>().DbName("TB_ORDER");
dbTable.Column<Order>(x => x.Id).DbName("ID_ORDER");
dbTable.Column<Order>(x => x.OrderNumber).DbName("ORDER_NUMBER").IsNotNull();
dbTable.Column<Order>(x => x.Price).IsNotNull();
DbHub.AddTableBuilder(tableBuilder);
```


Additionally you could perform writing operations like Insert, Update or Delete

```c#
public class UserWriteRepository
{
    public int Add(User user) => DbHub.Use("db2").Add<User, int>(user);
    public void Update(User user) => DbHub.Use("db2").Update(user);
    public void Delete(User user) => DbHub.Use("db2").Delete(user);
}
```

### More examples calls
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

Experience excellent performance without the complexity of manually managing DbConnection. Below is a benchmark test where connections are consistently disposed of after each use, ensuring fair and realistic performance metrics.
Note: Tests were conducted using Docker MSSQL on a local machine.

``` ini
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


| Detailed Runtime          | Type            | Method                         | Mean     | StdDev   | Error    | Op/s    | Gen0   | Allocated |
|-------------------------- |---------------- |------------------------------- |---------:|---------:|---------:|--------:|-------:|----------:|
| .NET 9.0.0 (9.0.24.52809) | TDataBenchmark  | FetchOne<>                     | 511.9 us |  5.38 us |  6.45 us | 1,953.7 |      - |   7.03 KB |
| .NET 9.0.0 (9.0.24.52809) | DapperBenckmark | QuerySingle<T>                 | 515.1 us |  5.11 us |  6.55 us | 1,941.2 |      - |   8.28 KB |
| .NET 9.0.0 (9.0.24.52809) | TDataBenchmark  | FetchList<>                    | 572.4 us |  7.47 us |  8.95 us | 1,747.1 | 1.9531 |  25.69 KB |
| .NET 9.0.0 (9.0.24.52809) | TDataBenchmark  | FetchListRecord<>              | 572.9 us | 10.83 us | 11.03 us | 1,745.6 | 1.9531 |  24.59 KB |
| .NET 9.0.0 (9.0.24.52809) | TDataBenchmark  | 'FetchList<> Expression'       | 576.6 us |  8.71 us |  9.83 us | 1,734.4 | 0.9766 |   23.7 KB |
| .NET 9.0.0 (9.0.24.52809) | DapperBenckmark | Query<T>                       | 702.5 us | 14.47 us | 13.52 us | 1,423.5 | 1.9531 |   26.4 KB |
| .NET 9.0.0 (9.0.24.52809) | DapperBenckmark | Query<dynamic>                 | 704.9 us | 23.44 us | 14.03 us | 1,418.7 | 1.9531 |  28.12 KB |
| .NET 9.0.0 (9.0.24.52809) | TDataBenchmark  | 'FetchListRecord<> Expression' | 717.5 us |  7.54 us |  8.06 us | 1,393.8 | 1.9531 |   27.4 KB |
```

Even without caching, the performance is comparable to Dapper, with similar memory allocation. However, when caching is utilized, performance improves dramatically:

``` ini
| Detailed Runtime          | Method                          | Mean         | StdDev     | Error      | Op/s         | Gen0   | Gen1   | Allocated |
|-------------------------- |-------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|-------:|----------:|
| .NET 9.0.0 (9.0.24.52809) | 'FetchList<> (cache in memory)' |     90.26 ns |   0.675 ns |   0.722 ns | 11,078,530.2 | 0.0267 |      - |     336 B |
| .NET 9.0.0 (9.0.24.52809) | 'FetchList<> (cache sqlite)'    | 47,002.95 ns | 453.128 ns | 484.421 ns |     21,275.3 | 8.7891 | 1.7700 |  110552 B |
```

#### Recommendation

To enhance performance, consider implementing a cache layer:

- In-memory caching provides extremely fast lookups but requires careful memory management.
- SQLite caching is suitable for large datasets and works well on systems with high SSD read speeds.

Both caching options can significantly reduce database calls and improve application responsiveness.

### Best practices

- For fastest data retrieve prefer use Store procedure over Sql text queries
- Prefer use Records over Anemic class to retrieve data
- Try different bufferSize values when need read streams for optimal performance 
- Columns defined as NOT NULL will enhance the generate algorithm because will avoid IsDBNull branching or consider use **IsNotNull** when configure the table builder, e.g. *dbTable.Column<User>(x => x.InternalCode).IsNotNull()*
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
- Not yet compatible with Ahead-of-Time (AOT) compilation; AOT support is under consideration for future releases.