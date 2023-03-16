# CsvExport
A very simple CSV-export tool for C#, code ispired by a thread at Stackoverflow, (C) Chris Hulbert

This was previously published as a "Gist" but I moved it here, for easier forking/contributing.

[![.NET](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jitbit/CsvExport/actions/workflows/dotnet.yml)

## Features

1. Excel-compatible export (separator detected automatically, friendly-trimming rows and values for compatibility)
2. Escapes commas, quotes, multiline text
3. Exports dates in timezone-proof format
4. Extremely easy to use
5. NET Standard 2.0 library (compatible with both .NET Core and .NET Framework)

### Usage example:

Install via Nuget `Install-Package CsvExport` then:

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
return File(myExport.ExportToBytes(), "text/csv", "results.csv");
```

### License

The code is licensed under *MIT License*.

Sucessfully tested for years in production with our [Jitbit Helpdesk Ticketing System](https://www.jitbit.com/helpdesk/)
