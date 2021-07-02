# ThomasDataAdapter
## Simple library to get data from Database SQL Server specially high load and low memory consum.
#
It Works matching fields class vs result set returned by database query. There are simples configurations can you setting guided for custom specifications.  

Combine with well query design is useful for really fast responses especially when you need treat with high volume of information.

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
    TypeMatchConvention = TypeMatchConvention.UpperCase
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
* **TypeMatchConvention**: Enum for match fields against column from result set. UpperCase, LowerCase, CapitalLetter and Default.
* **DetailErrorMessage**: Default false, return detail information when errors ocurrs and also parameters values.
* **SensitiveDataLog**:  Default false, hide Parameters values.
* **StrictMode**: Default false, all columns returned from database query must be match with fields in the class.
* **MaxDegreeOfParallelism**: Default 1, useful when you need retrieve high volume of data and maintain original order. Depends on amount of logical processors you have
* **ConnectionTimeout** : Default 0, time out for database request.

