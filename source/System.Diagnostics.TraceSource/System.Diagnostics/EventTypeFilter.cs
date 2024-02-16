namespace System.Diagnostics;

public class EventTypeFilter : TraceFilter
{
	private SourceLevels _level;

	public SourceLevels EventType
	{
		get
		{
			return _level;
		}
		set
		{
			_level = value;
		}
	}

	public EventTypeFilter(SourceLevels level)
	{
		_level = level;
	}

	public override bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, string? formatOrMessage, object?[]? args, object? data1, object?[]? data)
	{
		return ((uint)eventType & (uint)_level) != 0;
	}
}
