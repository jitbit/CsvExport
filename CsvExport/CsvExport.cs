/*
 CsvExport
 Very simple CSV-export tool for C#
 Repo: https://github.com/jitbit/CsvExport
 
 Usage:
	var myExport = new CsvExport();

	myExport.AddRow();
	myExport["Region"] = "Los Angeles, USA";
	myExport["Sales"] = 100000;
	myExport["Date Opened"] = new DateTime(2003, 12, 31);
		
	myExport.AddRow();
	myExport["Region"] = "Canberra \"in\" Australia";
	myExport["Sales"] = 50000;
	myExport["Date Opened"] = new DateTime(2005, 1, 1, 9, 30, 0);
	
	myExport.ExportToFile("Somefile.csv")
	
	
You can also export/compute file using any of following method:
	string myCsv = myExport.Export();
	byte[] myCsvData = myExport.ExportToBytes();
	File(myExport.ExportToBytes(), "text/csv", "results.csv");
*/

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;

namespace Csv
{
	public class CsvExport
	{
		/// <summary>
		/// To keep the list of column names with their indexes, like {"Column Name":"3"}
		/// </summary>
		Dictionary<string, int> _fields = new();

		/// <summary>
		/// The list of rows
		/// </summary>
		List<List<object>> _rows = new();

		/// <summary>
		/// The current row
		/// </summary>
		List<object> _currentRow = null;

		/// <summary>
		/// The character used to separate columns in the output
		/// </summary>
		private readonly char _separatorChar;

		/// <summary>
		/// Whether to include the preamble that declares which column separator is used in the output
		/// </summary>
		private readonly bool _includeColumnSeparatorDefinitionPreamble;

		/// <summary>
		/// Whether to include the header row with column names
		/// </summary>
		private readonly bool _includeHeaderRow;

		private SearchValues<char> _searchValues;

		/// <summary>
		/// Default encoding
		/// </summary>
		private readonly Encoding _defaultEncoding = Encoding.UTF8;

		/// <summary>
		/// Initializes a new instance of the <see cref="Jitbit.Utils.CsvExport"/> class.
		/// </summary>
		/// <param name="columnSeparator">
		/// The string used to separate columns in the output.
		/// By default this is a comma so that the generated output is a CSV file.
		/// </param>
		/// <param name="includeColumnSeparatorDefinitionPreamble">
		/// Whether to include the preamble that declares which column separator is used in the output.
		/// By default this is <c>true</c> so that Excel can open the generated CSV
		/// without asking the user to specify the delimiter used in the file.
		/// </param>
		/// <param name="includeHeaderRow">
		/// Whether to include the header row with the columns names in the export
		/// </param>
		public CsvExport(char columnSeparator = ',', bool includeColumnSeparatorDefinitionPreamble = true, bool includeHeaderRow = true)
		{
			_separatorChar = columnSeparator;
			_includeColumnSeparatorDefinitionPreamble = includeColumnSeparatorDefinitionPreamble;
			_includeHeaderRow = includeHeaderRow;
			_searchValues = SearchValues.Create($"{columnSeparator}\n\r");
		}

		/// <summary>
		/// Set a value on this column
		/// </summary>
		public object this[string field]
		{
			set
			{
				if (_currentRow is null) return; // no row has been added yet

				// Keep track of the field names
				if (!_fields.TryGetValue(field, out int num)) //get the field's index
				{
					//not found - add new
					num = _fields.Count;
					_fields.Add(field, num);
				}

				while (num >= _currentRow.Count) //fill the current row with nulls until we have the right size
					_currentRow.Add(null);

				_currentRow[num] = value; //set the raw value at position
			}
		}

		/// <summary>
		/// Call this before setting any fields on a row
		/// </summary>
		public void AddRow()
		{
			_currentRow = new(_fields.Count);
			_rows.Add(_currentRow);
		}

		/// <summary>
		/// Add a list of typed objects, maps object properties to CsvFields
		/// </summary>
		public void AddRows<T>(IEnumerable<T> list)
		{
			using var e = list.GetEnumerator();
			if (!e.MoveNext()) return; //empty - skip reflection cache warm-up

			var values = ReflectionCache<T>.Properties;
			do
			{
				AddRow();
				foreach (var value in values)
				{
					this[value.Name] = value.GetValue(e.Current, null);
				}
			} while (e.MoveNext());
		}

		/// <summary>
		/// Converts a value to how it should output in a csv file
		/// If it has a comma, it needs surrounding with double quotes
		/// Eg Sydney, Australia -> "Sydney, Australia"
		/// Also if it contains any double quotes ("), then they need to be replaced with quad quotes[sic] ("")
		/// Eg "Dangerous Dan" McGrew -> """Dangerous Dan"" McGrew"
		/// </summary>
		/// <param name="columnSeparator">
		/// The string used to separate columns in the output.
		/// By default this is a comma so that the generated output is a CSV document.
		/// </param>
		public static string MakeValueCsvFriendly(object value, char columnSeparator = ',')
		{
			var sb = new StringBuilder();
			new CsvExport(columnSeparator).WriteCsvFriendlyValues([value], new StringBuilderCsvWriter(sb));
			return sb.ToString();
		}

