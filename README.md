# ![](./ThomasIco.png "ThomasDataAdapter") _**ThomasDataAdapter**_

>It Works matching class fields againts result set returned by database query. There are simple configurations for applications that need fast response time without must deal with database DbConnections and DbCommands.

Features released 2.1.0:

- Set a unique signature for each database settings that must instantiate statically a database context whenever you want.
- Straightforward manner to match typed and anonymous types to query parameters.
- Transaction support using lambda expressions.
- Attributes to match parameter direction and size to configure DbParameter.
- Optional Cache layer to boost application performance having control what will be stored, updated and removed.
- Cache refresh throught a key identifier to allow refresh whenever you want update the cached data without use original query.


## Nuget : https://www.nuget.org/packages/ThomasDataAdapter.SqlServer/
#
## Basic configuration in startup.cs :

```c#
using Thomas.Database.SqlServer;
.
.
.
SqlServerFactory.AddDb(new DbSettings 
{
    Signature = "mssqldb1",
    StringConnection = @"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}"
});
```

## Custom configuration :

```c#
using Thomas.Database.SqlServer;
.
.
.
SqlServerFactory.AddDb(new DbSettings { 
    Signature = "mssqldb1",
    StringConnection = @"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}",
    DetailErrorMessage = true,
    ConnectionTimeout = 300,
    SensitiveDataLog = true
 });
```

## How to use it?
```c#
public class UserOffice
{
    private readonly IThomasDb _db;

    public User()
    {
        _db =  DbFactory.CreateDbContext("mssqldb1");
    }

    public IEnumerable<User> GetUsers() => _db.ToList<User>("dbo.GetPeople");
    public void SaveUser(User person) =>_db.Execute(person, "dbo.Save");
    public void UpdateUser(string id, int age) => _db.ExecuteOp(new { id = id, age = age}, "dbo.Update");
    public Tuple<IEnumerable<User>, IEnumerable<Office>> ProcessData() => _db.ToTupleOp<User, Office>("dbo.Process");

    public bool ActiveOffice(decimal officeId)
    {
        return _db.ExecuteTransaction((db) =>
        {
            db.Execute($"UPDATE User SET Active = 1 WHERE OfficeId = @OfficeId", new { OfficeId = officeId });
            db.Execute($"UPDATE Office SET Active = 1 WHERE OfficeId = @OfficeId", new { OfficeId = officeId });
            return db.Commit();
        });
    }
}
```

### Options to configure:

* **Culture**: Default 'en-US' (resource : https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c)
* **StringConnection**: This can be combine optionally with User and Password.
* **DetailErrorMessage**: Default false, return detail information when errors ocurrs and also parameters values.
* **HideSensibleDataValue**:  Default false, hide sensible data value in error message and log.
* **ConnectionTimeout** : Default 0, time out for database request.

## Perfomance

The benchmark is in project **Thomas.Tests.Performance**

```bash
dotnet run -p .\Thomas.Tests.Performance\ -c Release -f net5.0 -- -f * --join
```

``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.1466 (20H2/October2020Update)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.404
  [Host]     : .NET Core 3.1.22 (CoreCLR 4.700.21.56803, CoreFX 4.700.21.57101), X64 RyuJIT
  ShortRun   : .NET Core 3.1.22 (CoreCLR 4.700.21.56803, CoreFX 4.700.21.57101), X64 RyuJIT
  Job-QAXZEQ : .NET Core 3.1.22 (CoreCLR 4.700.21.56803, CoreFX 4.700.21.57101), X64 RyuJIT

Namespace=Thomas.Tests.Performance.Benchmark  Type=ThomasDataAdapterBenckmark


```
| Detailed Runtime                         | Type                       | Method                       | Mean           | StdDev       | Error        | Op/s        | GcMode             | Completed Work Items | Lock Contentions | Gen0   | Allocated |
|----------------------------------------- |--------------------------- |----------------------------- |---------------:|-------------:|-------------:|------------:|------------------- |---------------------:|-----------------:|-------:|----------:|
| .NET 8.0.1 (8.0.123.58001)               | ThomasDataAdapterBenckmark | 'ToList<> (buffered)'        |       881.0 ns |     24.99 ns |     17.42 ns | 1,135,053.3 | Toolchain=.NET 8.0 |                    - |                - | 0.0343 |     440 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | ThomasDataAdapterBenckmark | 'ToList<> (buffered)'        |     1,066.4 ns |      0.00 ns |           NA |   937,747.7 | ShortRun           |               0.0000 |                - | 0.0488 |     624 B |
| .NET 8.0.1 (8.0.123.58001)               | DapperBenckmark            | 'Query<T> List (unbuffered)' |   558,085.4 ns | 24,088.59 ns | 10,974.18 ns |     1,791.8 | Toolchain=.NET 8.0 |                    - |                - |      - |    8481 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | DapperBenckmark            | 'Query<T> List (unbuffered)' |   561,390.1 ns |      0.00 ns |           NA |     1,781.3 | ShortRun           |               0.0013 |                - | 0.6250 |    8168 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | ThomasDataAdapterBenckmark | 'ToList<> (unbuffered)'      |   562,313.2 ns |      0.00 ns |           NA |     1,778.4 | ShortRun           |               0.0013 |                - | 0.6250 |   10760 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | DapperBenckmark            | 'Query<dynamic> (buffered)'  |   567,313.9 ns |      0.00 ns |           NA |     1,762.7 | ShortRun           |               0.0013 |                - | 0.6250 |    8384 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | DapperBenckmark            | 'Query<T> (unbuffered)'      |   570,249.6 ns |      0.00 ns |           NA |     1,753.6 | ShortRun           |               0.0013 |                - | 0.6250 |    8096 B |
| .NET 8.0.1 (8.0.123.58001)               | ThomasDataAdapterBenckmark | 'ToList<> (unbuffered)'      |   592,551.6 ns | 22,874.55 ns | 11,727.99 ns |     1,687.6 | Toolchain=.NET 8.0 |                    - |                - |      - |   10161 B |
| .NET 8.0.1 (8.0.123.58001)               | DapperBenckmark            | 'Query<dynamic> (buffered)'  |   593,874.5 ns | 29,064.91 ns | 11,758.83 ns |     1,683.9 | Toolchain=.NET 8.0 |                    - |                - |      - |    8577 B |
| .NET 8.0.1 (8.0.123.58001)               | DapperBenckmark            | 'Query<T> (unbuffered)'      |   632,908.2 ns | 32,045.51 ns | 12,589.98 ns |     1,580.0 | Toolchain=.NET 8.0 |                    - |                - |      - |    8025 B |
| .NET Core 3.1.32 (CoreCLR 4.700.22.55902 | DapperBenckmark            | 'Query<T> (unbuffered)'      | 1,000,659.2 ns |      0.00 ns |           NA |       999.3 | ShortRun           |               0.0025 |                - |      - |    8464 B |
| .NET 8.0.1 (8.0.123.58001)               | DapperBenckmark            | 'Query<T> (unbuffered)'      | 1,032,863.4 ns | 30,836.70 ns | 20,192.91 ns |       968.2 | Toolchain=.NET 8.0 |                    - |                - |      - |    8249 B |