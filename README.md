# CsvExport
A very simple CSV-export tool for C#, code ispired by a thread at Stackoverflow, (C) Chris Hulbert

This was previously published as a "Gist" but I moved it here, for easier forking/contributing.

## Features

1. Excel-compatible export (separator detected automatically, friendly-trimming rows and values for compatibility)
2. Escapes commas, quotes, multiline text
3. Exports dates in timezone-proof format
4. Extremely easy to use

### Usage example:

Simply include one C# file into your project. Then:

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

# (NEW!) Nuget

I've published this to Nuget.

`Install-Package CsvExport`

This will simply add the cs-file to the root of your project.

### License

The code is licensed under *MIT License*.


Sucessfully tested in production with our [Jitbit Helpdesk Ticketing System](https://jitbit.github.com/helpdesk/)
