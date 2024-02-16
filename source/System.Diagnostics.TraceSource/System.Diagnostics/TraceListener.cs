using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

public abstract class TraceListener : MarshalByRefObject, IDisposable
{
	private int _indentLevel;

	private int _indentSize = 4;

	private TraceOptions _traceOptions;

	private bool _needIndent = true;

	private StringDictionary _attributes;

	private string _listenerName;

	private TraceFilter _filter;

	public StringDictionary Attributes
	{
		get
		{
			if (_attributes == null)
			{
				_attributes = new StringDictionary();
			}
			return _attributes;
		}
	}

	public virtual string Name
	{
		get
		{
			return _listenerName ?? "";
		}
		[param: AllowNull]
		set
		{
			_listenerName = value;
		}
	}

	public virtual bool IsThreadSafe => false;

	public int IndentLevel
	{
		get
		{
			return _indentLevel;
		}
		set
		{
			_indentLevel = ((value >= 0) ? value : 0);
		}
	}

	public int IndentSize
	{
		get
		{
			return _indentSize;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("IndentSize", value, System.SR.TraceListenerIndentSize);
			}
			_indentSize = value;
		}
	}

	public TraceFilter? Filter
	{
		get
		{
			return _filter;
		}
		set
		{
			_filter = value;
		}
	}

	protected bool NeedIndent
	{
		get
		{
			return _needIndent;
		}
		set
		{
			_needIndent = value;
		}
	}

	public TraceOptions TraceOutputOptions
	{
		get
		{
			return _traceOptions;
		}
		set
		{
			if ((int)value >> 6 != 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_traceOptions = value;
		}
	}

	protected TraceListener()
	{
	}

	protected TraceListener(string? name)
	{
		_listenerName = name;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public virtual void Flush()
	{
	}

	public virtual void Close()
	{
	}

	protected internal virtual string[]? GetSupportedAttributes()
	{
		return null;
	}

	public virtual void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
	{
		IFormatProvider formatProvider = null;
		IFormatProvider provider = formatProvider;
		Span<char> initialBuffer = stackalloc char[256];
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(20, 2, formatProvider, initialBuffer);
		handler.AppendFormatted(message);
		handler.AppendLiteral(", relatedActivityId=");
		handler.AppendFormatted(relatedActivityId);
		TraceEvent(eventCache, source, TraceEventType.Transfer, id, string.Create(provider, initialBuffer, ref handler));
	}

	public virtual void Fail(string? message)
	{
		Fail(message, null);
	}

	public virtual void Fail(string? message, string? detailMessage)
	{
		WriteLine((detailMessage == null) ? (System.SR.TraceListenerFail + " " + message) : $"{System.SR.TraceListenerFail} {message} {detailMessage}");
	}

	public abstract void Write(string? message);

	public virtual void Write(object? o)
	{
		if ((Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, null, null, o)) && o != null)
		{
			Write(o.ToString());
		}
	}

	public virtual void Write(string? message, string? category)
	{
		if (Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message))
		{
			if (category == null)
			{
				Write(message);
			}
			else
			{
				Write(category + ": " + ((message == null) ? string.Empty : message));
			}
		}
	}

	public virtual void Write(object? o, string? category)
	{
		if (Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, category, null, o))
		{
			if (category == null)
			{
				Write(o);
			}
			else
			{
				Write((o == null) ? "" : o.ToString(), category);
			}
		}
	}

	protected virtual void WriteIndent()
	{
		NeedIndent = false;
		for (int i = 0; i < _indentLevel; i++)
		{
			if (_indentSize == 4)
			{
				Write("    ");
				continue;
			}
			for (int j = 0; j < _indentSize; j++)
			{
				Write(" ");
			}
		}
	}

	public abstract void WriteLine(string? message);

	public virtual void WriteLine(object? o)
	{
		if (Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, null, null, o))
		{
			WriteLine((o == null) ? "" : o.ToString());
		}
	}

	public virtual void WriteLine(string? message, string? category)
	{
		if (Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message))
		{
			if (category == null)
			{
				WriteLine(message);
			}
			else
			{
				WriteLine(category + ": " + ((message == null) ? string.Empty : message));
			}
		}
	}

	public virtual void WriteLine(object? o, string? category)
	{
		if (Filter == null || Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, category, null, o))
		{
			WriteLine((o == null) ? "" : o.ToString(), category);
		}
	}

	public virtual void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
	{
		if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data))
		{
			WriteHeader(source, eventType, id);
			string message = string.Empty;
			if (data != null)
			{
				message = data.ToString();
			}
			WriteLine(message);
			WriteFooter(eventCache);
		}
	}

	public virtual void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
	{
		if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
		{
			WriteHeader(source, eventType, id);
			WriteLine((data != null) ? string.Join(", ", data) : string.Empty);
			WriteFooter(eventCache);
		}
	}

	public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id)
	{
		TraceEvent(eventCache, source, eventType, id, string.Empty);
	}

	public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, message))
		{
			WriteHeader(source, eventType, id);
			WriteLine(message);
			WriteFooter(eventCache);
		}
	}

	public virtual void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
	{
		if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, format, args))
		{
			WriteHeader(source, eventType, id);
			if (args != null)
			{
				WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
			}
			else
			{
				WriteLine(format);
			}
			WriteFooter(eventCache);
		}
	}

	private void WriteHeader(string source, TraceEventType eventType, int id)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		Span<char> initialBuffer = stackalloc char[256];
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(6, 3, invariantCulture, initialBuffer);
		handler.AppendFormatted(source);
		handler.AppendLiteral(" ");
		handler.AppendFormatted(eventType);
		handler.AppendLiteral(": ");
		handler.AppendFormatted(id);
		handler.AppendLiteral(" : ");
		Write(string.Create(provider, initialBuffer, ref handler));
	}

	private void WriteFooter(TraceEventCache eventCache)
	{
		if (eventCache == null)
		{
			return;
		}
		_indentLevel++;
		if (IsEnabled(TraceOptions.ProcessId))
		{
			WriteLine("ProcessId=" + eventCache.ProcessId);
		}
		if (IsEnabled(TraceOptions.LogicalOperationStack))
		{
			Write("LogicalOperationStack=");
			Stack logicalOperationStack = eventCache.LogicalOperationStack;
			bool flag = true;
			foreach (object item in logicalOperationStack)
			{
				if (!flag)
				{
					Write(", ");
				}
				else
				{
					flag = false;
				}
				Write(item.ToString());
			}
			WriteLine(string.Empty);
		}
		Span<char> span = stackalloc char[128];
		if (IsEnabled(TraceOptions.ThreadId))
		{
			WriteLine("ThreadId=" + eventCache.ThreadId);
		}
		if (IsEnabled(TraceOptions.DateTime))
		{
			IFormatProvider formatProvider = null;
			IFormatProvider provider = formatProvider;
			Span<char> span2 = span;
			Span<char> initialBuffer = span2;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(9, 1, formatProvider, span2);
			handler.AppendLiteral("DateTime=");
			handler.AppendFormatted(eventCache.DateTime, "o");
			WriteLine(string.Create(provider, initialBuffer, ref handler));
		}
		if (IsEnabled(TraceOptions.Timestamp))
		{
			IFormatProvider formatProvider = null;
			IFormatProvider provider2 = formatProvider;
			Span<char> span2 = span;
			Span<char> initialBuffer2 = span2;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(10, 1, formatProvider, span2);
			handler2.AppendLiteral("Timestamp=");
			handler2.AppendFormatted(eventCache.Timestamp);
			WriteLine(string.Create(provider2, initialBuffer2, ref handler2));
		}
		if (IsEnabled(TraceOptions.Callstack))
		{
			WriteLine("Callstack=" + eventCache.Callstack);
		}
		_indentLevel--;
	}

	internal bool IsEnabled(TraceOptions opts)
	{
		return (opts & TraceOutputOptions) != 0;
	}
}
