# CsvExport
A very simple and very fast CSV-export tool for C#.

[![.NET](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml)

## Features

1. Excel-compatible export (separator detected automatically, friendly-trimming rows and values for compatibility)
2. Escapes commas, quotes, multiline text
3. Exports dates in timezone-proof format
4. Extremely easy to use
5. NET Standard 2.0 library (compatible with both .NET Core and .NET Framework)
6. 30 times faster than CsvHelper
7. 4-times less memory usage

## Benchmarks

|            Method |        Mean |     Error |   StdDev |    Gen0 |   Gen1 | Allocated |
|------------------ |------------:|----------:|---------:|--------:|-------:|----------:|
| 😟        CsvHelper | 1,300.38 us | 32.043 us | 1.756 us | 17.5781 | 7.8125 | 114.25 KB |
| ✅ CsvExport_Manual |    31.22 us |  5.750 us | 0.315 us |  4.7607 | 0.2441 |  29.37 KB |
| ✅  CsvExport_Typed |    52.68 us |  1.453 us | 0.080 us |  4.7607 | 0.1221 |  29.46 KB |

This benchmark is generating a 100-line CSV file with 4 columns. Check the "SpeedBenchmarks" code.

## Usage examples:

Install via Nuget `Install-Package CsvExport`

For "manual" CSV ad-hoc generation use this:

```c#
var myExport = new CsvExport();

myExport.AddRow();
myExport["Region"] = "Los Angeles, USA";
myExport["Sales"] = 100000;
myExport["Date Opened"] = new DateTime(2003, 12, 31);

myExport.AddRow();
myExport["Region"] = "Canberra \"in\" Australia";
myExport["Sales"] = 50000;
myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

///ASP.NET MVC action example
return File(myExport.ExportAsMemoryStream(), "text/csv", "results.csv");
```

For generating CSV out of a typed `List<T>` of objects:

```c#

public class Foo
{
	public string Region { get; set; }
	public int Sales { get; set; }
	public DateTime DateOpened { get; set; }
}

var list = new List<Foo>
{
	new Foo { Region = "Los Angeles", Sales = 123321, DateOpened = DateTime.Now },
	new Foo { Region = "Canberra in Australia", Sales = 123321, DateOpened = DateTime.Now },
};

var myExport = new CsvExport();
myExport.AddRows(list);
string csv = myExport.Export();
```
Configuring is done via constructor parameters:

```c#
var myExport = new CsvExport(
	columnSeparator: ",",
	includeColumnSeparatorDefinitionPreamble: true, //Excel wants this in CSV files
	includeHeaderRow: true
);
```

Also, methods `ExportToFile` and `ExportAsMemoryStream` and `ExportToBytes` offer an optional encoding parameter.

### License

The code is licensed under *MIT License*.

Sucessfully tested for years in production with our [Jitbit Helpdesk Ticketing System](https://www.jitbit.com/helpdesk/)
