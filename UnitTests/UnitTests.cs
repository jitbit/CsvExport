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
		public void TestEmpty()
		{
			var myExport = new CsvExport();
			string csv = myExport.Export();

			Assert.IsTrue(csv.Trim() == "sep=,", csv);
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

		[TestMethod]
		public void TestConstructorParameters()
		{
			// Test custom separator
			var export1 = new CsvExport(";");
			export1.AddRow();
			export1["Name"] = "John";
			export1["Value"] = "Test;Data";
			string result1 = export1.Export();
			Assert.IsTrue(result1.Contains("sep=;"));
			Assert.IsTrue(result1.Contains("Name;Value"));
			Assert.IsTrue(result1.Contains("\"Test;Data\""));

			// Test no preamble
			var export2 = new CsvExport(",", false);
			export2.AddRow();
			export2["Name"] = "John";
			string result2 = export2.Export();
			Assert.IsFalse(result2.Contains("sep="));
			Assert.IsTrue(result2.StartsWith("Name\r\n"));

			// Test no header
			var export3 = new CsvExport(",", true, false);
			export3.AddRow();
			export3["Name"] = "John";
			string result3 = export3.Export();
			Assert.IsTrue(result3.Contains("sep="));
			Assert.IsFalse(result3.Contains("Name\r\n"));
			Assert.IsTrue(result3.Contains("John"));
		}

		[TestMethod]
		public void TestNullValues()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Name"] = "John";
			myExport["Value"] = null;
			myExport["Number"] = 123;

			string csv = myExport.Export();
			Assert.IsTrue(csv.Contains("John,,123"));
		}

		[TestMethod]
		public void TestSpecialCharacters()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Newline"] = "Line1\nLine2";
			myExport["CarriageReturn"] = "Text1\rText2";
			myExport["Both"] = "Start\r\nEnd";

			string csv = myExport.Export();
			Assert.IsTrue(csv.Contains("\"Line1\nLine2\""));
			Assert.IsTrue(csv.Contains("\"Text1\rText2\""));
			Assert.IsTrue(csv.Contains("\"Start\r\nEnd\""));
		}

		[TestMethod]
		public void TestLargeDataTruncation()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			// Create a string longer than 30000 characters
			string largeText = new string('A', 35000);
			myExport["LargeText"] = largeText;

			string csv = myExport.Export();
			// Should be truncated to 30000 characters
			Assert.IsTrue(csv.Contains(new string('A', 30000)));
			Assert.IsFalse(csv.Contains(new string('A', 30001)));
		}

		[TestMethod]
		public void TestSetValueWithoutAddRow()
		{
			var myExport = new CsvExport();
			// Setting value without calling AddRow first should be ignored
			myExport["Name"] = "Should be ignored";

			myExport.AddRow();
			myExport["Name"] = "John";

			string csv = myExport.Export();
			Assert.IsFalse(csv.Contains("Should be ignored"));
			Assert.IsTrue(csv.Contains("John"));
		}

		[TestMethod]
		public void TestDifferentDataTypes()
		{
			var myExport = new CsvExport();
			myExport.AddRow();
			myExport["Bool"] = true;
			myExport["Decimal"] = 123.45m;
			myExport["Double"] = 67.89;
			myExport["Float"] = 12.34f;
			myExport["Long"] = 9876543210L;

			string csv = myExport.Export();
			Assert.IsTrue(csv.Contains("True"));
			Assert.IsTrue(csv.Contains("123.45"));
			Assert.IsTrue(csv.Contains("67.89"));
			Assert.IsTrue(csv.Contains("12.34"));
			Assert.IsTrue(csv.Contains("9876543210"));
		}

		[TestMethod]
		public void TestEmptyGenericCollection()
		{
			List<MyClass> emptyList = new();
			var myExport = new CsvExport();
			myExport.AddRows(emptyList);

			string csv = myExport.Export();
			Assert.AreEqual("sep=,", csv.Trim());
		}
	}

	public class MyClass
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}