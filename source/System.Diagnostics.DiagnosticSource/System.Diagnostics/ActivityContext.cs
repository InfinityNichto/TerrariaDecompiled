using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public readonly struct ActivityContext : IEquatable<ActivityContext>
{
	public ActivityTraceId TraceId { get; }

	public ActivitySpanId SpanId { get; }

	public ActivityTraceFlags TraceFlags { get; }

	public string? TraceState { get; }

	public bool IsRemote { get; }

	public ActivityContext(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags traceFlags, string? traceState = null, bool isRemote = false)
	{
		TraceId = traceId;
		SpanId = spanId;
		TraceFlags = traceFlags;
		TraceState = traceState;
		IsRemote = isRemote;
	}

	public static bool TryParse(string? traceParent, string? traceState, out ActivityContext context)
	{
		if (traceParent == null)
		{
			context = default(ActivityContext);
			return false;
		}
		return Activity.TryConvertIdToContext(traceParent, traceState, out context);
	}

	public static ActivityContext Parse(string traceParent, string? traceState)
	{
		if (traceParent == null)
		{
			throw new ArgumentNullException("traceParent");
		}
		if (!Activity.TryConvertIdToContext(traceParent, traceState, out var context))
		{
			throw new ArgumentException(System.SR.InvalidTraceParent);
		}
		return context;
	}

	public bool Equals(ActivityContext value)
	{
		if (SpanId.Equals(value.SpanId) && TraceId.Equals(value.TraceId) && TraceFlags == value.TraceFlags && TraceState == value.TraceState)
		{
			return IsRemote == value.IsRemote;
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ActivityContext value))
		{
			return false;
		}
		return Equals(value);
	}

	public static bool operator ==(ActivityContext left, ActivityContext right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ActivityContext left, ActivityContext right)
	{
		return !(left == right);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(TraceId, SpanId, TraceFlags, TraceState);
	}
}
