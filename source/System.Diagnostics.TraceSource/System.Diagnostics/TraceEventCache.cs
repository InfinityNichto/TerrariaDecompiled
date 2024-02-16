using System.Collections;
using System.Globalization;

namespace System.Diagnostics;

public class TraceEventCache
{
	private long _timeStamp = -1L;

	private DateTime _dateTime = DateTime.MinValue;

	private string _stackTrace;

	public DateTime DateTime
	{
		get
		{
			if (_dateTime == DateTime.MinValue)
			{
				_dateTime = DateTime.UtcNow;
			}
			return _dateTime;
		}
	}

	public int ProcessId => Environment.ProcessId;

	public string ThreadId => Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);

	public long Timestamp
	{
		get
		{
			if (_timeStamp == -1)
			{
				_timeStamp = Stopwatch.GetTimestamp();
			}
			return _timeStamp;
		}
	}

	public string Callstack
	{
		get
		{
			if (_stackTrace == null)
			{
				_stackTrace = Environment.StackTrace;
			}
			return _stackTrace;
		}
	}

	public Stack LogicalOperationStack => Trace.CorrelationManager.LogicalOperationStack;
}
