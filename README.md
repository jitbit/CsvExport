# CsvExport
A very simple CSV-export tool for C#, code ispired by a thread at Stackoverflow, (C) Chris Hulbert

This was previously published as a "Gist" but I moved it here, for easier forking/contributing.

Usage example:

	var myExport = new CsvExport();

	myExport.AddRow();
	myExport["Region"] = "Los Angeles, USA";
	myExport["Sales"] = 100000;
	myExport["Date Opened"] = new DateTime(2003, 12, 31);
		
	myExport.AddRow();
	myExport["Region"] = "Canberra \"in\" Australia";
	myExport["Sales"] = 50000;
	myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);
