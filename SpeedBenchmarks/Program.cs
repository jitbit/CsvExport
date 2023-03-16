using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Csv;
using CsvHelper;
using Microsoft.Extensions.Logging;

BenchmarkRunner.Run<SpeedBenchmarks.BenchMark>();

namespace SpeedBenchmarks
{
	[ShortRunJob, MemoryDiagnoser]
	public class BenchMark
	{
		private static List<MyClass> _myClasses = new List<MyClass>();

		[GlobalSetup]
		public void Setup()
		{
			for (int i = 0; i < 100; i++)
			{
				_myClasses.Add(new MyClass { Col1 = "test1", Col2 = "test2", Col3 = "test3", Col4 = "test4" });
			}
		}

		[Benchmark]
		public void CsvHelper()
		{
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(_myClasses);
				writer.Flush();
				stream.Position = 0;
				var text = reader.ReadToEnd();
			}
		}

		[Benchmark]
		public void CsvExport_Manual()
		{
			var c = new CsvExport();
			for (int i = 0; i < 100; i++)
			{
				c.AddRow();
				c["Col1"] = "test1";
				c["Col2"] = "test2";
				c["Col3"] = "test3";
				c["Col4"] = "test4";
			}

			var r = c.Export();
		}

		[Benchmark]
		public void CsvExport_GenericType()
		{
			var c = new CsvExport();
			c.AddRows(_myClasses);

			var r = c.Export();
		}
	}

	public class MyClass
	{
		public string Col1 { get; set; }
		public string Col2 { get; set; }
		public string Col3 { get; set; }
		public string Col4 { get; set; }
	}
}