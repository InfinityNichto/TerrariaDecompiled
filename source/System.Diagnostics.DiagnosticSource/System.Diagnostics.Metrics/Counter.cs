using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public sealed class Counter<T> : Instrument<T> where T : struct
{
	internal Counter(Meter meter, string name, string unit, string description)
		: base(meter, name, unit, description)
	{
		Publish();
	}

	public void Add(T delta)
	{
		RecordMeasurement(delta);
	}

	public void Add(T delta, KeyValuePair<string, object?> tag)
	{
		RecordMeasurement(delta, tag);
	}

	public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
	{
		RecordMeasurement(delta, tag1, tag2);
	}

	public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
	{
		RecordMeasurement(delta, tag1, tag2, tag3);
	}

	public void Add(T delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		RecordMeasurement(delta, tags);
	}

	public void Add(T delta, params KeyValuePair<string, object?>[] tags)
	{
		RecordMeasurement(delta, tags.AsSpan());
	}

	public void Add(T delta, in TagList tagList)
	{
		RecordMeasurement(delta, in tagList);
	}
}
