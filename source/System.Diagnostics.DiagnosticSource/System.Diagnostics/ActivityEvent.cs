using System.Collections.Generic;

namespace System.Diagnostics;

public readonly struct ActivityEvent
{
	private static readonly ActivityTagsCollection s_emptyTags = new ActivityTagsCollection();

	public string Name { get; }

	public DateTimeOffset Timestamp { get; }

	public IEnumerable<KeyValuePair<string, object?>> Tags { get; }

	public ActivityEvent(string name)
		: this(name, DateTimeOffset.UtcNow, s_emptyTags)
	{
	}

	public ActivityEvent(string name, DateTimeOffset timestamp = default(DateTimeOffset), ActivityTagsCollection? tags = null)
	{
		Name = name ?? string.Empty;
		Tags = tags ?? s_emptyTags;
		Timestamp = ((timestamp != default(DateTimeOffset)) ? timestamp : DateTimeOffset.UtcNow);
	}
}
