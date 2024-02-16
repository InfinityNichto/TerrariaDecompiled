using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public class SourceFilter : TraceFilter
{
	private string _src;

	public string Source
	{
		get
		{
			return _src;
		}
		[MemberNotNull("_src")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("Source");
			}
			_src = value;
		}
	}

	public SourceFilter(string source)
	{
		Source = source;
	}

	public override bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, string? formatOrMessage, object?[]? args, object? data1, object?[]? data)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return string.Equals(_src, source);
	}
}
