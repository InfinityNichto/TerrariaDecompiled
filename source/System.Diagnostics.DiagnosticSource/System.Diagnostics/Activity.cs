using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Diagnostics;

public class Activity : IDisposable
{
	private sealed class BaggageLinkedList : IEnumerable<KeyValuePair<string, string>>, IEnumerable
	{
		private DiagNode<KeyValuePair<string, string>> _first;

		public DiagNode<KeyValuePair<string, string>> First => _first;

		public BaggageLinkedList(KeyValuePair<string, string> firstValue, bool set = false)
		{
			_first = ((set && firstValue.Value == null) ? null : new DiagNode<KeyValuePair<string, string>>(firstValue));
		}

		public void Add(KeyValuePair<string, string> value)
		{
			DiagNode<KeyValuePair<string, string>> diagNode = new DiagNode<KeyValuePair<string, string>>(value);
			lock (this)
			{
				diagNode.Next = _first;
				_first = diagNode;
			}
		}

		public void Set(KeyValuePair<string, string> value)
		{
			if (value.Value == null)
			{
				Remove(value.Key);
				return;
			}
			lock (this)
			{
				for (DiagNode<KeyValuePair<string, string>> diagNode = _first; diagNode != null; diagNode = diagNode.Next)
				{
					if (diagNode.Value.Key == value.Key)
					{
						diagNode.Value = value;
						return;
					}
				}
				DiagNode<KeyValuePair<string, string>> diagNode2 = new DiagNode<KeyValuePair<string, string>>(value);
				diagNode2.Next = _first;
				_first = diagNode2;
			}
		}

		public void Remove(string key)
		{
			lock (this)
			{
				if (_first == null)
				{
					return;
				}
				if (_first.Value.Key == key)
				{
					_first = _first.Next;
					return;
				}
				DiagNode<KeyValuePair<string, string>> diagNode = _first;
				while (diagNode.Next != null)
				{
					if (diagNode.Next.Value.Key == key)
					{
						diagNode.Next = diagNode.Next.Next;
						break;
					}
					diagNode = diagNode.Next;
				}
			}
		}

