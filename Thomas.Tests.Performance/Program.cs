using System;
using BenchmarkDotNet.Running;
using Thomas.Tests.Performance.Benchmark;

namespace Thomas.Tests.Performance
{
    class Program 
    {
        static void Main(string[] args)
        {

            new BenchmarkSwitcher(typeof(ThomasDataAdapterBenckmark).Assembly).Run(args, new BenchmarkConfig());

            //var a = new Setup();
            //a.Start();

            //var result = a.service.ToList<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {a.TableName} WHERE Id = 1", false);
            //System.Console.WriteLine(result.ErrorMessage);
        }

    }
}
