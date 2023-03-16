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

			Assert.IsTrue(csv.Trim() ==
				@"sep=,
Region,Sales,Date Opened
""Los Angeles, USA"",100000,2003-12-31
""Canberra """"in"""" Australia"",50000,2005-01-01 09:30:00");

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

			Assert.IsTrue(csv.Trim() ==
				@"sep=,
Region,Sales,Date Opened
""Los Angeles, USA"",100000
""Canberra """"in"""" Australia"",,2005-01-01 09:30:00");
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

			Assert.IsTrue(csv.Trim() ==
				@"sep=,
Region,Sales,Date Opened
""Los Angeles, USA"",100000
,,2005-01-01 09:30:00
""Los Angeles, USA"",100000,2005-01-01 09:30:00");
		}

		[TestMethod]
		public void TestGeneric()
		{
			List<MyClass> list = new() { new MyClass { Id = 123, Name = "Ffff" }, new MyClass { Id = 321, Name = "ddd" } };

			var myExport = new CsvExport();
			myExport.AddRows(list);

			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() ==
				@"sep=,
Id,Name
123,Ffff
321,ddd");
		}
	}

	public class MyClass
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}