		public Enumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return new Enumerator<KeyValuePair<string, string>>(_first);
		}

		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private sealed class TagsLinkedList : IEnumerable<KeyValuePair<string, object>>, IEnumerable
	{
		private DiagNode<KeyValuePair<string, object>> _first;

		private DiagNode<KeyValuePair<string, object>> _last;

		private StringBuilder _stringBuilder;

		public TagsLinkedList(KeyValuePair<string, object> firstValue, bool set = false)
		{
			_last = (_first = ((set && firstValue.Value == null) ? null : new DiagNode<KeyValuePair<string, object>>(firstValue)));
		}

		public TagsLinkedList(IEnumerator<KeyValuePair<string, object>> e)
		{
			_last = (_first = new DiagNode<KeyValuePair<string, object>>(e.Current));
			while (e.MoveNext())
			{
				_last.Next = new DiagNode<KeyValuePair<string, object>>(e.Current);
				_last = _last.Next;
			}
		}

		public TagsLinkedList(IEnumerable<KeyValuePair<string, object>> list)
		{
			Add(list);
		}

		public void Add(IEnumerable<KeyValuePair<string, object>> list)
		{
			IEnumerator<KeyValuePair<string, object>> enumerator = list.GetEnumerator();
			if (enumerator.MoveNext())
			{
				if (_first == null)
				{
					_last = (_first = new DiagNode<KeyValuePair<string, object>>(enumerator.Current));
				}
				else
				{
					_last.Next = new DiagNode<KeyValuePair<string, object>>(enumerator.Current);
					_last = _last.Next;
				}
				while (enumerator.MoveNext())
				{
					_last.Next = new DiagNode<KeyValuePair<string, object>>(enumerator.Current);
					_last = _last.Next;
				}
			}
		}

		public void Add(KeyValuePair<string, object> value)
		{
			DiagNode<KeyValuePair<string, object>> diagNode = new DiagNode<KeyValuePair<string, object>>(value);
			lock (this)
			{
				if (_first == null)
				{
					_first = (_last = diagNode);
					return;
				}
				_last.Next = diagNode;
				_last = diagNode;
			}
		}

		public object Get(string key)
		{
			for (DiagNode<KeyValuePair<string, object>> diagNode = _first; diagNode != null; diagNode = diagNode.Next)
			{
				if (diagNode.Value.Key == key)
				{
					return diagNode.Value.Value;
				}
			}
			return null;
		}

		public void Remove(string key)
		{
			lock (this)
			{
				if (_first == null)
				{
					return;
				}
				if (_first.Value.Key == key)
				{
					_first = _first.Next;
					if (_first == null)
					{
						_last = null;
					}
					return;
				}
				DiagNode<KeyValuePair<string, object>> diagNode = _first;
				while (diagNode.Next != null)
				{
					if (diagNode.Next.Value.Key == key)
					{
						if (_last == diagNode.Next)
						{
							_last = diagNode;
						}
						diagNode.Next = diagNode.Next.Next;
						break;
					}
					diagNode = diagNode.Next;
				}
			}
		}

		public void Set(KeyValuePair<string, object> value)
		{
			if (value.Value == null)
			{
				Remove(value.Key);
				return;
			}
			lock (this)
			{
				for (DiagNode<KeyValuePair<string, object>> diagNode = _first; diagNode != null; diagNode = diagNode.Next)
				{
					if (diagNode.Value.Key == value.Key)
					{
						diagNode.Value = value;
						return;
					}
				}
				DiagNode<KeyValuePair<string, object>> diagNode2 = new DiagNode<KeyValuePair<string, object>>(value);
				if (_first == null)
				{
					_first = (_last = diagNode2);
					return;
				}
				_last.Next = diagNode2;
				_last = diagNode2;
			}
		}

		public Enumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return new Enumerator<KeyValuePair<string, object>>(_first);
		}

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerable<KeyValuePair<string, string>> EnumerateStringValues()
		{
			for (DiagNode<KeyValuePair<string, object>> current = _first; current != null; current = current.Next)
			{
				if (current.Value.Value is string || current.Value.Value == null)
				{
					yield return new KeyValuePair<string, string>(current.Value.Key, (string)current.Value.Value);
				}
			}
		}

