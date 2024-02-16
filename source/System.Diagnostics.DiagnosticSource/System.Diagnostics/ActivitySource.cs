using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics;

public sealed class ActivitySource : IDisposable
{
	internal delegate void Function<T, TParent>(T item, ref ActivityCreationOptions<TParent> data, ref ActivitySamplingResult samplingResult, ref ActivityCreationOptions<ActivityContext> dataWithContext);

	private static readonly SynchronizedList<ActivitySource> s_activeSources = new SynchronizedList<ActivitySource>();

	private static readonly SynchronizedList<ActivityListener> s_allListeners = new SynchronizedList<ActivityListener>();

	private SynchronizedList<ActivityListener> _listeners;

	public string Name { get; }

	public string? Version { get; }

	public ActivitySource(string name, string? version = "")
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		Name = name;
		Version = version;
		s_activeSources.Add(this);
		if (s_allListeners.Count > 0)
		{
			s_allListeners.EnumWithAction(delegate(ActivityListener listener, object source)
			{
				Func<ActivitySource, bool> shouldListenTo = listener.ShouldListenTo;
				if (shouldListenTo != null)
				{
					ActivitySource activitySource = (ActivitySource)source;
					if (shouldListenTo(activitySource))
					{
						activitySource.AddListener(listener);
					}
				}
			}, this);
		}
		GC.KeepAlive(DiagnosticSourceEventSource.Log);
	}

	public bool HasListeners()
	{
		SynchronizedList<ActivityListener> listeners = _listeners;
		if (listeners != null)
		{
			return listeners.Count > 0;
		}
		return false;
	}

	public Activity? CreateActivity(string name, ActivityKind kind)
	{
		return CreateActivity(name, kind, default(ActivityContext), null, null, null, default(DateTimeOffset), startIt: false);
	}

	public Activity? CreateActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown)
	{
		return CreateActivity(name, kind, parentContext, null, tags, links, default(DateTimeOffset), startIt: false, idFormat);
	}

	public Activity? CreateActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, ActivityIdFormat idFormat = ActivityIdFormat.Unknown)
	{
		return CreateActivity(name, kind, default(ActivityContext), parentId, tags, links, default(DateTimeOffset), startIt: false, idFormat);
	}

	public Activity? StartActivity([CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal)
	{
		return CreateActivity(name, kind, default(ActivityContext), null, null, null, default(DateTimeOffset));
	}

	public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default(DateTimeOffset))
	{
		return CreateActivity(name, kind, parentContext, null, tags, links, startTime);
	}

	public Activity? StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default(DateTimeOffset))
	{
		return CreateActivity(name, kind, default(ActivityContext), parentId, tags, links, startTime);
	}

	public Activity? StartActivity(ActivityKind kind, ActivityContext parentContext = default(ActivityContext), IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default(DateTimeOffset), [CallerMemberName] string name = "")
	{
		return CreateActivity(name, kind, parentContext, null, tags, links, startTime);
	}

	private Activity CreateActivity(string name, ActivityKind kind, ActivityContext context, string parentId, IEnumerable<KeyValuePair<string, object>> tags, IEnumerable<ActivityLink> links, DateTimeOffset startTime, bool startIt = true, ActivityIdFormat idFormat = ActivityIdFormat.Unknown)
	{
		SynchronizedList<ActivityListener> listeners = _listeners;
		if (listeners == null || listeners.Count == 0)
		{
			return null;
		}
		Activity result2 = null;
		ActivitySamplingResult samplingResult = ActivitySamplingResult.None;
		ActivityTagsCollection activityTagsCollection;
		if (parentId != null)
		{
			ActivityCreationOptions<string> activityCreationOptions = default(ActivityCreationOptions<string>);
			ActivityCreationOptions<ActivityContext> dataWithContext2 = default(ActivityCreationOptions<ActivityContext>);
			activityCreationOptions = new ActivityCreationOptions<string>(this, name, parentId, kind, tags, links, idFormat);
			if (activityCreationOptions.IdFormat == ActivityIdFormat.W3C)
			{
				dataWithContext2 = new ActivityCreationOptions<ActivityContext>(this, name, activityCreationOptions.GetContext(), kind, tags, links, ActivityIdFormat.W3C);
			}
			listeners.EnumWithFunc(delegate(ActivityListener listener, ref ActivityCreationOptions<string> data, ref ActivitySamplingResult result, ref ActivityCreationOptions<ActivityContext> dataWithContext)
			{
				SampleActivity<string> sampleUsingParentId = listener.SampleUsingParentId;
				if (sampleUsingParentId != null)
				{
					ActivitySamplingResult activitySamplingResult2 = sampleUsingParentId(ref data);
					if (activitySamplingResult2 > result)
					{
						result = activitySamplingResult2;
					}
				}
				else if (data.IdFormat == ActivityIdFormat.W3C)
				{
					SampleActivity<ActivityContext> sample2 = listener.Sample;
					if (sample2 != null)
					{
						ActivitySamplingResult activitySamplingResult3 = sample2(ref dataWithContext);
						if (activitySamplingResult3 > result)
						{
							result = activitySamplingResult3;
						}
					}
				}
			}, ref activityCreationOptions, ref samplingResult, ref dataWithContext2);
			if (context == default(ActivityContext))
			{
				if (activityCreationOptions.GetContext() != default(ActivityContext))
				{
					context = activityCreationOptions.GetContext();
					parentId = null;
				}
				else if (dataWithContext2.GetContext() != default(ActivityContext))
				{
					context = dataWithContext2.GetContext();
					parentId = null;
				}
			}
			activityTagsCollection = activityCreationOptions.GetSamplingTags();
			ActivityTagsCollection samplingTags = dataWithContext2.GetSamplingTags();
			if (samplingTags != null)
			{
				if (activityTagsCollection == null)
				{
					activityTagsCollection = samplingTags;
				}
				else
				{
					foreach (KeyValuePair<string, object?> item in samplingTags)
					{
						activityTagsCollection.Add(item);
					}
				}
			}
			idFormat = activityCreationOptions.IdFormat;
		}
		else
		{
			bool flag = context == default(ActivityContext) && Activity.Current != null;
			ActivityCreationOptions<ActivityContext> data2 = new ActivityCreationOptions<ActivityContext>(this, name, flag ? Activity.Current.Context : context, kind, tags, links, idFormat);
			listeners.EnumWithFunc(delegate(ActivityListener listener, ref ActivityCreationOptions<ActivityContext> data, ref ActivitySamplingResult result, ref ActivityCreationOptions<ActivityContext> unused)
			{
				SampleActivity<ActivityContext> sample = listener.Sample;
				if (sample != null)
				{
					ActivitySamplingResult activitySamplingResult = sample(ref data);
					if (activitySamplingResult > result)
					{
						result = activitySamplingResult;
					}
				}
			}, ref data2, ref samplingResult, ref data2);
			if (!flag)
			{
				context = data2.GetContext();
			}
			activityTagsCollection = data2.GetSamplingTags();
			idFormat = data2.IdFormat;
		}
		if (samplingResult != 0)
		{
			result2 = Activity.Create(this, name, kind, parentId, context, tags, links, startTime, activityTagsCollection, samplingResult, startIt, idFormat);
		}
		return result2;
	}

	public void Dispose()
	{
		_listeners = null;
		s_activeSources.Remove(this);
	}

	public static void AddActivityListener(ActivityListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		if (!s_allListeners.AddIfNotExist(listener))
		{
			return;
		}
		s_activeSources.EnumWithAction(delegate(ActivitySource source, object obj)
		{
			Func<ActivitySource, bool> shouldListenTo = ((ActivityListener)obj).ShouldListenTo;
			if (shouldListenTo != null && shouldListenTo(source))
			{
				source.AddListener((ActivityListener)obj);
			}
		}, listener);
	}

	internal void AddListener(ActivityListener listener)
	{
		if (_listeners == null)
		{
			Interlocked.CompareExchange(ref _listeners, new SynchronizedList<ActivityListener>(), null);
		}
		_listeners.AddIfNotExist(listener);
	}

	internal static void DetachListener(ActivityListener listener)
	{
		s_allListeners.Remove(listener);
		s_activeSources.EnumWithAction(delegate(ActivitySource source, object obj)
		{
			source._listeners?.Remove((ActivityListener)obj);
		}, listener);
	}

	internal void NotifyActivityStart(Activity activity)
	{
		SynchronizedList<ActivityListener> listeners = _listeners;
		if (listeners != null && listeners.Count > 0)
		{
			listeners.EnumWithAction(delegate(ActivityListener listener, object obj)
			{
				listener.ActivityStarted?.Invoke((Activity)obj);
			}, activity);
		}
	}

	internal void NotifyActivityStop(Activity activity)
	{
		SynchronizedList<ActivityListener> listeners = _listeners;
		if (listeners != null && listeners.Count > 0)
		{
			listeners.EnumWithAction(delegate(ActivityListener listener, object obj)
			{
				listener.ActivityStopped?.Invoke((Activity)obj);
			}, activity);
		}
	}
}
