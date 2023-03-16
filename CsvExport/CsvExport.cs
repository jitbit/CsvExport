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
		List<List<string>> _rows = new();

		/// <summary>
		/// The current row
		/// </summary>
		List<string> _currentRow { get { return _rows[_rows.Count - 1]; } }

		/// <summary>
		/// The string used to separate columns in the output
		/// </summary>
		private readonly string _columnSeparator;

		/// <summary>
		/// Whether to include the preamble that declares which column separator is used in the output
		/// </summary>
		private readonly bool _includeColumnSeparatorDefinitionPreamble;

		/// <summary>
		/// Whether to include the header row with column names
		/// </summary>
		private readonly bool _includeHeaderRow;

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
		public CsvExport(string columnSeparator = ",", bool includeColumnSeparatorDefinitionPreamble = true, bool includeHeaderRow = true)
		{
			_columnSeparator = columnSeparator;
			_includeColumnSeparatorDefinitionPreamble = includeColumnSeparatorDefinitionPreamble;
			_includeHeaderRow = includeHeaderRow;
		}

		/// <summary>
		/// Set a value on this column
		/// </summary>
		public object this[string field]
		{
			set {
				// Keep track of the field names
				if (!_fields.TryGetValue(field, out int num)) //get the field's index
				{
					//not found - add new
					num = _fields.Count;
					_fields.Add(field, num);
				}

				while (num >= _currentRow.Count) //fill the current row with nulls until we have the right size
					_currentRow.Add(null);

				_currentRow[num] = MakeValueCsvFriendly(value); //set the value at position
			}
		}

		/// <summary>
		/// Call this before setting any fields on a row
		/// </summary>
		public void AddRow()
		{
			_rows.Add(new(_fields.Count));
		}

		/// <summary>
		/// Add a list of typed objects, maps object properties to CsvFields
		/// </summary>
		public void AddRows<T>(IEnumerable<T> list)
		{
			if (list.Any())
			{
				var values = typeof(T).GetProperties();
				foreach (T obj in list)
				{
					AddRow();
					foreach (var value in values)
					{
						this[value.Name] = value.GetValue(obj, null);
					}
				}
			}
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
		public static string MakeValueCsvFriendly(object value, string columnSeparator = ",")
		{
			if (value == null) return "";
			if (value is INullable && ((INullable)value).IsNull) return "";

			string output;
			if (value is DateTime)
			{
				if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
				{
					output = ((DateTime)value).ToString("yyyy-MM-dd");
				}
				else
				{
					output = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
				}
			}
			else
			{
				output = value.ToString().Trim();
			}

			if (output.Length > 30000) //cropping value for stupid Excel
				output = output.Substring(0, 30000);

			if (output.Contains(columnSeparator) || output.Contains("\"") || output.Contains("\n") || output.Contains("\r"))
				output = '"' + output.Replace("\"", "\"\"") + '"';

			return output;
		}

		/// <summary>
		/// Outputs all rows as a CSV, returning one "line" at a time
		/// Where "line" is a IEnumerable of string values
		/// </summary>
		private IEnumerable<IEnumerable<string>> ExportToLines()
		{
			// The header
			if (_includeHeaderRow)
				yield return _fields.OrderBy(f => f.Value).Select(f => MakeValueCsvFriendly(f.Key, _columnSeparator));

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

			if (_includeColumnSeparatorDefinitionPreamble)
				sb.AppendLine("sep=" + _columnSeparator);

			foreach (var line in ExportToLines())
			{
				foreach (var value in line)
				{
					sb.Append(value);
					sb.Append(_columnSeparator);
				}
				sb.Length = sb.Length - _columnSeparator.Length; //remove the trailing comma (shut up)
				sb.Append("\r\n");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Exports to a file
		/// </summary>
		public void ExportToFile(string path, Encoding encoding = null)
		{
			File.WriteAllBytes(path, ExportToBytes(encoding ?? _defaultEncoding));
		}

		/// <summary>
		/// Exports as raw bytes.
		/// </summary>
		public byte[] ExportToBytes(Encoding encoding = null)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				encoding = encoding ?? _defaultEncoding;
				var preamble = encoding.GetPreamble();
				ms.Write(preamble, 0, preamble.Length);


				using (var sw = new StreamWriter(ms, encoding))
				{
					if (_includeColumnSeparatorDefinitionPreamble)
						sw.WriteLine("sep=" + _columnSeparator);

					foreach (var line in ExportToLines())
					{
						int i = 0;
						foreach (var value in line)
						{
							sw.Write(value);

							if (++i != _fields.Count)
								sw.Write(_columnSeparator);
						}
						sw.Write("\r\n");
					}

					sw.Flush(); //otherwise we're risking empty stream
				}
				return ms.ToArray();
			}
		}
	}
}