		public override string ToString()
		{
			lock (this)
			{
				if (_first == null)
				{
					return string.Empty;
				}
				if (_stringBuilder == null)
				{
					_stringBuilder = new StringBuilder();
				}
				_stringBuilder.Append(_first.Value.Key);
				_stringBuilder.Append(':');
				_stringBuilder.Append(_first.Value.Value);
				for (DiagNode<KeyValuePair<string, object>> next = _first.Next; next != null; next = next.Next)
				{
					_stringBuilder.Append(", ");
					_stringBuilder.Append(next.Value.Key);
					_stringBuilder.Append(':');
					_stringBuilder.Append(next.Value.Value);
				}
				string result = _stringBuilder.ToString();
				_stringBuilder.Clear();
				return result;
			}
		}
	}

	[Flags]
	private enum State : byte
	{
		None = 0,
		FormatUnknown = 0,
		FormatHierarchical = 1,
		FormatW3C = 2,
		FormatFlags = 3,
		IsFinished = 0x80
	}

	private static readonly IEnumerable<KeyValuePair<string, string>> s_emptyBaggageTags = new KeyValuePair<string, string>[0];

	private static readonly IEnumerable<KeyValuePair<string, object>> s_emptyTagObjects = new KeyValuePair<string, object>[0];

	private static readonly IEnumerable<ActivityLink> s_emptyLinks = new ActivityLink[0];

	private static readonly IEnumerable<ActivityEvent> s_emptyEvents = new ActivityEvent[0];

	private static readonly ActivitySource s_defaultSource = new ActivitySource(string.Empty);

	private static readonly string s_uniqSuffix = "-" + GetRandomNumber().ToString("x") + ".";

	private static long s_currentRootId = (uint)GetRandomNumber();

	private static ActivityIdFormat s_defaultIdFormat;

	private string _traceState;

	private State _state;

	private int _currentChildId;

	private string _id;

	private string _rootId;

	private string _parentId;

	private string _parentSpanId;

	private string _traceId;

	private string _spanId;

	private byte _w3CIdFlags;

	private byte _parentTraceFlags;

	private TagsLinkedList _tags;

	private BaggageLinkedList _baggage;

	private DiagLinkedList<ActivityLink> _links;

	private DiagLinkedList<ActivityEvent> _events;

	private Dictionary<string, object> _customProperties;

	private string _displayName;

	private ActivityStatusCode _statusCode;

	private string _statusDescription;

	private Activity _previousActiveActivity;

	private static readonly AsyncLocal<Activity> s_current = new AsyncLocal<Activity>();

	public static bool ForceDefaultIdFormat { get; set; }

	public ActivityStatusCode Status => _statusCode;

	public string? StatusDescription => _statusDescription;

	public ActivityKind Kind { get; private set; }

	public string OperationName { get; }

	public string DisplayName
	{
		get
		{
			return _displayName ?? OperationName;
		}
		set
		{
			_displayName = value ?? throw new ArgumentNullException("value");
		}
	}

	public ActivitySource Source { get; private set; }

	public Activity? Parent { get; private set; }

	public TimeSpan Duration { get; private set; }

	public DateTime StartTimeUtc { get; private set; }

	public string? Id
	{
		get
		{
			if (_id == null && _spanId != null)
			{
				Span<char> buffer = stackalloc char[2];
				System.HexConverter.ToCharsBuffer((byte)(0xFFFFFF7Fu & _w3CIdFlags), buffer, 0, System.HexConverter.Casing.Lower);
				string value = "00-" + _traceId + "-" + _spanId + "-" + buffer;
				Interlocked.CompareExchange(ref _id, value, null);
			}
			return _id;
		}
	}

	public string? ParentId
	{
		get
		{
			if (_parentId == null)
			{
				if (_parentSpanId != null)
				{
					Span<char> buffer = stackalloc char[2];
					System.HexConverter.ToCharsBuffer((byte)(0xFFFFFF7Fu & _parentTraceFlags), buffer, 0, System.HexConverter.Casing.Lower);
					string value = "00-" + _traceId + "-" + _parentSpanId + "-" + buffer;
					Interlocked.CompareExchange(ref _parentId, value, null);
				}
				else if (Parent != null)
				{
					Interlocked.CompareExchange(ref _parentId, Parent.Id, null);
				}
			}
			return _parentId;
		}
	}

	public string? RootId
	{
		get
		{
			if (_rootId == null)
			{
				string text = null;
				if (Id != null)
				{
					text = GetRootId(Id);
				}
				else if (ParentId != null)
				{
					text = GetRootId(ParentId);
				}
				if (text != null)
				{
					Interlocked.CompareExchange(ref _rootId, text, null);
				}
			}
			return _rootId;
		}
	}

	public IEnumerable<KeyValuePair<string, string?>> Tags => _tags?.EnumerateStringValues() ?? s_emptyBaggageTags;

	public IEnumerable<KeyValuePair<string, object?>> TagObjects
	{
		get
		{
			IEnumerable<KeyValuePair<string, object>> tags = _tags;
			return tags ?? s_emptyTagObjects;
		}
	}

	public IEnumerable<ActivityEvent> Events
	{
		get
		{
			IEnumerable<ActivityEvent> events = _events;
			return events ?? s_emptyEvents;
		}
	}

	public IEnumerable<ActivityLink> Links
	{
		get
		{
			IEnumerable<ActivityLink> links = _links;
			return links ?? s_emptyLinks;
		}
	}

	public IEnumerable<KeyValuePair<string, string?>> Baggage
	{
		get
		{
			for (Activity activity2 = this; activity2 != null; activity2 = activity2.Parent)
			{
				if (activity2._baggage != null)
				{
					return Iterate(activity2);
				}
			}
			return s_emptyBaggageTags;
			static IEnumerable<KeyValuePair<string, string>> Iterate(Activity activity)
			{
				do
				{
					if (activity._baggage != null)
					{
						for (DiagNode<KeyValuePair<string, string>> current = activity._baggage.First; current != null; current = current.Next)
						{
							yield return current.Value;
						}
					}
					activity = activity.Parent;
				}
				while (activity != null);
			}
		}
	}

	public ActivityContext Context => new ActivityContext(TraceId, SpanId, ActivityTraceFlags, TraceStateString);

	public string? TraceStateString
	{
		get
		{
			for (Activity activity = this; activity != null; activity = activity.Parent)
			{
				string traceState = activity._traceState;
				if (traceState != null)
				{
					return traceState;
				}
			}
			return null;
		}
		set
		{
			_traceState = value;
		}
	}

	public ActivitySpanId SpanId
	{
		get
		{
			if (_spanId == null && _id != null && IdFormat == ActivityIdFormat.W3C)
			{
				string value = ActivitySpanId.CreateFromString(_id.AsSpan(36, 16)).ToHexString();
				Interlocked.CompareExchange(ref _spanId, value, null);
			}
			return new ActivitySpanId(_spanId);
		}
	}

	public ActivityTraceId TraceId
	{
		get
		{
			if (_traceId == null)
			{
				TrySetTraceIdFromParent();
			}
			return new ActivityTraceId(_traceId);
		}
	}

	public bool Recorded => (ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0;

	public bool IsAllDataRequested { get; set; }

	public ActivityTraceFlags ActivityTraceFlags
	{
		get
		{
			if (!W3CIdFlagsSet)
			{
				TrySetTraceFlagsFromParent();
			}
			return (ActivityTraceFlags)(-129 & (int)_w3CIdFlags);
		}
		set
		{
			_w3CIdFlags = (byte)(0x80u | (byte)value);
		}
	}

	public ActivitySpanId ParentSpanId
	{
		get
		{
			if (_parentSpanId == null)
			{
				string text = null;
				if (_parentId != null && IsW3CId(_parentId))
				{
					try
					{
						text = ActivitySpanId.CreateFromString(_parentId.AsSpan(36, 16)).ToHexString();
					}
					catch
					{
					}
				}
				else if (Parent != null && Parent.IdFormat == ActivityIdFormat.W3C)
				{
					text = Parent.SpanId.ToHexString();
				}
				if (text != null)
				{
					Interlocked.CompareExchange(ref _parentSpanId, text, null);
				}
			}
			return new ActivitySpanId(_parentSpanId);
		}
	}

	public static Func<ActivityTraceId>? TraceIdGenerator { get; set; }

	public static ActivityIdFormat DefaultIdFormat
	{
		get
		{
			if (s_defaultIdFormat == ActivityIdFormat.Unknown)
			{
				s_defaultIdFormat = (System.LocalAppContextSwitches.DefaultActivityIdFormatIsHierarchial ? ActivityIdFormat.Hierarchical : ActivityIdFormat.W3C);
			}
			return s_defaultIdFormat;
		}
		set
		{
			if (ActivityIdFormat.Hierarchical > value || value > ActivityIdFormat.W3C)
			{
				throw new ArgumentException(System.SR.ActivityIdFormatInvalid);
			}
			s_defaultIdFormat = value;
		}
	}

	private bool W3CIdFlagsSet => (_w3CIdFlags & 0x80) != 0;

	private bool IsFinished
	{
		get
		{
			return (_state & State.IsFinished) != 0;
		}
		set
		{
			if (value)
			{
				_state |= State.IsFinished;
			}
			else
			{
				_state &= ~State.IsFinished;
			}
		}
	}

	public ActivityIdFormat IdFormat
	{
		get
		{
			return (ActivityIdFormat)(_state & State.FormatFlags);
		}
		private set
		{
			_state = (_state & ~State.FormatFlags) | (State)((byte)value & 3u);
		}
	}

	public static Activity? Current
	{
		get
		{
			return s_current.Value;
		}
		set
		{
			if (ValidateSetCurrent(value))
			{
				SetCurrent(value);
			}
		}
	}

	public Activity SetStatus(ActivityStatusCode code, string? description = null)
	{
		_statusCode = code;
		_statusDescription = ((code == ActivityStatusCode.Error) ? description : null);
		return this;
	}

	public string? GetBaggageItem(string key)
	{
		foreach (KeyValuePair<string, string> item in Baggage)
		{
			if (key == item.Key)
			{
				return item.Value;
			}
		}
		return null;
	}

	public object? GetTagItem(string key)
	{
		return _tags?.Get(key) ?? null;
	}

	public Activity(string operationName)
	{
		Source = s_defaultSource;
		IsAllDataRequested = true;
		if (string.IsNullOrEmpty(operationName))
		{
			NotifyError(new ArgumentException(System.SR.OperationNameInvalid));
		}
		OperationName = operationName;
	}

	public Activity AddTag(string key, string? value)
	{
		return AddTag(key, (object?)value);
	}

	public Activity AddTag(string key, object? value)
	{
		KeyValuePair<string, object> keyValuePair = new KeyValuePair<string, object>(key, value);
		if (_tags != null || Interlocked.CompareExchange(ref _tags, new TagsLinkedList(keyValuePair), null) != null)
		{
			_tags.Add(keyValuePair);
		}
		return this;
	}

	public Activity SetTag(string key, object? value)
	{
		KeyValuePair<string, object> keyValuePair = new KeyValuePair<string, object>(key, value);
		if (_tags != null || Interlocked.CompareExchange(ref _tags, new TagsLinkedList(keyValuePair, set: true), null) != null)
		{
			_tags.Set(keyValuePair);
		}
		return this;
	}

	public Activity AddEvent(ActivityEvent e)
	{
		if (_events != null || Interlocked.CompareExchange(ref _events, new DiagLinkedList<ActivityEvent>(e), null) != null)
		{
			_events.Add(e);
		}
		return this;
	}

	public Activity AddBaggage(string key, string? value)
	{
		KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(key, value);
		if (_baggage != null || Interlocked.CompareExchange(ref _baggage, new BaggageLinkedList(keyValuePair), null) != null)
		{
			_baggage.Add(keyValuePair);
		}
		return this;
	}

	public Activity SetBaggage(string key, string? value)
	{
		KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(key, value);
		if (_baggage != null || Interlocked.CompareExchange(ref _baggage, new BaggageLinkedList(keyValuePair, set: true), null) != null)
		{
			_baggage.Set(keyValuePair);
		}
		return this;
	}

	public Activity SetParentId(string parentId)
	{
		if (Parent != null)
		{
			NotifyError(new InvalidOperationException(System.SR.SetParentIdOnActivityWithParent));
		}
		else if (ParentId != null || _parentSpanId != null)
		{
			NotifyError(new InvalidOperationException(System.SR.ParentIdAlreadySet));
		}
		else if (string.IsNullOrEmpty(parentId))
		{
			NotifyError(new ArgumentException(System.SR.ParentIdInvalid));
		}
		else
		{
			_parentId = parentId;
		}
		return this;
	}

	public Activity SetParentId(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
	{
		if (Parent != null)
		{
			NotifyError(new InvalidOperationException(System.SR.SetParentIdOnActivityWithParent));
		}
		else if (ParentId != null || _parentSpanId != null)
		{
			NotifyError(new InvalidOperationException(System.SR.ParentIdAlreadySet));
		}
		else
		{
			_traceId = traceId.ToHexString();
			_parentSpanId = spanId.ToHexString();
			ActivityTraceFlags = activityTraceFlags;
			_parentTraceFlags = (byte)activityTraceFlags;
		}
		return this;
	}

	public Activity SetStartTime(DateTime startTimeUtc)
	{
		if (startTimeUtc.Kind != DateTimeKind.Utc)
		{
			NotifyError(new InvalidOperationException(System.SR.StartTimeNotUtc));
		}
		else
		{
			StartTimeUtc = startTimeUtc;
		}
		return this;
	}

	public Activity SetEndTime(DateTime endTimeUtc)
	{
		if (endTimeUtc.Kind != DateTimeKind.Utc)
		{
			NotifyError(new InvalidOperationException(System.SR.EndTimeNotUtc));
		}
		else
		{
			Duration = endTimeUtc - StartTimeUtc;
			if (Duration.Ticks <= 0)
			{
				Duration = new TimeSpan(1L);
			}
		}
		return this;
	}

	public Activity Start()
	{
		if (_id != null || _spanId != null)
		{
			NotifyError(new InvalidOperationException(System.SR.ActivityStartAlreadyStarted));
		}
		else
		{
			_previousActiveActivity = Current;
			if (_parentId == null && _parentSpanId == null && _previousActiveActivity != null)
			{
				Parent = _previousActiveActivity;
			}
			if (StartTimeUtc == default(DateTime))
			{
				StartTimeUtc = GetUtcNow();
			}
			if (IdFormat == ActivityIdFormat.Unknown)
			{
				IdFormat = (ForceDefaultIdFormat ? DefaultIdFormat : ((Parent != null) ? Parent.IdFormat : ((_parentSpanId != null) ? ActivityIdFormat.W3C : ((_parentId == null) ? DefaultIdFormat : ((!IsW3CId(_parentId)) ? ActivityIdFormat.Hierarchical : ActivityIdFormat.W3C)))));
			}
			if (IdFormat == ActivityIdFormat.W3C)
			{
				GenerateW3CId();
			}
			else
			{
				_id = GenerateHierarchicalId();
			}
			SetCurrent(this);
			Source.NotifyActivityStart(this);
		}
		return this;
	}

	public void Stop()
	{
		if (_id == null && _spanId == null)
		{
			NotifyError(new InvalidOperationException(System.SR.ActivityNotStarted));
		}
		else if (!IsFinished)
		{
			IsFinished = true;
			if (Duration == TimeSpan.Zero)
			{
				SetEndTime(GetUtcNow());
			}
			Source.NotifyActivityStop(this);
			SetCurrent(_previousActiveActivity);
		}
	}

	public Activity SetIdFormat(ActivityIdFormat format)
	{
		if (_id != null || _spanId != null)
		{
			NotifyError(new InvalidOperationException(System.SR.SetFormatOnStartedActivity));
		}
		else
		{
			IdFormat = format;
		}
		return this;
	}

	private static bool IsW3CId(string id)
	{
		if (id.Length == 55 && (('0' <= id[0] && id[0] <= '9') || ('a' <= id[0] && id[0] <= 'f')) && (('0' <= id[1] && id[1] <= '9') || ('a' <= id[1] && id[1] <= 'f')))
		{
			if (id[0] == 'f')
			{
				return id[1] != 'f';
			}
			return true;
		}
		return false;
	}

	internal static bool TryConvertIdToContext(string traceParent, string traceState, out ActivityContext context)
	{
		context = default(ActivityContext);
		if (!IsW3CId(traceParent))
		{
			return false;
		}
		ReadOnlySpan<char> idData = traceParent.AsSpan(3, 32);
		ReadOnlySpan<char> idData2 = traceParent.AsSpan(36, 16);
		if (!ActivityTraceId.IsLowerCaseHexAndNotAllZeros(idData) || !ActivityTraceId.IsLowerCaseHexAndNotAllZeros(idData2) || !System.HexConverter.IsHexLowerChar(traceParent[53]) || !System.HexConverter.IsHexLowerChar(traceParent[54]))
		{
			return false;
		}
		context = new ActivityContext(new ActivityTraceId(idData.ToString()), new ActivitySpanId(idData2.ToString()), (ActivityTraceFlags)ActivityTraceId.HexByteFromChars(traceParent[53], traceParent[54]), traceState);
		return true;
	}

	public void Dispose()
	{
		if (!IsFinished)
		{
			Stop();
		}
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public void SetCustomProperty(string propertyName, object? propertyValue)
	{
		if (_customProperties == null)
		{
			Interlocked.CompareExchange(ref _customProperties, new Dictionary<string, object>(), null);
		}
		lock (_customProperties)
		{
			if (propertyValue == null)
			{
				_customProperties.Remove(propertyName);
			}
			else
			{
				_customProperties[propertyName] = propertyValue;
			}
		}
	}

	public object? GetCustomProperty(string propertyName)
	{
		if (_customProperties == null)
		{
			return null;
		}
		lock (_customProperties)
		{
			object value;
			return _customProperties.TryGetValue(propertyName, out value) ? value : null;
		}
	}

	internal static Activity Create(ActivitySource source, string name, ActivityKind kind, string parentId, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object>> tags, IEnumerable<ActivityLink> links, DateTimeOffset startTime, ActivityTagsCollection samplerTags, ActivitySamplingResult request, bool startIt, ActivityIdFormat idFormat)
	{
		Activity activity = new Activity(name);
		activity.Source = source;
		activity.Kind = kind;
		activity.IdFormat = idFormat;
		if (links != null)
		{
			using IEnumerator<ActivityLink> enumerator = links.GetEnumerator();
			if (enumerator.MoveNext())
			{
				activity._links = new DiagLinkedList<ActivityLink>(enumerator);
			}
		}
		if (tags != null)
		{
			using IEnumerator<KeyValuePair<string, object>> enumerator2 = tags.GetEnumerator();
			if (enumerator2.MoveNext())
			{
				activity._tags = new TagsLinkedList(enumerator2);
			}
		}
		if (samplerTags != null)
		{
			if (activity._tags == null)
			{
				activity._tags = new TagsLinkedList(samplerTags);
			}
			else
			{
				activity._tags.Add(samplerTags);
			}
		}
		if (parentId != null)
		{
			activity._parentId = parentId;
		}
		else if (parentContext != default(ActivityContext))
		{
			activity._traceId = parentContext.TraceId.ToString();
			if (parentContext.SpanId != default(ActivitySpanId))
			{
				activity._parentSpanId = parentContext.SpanId.ToString();
			}
			activity.ActivityTraceFlags = parentContext.TraceFlags;
			activity._parentTraceFlags = (byte)parentContext.TraceFlags;
			activity._traceState = parentContext.TraceState;
		}
		activity.IsAllDataRequested = request == ActivitySamplingResult.AllData || request == ActivitySamplingResult.AllDataAndRecorded;
		if (request == ActivitySamplingResult.AllDataAndRecorded)
		{
			activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
		}
		if (startTime != default(DateTimeOffset))
		{
			activity.StartTimeUtc = startTime.UtcDateTime;
		}
		if (startIt)
		{
			activity.Start();
		}
		return activity;
	}

	private void GenerateW3CId()
	{
		if (_traceId == null && !TrySetTraceIdFromParent())
		{
			_traceId = (TraceIdGenerator?.Invoke() ?? ActivityTraceId.CreateRandom()).ToHexString();
		}
		if (!W3CIdFlagsSet)
		{
			TrySetTraceFlagsFromParent();
		}
		_spanId = ActivitySpanId.CreateRandom().ToHexString();
	}

	private static void NotifyError(Exception exception)
	{
		try
		{
			throw exception;
		}
		catch
		{
		}
	}

	private string GenerateHierarchicalId()
	{
		if (Parent != null)
		{
			return AppendSuffix(Parent.Id, Interlocked.Increment(ref Parent._currentChildId).ToString(), '.');
		}
		if (ParentId != null)
		{
			string text = ((ParentId[0] == '|') ? ParentId : ("|" + ParentId));
			char c = text[text.Length - 1];
			if (c != '.' && c != '_')
			{
				text += ".";
			}
			return AppendSuffix(text, Interlocked.Increment(ref s_currentRootId).ToString("x"), '_');
		}
		return GenerateRootId();
	}

	private string GetRootId(string id)
	{
		if (IdFormat == ActivityIdFormat.W3C)
		{
			return id.Substring(3, 32);
		}
		int num = id.IndexOf('.');
		if (num < 0)
		{
			num = id.Length;
		}
		int num2 = ((id[0] == '|') ? 1 : 0);
		return id.Substring(num2, num - num2);
	}

	private string AppendSuffix(string parentId, string suffix, char delimiter)
	{
		if (parentId.Length + suffix.Length < 1024)
		{
			return parentId + suffix + delimiter;
		}
		int num = 1015;
		while (num > 1 && parentId[num - 1] != '.' && parentId[num - 1] != '_')
		{
			num--;
		}
		if (num == 1)
		{
			return GenerateRootId();
		}
		string text = ((int)GetRandomNumber()).ToString("x8");
		return parentId.Substring(0, num) + text + "#";
	}

	private unsafe static long GetRandomNumber()
	{
		Guid guid = Guid.NewGuid();
		return *(long*)(&guid);
	}

	private static bool ValidateSetCurrent(Activity activity)
	{
		bool flag = activity == null || (activity.Id != null && !activity.IsFinished);
		if (!flag)
		{
			NotifyError(new InvalidOperationException(System.SR.ActivityNotRunning));
		}
		return flag;
	}

	private bool TrySetTraceIdFromParent()
	{
		if (Parent != null && Parent.IdFormat == ActivityIdFormat.W3C)
		{
			_traceId = Parent.TraceId.ToHexString();
		}
		else if (_parentId != null && IsW3CId(_parentId))
		{
			try
			{
				_traceId = ActivityTraceId.CreateFromString(_parentId.AsSpan(3, 32)).ToHexString();
			}
			catch
			{
			}
		}
		return _traceId != null;
	}

	private void TrySetTraceFlagsFromParent()
	{
		if (W3CIdFlagsSet)
		{
			return;
		}
		if (Parent != null)
		{
			ActivityTraceFlags = Parent.ActivityTraceFlags;
		}
		else if (_parentId != null && IsW3CId(_parentId))
		{
			if (System.HexConverter.IsHexLowerChar(_parentId[53]) && System.HexConverter.IsHexLowerChar(_parentId[54]))
			{
				_w3CIdFlags = (byte)(ActivityTraceId.HexByteFromChars(_parentId[53], _parentId[54]) | 0x80u);
			}
			else
			{
				_w3CIdFlags = 128;
			}
		}
	}

	private static void SetCurrent(Activity activity)
	{
		s_current.Value = activity;
	}

	internal static DateTime GetUtcNow()
	{
		return DateTime.UtcNow;
	}

	private static string GenerateRootId()
	{
		Span<char> span = stackalloc char[17 + s_uniqSuffix.Length];
		span[0] = '|';
		int charsWritten;
		bool flag = Interlocked.Increment(ref s_currentRootId).TryFormat(span.Slice(1), out charsWritten, "x");
		s_uniqSuffix.AsSpan().CopyTo(span.Slice(1 + charsWritten));
		return new string(span.Slice(0, 1 + charsWritten + s_uniqSuffix.Length));
	}
}
