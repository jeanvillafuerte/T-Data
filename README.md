# ![](./ThomasIco.png "ThomasDataAdapter") _**ThomasDataAdapter**_

## Simple configuration for powerful Db requests.

### Features released 3.0.0:

- Support for SqlServer, Postgresql, Oracle, Mysql and Sqlite
- Set a unique signature for each database settings that must instantiate statically a database context whenever you want.
- Straightforward manner to match typed and anonymous types to query parameters.
- Transaction support using lambda expressions.
- Attributes to match parameter direction, size and precision to configure DbParameter.
- Optional Cache layer to boost application performance having control what will be stored, updated and removed as well as refresh throught a key identifier to allow refresh whenever you required refreshing using cached query and/or expression and parameters.

### Supported database provider libraries:
- Microsoft.Data.SqlClient: 2.1.7 and UP
- Oracle.ManagedDataAccess.Core: 2.18.3 and UP
- Npgsql: 3.2.4 and UP
- Mysql.Data: 8.0.28 and UP
- Microsoft.Data.Sqlite: 3.0.0 and UP


## Quick start

### Basic configuration in startup.cs :

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

### Custom configuration :

```c#
DbConfigurationFactory.Register(new DbSettings { 
    Signature = "db1",
    StringConnection = $"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}",
    DetailErrorMessage = true,
    ConnectionTimeout = 300,
    SensitiveDataLog = true
 });
```
### Expression configuration :
```c#
   var builder = new TableBuilder();
   builder.Configure<User>(key: x => x.Id).AddFieldsAsColumns<User>().DbName("system_user");
   DbFactory.AddDbBuilder(builder);
```

### Expressions
```c#
public class UserRepository
{
    public List<User> GetUsers() => DbFactory.GetDbContext("db1").ToList<User>();
    public int Add(User user) => DbFactory.GetDbContext("db2").Add<User, int>(user);
    public void Update(User user) => DbFactory.GetDbContext("db2").Update(user);
    public void Delete(User user) => DbFactory.GetDbContext("db2").Delete(user);
}
```
### Direct SqlText
```c#
    public void DisableUser(int id) => DbFactory.GetDbContext("db2").ExecuteOp(new { id }, "disable_user");
    public Tuple<IEnumerable<User>, IEnumerable<Office>> ProcessData() => DbFactory.GetDbContext("db1").ToTupleOp<User, Office>("sp_process");

    public bool ActiveOffice(decimal officeId)
    {
        return DbFactory.GetDbContext("db2").ExecuteTransaction((db) =>
        {
            db.Execute("UPDATE User SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            db.Execute("UPDATE Office SET Active = 1 WHERE OfficeId = $officeId", new { officeId });
            return db.Commit();
        });
    }
```

### Perfomance

Excellent performance considering that you do not have to deal with database objects.
Here a short comparation with dapper:

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3527/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GMJVPM : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


```
| Detailed Runtime           | Type                       | Method                             | Mean         | StdDev       | Error        | Op/s        | GcMode             | Completed Work Items | Lock Contentions | Gen0   | Allocated |
|--------------------------- |--------------------------- |----------------------------------- |-------------:|-------------:|-------------:|------------:|------------------- |---------------------:|-----------------:|-------:|----------:|
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | 'ToList<> (buffered)'              |     141.3 ns |      5.49 ns |      2.63 ns | 7,077,184.3 | Toolchain=.NET 8.0 |                    - |                - | 0.0184 |     232 B |
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | 'ToList<> Expression (buffered)'   |     641.1 ns |     12.38 ns |     12.60 ns | 1,559,918.5 | Toolchain=.NET 8.0 |                    - |                - | 0.0916 |    1163 B |
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | 'ToList<> (unbuffered)'            | 470,478.3 ns | 42,804.51 ns | 15,355.33 ns |     2,125.5 | Toolchain=.NET 8.0 |                    - |                - |      - |    8217 B |
| .NET 8.0.1 (8.0.123.58001) | ThomasDataAdapterBenckmark | 'ToList<> Expression (unbuffered)' | 480,278.6 ns | 22,123.27 ns |  9,545.55 ns |     2,082.1 | Toolchain=.NET 8.0 |                    - |                - |      - |   10746 B |
| .NET 8.0.1 (8.0.123.58001) | DapperBenckmark            | 'Query<dynamic> (buffered)'        | 510,762.6 ns | 20,403.67 ns | 10,100.58 ns |     1,957.9 | Toolchain=.NET 8.0 |                    - |                - |      - |    9264 B |
| .NET 8.0.1 (8.0.123.58001) | DapperBenckmark            | 'Query<T> (unbuffered)'            | 598,158.0 ns | 26,237.02 ns | 11,952.95 ns |     1,671.8 | Toolchain=.NET 8.0 |                    - |                - | 1.9531 |   29673 B |