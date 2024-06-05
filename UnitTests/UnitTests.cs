using Csv;
using System.Text;

namespace UnitTests
{
	[TestClass]
	public class UnitTests
	{
		[TestMethod]
		public void TestMain()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Region"] = "Los Angeles, USA";
			myExport["Sales"] = 100000;
			myExport["Date Opened"] = new DateTime(2003, 12, 31);

			myExport.AddRow();
			myExport["Region"] = "Canberra \"in\" Australia";
			myExport["Sales"] = 50000;
			myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() == "sep=,\r\nRegion,Sales,Date Opened\r\n\"Los Angeles, USA\",100000,2003-12-31\r\n\"Canberra \"\"in\"\" Australia\",50000,2005-01-01 09:30:00", csv);

			//export to bytes now
			string csv2 = Encoding.UTF8.GetString(myExport.ExportToBytes())
				.Trim(new char[] { '\uFEFF' }); //remove the BOM character

			Assert.IsTrue(csv.Trim() == csv2.Trim());
		}

		[TestMethod]
		public void TestMissingColumns()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Region"] = "Los Angeles, USA";
			myExport["Sales"] = 100000;

			myExport.AddRow();
			myExport["Region"] = "Canberra \"in\" Australia";
			myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() == "sep=,\r\nRegion,Sales,Date Opened\r\n\"Los Angeles, USA\",100000\r\n\"Canberra \"\"in\"\" Australia\",,2005-01-01 09:30:00", csv);
		}

		[TestMethod]
		public void TestMissingColumns2()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Region"] = "Los Angeles, USA";
			myExport["Sales"] = 100000;

			myExport.AddRow();
			myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

			myExport.AddRow();
			myExport["Sales"] = 100000;
			myExport["Region"] = "Los Angeles, USA";
			myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() == "sep=,\r\nRegion,Sales,Date Opened\r\n\"Los Angeles, USA\",100000\r\n,,2005-01-01 09:30:00\r\n\"Los Angeles, USA\",100000,2005-01-01 09:30:00", csv);
		}

		[TestMethod]
		public void TestGeneric()
		{
			List<MyClass> list = new() { new MyClass { Id = 123, Name = "Ffff" }, new MyClass { Id = 321, Name = "ddd" } };

			var myExport = new CsvExport();
			myExport.AddRows(list);

			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() == "sep=,\r\nId,Name\r\n123,Ffff\r\n321,ddd", csv);
		}

		[TestMethod]
		public void WriteToFile()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Region"] = "Los Angeles, USA";
			myExport["Sales"] = 100000;
			myExport["Date Opened"] = new DateTime(2003, 12, 31);

			myExport.AddRow();
			myExport["Region"] = "Canberra \"in\" Australia";
			myExport["Sales"] = 50000;
			myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);

			var filePath = Path.GetTempFileName();
			myExport.ExportToFile(filePath);

			Assert.IsTrue(File.Exists(filePath));
			Assert.IsTrue(File.ReadAllText(filePath).Trim() == "sep=,\r\nRegion,Sales,Date Opened\r\n\"Los Angeles, USA\",100000,2003-12-31\r\n\"Canberra \"\"in\"\" Australia\",50000,2005-01-01 09:30:00", File.ReadAllText(filePath));

			File.Delete(filePath);
		}
	}

	public class MyClass
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}