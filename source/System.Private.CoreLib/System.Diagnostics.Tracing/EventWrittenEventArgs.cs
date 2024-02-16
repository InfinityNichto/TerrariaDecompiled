using System.Collections.ObjectModel;
using System.Threading;

namespace System.Diagnostics.Tracing;

public class EventWrittenEventArgs : EventArgs
{
	private sealed class MoreEventInfo
	{
		public string Message;

		public string EventName;

		public ReadOnlyCollection<string> PayloadNames;

		public Guid RelatedActivityId;

		public long? OsThreadId;

		public EventTags Tags;

		public EventOpcode Opcode;

		public EventLevel Level;

		public EventKeywords Keywords;
	}

	internal static readonly ReadOnlyCollection<object> EmptyPayload = new ReadOnlyCollection<object>(Array.Empty<object>());

	private Guid _activityId;

	private MoreEventInfo _moreInfo;

	private ref EventSource.EventMetadata Metadata => ref EventSource.m_eventData[EventId];

	public string? EventName
	{
		get
		{
			object obj = _moreInfo?.EventName;
			if (obj == null)
			{
				if (EventId > 0)
				{
					return Metadata.Name;
				}
				obj = null;
			}
			return (string?)obj;
		}
		internal set
		{
			MoreInfo.EventName = value;
		}
	}

	public int EventId { get; }

	public Guid ActivityId
	{
		get
		{
			if (_activityId == Guid.Empty)
			{
				_activityId = System.Diagnostics.Tracing.EventSource.CurrentThreadActivityId;
			}
			return _activityId;
		}
	}

	public Guid RelatedActivityId => _moreInfo?.RelatedActivityId ?? default(Guid);

	public ReadOnlyCollection<object?>? Payload { get; internal set; }

	public ReadOnlyCollection<string>? PayloadNames
	{
		get
		{
			object obj = _moreInfo?.PayloadNames;
			if (obj == null)
			{
				if (EventId > 0)
				{
					return Metadata.ParameterNames;
				}
				obj = null;
			}
			return (ReadOnlyCollection<string>?)obj;
		}
		internal set
		{
			MoreInfo.PayloadNames = value;
		}
	}

	public EventSource EventSource { get; }

	public EventKeywords Keywords
	{
		get
		{
			if (EventId > 0)
			{
				return (EventKeywords)Metadata.Descriptor.Keywords;
			}
			return _moreInfo?.Keywords ?? EventKeywords.None;
		}
		internal set
		{
			MoreInfo.Keywords = value;
		}
	}

	public EventOpcode Opcode
	{
		get
		{
			if (EventId > 0)
			{
				return (EventOpcode)Metadata.Descriptor.Opcode;
			}
			return _moreInfo?.Opcode ?? EventOpcode.Info;
		}
		internal set
		{
			MoreInfo.Opcode = value;
		}
	}

	public EventTask Task
	{
		get
		{
			if (EventId > 0)
			{
				return (EventTask)Metadata.Descriptor.Task;
			}
			return EventTask.None;
		}
	}

	public EventTags Tags
	{
		get
		{
			if (EventId > 0)
			{
				return Metadata.Tags;
			}
			return _moreInfo?.Tags ?? EventTags.None;
		}
		internal set
		{
			MoreInfo.Tags = value;
		}
	}

	public string? Message
	{
		get
		{
			object obj = _moreInfo?.Message;
			if (obj == null)
			{
				if (EventId > 0)
				{
					return Metadata.Message;
				}
				obj = null;
			}
			return (string?)obj;
		}
		internal set
		{
			MoreInfo.Message = value;
		}
	}

	public EventChannel Channel
	{
		get
		{
			if (EventId > 0)
			{
				return (EventChannel)Metadata.Descriptor.Channel;
			}
			return EventChannel.None;
		}
	}

	public byte Version
	{
		get
		{
			if (EventId > 0)
			{
				return Metadata.Descriptor.Version;
			}
			return 0;
		}
	}

	public EventLevel Level
	{
		get
		{
			if (EventId > 0)
			{
				return (EventLevel)Metadata.Descriptor.Level;
			}
			return _moreInfo?.Level ?? EventLevel.LogAlways;
		}
		internal set
		{
			MoreInfo.Level = value;
		}
	}

	public long OSThreadId
	{
		get
		{
			ref long? osThreadId = ref MoreInfo.OsThreadId;
			if (!osThreadId.HasValue)
			{
				osThreadId = (long)Thread.CurrentOSThreadId;
			}
			return osThreadId.Value;
		}
		internal set
		{
			MoreInfo.OsThreadId = value;
		}
	}

	public DateTime TimeStamp { get; internal set; }

	private MoreEventInfo MoreInfo => _moreInfo ?? (_moreInfo = new MoreEventInfo());

	internal EventWrittenEventArgs(EventSource eventSource, int eventId)
	{
		EventSource = eventSource;
		EventId = eventId;
		TimeStamp = DateTime.UtcNow;
	}

	internal unsafe EventWrittenEventArgs(EventSource eventSource, int eventId, Guid* pActivityID, Guid* pChildActivityID)
		: this(eventSource, eventId)
	{
		if (pActivityID != null)
		{
			_activityId = *pActivityID;
		}
		if (pChildActivityID != null)
		{
			MoreInfo.RelatedActivityId = *pChildActivityID;
		}
	}
}
