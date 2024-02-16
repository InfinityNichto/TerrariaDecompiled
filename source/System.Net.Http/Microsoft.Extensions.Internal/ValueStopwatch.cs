using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Internal;

internal struct ValueStopwatch
{
	private static readonly double TimestampToTicks = 10000000.0 / (double)Stopwatch.Frequency;

	private long _startTimestamp;

	public bool IsActive => _startTimestamp != 0;

	private ValueStopwatch(long startTimestamp)
	{
		_startTimestamp = startTimestamp;
	}

	public static Microsoft.Extensions.Internal.ValueStopwatch StartNew()
	{
		return new Microsoft.Extensions.Internal.ValueStopwatch(Stopwatch.GetTimestamp());
	}

	public TimeSpan GetElapsedTime()
	{
		if (!IsActive)
		{
			throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
		}
		long timestamp = Stopwatch.GetTimestamp();
		long num = timestamp - _startTimestamp;
		long ticks = (long)(TimestampToTicks * (double)num);
		return new TimeSpan(ticks);
	}
}
