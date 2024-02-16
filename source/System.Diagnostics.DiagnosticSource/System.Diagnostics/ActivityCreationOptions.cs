using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

public readonly struct ActivityCreationOptions<T>
{
	private readonly ActivityTagsCollection _samplerTags;

	private readonly ActivityContext _context;

	public ActivitySource Source { get; }

	public string Name { get; }

	public ActivityKind Kind { get; }

	public T Parent { get; }

	public IEnumerable<KeyValuePair<string, object?>>? Tags { get; }

	public IEnumerable<ActivityLink>? Links { get; }

	public ActivityTagsCollection SamplingTags
	{
		get
		{
			if (_samplerTags == null)
			{
				Unsafe.AsRef(in _samplerTags) = new ActivityTagsCollection();
			}
			return _samplerTags;
		}
	}

	public ActivityTraceId TraceId
	{
		get
		{
			if (Parent is ActivityContext && IdFormat == ActivityIdFormat.W3C && _context == default(ActivityContext))
			{
				ActivityTraceId traceId = Activity.TraceIdGenerator?.Invoke() ?? ActivityTraceId.CreateRandom();
				Unsafe.AsRef(in _context) = new ActivityContext(traceId, default(ActivitySpanId), ActivityTraceFlags.None);
			}
			return _context.TraceId;
		}
	}

	internal ActivityIdFormat IdFormat { get; }

	internal ActivityCreationOptions(ActivitySource source, string name, T parent, ActivityKind kind, IEnumerable<KeyValuePair<string, object>> tags, IEnumerable<ActivityLink> links, ActivityIdFormat idFormat)
	{
		Source = source;
		Name = name;
		Kind = kind;
		Parent = parent;
		Tags = tags;
		Links = links;
		IdFormat = idFormat;
		if (IdFormat == ActivityIdFormat.Unknown && Activity.ForceDefaultIdFormat)
		{
			IdFormat = Activity.DefaultIdFormat;
		}
		_samplerTags = null;
		if (parent is ActivityContext activityContext && activityContext != default(ActivityContext))
		{
			_context = activityContext;
			if (IdFormat == ActivityIdFormat.Unknown)
			{
				IdFormat = ActivityIdFormat.W3C;
			}
		}
		else if (parent is string text && text != null)
		{
			if (IdFormat != ActivityIdFormat.Hierarchical)
			{
				if (ActivityContext.TryParse(text, null, out _context))
				{
					IdFormat = ActivityIdFormat.W3C;
				}
				if (IdFormat == ActivityIdFormat.Unknown)
				{
					IdFormat = ActivityIdFormat.Hierarchical;
				}
			}
			else
			{
				_context = default(ActivityContext);
			}
		}
		else
		{
			_context = default(ActivityContext);
			if (IdFormat == ActivityIdFormat.Unknown)
			{
				IdFormat = ((Activity.Current != null) ? Activity.Current.IdFormat : Activity.DefaultIdFormat);
			}
		}
	}

	internal ActivityTagsCollection GetSamplingTags()
	{
		return _samplerTags;
	}

	internal ActivityContext GetContext()
	{
		return _context;
	}
}
