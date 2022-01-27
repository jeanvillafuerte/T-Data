# ![](./ThomasIco.png "ThomasDataAdapter") _**ThomasDataAdapter**_
## Simple strong typed library to get data from Database SQL Server especially high load and low memory consum.
#
>It Works matching class fields vs result set returned by database query. There are simples configurations for general purpose as high load work.


## Nuget : https://www.nuget.org/packages/ThomasDataAdapter.SqlServer/
#
### Basic configuration in startup.cs :

```c#
using Thomas.Database.SqlServer;
.
.
.
serviceCollection.AddThomasSqlDatabase((options) => new ThomasDbStrategyOptions()
{
    StringConnection = @"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}"
});
```

### Custom configuration :

```c#
using Thomas.Database.SqlServer;
.
.
.
serviceCollection.AddThomasSqlDatabase((options) => new ThomasDbStrategyOptions()
{
    StringConnection = @"Data Source={SourceName};Initial Catalog={database};User ID={User};Password={Pass}",
    DetailErrorMessage = true,
    MaxDegreeOfParallelism = 4,
    ConnectionTimeout = 300,
    SensitiveDataLog = true
});
```

### Inject **IThomasDb** interface in your component:
```c#
public class MyComponent
{
    private readonly IThomasDb _db;

    public MyComponent(IThomasDb db)
    {
        _db = db;
    }

    public IList<Person> GetPeople()
    {
        var response = _db.ToListOp<Person>("dbo.GetPeople");

        if(response.Success)
        {
            return response.Result;
        }
        else
        {
            Log.WriteLog(response.ErrorMessage);
        }
    }

    public void SavePeople(Person person)
    {
        var response = _db.ExecuteOp(person, "dbo.SavePerson");

        if(response.Success)
        {
            .
            .
            .
        }
        else
        {
            Log.WriteLog(response.ErrorMessage);
        }
    }

    public void UpdateAge()
    {
        var response = _db.ExecuteOp("dbo.UpdateAge");

        if(response.Success)
        {
            .
            .
            .
        }
        else
        {
            Log.WriteLog(response.ErrorMessage);
        }
    }

    public void ProcessData()
    {
        var response = _db.ToTupleOp<Person, Office>("dbo.GetDataForProcess");

        if(response.Success)
        {
            IReadOnlyList<Person> persons = response.Result.Item1;
            IReadOnlyList<Office> offices = response.Result.Item2;
            .
            .
            .
        }
        else
        {
            Log.WriteLog(response.ErrorMessage);
        }
    }

    public void ManyFormsToGetData()
    {
        Person person = _db.ToSingle<Person>("SELECT * FROM Person WHERE Id = 43");

        IReadOnlyList<Person> people = _db.ToList<Person>("SELECT * FROM Person WHERE name like '%jhon%'");

        Tuple<IReadOnlyList<Person>, IReadOnlyList<Office>> peopleOffice = _db.Tuple<Person, Office>("SELECT * FROM Person WHERE name like '%jhon%'; SELECT * FROM Office WHERE name like '%US_%");
    }
}
```

### More options to configure:

* **Culture**: Default 'en-US' (resource : https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c)
* **User**: Username for string connection.
* **Password**: Password for string connection. (type SecureString)
* **StringConnection**: This can be combine optionally with User and Password.
* **DetailErrorMessage**: Default false, return detail information when errors ocurrs and also parameters values.
* **SensitiveDataLog**:  Default false, hide Parameters values.
* **StrictMode**: Default false, all columns returned from database query must be match with fields in the class.
* **MaxDegreeOfParallelism**: Default 1, useful when you need retrieve high volume of data and maintain original order. Depends on amount of logical processors you have
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
|                         Method |     Mean |   StdDev |    Error |    Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |---------:|---------:|---------:|--------:|-------:|------:|------:|----------:|
|                       Single<> | 123.5 us |  0.37 us |  0.71 us | 8,095.1 | 1.8750 |     - |     - |      8 KB |
|                     SingleOp<> | 124.3 us |  0.93 us |  1.12 us | 8,046.2 | 1.9531 |     - |     - |      8 KB |
|  'SingleOp<> T with nullables' | 124.4 us |  0.46 us |  0.55 us | 8,038.0 | 1.9531 |     - |     - |      8 KB |
|  'ToListOp<> T with nullables' | 125.8 us |  2.95 us |  4.96 us | 7,946.5 | 2.0313 |     - |     - |      8 KB |
|  'ToListOp<> T with nullables' | 126.0 us |  0.62 us |  0.74 us | 7,935.7 | 1.9531 |     - |     - |      8 KB |
|                     ToListOp<> | 126.0 us |  1.37 us |  2.62 us | 7,933.5 | 2.0313 |     - |     - |      8 KB |
|    'ToList<> T with nullables' | 127.1 us |  0.69 us |  1.32 us | 7,868.4 | 1.8750 |     - |     - |      8 KB |
|    'Single<> T with nullables' | 127.5 us |  4.29 us |  6.48 us | 7,840.7 | 1.8750 |     - |     - |      8 KB |
|  'SingleOp<> T with nullables' | 128.0 us |  0.39 us |  0.74 us | 7,815.2 | 2.0313 |     - |     - |      8 KB |
|                       Single<> | 128.1 us |  3.38 us |  2.47 us | 7,807.5 | 1.9531 |     - |     - |      8 KB |
|                       ToList<> | 128.1 us |  3.38 us |  5.11 us | 7,806.2 | 1.8750 |     - |     - |      8 KB |
|    'ToList<> T with nullables' | 128.2 us |  2.33 us |  2.37 us | 7,800.7 | 1.9531 |     - |     - |      8 KB |
|                       ToList<> | 128.8 us |  2.81 us |  2.53 us | 7,762.4 | 1.9531 |     - |     - |      8 KB |
|                     SingleOp<> | 129.7 us |  0.56 us |  0.85 us | 7,711.0 | 1.8750 |     - |     - |      8 KB |
|    'Single<> T with nullables' | 130.0 us |  0.73 us |  0.82 us | 7,691.1 | 1.9531 |     - |     - |      8 KB |
|                     ToListOp<> | 139.3 us |  2.23 us |  2.38 us | 7,177.7 | 1.9531 |     - |     - |      8 KB |
|                    ToTupleOp<> | 140.1 us |  0.50 us |  0.59 us | 7,137.9 | 3.9063 |     - |     - |     17 KB |
| 'ToTupleOp<> T with nullables' | 141.0 us |  0.37 us |  0.40 us | 7,092.4 | 3.9063 |     - |     - |     17 KB |
|   'ToTuple<> T with nullables' | 144.0 us |  1.63 us |  1.95 us | 6,946.8 | 3.9063 |     - |     - |     17 KB |
|   'ToTuple<> T with nullables' | 144.7 us |  6.08 us |  9.19 us | 6,909.9 | 3.9063 |     - |     - |     17 KB |
|                      ToTuple<> | 147.9 us |  1.90 us |  2.14 us | 6,760.0 | 3.9063 |     - |     - |     17 KB |
|                    ToTupleOp<> | 149.3 us |  0.75 us |  1.27 us | 6,699.1 | 3.9063 |     - |     - |     17 KB |
| 'ToTupleOp<> T with nullables' | 161.8 us |  7.99 us | 12.08 us | 6,181.6 | 3.9063 |     - |     - |     17 KB |
|                      ToTuple<> | 173.3 us | 32.93 us | 49.79 us | 5,769.8 | 3.9063 |     - |     - |     17 KB |