		/// <summary>
		/// Interface for abstracting write operations to different output targets
		/// </summary>
		private interface ICsvWriter
		{
			void Write(string value);
			void Write(char value);
		}

		/// <summary>
		/// StringBuilder wrapper for ICsvWriter
		/// </summary>
		private class StringBuilderCsvWriter : ICsvWriter
		{
			private readonly StringBuilder _sb;
			public StringBuilderCsvWriter(StringBuilder sb) => _sb = sb;
			public void Write(string value) => _sb.Append(value);
			public void Write(char value) => _sb.Append(value);
		}

		/// <summary>
		/// StreamWriter wrapper for ICsvWriter
		/// </summary>
		private class StreamWriterCsvWriter : ICsvWriter
		{
			private readonly StreamWriter _sw;
			public StreamWriterCsvWriter(StreamWriter sw) => _sw = sw;
			public void Write(string value) => _sw.Write(value);
			public void Write(char value) => _sw.Write(value);
		}

		/// <summary>
		/// Writes all values of a line (separated by the column separator) to an ICsvWriter, each converted to CSV-friendly format.
		/// </summary>
		private void WriteCsvFriendlyValues(IEnumerable<object> line, ICsvWriter writer)
		{
			bool first = true;
			foreach (var value in line)
			{
				if (!first)
				{
					writer.Write(_separatorChar);
				}
				first = false;

				if (value == null) continue;
				if (value is INullable nullable && nullable.IsNull) continue;

				if (value is DateTime date)
				{
					if (date.TimeOfDay.TotalSeconds == 0)
						writer.Write(date.ToString("yyyy-MM-dd"));
					else
						writer.Write(date.ToString("yyyy-MM-dd HH:mm:ss"));
					continue;
				}

				var output = value.ToString().Trim();

				if (output.Length > 30000) //cropping value for stupid Excel
					output = output.Substring(0, 30000);

				if (output.AsSpan().ContainsAny(_searchValues))
				{
					writer.Write('"');
					writer.Write(output.Replace("\"", "\"\""));
					writer.Write('"');
				}
				else
				{
					writer.Write(output);
				}
			}
		}

		/// <summary>
		/// Outputs all rows as a CSV, returning one "line" at a time
		/// Where "line" is a IEnumerable of object values
		/// </summary>
		private IEnumerable<IEnumerable<object>> ExportToLines()
		{
			// The header
			if (_includeHeaderRow)
				yield return _fields.OrderBy(f => f.Value).Select(f => f.Key);

			// The rows
			foreach (var row in _rows)
			{
				yield return row;
			}
		}

		/// <summary>
		/// Output all rows as a CSV returning a string
		/// </summary>
		public string Export()
		{
			StringBuilder sb = new StringBuilder();
			ICsvWriter writer = new StringBuilderCsvWriter(sb);

			if (_includeColumnSeparatorDefinitionPreamble)
				sb.Append($"sep={_separatorChar}\r\n");

			foreach (var line in ExportToLines())
			{
				WriteCsvFriendlyValues(line, writer);
				sb.Append("\r\n");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Exports directly to a file, streaming to disk without buffering the whole CSV in memory.
		/// </summary>
		public void ExportToFile(string path, Encoding encoding = null)
		{
			using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
			WriteToStream(fs, encoding);
		}

		/// <summary>
		/// Exports as raw bytes.
		/// </summary>
		public byte[] ExportToBytes(Encoding encoding = null)
		{
			using var ms = ExportAsMemoryStream(encoding);
			return ms.ToArray();
		}

		public MemoryStream ExportAsMemoryStream(Encoding encoding = null)
		{
			MemoryStream ms = new MemoryStream();
			WriteToStream(ms, encoding);
			ms.Position = 0;
			return ms;
		}

		/// <summary>
		/// Writes the CSV directly to the given stream using the specified encoding.
		/// The stream is not closed; the caller owns its lifetime.
		/// </summary>
		public void WriteToStream(Stream stream, Encoding encoding = null)
		{
			encoding = encoding ?? _defaultEncoding;
			var preamble = encoding.GetPreamble();
			if (preamble.Length > 0)
				stream.Write(preamble, 0, preamble.Length);

			using var sw = new StreamWriter(stream, encoding, 1024, leaveOpen: true);
			ICsvWriter writer = new StreamWriterCsvWriter(sw);

			if (_includeColumnSeparatorDefinitionPreamble)
			{
				sw.Write("sep="); sw.Write(_separatorChar); sw.Write("\r\n"); //just tiny way to avoid string concatenation
			}

			foreach (var line in ExportToLines())
			{
				WriteCsvFriendlyValues(line, writer);
				sw.Write("\r\n");
			}
		}
	}

	internal static class ReflectionCache<T>
	{
		private static System.Reflection.PropertyInfo[] _properties;
		public static System.Reflection.PropertyInfo[] Properties => _properties ??= typeof(T).GetProperties();
	}
}
