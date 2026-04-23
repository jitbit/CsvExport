# CsvExport
A very simple and very fast CSV-export tool for C#.

[![.NET](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml)

## V3 Breaking changes:

- .NET 8 targeting (use v2 for .NET Framework, we'll backport critical fixes)
- Uses `char` instead of `string` for column separator

## Features

1. Excel-compatible export (separator detected automatically, friendly-trimming rows and values for compatibility)
2. Escapes commas, quotes, multiline text
3. Exports dates in timezone-proof format
4. Extremely easy to use
5. 30 times faster than CsvHelper
6. 4-times less memory usage

## Benchmarks

|                   Method |      Mean |      Error |    StdDev |   Gen0 |   Gen1 | Allocated |
|------------------------- |----------:|-----------:|----------:|-------:|-------:|----------:|
| 😟             CsvHelper | 372.90 us | 390.842 us | 21.423 us | 9.7656 | 4.8828 |   85.4 KB |
| ✅      CsvExport_Manual |  12.71 us |   1.040 us |  0.057 us | 3.5858 | 0.1984 |  29.35 KB |
| ✅ CsvExport_GenericType |  13.22 us |   1.240 us |  0.068 us | 3.5858 | 0.2289 |  29.39 KB |

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

Also, methods `ExportToFile` and `WriteToStream` and `ExportToBytes` offer an optional encoding parameter.

### Using with ASP.NET Core:

For big CSV files (megabytes) use `WriteToStreamAsync` and write to `Response.Body` directly. This is very important to save memory usage. Here's a handy example:

```c#
public class CsvExportResult(Csv.CsvExport csv, string fileName) : ActionResult
{
	public override Task ExecuteResultAsync(ActionContext ctx)
	{
		var res = ctx.HttpContext.Response;
		res.ContentType = "text/csv";
		res.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
		return csv.WriteToStreamAsync(res.Body, cancellationToken: ctx.HttpContext.RequestAborted);
	}
}
```

Use like this: `return new CsvExportResult(csvExport, "filename.csv");`

### License

The code is licensed under *[MIT License](/LICENSE)*.

Sucessfully tested for years in production with our [Jitbit Helpdesk Ticketing System](https://www.jitbit.com/helpdesk/)
