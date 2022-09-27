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

namespace Jitbit.Utils
{
	public class CsvExport
	{
		/// <summary>
		/// To keep the ordered list of column names
		/// </summary>
		List<string> _fields = new List<string>();

		/// <summary>
		/// The list of rows
		/// </summary>
		List<Dictionary<string, object>> _rows = new List<Dictionary<string, object>>();

		/// <summary>
		/// The current row
		/// </summary>
		Dictionary<string, object> _currentRow { get { return _rows[_rows.Count - 1]; } }

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
			set
			{
				// Keep track of the field names, because the dictionary loses the ordering
				if (!_fields.Contains(field)) _fields.Add(field);
				_currentRow[field] = value;
			}
		}

		/// <summary>
		/// Call this before setting any fields on a row
		/// </summary>
		public void AddRow()
		{
			_rows.Add(new Dictionary<string, object>());
		}

		/// <summary>
		/// Add a list of typed objects, maps object properties to CsvFields
		/// </summary>
		public void AddRows<T>(IEnumerable<T> list)
		{
			if (list.Any())
			{
				foreach (var obj in list)
				{
					AddRow();
					var values = obj.GetType().GetProperties();
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
		public static string MakeValueCsvFriendly(object value, string columnSeparator=",")
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
				yield return _fields.Select(f => MakeValueCsvFriendly(f, _columnSeparator));

			// The rows
			foreach (Dictionary<string, object> row in _rows)
			{
				yield return _fields
					.Select(field => row.TryGetValue(field, out object value) ? MakeValueCsvFriendly(value) : "");
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
		public void ExportToFile(string path)
		{
			File.WriteAllBytes(path, ExportToBytes());
		}

		/// <summary>
		/// Exports as raw Unicode bytes. We use Unicode to make it Excel compatible.
		/// </summary>
		public byte[] ExportToBytes()
		{
			var data = Encoding.Unicode.GetBytes(Export());
			return Encoding.Unicode.GetPreamble().Concat(data).ToArray();
		}
	}
}