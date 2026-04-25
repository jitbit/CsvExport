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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

		// cache common search values for the two most common separators to avoid SearchValues.Create (expensive)
		// slices half-microsecond from benchmarks
		private static readonly SearchValues<char> _commaSearchValues = SearchValues.Create(",\n\r\"");
		private static readonly SearchValues<char> _semicolonSearchValues = SearchValues.Create(";\n\r\"");

		private readonly SearchValues<char> _searchValues;

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
			_searchValues = GetSearchValues(columnSeparator);
		}

		private static SearchValues<char> GetSearchValues(char columnSeparator) =>
			columnSeparator switch
			{
				',' => _commaSearchValues,
				';' => _semicolonSearchValues,
				_ => SearchValues.Create($"{columnSeparator}\n\r\"")
			};

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

			var accessors = ReflectionCache<T>.Accessors;
			do
			{
				AddRow();
				foreach (var a in accessors)
				{
					this[a.Name] = a.Getter(e.Current);
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
		/// Interface for abstracting write operations to different output targets.
		/// Implemented by readonly structs and consumed through a generic constraint so the JIT
		/// specializes and inlines each Write call (no virtual dispatch on the hot path).
		/// </summary>
		private interface ICsvWriter
		{
			void Write(char value);
			void Write(ReadOnlySpan<char> value);
		}

		private readonly struct StringBuilderCsvWriter : ICsvWriter
		{
			private readonly StringBuilder _sb;
			public StringBuilderCsvWriter(StringBuilder sb) => _sb = sb;
			public void Write(char value) => _sb.Append(value);
			public void Write(ReadOnlySpan<char> value) => _sb.Append(value);
		}

		private readonly struct StreamWriterCsvWriter : ICsvWriter
		{
			private readonly StreamWriter _sw;
			public StreamWriterCsvWriter(StreamWriter sw) => _sw = sw;
			public void Write(char value) => _sw.Write(value);
			public void Write(ReadOnlySpan<char> value) => _sw.Write(value);
		}

		/// <summary>
		/// Writes all values of a line (separated by the column separator) to an ICsvWriter, each converted to CSV-friendly format.
		/// </summary>
		private void WriteCsvFriendlyValues<TWriter>(IEnumerable<object> line, TWriter writer) where TWriter : struct, ICsvWriter
		{
			Span<char> buffer = stackalloc char[128];
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

				// Format into a stack buffer when possible to avoid allocating a string per cell.
				scoped ReadOnlySpan<char> span;
				string fallback = null;

				if (value is DateTime date)
				{
					var format = date.TimeOfDay.TotalSeconds == 0 ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss";
					if (date.TryFormat(buffer, out int written, format, CultureInfo.InvariantCulture))
						span = buffer[..written];
					else
					{
						fallback = date.ToString(format, CultureInfo.InvariantCulture);
						span = fallback.AsSpan();
					}
				}
				else if (value is ISpanFormattable sf && sf.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture))
				{
					span = (ReadOnlySpan<char>)buffer[..written];
				}
				else
				{
					fallback = (value is IFormattable f
						? f.ToString(null, CultureInfo.InvariantCulture)
						: value.ToString()).Trim();
					span = fallback.AsSpan();
				}

				if (span.Length > 30000) //cropping value for stupid Excel
					span = span[..30000];

				if (span.ContainsAny(_searchValues))
				{
					writer.Write('"');
					int idx;
					while ((idx = span.IndexOf('"')) >= 0)
					{
						writer.Write(span[..idx]);
						writer.Write("\"\"");
						span = span[(idx + 1)..];
					}
					writer.Write(span);
					writer.Write('"');
				}
				else
				{
					writer.Write(span);
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
			var writer = new StringBuilderCsvWriter(sb);

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
			using var ms = new MemoryStream();
			WriteToStream(ms, encoding);
			return ms.ToArray();
		}

		[Obsolete("Use 'WriteToStream' instead")]
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
			using var sw = new StreamWriter(stream, encoding ?? _defaultEncoding, 1024, leaveOpen: true); //streamwriter writes BOM automatically if we're at the start of the stream
			var writer = new StreamWriterCsvWriter(sw);

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

		/// <summary>
		/// Async version of <see cref="WriteToStream"/>. Use this for streams that disallow
		/// sync I/O (e.g. ASP.NET Core Response.Body).
		/// Streams row-by-row — peak memory is bounded by the widest row, not the file size.
		/// The stream is not closed; the caller owns its lifetime.
		/// </summary>
		public async Task WriteToStreamAsync(Stream stream, Encoding encoding = null, CancellationToken cancellationToken = default)
		{
			// Use a MemoryStream as an intermediate buffer. Otherwise we either duplicate WriteCsvFriendlyValues as async
			// or rewrite ICsvWriter as a class with async methods which will ruin JIT inlining optimizations
			using var ms = new MemoryStream();
			using (var sw = new StreamWriter(ms, encoding ?? _defaultEncoding, 1024, leaveOpen: true)) //StreamWriter handles the BOM automatically on first write
			{
				var writer = new StreamWriterCsvWriter(sw);

				if (_includeColumnSeparatorDefinitionPreamble)
				{
					sw.Write("sep="); sw.Write(_separatorChar); sw.Write("\r\n");
				}

				foreach (var line in ExportToLines())
				{
					WriteCsvFriendlyValues(line, writer);
					sw.Write("\r\n");
					sw.Flush(); //push StreamWriter's char buffer into the local MemoryStream (sync but in-memory, so safe)

					if (ms.Length > 0)
					{
						//drain the accumulated bytes to the real destination asynchronously, then reuse the MemoryStream's buffer
						await stream.WriteAsync(ms.GetBuffer().AsMemory(0, (int)ms.Length), cancellationToken).ConfigureAwait(false);
						ms.SetLength(0);
					}
				}
			} //disposing StreamWriter flushes anything still buffered into ms — drained below

			if (ms.Length > 0)
				await stream.WriteAsync(ms.GetBuffer().AsMemory(0, (int)ms.Length), cancellationToken).ConfigureAwait(false);
		}
	}

	internal static class ReflectionCache<T>
	{
		public readonly struct Accessor
		{
			public readonly string Name;
			public readonly Func<T, object> Getter;
			public Accessor(string name, Func<T, object> getter) { Name = name; Getter = getter; }
		}

		private static Accessor[] _accessors;

		public static Accessor[] Accessors => _accessors ??= Build();

		private static Accessor[] Build()
		{
			var props = typeof(T).GetProperties();
			var result = new Accessor[props.Length];
			var param = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
			for (int i = 0; i < props.Length; i++)
			{
				var p = props[i];
				// (T x) => (object)x.Prop   — boxes value types as needed
				var body = System.Linq.Expressions.Expression.Convert(
					System.Linq.Expressions.Expression.Property(param, p),
					typeof(object));
				var getter = System.Linq.Expressions.Expression.Lambda<Func<T, object>>(body, param).Compile();
				result[i] = new Accessor(p.Name, getter);
			}
			return result;
		}
	}
}
