# ![](./ThomasIco.png "ThomasDataAdapter") _**ThomasDataAdapter**_

## Straightforward configuration for powerful Db requests.

### Features released 3.0.0:

- Straightforward manner to match typed and anonymous types to query parameters.
- Transaction support using lambda expressions.
- Attributes to match parameter direction, size and precision to configure DbParameter.
- Set a unique signature for each database settings that must instantiate statically a database context whenever you want.
- Support for SqlServer, Postgresql, Oracle, Mysql and Sqlite
- Optional Cache layer to boost application performance having control what will be stored, updated and removed as well as refresh throught a key identifier to allow refresh whenever you required refreshing using cached query and/or expression and parameters.

### Supported database provider libraries:
- Microsoft.Data.SqlClient: 2.1.7 and UP
- Oracle.ManagedDataAccess.Core: 2.18.3 and UP
- Npgsql: 3.2.4 and UP
- Mysql.Data: 8.0.28 and UP
- Microsoft.Data.Sqlite: 3.0.0 and UP


## Quick start

### Basic configuration in startup.cs :

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

### Expressions calls:
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
### Direct calls:
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
```

### Perfomance

Excellent performance having as a main key usability taking into account that you don't have to open and close connection anymore so makes you 

``` ini
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3527/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GMJVPM : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
