using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Diagnostics;

public class DelimitedListTraceListener : TextWriterTraceListener
{
	private string _delimiter = ";";

	private string _secondaryDelim = ",";

	private bool _initializedDelim;

	public string Delimiter
	{
		get
		{
			lock (this)
			{
				if (!_initializedDelim)
				{
					if (base.Attributes.ContainsKey("delimiter"))
					{
						string text = base.Attributes["delimiter"];
						if (!string.IsNullOrEmpty(text))
						{
							_delimiter = text;
						}
					}
					_initializedDelim = true;
				}
			}
			return _delimiter;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("Delimiter");
			}
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Generic_ArgCantBeEmptyString, "Delimiter"));
			}
			lock (this)
			{
				_delimiter = value;
				_initializedDelim = true;
			}
			if (_delimiter == ",")
			{
				_secondaryDelim = ";";
			}
			else
			{
				_secondaryDelim = ",";
			}
		}
	}

	public DelimitedListTraceListener(Stream stream)
		: base(stream)
	{
	}

	public DelimitedListTraceListener(Stream stream, string? name)
		: base(stream, name)
	{
	}

	public DelimitedListTraceListener(TextWriter writer)
		: base(writer)
	{
	}

	public DelimitedListTraceListener(TextWriter writer, string? name)
		: base(writer, name)
	{
	}

	public DelimitedListTraceListener(string? fileName)
		: base(fileName)
	{
	}

	public DelimitedListTraceListener(string? fileName, string? name)
		: base(fileName, name)
	{
	}

	protected override string[] GetSupportedAttributes()
	{
		return new string[1] { "delimiter" };
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
		{
			WriteHeader(source, eventType, id);
			if (args != null)
			{
				WriteEscaped(string.Format(CultureInfo.InvariantCulture, format, args));
			}
			else
			{
				WriteEscaped(format);
			}
			Write(Delimiter);
			Write(Delimiter);
			WriteFooter(eventCache);
		}
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
		{
			WriteHeader(source, eventType, id);
			WriteEscaped(message);
			Write(Delimiter);
			Write(Delimiter);
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
		{
			WriteHeader(source, eventType, id);
			Write(Delimiter);
			WriteEscaped(data?.ToString());
			Write(Delimiter);
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
	{
		if (base.Filter != null && !base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
		{
			return;
		}
		WriteHeader(source, eventType, id);
		Write(Delimiter);
		if (data != null)
		{
			for (int i = 0; i < data.Length; i++)
			{
				if (i != 0)
				{
					Write(_secondaryDelim);
				}
				WriteEscaped(data[i]?.ToString());
			}
		}
		Write(Delimiter);
		WriteFooter(eventCache);
	}

	private void WriteHeader(string source, TraceEventType eventType, int id)
	{
		WriteEscaped(source);
		Write(Delimiter);
		Write(eventType.ToString());
		Write(Delimiter);
		Write(id.ToString(CultureInfo.InvariantCulture));
		Write(Delimiter);
	}

	private void WriteFooter(TraceEventCache eventCache)
	{
		if (eventCache != null)
		{
			if (IsEnabled(TraceOptions.ProcessId))
			{
				Write(eventCache.ProcessId.ToString(CultureInfo.InvariantCulture));
			}
			Write(Delimiter);
			if (IsEnabled(TraceOptions.LogicalOperationStack))
			{
				WriteStackEscaped(eventCache.LogicalOperationStack);
			}
			Write(Delimiter);
			if (IsEnabled(TraceOptions.ThreadId))
			{
				WriteEscaped(eventCache.ThreadId);
			}
			Write(Delimiter);
			if (IsEnabled(TraceOptions.DateTime))
			{
				WriteEscaped(eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));
			}
			Write(Delimiter);
			if (IsEnabled(TraceOptions.Timestamp))
			{
				Write(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture));
			}
			Write(Delimiter);
			if (IsEnabled(TraceOptions.Callstack))
			{
				WriteEscaped(eventCache.Callstack);
			}
		}
		else
		{
			for (int i = 0; i < 5; i++)
			{
				Write(Delimiter);
			}
		}
		WriteLine("");
	}

	private void WriteEscaped(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			StringBuilder stringBuilder = new StringBuilder("\"");
			EscapeMessage(message, stringBuilder);
			stringBuilder.Append('"');
			Write(stringBuilder.ToString());
		}
	}

	private void WriteStackEscaped(Stack stack)
	{
		StringBuilder stringBuilder = new StringBuilder("\"");
		bool flag = true;
		foreach (object item in stack)
		{
			if (!flag)
			{
				stringBuilder.Append(", ");
			}
			else
			{
				flag = false;
			}
			string message = item?.ToString();
			EscapeMessage(message, stringBuilder);
		}
		stringBuilder.Append('"');
		Write(stringBuilder.ToString());
	}

	private void EscapeMessage(string message, StringBuilder sb)
	{
		if (!string.IsNullOrEmpty(message))
		{
			int num = 0;
			int num2;
			while ((num2 = message.IndexOf('"', num)) != -1)
			{
				sb.Append(message, num, num2 - num);
				sb.Append("\"\"");
				num = num2 + 1;
			}
			sb.Append(message, num, message.Length - num);
		}
	}
}
