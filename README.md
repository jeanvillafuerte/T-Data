# ThomasDataAdapter
## Simple library to get data from Database SQL Server specially high load and low memory consum.
#
It Works matching class fields vs result set returned by database query. There are simples configurations for general purpose as high load work.


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
        var response = _db.ToList<Person>("dbo.GetPeople");

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
        var response = _db.Execute(person, "dbo.SavePerson");

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
        var response = _db.Execute("dbo.UpdateAge");

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
        var response = _db.ToTuple<Person, Office>("dbo.GetDataForProcess");

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
dotnet run -p .\benchmarks\Dapper.Tests.Performance\ -c Release -f netcoreapp3.1 -- -f * --join
```

``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19042.1083 (20H2/October2020Update)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.202
  [Host]   : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  ShortRun : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT


```
|    Method |     Mean |  StdDev |    Error |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------- |---------:|--------:|---------:|-------:|-------:|------:|----------:|
|  Single&lt;&gt; | 136.8 μs | 0.39 μs |  0.65 μs | 2.5000 |      - |     - |     10 KB |
|  ToList&lt;&gt; | 146.6 μs | 9.12 μs | 13.79 μs | 2.5000 |      - |     - |     10 KB |
| ToTuple&lt;&gt; | 158.4 μs | 0.74 μs |  1.12 μs | 4.5000 | 0.2500 |     - |     19 KB |
