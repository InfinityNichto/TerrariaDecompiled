using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.Diagnostics;

public class XmlWriterTraceListener : TextWriterTraceListener
{
	private static volatile string s_processName;

	private readonly string _machineName = Environment.MachineName;

	private StringBuilder _strBldr;

	private XmlTextWriter _xmlBlobWriter;

	public XmlWriterTraceListener(Stream stream)
		: base(stream)
	{
	}

	public XmlWriterTraceListener(Stream stream, string? name)
		: base(stream, name)
	{
	}

	public XmlWriterTraceListener(TextWriter writer)
		: base(writer)
	{
	}

	public XmlWriterTraceListener(TextWriter writer, string? name)
		: base(writer, name)
	{
	}

	public XmlWriterTraceListener(string? filename)
		: base(filename)
	{
	}

	public XmlWriterTraceListener(string? filename, string? name)
		: base(filename, name)
	{
	}

	public override void Write(string? message)
	{
		WriteLine(message);
	}

	public override void WriteLine(string? message)
	{
		TraceEvent(null, System.SR.TraceAsTraceSource, TraceEventType.Information, 0, message);
	}

	public override void Fail(string? message, string? detailMessage)
	{
		if (message == null)
		{
			message = string.Empty;
		}
		int length = ((detailMessage != null) ? (message.Length + 1 + detailMessage.Length) : message.Length);
		TraceEvent(null, System.SR.TraceAsTraceSource, TraceEventType.Error, 0, string.Create(length, (message, detailMessage), delegate(Span<char> dst, (string message, string detailMessage) v)
		{
			var (text, _) = v;
			text.CopyTo(dst);
			if (v.detailMessage != null)
			{
				dst[text.Length] = ' ';
				string item = v.detailMessage;
				item.CopyTo(dst.Slice(text.Length + 1, item.Length));
			}
		}));
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			WriteEscaped((args != null && args.Length != 0) ? string.Format(CultureInfo.InvariantCulture, format, args) : format);
			WriteFooter(eventCache);
		}
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			WriteEscaped(message);
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			InternalWrite("<TraceData>");
			if (data != null)
			{
				InternalWrite("<DataItem>");
				WriteData(data);
				InternalWrite("</DataItem>");
			}
			InternalWrite("</TraceData>");
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
	{
		if (base.Filter != null && !base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
		{
			return;
		}
		WriteHeader(source, eventType, id, eventCache);
		InternalWrite("<TraceData>");
		if (data != null)
		{
			for (int i = 0; i < data.Length; i++)
			{
				InternalWrite("<DataItem>");
				if (data[i] != null)
				{
					WriteData(data[i]);
				}
				InternalWrite("</DataItem>");
			}
		}
		InternalWrite("</TraceData>");
		WriteFooter(eventCache);
	}

	private void WriteData(object data)
	{
		if (!(data is XPathNavigator xPathNavigator))
		{
			WriteEscaped(data.ToString());
			return;
		}
		if (_strBldr == null)
		{
			_strBldr = new StringBuilder();
			_xmlBlobWriter = new XmlTextWriter(new StringWriter(_strBldr, CultureInfo.CurrentCulture));
		}
		else
		{
			_strBldr.Length = 0;
		}
		try
		{
			xPathNavigator.MoveToRoot();
			_xmlBlobWriter.WriteNode(xPathNavigator, defattr: false);
			InternalWrite(_strBldr.ToString());
		}
		catch (Exception)
		{
			InternalWrite(data.ToString());
		}
	}

	public override void Close()
	{
		base.Close();
		_xmlBlobWriter?.Close();
		_xmlBlobWriter = null;
		_strBldr = null;
	}

	public override void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, TraceEventType.Transfer, id, message, null, null, null))
		{
			WriteHeader(source, TraceEventType.Transfer, id, eventCache, relatedActivityId);
			WriteEscaped(message);
			WriteFooter(eventCache);
		}
	}

	private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache, Guid relatedActivityId)
	{
		WriteStartHeader(source, eventType, id, eventCache);
		InternalWrite("\" RelatedActivityID=\"");
		InternalWrite(relatedActivityId.ToString("B"));
		WriteEndHeader();
	}

	private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
	{
		WriteStartHeader(source, eventType, id, eventCache);
		WriteEndHeader();
	}

	private void WriteStartHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
	{
		InternalWrite("<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\">");
		InternalWrite("<EventID>");
		uint num = (uint)id;
		InternalWrite(num.ToString(CultureInfo.InvariantCulture));
		InternalWrite("</EventID>");
		InternalWrite("<Type>3</Type>");
		InternalWrite("<SubType Name=\"");
		InternalWrite(eventType.ToString());
		InternalWrite("\">0</SubType>");
		InternalWrite("<Level>");
		int num2 = (int)eventType;
		if (num2 > 255)
		{
			num2 = 255;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		InternalWrite(num2.ToString(CultureInfo.InvariantCulture));
		InternalWrite("</Level>");
		InternalWrite("<TimeCreated SystemTime=\"");
		if (eventCache != null)
		{
			InternalWrite(eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));
		}
		else
		{
			InternalWrite(DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
		}
		InternalWrite("\" />");
		InternalWrite("<Source Name=\"");
		WriteEscaped(source);
		InternalWrite("\" />");
		InternalWrite("<Correlation ActivityID=\"");
		if (eventCache != null)
		{
			InternalWrite(Trace.CorrelationManager.ActivityId.ToString("B"));
		}
		else
		{
			InternalWrite(Guid.Empty.ToString("B"));
		}
	}

	private void WriteEndHeader()
	{
		string text = s_processName;
		if (text == null)
		{
			if (OperatingSystem.IsBrowser())
			{
				text = (s_processName = string.Empty);
			}
			else
			{
				using Process process = Process.GetCurrentProcess();
				text = (s_processName = process.ProcessName);
			}
		}
		InternalWrite("\" />");
		InternalWrite("<Execution ProcessName=\"");
		InternalWrite(text);
		InternalWrite("\" ProcessID=\"");
		InternalWrite(((uint)Environment.ProcessId).ToString(CultureInfo.InvariantCulture));
		InternalWrite("\" ThreadID=\"");
		WriteEscaped(Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture));
		InternalWrite("\" />");
		InternalWrite("<Channel/>");
		InternalWrite("<Computer>");
		InternalWrite(_machineName);
		InternalWrite("</Computer>");
		InternalWrite("</System>");
		InternalWrite("<ApplicationData>");
	}

	private void WriteFooter(TraceEventCache eventCache)
	{
		bool flag = IsEnabled(TraceOptions.LogicalOperationStack);
		bool flag2 = IsEnabled(TraceOptions.Callstack);
		if (eventCache != null && (flag || flag2))
		{
			InternalWrite("<System.Diagnostics xmlns=\"http://schemas.microsoft.com/2004/08/System.Diagnostics\">");
			if (flag)
			{
				InternalWrite("<LogicalOperationStack>");
				Stack logicalOperationStack = eventCache.LogicalOperationStack;
				foreach (object item in logicalOperationStack)
				{
					InternalWrite("<LogicalOperation>");
					WriteEscaped(item?.ToString());
					InternalWrite("</LogicalOperation>");
				}
				InternalWrite("</LogicalOperationStack>");
			}
			InternalWrite("<Timestamp>");
			InternalWrite(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture));
			InternalWrite("</Timestamp>");
			if (flag2)
			{
				InternalWrite("<Callstack>");
				WriteEscaped(eventCache.Callstack);
				InternalWrite("</Callstack>");
			}
			InternalWrite("</System.Diagnostics>");
		}
		InternalWrite("</ApplicationData></E2ETraceEvent>");
	}

	private void WriteEscaped(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < str.Length; i++)
		{
			switch (str[i])
			{
			case '&':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&amp;");
				num = i + 1;
				break;
			case '<':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&lt;");
				num = i + 1;
				break;
			case '>':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&gt;");
				num = i + 1;
				break;
			case '"':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&quot;");
				num = i + 1;
				break;
			case '\'':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&apos;");
				num = i + 1;
				break;
			case '\r':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&#xD;");
				num = i + 1;
				break;
			case '\n':
				InternalWrite(str.Substring(num, i - num));
				InternalWrite("&#xA;");
				num = i + 1;
				break;
			}
		}
		InternalWrite(str.Substring(num, str.Length - num));
	}

	private void InternalWrite(string message)
	{
		EnsureWriter();
		_writer?.Write(message);
	}
}
