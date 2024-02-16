using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2113:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate because the nested type OverrideEventProvider's base type EventProvider defines a delegate. This includes Delegate and MulticastDelegate methods which require unreferenced code, but EnsureDescriptorsInitialized does not access these members and is safe to call.")]
[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2115:ReflectionToDynamicallyAccessedMembers", Justification = "EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate because the nested type OverrideEventProvider's base type EventProvider defines a delegate. This includes Delegate and MulticastDelegate methods which have dynamically accessed members requirements, but EnsureDescriptorsInitialized does not access these members and is safe to call.")]
public class EventSource : IDisposable
{
	protected internal struct EventData
	{
		internal ulong m_Ptr;

		internal int m_Size;

		internal int m_Reserved;

		public unsafe IntPtr DataPointer
		{
			get
			{
				return (IntPtr)(void*)m_Ptr;
			}
			set
			{
				m_Ptr = (ulong)(void*)value;
			}
		}

		public int Size
		{
			get
			{
				return m_Size;
			}
			set
			{
				m_Size = value;
			}
		}

		internal int Reserved
		{
			get
			{
				return m_Reserved;
			}
			set
			{
				m_Reserved = value;
			}
		}

		internal unsafe void SetMetadata(byte* pointer, int size, int reserved)
		{
			m_Ptr = (ulong)pointer;
			m_Size = size;
			m_Reserved = reserved;
		}
	}

	private sealed class OverrideEventProvider : EventProvider
	{
		private readonly EventSource m_eventSource;

		private readonly EventProviderType m_eventProviderType;

		public OverrideEventProvider(EventSource eventSource, EventProviderType providerType)
			: base(providerType)
		{
			m_eventSource = eventSource;
			m_eventProviderType = providerType;
		}

		protected override void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments, int perEventSourceSessionId, int etwSessionId)
		{
			EventListener listener = null;
			m_eventSource.SendCommand(listener, m_eventProviderType, perEventSourceSessionId, etwSessionId, (EventCommand)command, IsEnabled(), base.Level, base.MatchAnyKeyword, arguments);
		}
	}

	internal struct EventMetadata
	{
		public EventDescriptor Descriptor;

		public IntPtr EventHandle;

		public EventTags Tags;

		public bool EnabledForAnyListener;

		public bool EnabledForETW;

		public bool EnabledForEventPipe;

		public bool HasRelatedActivityID;

		public string Name;

		public string Message;

		public ParameterInfo[] Parameters;

		public int EventListenerParameterCount;

		public bool AllParametersAreString;

		public bool AllParametersAreInt32;

		public EventActivityOptions ActivityOptions;

		private TraceLoggingEventTypes _traceLoggingEventTypes;

		private ReadOnlyCollection<string> _parameterNames;

		private Type[] _parameterTypes;

		public TraceLoggingEventTypes TraceLoggingEventTypes
		{
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
			[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
			get
			{
				if (_traceLoggingEventTypes == null)
				{
					TraceLoggingEventTypes value = new TraceLoggingEventTypes(Name, Tags, Parameters);
					Interlocked.CompareExchange(ref _traceLoggingEventTypes, value, null);
				}
				return _traceLoggingEventTypes;
			}
		}

		public ReadOnlyCollection<string> ParameterNames
		{
			get
			{
				if (_parameterNames == null)
				{
					ParameterInfo[] parameters = Parameters;
					string[] array = new string[parameters.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = parameters[i].Name;
					}
					_parameterNames = new ReadOnlyCollection<string>(array);
				}
				return _parameterNames;
			}
		}

		public Type[] ParameterTypes
		{
			get
			{
				return _parameterTypes ?? (_parameterTypes = GetParameterTypes(Parameters));
				static Type[] GetParameterTypes(ParameterInfo[] parameters)
				{
					Type[] array = new Type[parameters.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = parameters[i].ParameterType;
					}
					return array;
				}
			}
		}
	}

	private const DynamicallyAccessedMemberTypes ManifestMemberTypes = DynamicallyAccessedMemberTypes.All;

	private string m_name;

	internal int m_id;

	private Guid m_guid;

	internal volatile EventMetadata[] m_eventData;

	private volatile byte[] m_rawManifest;

	private EventHandler<EventCommandEventArgs> m_eventCommandExecuted;

	private readonly EventSourceSettings m_config;

	private bool m_eventSourceDisposed;

	private bool m_eventSourceEnabled;

	internal EventLevel m_level;

	internal EventKeywords m_matchAnyKeyword;

	internal volatile EventDispatcher m_Dispatchers;

	private volatile OverrideEventProvider m_etwProvider;

	private object m_createEventLock;

	private IntPtr m_writeEventStringEventHandle = IntPtr.Zero;

	private volatile OverrideEventProvider m_eventPipeProvider;

	private bool m_completelyInited;

	private Exception m_constructionException;

	private byte m_outOfBandMessageCount;

	private EventCommandEventArgs m_deferredCommands;

	private string[] m_traits;

	[ThreadStatic]
	private static byte m_EventSourceExceptionRecurenceCount;

	internal volatile ulong[] m_channelData;

	private ActivityTracker m_activityTracker;

	internal const string s_ActivityStartSuffix = "Start";

	internal const string s_ActivityStopSuffix = "Stop";

	internal const string DuplicateSourceNamesSwitch = "System.Diagnostics.Tracing.EventSource.AllowDuplicateSourceNames";

	private static readonly bool AllowDuplicateSourceNames = AppContext.TryGetSwitch("System.Diagnostics.Tracing.EventSource.AllowDuplicateSourceNames", out var isEnabled) && isEnabled;

	private byte[] m_providerMetadata;

	private const string EventSourceRequiresUnreferenceMessage = "EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type";

	private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";

	private readonly TraceLoggingEventHandleTable m_eventHandleTable;

	internal static bool IsSupported { get; } = InitializeIsSupported();


	public string Name => m_name;

	public Guid Guid => m_guid;

	public EventSourceSettings Settings => m_config;

	public Exception? ConstructionException => m_constructionException;

	public static Guid CurrentThreadActivityId
	{
		get
		{
			if (!IsSupported)
			{
				return default(Guid);
			}
			Guid ActivityId = default(Guid);
			Interop.Advapi32.EventActivityIdControl(Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID, ref ActivityId);
			return ActivityId;
		}
	}

	private bool IsDisposed => m_eventSourceDisposed;

	private bool ThrowOnEventWriteErrors => (m_config & EventSourceSettings.ThrowOnEventWriteErrors) != 0;

	private bool SelfDescribingEvents => (m_config & EventSourceSettings.EtwSelfDescribingEventFormat) != 0;

	private protected virtual ReadOnlySpan<byte> ProviderMetadata => m_providerMetadata;

	public event EventHandler<EventCommandEventArgs>? EventCommandExecuted
	{
		add
		{
			if (value != null)
			{
				m_eventCommandExecuted = (EventHandler<EventCommandEventArgs>)Delegate.Combine(m_eventCommandExecuted, value);
				for (EventCommandEventArgs eventCommandEventArgs = m_deferredCommands; eventCommandEventArgs != null; eventCommandEventArgs = eventCommandEventArgs.nextCommand)
				{
					value(this, eventCommandEventArgs);
				}
			}
		}
		remove
		{
			m_eventCommandExecuted = (EventHandler<EventCommandEventArgs>)Delegate.Remove(m_eventCommandExecuted, value);
		}
	}

	private static bool InitializeIsSupported()
	{
		if (!AppContext.TryGetSwitch("System.Diagnostics.Tracing.EventSource.IsSupported", out var isEnabled))
		{
			return true;
		}
		return isEnabled;
	}

	public bool IsEnabled()
	{
		return m_eventSourceEnabled;
	}

	public bool IsEnabled(EventLevel level, EventKeywords keywords)
	{
		return IsEnabled(level, keywords, EventChannel.None);
	}

	public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel)
	{
		if (!IsEnabled())
		{
			return false;
		}
		if (!IsEnabledCommon(m_eventSourceEnabled, m_level, m_matchAnyKeyword, level, keywords, channel))
		{
			return false;
		}
		return true;
	}

	public static Guid GetGuid(Type eventSourceType)
	{
		if (eventSourceType == null)
		{
			throw new ArgumentNullException("eventSourceType");
		}
		EventSourceAttribute eventSourceAttribute = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute));
		string name = eventSourceType.Name;
		if (eventSourceAttribute != null)
		{
			if (eventSourceAttribute.Guid != null && Guid.TryParse(eventSourceAttribute.Guid, out var result))
			{
				return result;
			}
			if (eventSourceAttribute.Name != null)
			{
				name = eventSourceAttribute.Name;
			}
		}
		if (name == null)
		{
			throw new ArgumentException(SR.Argument_InvalidTypeName, "eventSourceType");
		}
		return GenerateGuidFromName(name.ToUpperInvariant());
	}

	public static string GetName(Type eventSourceType)
	{
		return GetName(eventSourceType, EventManifestOptions.None);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	public static string? GenerateManifest([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type eventSourceType, string? assemblyPathToIncludeInManifest)
	{
		return GenerateManifest(eventSourceType, assemblyPathToIncludeInManifest, EventManifestOptions.None);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	public static string? GenerateManifest([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type eventSourceType, string? assemblyPathToIncludeInManifest, EventManifestOptions flags)
	{
		if (!IsSupported)
		{
			return null;
		}
		if (eventSourceType == null)
		{
			throw new ArgumentNullException("eventSourceType");
		}
		byte[] array = CreateManifestAndDescriptors(eventSourceType, assemblyPathToIncludeInManifest, null, flags);
		if (array != null)
		{
			return Encoding.UTF8.GetString(array, 0, array.Length);
		}
		return null;
	}

	public static IEnumerable<EventSource> GetSources()
	{
		if (!IsSupported)
		{
			return Array.Empty<EventSource>();
		}
		List<EventSource> list = new List<EventSource>();
		lock (EventListener.EventListenersLock)
		{
			foreach (WeakReference<EventSource> s_EventSource in EventListener.s_EventSources)
			{
				if (s_EventSource.TryGetTarget(out var target) && !target.IsDisposed)
				{
					list.Add(target);
				}
			}
			return list;
		}
	}

	public static void SendCommand(EventSource eventSource, EventCommand command, IDictionary<string, string?>? commandArguments)
	{
		if (IsSupported)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			if (command <= EventCommand.Update && command != EventCommand.SendManifest)
			{
				throw new ArgumentException(SR.EventSource_InvalidCommand, "command");
			}
			eventSource.SendCommand(null, EventProviderType.ETW, 0, 0, command, enable: true, EventLevel.LogAlways, EventKeywords.None, commandArguments);
		}
	}

	public string? GetTrait(string key)
	{
		if (m_traits != null)
		{
			for (int i = 0; i < m_traits.Length - 1; i += 2)
			{
				if (m_traits[i] == key)
				{
					return m_traits[i + 1];
				}
			}
		}
		return null;
	}

	public override string ToString()
	{
		if (!IsSupported)
		{
			return base.ToString();
		}
		return SR.Format(SR.EventSource_ToString, Name, Guid);
	}

	public static void SetCurrentThreadActivityId(Guid activityId)
	{
		if (IsSupported)
		{
			if (TplEventSource.Log != null)
			{
				TplEventSource.Log.SetActivityId(activityId);
			}
			EventPipeEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID, ref activityId);
			Interop.Advapi32.EventActivityIdControl(Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID, ref activityId);
		}
	}

	public static void SetCurrentThreadActivityId(Guid activityId, out Guid oldActivityThatWillContinue)
	{
		if (!IsSupported)
		{
			oldActivityThatWillContinue = default(Guid);
			return;
		}
		oldActivityThatWillContinue = activityId;
		EventPipeEventProvider.EventActivityIdControl(Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID, ref oldActivityThatWillContinue);
		Interop.Advapi32.EventActivityIdControl(Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID, ref oldActivityThatWillContinue);
		if (TplEventSource.Log != null)
		{
			TplEventSource.Log.SetActivityId(activityId);
		}
	}

	protected EventSource()
		: this(EventSourceSettings.EtwManifestEventFormat)
	{
	}

	protected EventSource(bool throwOnEventWriteErrors)
		: this(EventSourceSettings.EtwManifestEventFormat | (throwOnEventWriteErrors ? EventSourceSettings.ThrowOnEventWriteErrors : EventSourceSettings.Default))
	{
	}

	protected EventSource(EventSourceSettings settings)
		: this(settings, (string[]?)null)
	{
	}

	protected EventSource(EventSourceSettings settings, params string[]? traits)
	{
		if (IsSupported)
		{
			m_eventHandleTable = new TraceLoggingEventHandleTable();
			m_config = ValidateSettings(settings);
			Type type = GetType();
			Guid guid = GetGuid(type);
			string name = GetName(type);
			Initialize(guid, name, traits);
		}
	}

	private unsafe void DefineEventPipeEvents()
	{
		if (SelfDescribingEvents)
		{
			return;
		}
		int num = m_eventData.Length;
		for (int i = 0; i < num; i++)
		{
			uint eventId = (uint)m_eventData[i].Descriptor.EventId;
			if (eventId != 0)
			{
				byte[] array = EventPipeMetadataGenerator.Instance.GenerateEventMetadata(m_eventData[i]);
				uint metadataLength = ((array != null) ? ((uint)array.Length) : 0u);
				string name = m_eventData[i].Name;
				long keywords = m_eventData[i].Descriptor.Keywords;
				uint version = m_eventData[i].Descriptor.Version;
				uint level = m_eventData[i].Descriptor.Level;
				fixed (byte* pMetadata = array)
				{
					IntPtr eventHandle = m_eventPipeProvider.m_eventProvider.DefineEventHandle(eventId, name, keywords, version, level, pMetadata, metadataLength);
					m_eventData[i].EventHandle = eventHandle;
				}
			}
		}
	}

	protected virtual void OnEventCommand(EventCommandEventArgs command)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId)
	{
		WriteEventCore(eventId, 0, null);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, int arg1)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[1];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 4;
			ptr->Reserved = 0;
			WriteEventCore(eventId, 1, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, int arg1, int arg2)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[2];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 4;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)(&arg2);
			ptr[1].Size = 4;
			ptr[1].Reserved = 0;
			WriteEventCore(eventId, 2, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[3];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 4;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)(&arg2);
			ptr[1].Size = 4;
			ptr[1].Reserved = 0;
			ptr[2].DataPointer = (IntPtr)(&arg3);
			ptr[2].Size = 4;
			ptr[2].Reserved = 0;
			WriteEventCore(eventId, 3, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, long arg1)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[1];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 8;
			ptr->Reserved = 0;
			WriteEventCore(eventId, 1, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, long arg1, long arg2)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[2];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 8;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)(&arg2);
			ptr[1].Size = 8;
			ptr[1].Reserved = 0;
			WriteEventCore(eventId, 2, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, long arg1, long arg2, long arg3)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[3];
			ptr->DataPointer = (IntPtr)(&arg1);
			ptr->Size = 8;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)(&arg2);
			ptr[1].Size = 8;
			ptr[1].Reserved = 0;
			ptr[2].DataPointer = (IntPtr)(&arg3);
			ptr[2].Size = 8;
			ptr[2].Reserved = 0;
			WriteEventCore(eventId, 3, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1)
	{
		if (IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[1];
				ptr->DataPointer = (IntPtr)ptr2;
				ptr->Size = (arg1.Length + 1) * 2;
				ptr->Reserved = 0;
				WriteEventCore(eventId, 1, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1, string? arg2)
	{
		//The blocks IL_0040 are reachable both inside and outside the pinned region starting at IL_003d. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		fixed (char* ptr3 = arg1)
		{
			char* intPtr;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				EventData* ptr2 = stackalloc EventData[2];
				ptr2->DataPointer = (IntPtr)ptr3;
				ptr2->Size = (arg1.Length + 1) * 2;
				ptr2->Reserved = 0;
				ptr2[1].DataPointer = (IntPtr)ptr;
				ptr2[1].Size = (arg2.Length + 1) * 2;
				ptr2[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr2);
				return;
			}
			fixed (char* ptr4 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr4);
				EventData* ptr2 = stackalloc EventData[2];
				ptr2->DataPointer = (IntPtr)ptr3;
				ptr2->Size = (arg1.Length + 1) * 2;
				ptr2->Reserved = 0;
				ptr2[1].DataPointer = (IntPtr)ptr;
				ptr2[1].Size = (arg2.Length + 1) * 2;
				ptr2[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr2);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3)
	{
		//The blocks IL_004b, IL_004e, IL_0060, IL_0122 are reachable both inside and outside the pinned region starting at IL_0048. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg3 == null)
		{
			arg3 = "";
		}
		fixed (char* ptr5 = arg1)
		{
			char* intPtr;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[3];
					ptr4->DataPointer = (IntPtr)ptr5;
					ptr4->Size = (arg1.Length + 1) * 2;
					ptr4->Reserved = 0;
					ptr4[1].DataPointer = (IntPtr)ptr;
					ptr4[1].Size = (arg2.Length + 1) * 2;
					ptr4[1].Reserved = 0;
					ptr4[2].DataPointer = (IntPtr)ptr3;
					ptr4[2].Size = (arg3.Length + 1) * 2;
					ptr4[2].Reserved = 0;
					WriteEventCore(eventId, 3, ptr4);
				}
				return;
			}
			fixed (char* ptr6 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr6);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[3];
					ptr4->DataPointer = (IntPtr)ptr5;
					ptr4->Size = (arg1.Length + 1) * 2;
					ptr4->Reserved = 0;
					ptr4[1].DataPointer = (IntPtr)ptr;
					ptr4[1].Size = (arg2.Length + 1) * 2;
					ptr4[1].Reserved = 0;
					ptr4[2].DataPointer = (IntPtr)ptr3;
					ptr4[2].Size = (arg3.Length + 1) * 2;
					ptr4[2].Reserved = 0;
					WriteEventCore(eventId, 3, ptr4);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1, int arg2)
	{
		if (IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[2];
				ptr->DataPointer = (IntPtr)ptr2;
				ptr->Size = (arg1.Length + 1) * 2;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)(&arg2);
				ptr[1].Size = 4;
				ptr[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1, int arg2, int arg3)
	{
		if (IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[3];
				ptr->DataPointer = (IntPtr)ptr2;
				ptr->Size = (arg1.Length + 1) * 2;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)(&arg2);
				ptr[1].Size = 4;
				ptr[1].Reserved = 0;
				ptr[2].DataPointer = (IntPtr)(&arg3);
				ptr[2].Size = 4;
				ptr[2].Reserved = 0;
				WriteEventCore(eventId, 3, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, string? arg1, long arg2)
	{
		if (IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[2];
				ptr->DataPointer = (IntPtr)ptr2;
				ptr->Size = (arg1.Length + 1) * 2;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)(&arg2);
				ptr[1].Size = 8;
				ptr[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, long arg1, string? arg2)
	{
		if (IsEnabled())
		{
			if (arg2 == null)
			{
				arg2 = "";
			}
			fixed (char* ptr2 = arg2)
			{
				EventData* ptr = stackalloc EventData[2];
				ptr->DataPointer = (IntPtr)(&arg1);
				ptr->Size = 8;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)ptr2;
				ptr[1].Size = (arg2.Length + 1) * 2;
				ptr[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, int arg1, string? arg2)
	{
		if (IsEnabled())
		{
			if (arg2 == null)
			{
				arg2 = "";
			}
			fixed (char* ptr2 = arg2)
			{
				EventData* ptr = stackalloc EventData[2];
				ptr->DataPointer = (IntPtr)(&arg1);
				ptr->Size = 4;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)ptr2;
				ptr[1].Size = (arg2.Length + 1) * 2;
				ptr[1].Reserved = 0;
				WriteEventCore(eventId, 2, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, byte[]? arg1)
	{
		if (!IsEnabled())
		{
			return;
		}
		EventData* ptr = stackalloc EventData[2];
		if (arg1 == null || arg1.Length == 0)
		{
			int num = 0;
			ptr->DataPointer = (IntPtr)(&num);
			ptr->Size = 4;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)(&num);
			ptr[1].Size = 0;
			ptr[1].Reserved = 0;
			WriteEventCore(eventId, 2, ptr);
			return;
		}
		int size = arg1.Length;
		fixed (byte* ptr2 = &arg1[0])
		{
			ptr->DataPointer = (IntPtr)(&size);
			ptr->Size = 4;
			ptr->Reserved = 0;
			ptr[1].DataPointer = (IntPtr)ptr2;
			ptr[1].Size = size;
			ptr[1].Reserved = 0;
			WriteEventCore(eventId, 2, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	protected unsafe void WriteEvent(int eventId, long arg1, byte[]? arg2)
	{
		if (!IsEnabled())
		{
			return;
		}
		EventData* ptr = stackalloc EventData[3];
		ptr->DataPointer = (IntPtr)(&arg1);
		ptr->Size = 8;
		ptr->Reserved = 0;
		if (arg2 == null || arg2.Length == 0)
		{
			int num = 0;
			ptr[1].DataPointer = (IntPtr)(&num);
			ptr[1].Size = 4;
			ptr[1].Reserved = 0;
			ptr[2].DataPointer = (IntPtr)(&num);
			ptr[2].Size = 0;
			ptr[2].Reserved = 0;
			WriteEventCore(eventId, 3, ptr);
			return;
		}
		int size = arg2.Length;
		fixed (byte* ptr2 = &arg2[0])
		{
			ptr[1].DataPointer = (IntPtr)(&size);
			ptr[1].Size = 4;
			ptr[1].Reserved = 0;
			ptr[2].DataPointer = (IntPtr)ptr2;
			ptr[2].Size = size;
			ptr[2].Reserved = 0;
			WriteEventCore(eventId, 3, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	[CLSCompliant(false)]
	protected unsafe void WriteEventCore(int eventId, int eventDataCount, EventData* data)
	{
		WriteEventWithRelatedActivityIdCore(eventId, null, eventDataCount, data);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	[CLSCompliant(false)]
	protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, Guid* relatedActivityId, int eventDataCount, EventData* data)
	{
		if (!IsEnabled())
		{
			return;
		}
		try
		{
			ref EventMetadata reference = ref m_eventData[eventId];
			EventOpcode opcode = (EventOpcode)reference.Descriptor.Opcode;
			Guid* ptr = null;
			Guid activityId = Guid.Empty;
			Guid relatedActivityId2 = Guid.Empty;
			if (opcode != 0 && relatedActivityId == null && (reference.ActivityOptions & EventActivityOptions.Disable) == 0)
			{
				switch (opcode)
				{
				case EventOpcode.Start:
					m_activityTracker.OnStart(m_name, reference.Name, reference.Descriptor.Task, ref activityId, ref relatedActivityId2, reference.ActivityOptions);
					break;
				case EventOpcode.Stop:
					m_activityTracker.OnStop(m_name, reference.Name, reference.Descriptor.Task, ref activityId);
					break;
				}
				if (activityId != Guid.Empty)
				{
					ptr = &activityId;
				}
				if (relatedActivityId2 != Guid.Empty)
				{
					relatedActivityId = &relatedActivityId2;
				}
			}
			if (!SelfDescribingEvents)
			{
				if (reference.EnabledForETW && !m_etwProvider.WriteEvent(ref reference.Descriptor, reference.EventHandle, ptr, relatedActivityId, eventDataCount, (IntPtr)data))
				{
					ThrowEventSourceException(reference.Name);
				}
				if (reference.EnabledForEventPipe && !m_eventPipeProvider.WriteEvent(ref reference.Descriptor, reference.EventHandle, ptr, relatedActivityId, eventDataCount, (IntPtr)data))
				{
					ThrowEventSourceException(reference.Name);
				}
			}
			else if (reference.EnabledForETW || reference.EnabledForEventPipe)
			{
				EventSourceOptions eventSourceOptions = default(EventSourceOptions);
				eventSourceOptions.Keywords = (EventKeywords)reference.Descriptor.Keywords;
				eventSourceOptions.Level = (EventLevel)reference.Descriptor.Level;
				eventSourceOptions.Opcode = (EventOpcode)reference.Descriptor.Opcode;
				EventSourceOptions options = eventSourceOptions;
				WriteMultiMerge(reference.Name, ref options, reference.TraceLoggingEventTypes, ptr, relatedActivityId, data);
			}
			if (m_Dispatchers != null && reference.EnabledForAnyListener)
			{
				EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this, eventId, ptr, relatedActivityId);
				WriteToAllListeners(eventCallbackArgs, eventDataCount, data);
			}
		}
		catch (Exception ex)
		{
			if (ex is EventSourceException)
			{
				throw;
			}
			ThrowEventSourceException(m_eventData[eventId].Name, ex);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	protected unsafe void WriteEvent(int eventId, params object?[] args)
	{
		WriteEventVarargs(eventId, null, args);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	protected unsafe void WriteEventWithRelatedActivityId(int eventId, Guid relatedActivityId, params object?[] args)
	{
		WriteEventVarargs(eventId, &relatedActivityId, args);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsSupported)
		{
			return;
		}
		if (disposing)
		{
			if (m_eventSourceEnabled)
			{
				try
				{
					SendManifest(m_rawManifest);
				}
				catch
				{
				}
				m_eventSourceEnabled = false;
			}
			if (m_etwProvider != null)
			{
				m_etwProvider.Dispose();
				m_etwProvider = null;
			}
			if (m_eventPipeProvider != null)
			{
				m_eventPipeProvider.Dispose();
				m_eventPipeProvider = null;
			}
		}
		m_eventSourceEnabled = false;
		m_eventSourceDisposed = true;
	}

	~EventSource()
	{
		Dispose(disposing: false);
	}

	private unsafe void WriteEventRaw(string eventName, ref EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityID, Guid* relatedActivityID, int dataCount, IntPtr data)
	{
		bool flag = true;
		flag &= m_etwProvider == null;
		if (m_etwProvider != null && !m_etwProvider.WriteEventRaw(ref eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, data))
		{
			ThrowEventSourceException(eventName);
		}
		flag &= m_eventPipeProvider == null;
		if (m_eventPipeProvider != null && !m_eventPipeProvider.WriteEventRaw(ref eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, data))
		{
			ThrowEventSourceException(eventName);
		}
		if (flag)
		{
			ThrowEventSourceException(eventName);
		}
	}

	internal EventSource(Guid eventSourceGuid, string eventSourceName)
		: this(eventSourceGuid, eventSourceName, EventSourceSettings.EtwManifestEventFormat)
	{
	}

	internal EventSource(Guid eventSourceGuid, string eventSourceName, EventSourceSettings settings, string[] traits = null)
	{
		if (IsSupported)
		{
			m_eventHandleTable = new TraceLoggingEventHandleTable();
			m_config = ValidateSettings(settings);
			Initialize(eventSourceGuid, eventSourceName, traits);
		}
	}

	private unsafe void Initialize(Guid eventSourceGuid, string eventSourceName, string[] traits)
	{
		try
		{
			m_traits = traits;
			if (m_traits != null && m_traits.Length % 2 != 0)
			{
				throw new ArgumentException(SR.EventSource_TraitEven, "traits");
			}
			if (eventSourceGuid == Guid.Empty)
			{
				throw new ArgumentException(SR.EventSource_NeedGuid);
			}
			if (eventSourceName == null)
			{
				throw new ArgumentException(SR.EventSource_NeedName);
			}
			m_name = eventSourceName;
			m_guid = eventSourceGuid;
			m_activityTracker = ActivityTracker.Instance;
			if (ProviderMetadata.Length == 0)
			{
				InitializeProviderMetadata();
			}
			OverrideEventProvider overrideEventProvider = new OverrideEventProvider(this, EventProviderType.ETW);
			overrideEventProvider.Register(this);
			OverrideEventProvider overrideEventProvider2 = new OverrideEventProvider(this, EventProviderType.EventPipe);
			lock (EventListener.EventListenersLock)
			{
				overrideEventProvider2.Register(this);
			}
			EventListener.AddEventSource(this);
			m_etwProvider = overrideEventProvider;
			if (Name != "System.Diagnostics.Eventing.FrameworkEventSource" || Environment.IsWindows8OrAbove)
			{
				ReadOnlySpan<byte> providerMetadata = ProviderMetadata;
				fixed (byte* data = providerMetadata)
				{
					m_etwProvider.SetInformation(Interop.Advapi32.EVENT_INFO_CLASS.SetTraits, data, (uint)providerMetadata.Length);
				}
			}
			m_eventPipeProvider = overrideEventProvider2;
			m_completelyInited = true;
		}
		catch (Exception ex)
		{
			if (m_constructionException == null)
			{
				m_constructionException = ex;
			}
			ReportOutOfBandMessage("ERROR: Exception during construction of EventSource " + Name + ": " + ex.Message);
		}
		lock (EventListener.EventListenersLock)
		{
			for (EventCommandEventArgs eventCommandEventArgs = m_deferredCommands; eventCommandEventArgs != null; eventCommandEventArgs = eventCommandEventArgs.nextCommand)
			{
				DoCommand(eventCommandEventArgs);
			}
		}
	}

	private static string GetName(Type eventSourceType, EventManifestOptions flags)
	{
		if (eventSourceType == null)
		{
			throw new ArgumentNullException("eventSourceType");
		}
		EventSourceAttribute eventSourceAttribute = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute), flags);
		if (eventSourceAttribute != null && eventSourceAttribute.Name != null)
		{
			return eventSourceAttribute.Name;
		}
		return eventSourceType.Name;
	}

	private static Guid GenerateGuidFromName(string name)
	{
		ReadOnlySpan<byte> input = new byte[16]
		{
			72, 44, 45, 178, 195, 144, 71, 200, 135, 248,
			26, 21, 191, 193, 48, 251
		};
		byte[] array = Encoding.BigEndianUnicode.GetBytes(name);
		Sha1ForNonSecretPurposes sha1ForNonSecretPurposes = default(Sha1ForNonSecretPurposes);
		sha1ForNonSecretPurposes.Start();
		sha1ForNonSecretPurposes.Append(input);
		sha1ForNonSecretPurposes.Append(array);
		Array.Resize(ref array, 16);
		sha1ForNonSecretPurposes.Finish(array);
		array[7] = (byte)((array[7] & 0xFu) | 0x50u);
		return new Guid(array);
	}

	private unsafe static void DecodeObjects(object[] decodedObjects, Type[] parameterTypes, EventData* data)
	{
		object obj;
		for (int i = 0; i < decodedObjects.Length; decodedObjects[i] = obj, i++, data++)
		{
			IntPtr dataPointer = data->DataPointer;
			Type type = parameterTypes[i];
			if (!(type == typeof(string)))
			{
				if (type == typeof(int))
				{
					obj = *(int*)(void*)dataPointer;
					continue;
				}
				TypeCode typeCode = Type.GetTypeCode(type);
				int size = data->Size;
				if (size == 4)
				{
					if ((uint)(typeCode - 5) <= 4u)
					{
						obj = *(int*)(void*)dataPointer;
						continue;
					}
					if (typeCode == TypeCode.UInt32)
					{
						obj = *(uint*)(void*)dataPointer;
						continue;
					}
					if (typeCode == TypeCode.Single)
					{
						obj = *(float*)(void*)dataPointer;
						continue;
					}
					if (typeCode == TypeCode.Boolean)
					{
						obj = *(int*)(void*)dataPointer == 1;
						continue;
					}
					if (type == typeof(byte[]))
					{
						data++;
						goto IL_0256;
					}
					if (IntPtr.Size != 4)
					{
						goto IL_0244;
					}
				}
				if (size <= 2)
				{
					switch (typeCode)
					{
					case TypeCode.Byte:
						obj = *(byte*)(void*)dataPointer;
						continue;
					case TypeCode.SByte:
						obj = *(sbyte*)(void*)dataPointer;
						continue;
					case TypeCode.Int16:
						obj = *(short*)(void*)dataPointer;
						continue;
					case TypeCode.UInt16:
						obj = *(ushort*)(void*)dataPointer;
						continue;
					case TypeCode.Char:
						obj = *(char*)(void*)dataPointer;
						continue;
					}
				}
				else if (size == 8)
				{
					switch (typeCode)
					{
					case TypeCode.Int64:
						obj = *(long*)(void*)dataPointer;
						continue;
					case TypeCode.UInt64:
						obj = *(ulong*)(void*)dataPointer;
						continue;
					case TypeCode.Double:
						obj = *(double*)(void*)dataPointer;
						continue;
					case TypeCode.DateTime:
						obj = *(DateTime*)(void*)dataPointer;
						continue;
					}
					_ = IntPtr.Size;
					if (type == typeof(IntPtr))
					{
						obj = *(IntPtr*)(void*)dataPointer;
						continue;
					}
				}
				else
				{
					if (typeCode == TypeCode.Decimal)
					{
						obj = *(decimal*)(void*)dataPointer;
						continue;
					}
					if (type == typeof(Guid))
					{
						obj = *(Guid*)(void*)dataPointer;
						continue;
					}
				}
				goto IL_0244;
			}
			goto IL_028a;
			IL_0256:
			if (data->Size == 0)
			{
				obj = Array.Empty<byte>();
				continue;
			}
			byte[] array = new byte[data->Size];
			Marshal.Copy(data->DataPointer, array, 0, array.Length);
			obj = array;
			continue;
			IL_0244:
			if (!(type != typeof(byte*)))
			{
				goto IL_0256;
			}
			goto IL_028a;
			IL_028a:
			obj = ((dataPointer == IntPtr.Zero) ? null : new string((char*)(void*)dataPointer, 0, (data->Size >> 1) - 1));
		}
	}

	[Conditional("DEBUG")]
	private unsafe static void AssertValidString(EventData* data)
	{
		char* ptr = (char*)(void*)data->DataPointer;
		int num = data->Size / 2 - 1;
		for (int i = 0; i < num; i++)
		{
		}
	}

	private EventDispatcher GetDispatcher(EventListener listener)
	{
		EventDispatcher eventDispatcher;
		for (eventDispatcher = m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
		{
			if (eventDispatcher.m_Listener == listener)
			{
				return eventDispatcher;
			}
		}
		return eventDispatcher;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	private unsafe void WriteEventVarargs(int eventId, Guid* childActivityID, object[] args)
	{
		if (!IsEnabled())
		{
			return;
		}
		try
		{
			ref EventMetadata reference = ref m_eventData[eventId];
			if (childActivityID != null && !reference.HasRelatedActivityID)
			{
				throw new ArgumentException(SR.EventSource_NoRelatedActivityId);
			}
			LogEventArgsMismatches(eventId, args);
			Guid* ptr = null;
			Guid activityId = Guid.Empty;
			Guid relatedActivityId = Guid.Empty;
			EventOpcode opcode = (EventOpcode)reference.Descriptor.Opcode;
			EventActivityOptions activityOptions = reference.ActivityOptions;
			if (childActivityID == null && (activityOptions & EventActivityOptions.Disable) == 0)
			{
				switch (opcode)
				{
				case EventOpcode.Start:
					m_activityTracker.OnStart(m_name, reference.Name, reference.Descriptor.Task, ref activityId, ref relatedActivityId, reference.ActivityOptions);
					break;
				case EventOpcode.Stop:
					m_activityTracker.OnStop(m_name, reference.Name, reference.Descriptor.Task, ref activityId);
					break;
				}
				if (activityId != Guid.Empty)
				{
					ptr = &activityId;
				}
				if (relatedActivityId != Guid.Empty)
				{
					childActivityID = &relatedActivityId;
				}
			}
			if (reference.EnabledForETW || reference.EnabledForEventPipe)
			{
				if (!SelfDescribingEvents)
				{
					if (!m_etwProvider.WriteEvent(ref reference.Descriptor, reference.EventHandle, ptr, childActivityID, args))
					{
						ThrowEventSourceException(reference.Name);
					}
					if (!m_eventPipeProvider.WriteEvent(ref reference.Descriptor, reference.EventHandle, ptr, childActivityID, args))
					{
						ThrowEventSourceException(reference.Name);
					}
				}
				else
				{
					EventSourceOptions eventSourceOptions = default(EventSourceOptions);
					eventSourceOptions.Keywords = (EventKeywords)reference.Descriptor.Keywords;
					eventSourceOptions.Level = (EventLevel)reference.Descriptor.Level;
					eventSourceOptions.Opcode = (EventOpcode)reference.Descriptor.Opcode;
					EventSourceOptions options = eventSourceOptions;
					WriteMultiMerge(reference.Name, ref options, reference.TraceLoggingEventTypes, ptr, childActivityID, args);
				}
			}
			if (m_Dispatchers != null && reference.EnabledForAnyListener)
			{
				if (!LocalAppContextSwitches.PreserveEventListnerObjectIdentity)
				{
					args = SerializeEventArgs(eventId, args);
				}
				EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this, eventId, ptr, childActivityID)
				{
					Payload = new ReadOnlyCollection<object>(args)
				};
				DispatchToAllListeners(eventCallbackArgs);
			}
		}
		catch (Exception ex)
		{
			if (ex is EventSourceException)
			{
				throw;
			}
			ThrowEventSourceException(m_eventData[eventId].Name, ex);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	private object[] SerializeEventArgs(int eventId, object[] args)
	{
		TraceLoggingEventTypes traceLoggingEventTypes = m_eventData[eventId].TraceLoggingEventTypes;
		int num = Math.Min(traceLoggingEventTypes.typeInfos.Length, args.Length);
		object[] array = new object[traceLoggingEventTypes.typeInfos.Length];
		for (int i = 0; i < num; i++)
		{
			array[i] = traceLoggingEventTypes.typeInfos[i].GetData(args[i]);
		}
		return array;
	}

	private void LogEventArgsMismatches(int eventId, object[] args)
	{
		ParameterInfo[] parameters = m_eventData[eventId].Parameters;
		if (args.Length != parameters.Length)
		{
			ReportOutOfBandMessage(SR.Format(SR.EventSource_EventParametersMismatch, eventId, args.Length, parameters.Length));
			return;
		}
		for (int i = 0; i < args.Length; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			object obj = args[i];
			if ((obj != null && !parameterType.IsAssignableFrom(obj.GetType())) || (obj == null && parameterType.IsValueType && (!parameterType.IsGenericType || !(parameterType.GetGenericTypeDefinition() == typeof(Nullable<>)))))
			{
				ReportOutOfBandMessage(SR.Format(SR.EventSource_VarArgsParameterMismatch, eventId, parameters[i].Name));
				break;
			}
		}
	}

	private unsafe void WriteToAllListeners(EventWrittenEventArgs eventCallbackArgs, int eventDataCount, EventData* data)
	{
		ref EventMetadata reference = ref m_eventData[eventCallbackArgs.EventId];
		if (eventDataCount != reference.EventListenerParameterCount)
		{
			ReportOutOfBandMessage(SR.Format(SR.EventSource_EventParametersMismatch, eventCallbackArgs.EventId, eventDataCount, reference.Parameters.Length));
		}
		if (eventDataCount == 0)
		{
			eventCallbackArgs.Payload = EventWrittenEventArgs.EmptyPayload;
		}
		else
		{
			object[] array = new object[Math.Min(eventDataCount, reference.Parameters.Length)];
			if (reference.AllParametersAreString)
			{
				int num = 0;
				while (num < array.Length)
				{
					IntPtr dataPointer = data->DataPointer;
					array[num] = ((dataPointer == IntPtr.Zero) ? null : new string((char*)(void*)dataPointer, 0, (data->Size >> 1) - 1));
					num++;
					data++;
				}
			}
			else if (reference.AllParametersAreInt32)
			{
				int num2 = 0;
				while (num2 < array.Length)
				{
					array[num2] = *(int*)(void*)data->DataPointer;
					num2++;
					data++;
				}
			}
			else
			{
				DecodeObjects(array, reference.ParameterTypes, data);
			}
			eventCallbackArgs.Payload = new ReadOnlyCollection<object>(array);
		}
		DispatchToAllListeners(eventCallbackArgs);
	}

	internal void DispatchToAllListeners(EventWrittenEventArgs eventCallbackArgs)
	{
		int eventId = eventCallbackArgs.EventId;
		Exception ex = null;
		for (EventDispatcher eventDispatcher = m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
		{
			if (eventId == -1 || eventDispatcher.m_EventEnabled[eventId])
			{
				try
				{
					eventDispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
				}
				catch (Exception ex2)
				{
					ReportOutOfBandMessage("ERROR: Exception during EventSource.OnEventWritten: " + ex2.Message);
					ex = ex2;
				}
			}
		}
		if (ex != null && ThrowOnEventWriteErrors)
		{
			throw new EventSourceException(ex);
		}
	}

	private unsafe void WriteEventString(string msgString)
	{
		bool flag = true;
		flag &= m_etwProvider == null;
		if (flag & (m_eventPipeProvider == null))
		{
			return;
		}
		EventLevel eventLevel = EventLevel.LogAlways;
		long keywords = -1L;
		if (SelfDescribingEvents)
		{
			EventSourceOptions eventSourceOptions = default(EventSourceOptions);
			eventSourceOptions.Keywords = (EventKeywords)keywords;
			eventSourceOptions.Level = eventLevel;
			EventSourceOptions options = eventSourceOptions;
			TraceLoggingEventTypes eventTypes = GetTrimSafeTraceLoggingEventTypes();
			WriteMultiMergeInner("EventSourceMessage", ref options, eventTypes, null, null, msgString);
			return;
		}
		if (m_rawManifest == null && m_outOfBandMessageCount == 1)
		{
			ManifestBuilder manifestBuilder = new ManifestBuilder(Name, Guid, Name, null, EventManifestOptions.None);
			manifestBuilder.StartEvent("EventSourceMessage", new EventAttribute(0)
			{
				Level = eventLevel,
				Task = (EventTask)65534
			});
			manifestBuilder.AddEventParameter(typeof(string), "message");
			manifestBuilder.EndEvent();
			SendManifest(manifestBuilder.CreateManifest());
		}
		fixed (char* ptr = msgString)
		{
			EventDescriptor eventDescriptor = new EventDescriptor(0, 0, 0, (byte)eventLevel, 0, 0, keywords);
			EventProvider.EventData eventData = default(EventProvider.EventData);
			eventData.Ptr = (ulong)ptr;
			eventData.Size = (uint)(2 * (msgString.Length + 1));
			eventData.Reserved = 0u;
			if (m_etwProvider != null)
			{
				m_etwProvider.WriteEvent(ref eventDescriptor, IntPtr.Zero, null, null, 1, (IntPtr)(&eventData));
			}
			if (m_eventPipeProvider == null)
			{
				return;
			}
			if (m_writeEventStringEventHandle == IntPtr.Zero)
			{
				if (m_createEventLock == null)
				{
					Interlocked.CompareExchange(ref m_createEventLock, new object(), null);
				}
				lock (m_createEventLock)
				{
					if (m_writeEventStringEventHandle == IntPtr.Zero)
					{
						string eventName = "EventSourceMessage";
						EventParameterInfo eventParameterInfo = default(EventParameterInfo);
						eventParameterInfo.SetInfo("message", typeof(string));
						byte[] array = EventPipeMetadataGenerator.Instance.GenerateMetadata(0, eventName, keywords, (uint)eventLevel, 0u, EventOpcode.Info, new EventParameterInfo[1] { eventParameterInfo });
						uint metadataLength = ((array != null) ? ((uint)array.Length) : 0u);
						fixed (byte* pMetadata = array)
						{
							m_writeEventStringEventHandle = m_eventPipeProvider.m_eventProvider.DefineEventHandle(0u, eventName, keywords, 0u, (uint)eventLevel, pMetadata, metadataLength);
						}
					}
				}
			}
			m_eventPipeProvider.WriteEvent(ref eventDescriptor, m_writeEventStringEventHandle, null, null, 1, (IntPtr)(&eventData));
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The call to TraceLoggingEventTypes with the below parameter values are trim safe")]
		static TraceLoggingEventTypes GetTrimSafeTraceLoggingEventTypes()
		{
			return new TraceLoggingEventTypes("EventSourceMessage", EventTags.None, typeof(string));
		}
	}

	private void WriteStringToAllListeners(string eventName, string msg)
	{
		EventWrittenEventArgs eventWrittenEventArgs = new EventWrittenEventArgs(this, 0);
		eventWrittenEventArgs.EventName = eventName;
		eventWrittenEventArgs.Message = msg;
		eventWrittenEventArgs.Payload = new ReadOnlyCollection<object>(new object[1] { msg });
		eventWrittenEventArgs.PayloadNames = new ReadOnlyCollection<string>(new string[1] { "message" });
		EventWrittenEventArgs eventData = eventWrittenEventArgs;
		for (EventDispatcher eventDispatcher = m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
		{
			bool flag = false;
			if (eventDispatcher.m_EventEnabled == null)
			{
				flag = true;
			}
			else
			{
				for (int i = 0; i < eventDispatcher.m_EventEnabled.Length; i++)
				{
					if (eventDispatcher.m_EventEnabled[i])
					{
						flag = true;
						break;
					}
				}
			}
			try
			{
				if (flag)
				{
					eventDispatcher.m_Listener.OnEventWritten(eventData);
				}
			}
			catch
			{
			}
		}
	}

	private bool IsEnabledByDefault(int eventNum, bool enable, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword)
	{
		if (!enable)
		{
			return false;
		}
		EventLevel level = (EventLevel)m_eventData[eventNum].Descriptor.Level;
		EventKeywords eventKeywords = (EventKeywords)(m_eventData[eventNum].Descriptor.Keywords & (long)(~SessionMask.All.ToEventKeywords()));
		EventChannel channel = (EventChannel)m_eventData[eventNum].Descriptor.Channel;
		return IsEnabledCommon(enable, currentLevel, currentMatchAnyKeyword, level, eventKeywords, channel);
	}

	private bool IsEnabledCommon(bool enabled, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword, EventLevel eventLevel, EventKeywords eventKeywords, EventChannel eventChannel)
	{
		if (!enabled)
		{
			return false;
		}
		if (currentLevel != 0 && currentLevel < eventLevel)
		{
			return false;
		}
		if (currentMatchAnyKeyword != EventKeywords.None && eventKeywords != EventKeywords.None)
		{
			if (eventChannel != 0 && m_channelData != null && m_channelData.Length > (int)eventChannel)
			{
				EventKeywords eventKeywords2 = (EventKeywords)((long)m_channelData[(uint)eventChannel] | (long)eventKeywords);
				if (eventKeywords2 != EventKeywords.None && (eventKeywords2 & currentMatchAnyKeyword) == EventKeywords.None)
				{
					return false;
				}
			}
			else if ((eventKeywords & currentMatchAnyKeyword) == EventKeywords.None)
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ThrowEventSourceException(string eventName, Exception innerEx = null)
	{
		if (m_EventSourceExceptionRecurenceCount > 0)
		{
			return;
		}
		try
		{
			m_EventSourceExceptionRecurenceCount++;
			string text = "EventSourceException";
			if (eventName != null)
			{
				text = text + " while processing event \"" + eventName + "\"";
			}
			switch (EventProvider.GetLastWriteEventError())
			{
			case EventProvider.WriteEventErrorCode.EventTooBig:
				ReportOutOfBandMessage(text + ": " + SR.EventSource_EventTooBig);
				if (ThrowOnEventWriteErrors)
				{
					throw new EventSourceException(SR.EventSource_EventTooBig, innerEx);
				}
				return;
			case EventProvider.WriteEventErrorCode.NoFreeBuffers:
				ReportOutOfBandMessage(text + ": " + SR.EventSource_NoFreeBuffers);
				if (ThrowOnEventWriteErrors)
				{
					throw new EventSourceException(SR.EventSource_NoFreeBuffers, innerEx);
				}
				return;
			case EventProvider.WriteEventErrorCode.NullInput:
				ReportOutOfBandMessage(text + ": " + SR.EventSource_NullInput);
				if (ThrowOnEventWriteErrors)
				{
					throw new EventSourceException(SR.EventSource_NullInput, innerEx);
				}
				return;
			case EventProvider.WriteEventErrorCode.TooManyArgs:
				ReportOutOfBandMessage(text + ": " + SR.EventSource_TooManyArgs);
				if (ThrowOnEventWriteErrors)
				{
					throw new EventSourceException(SR.EventSource_TooManyArgs, innerEx);
				}
				return;
			}
			if (innerEx != null)
			{
				innerEx = innerEx.GetBaseException();
				ReportOutOfBandMessage(text + ": " + innerEx.GetType()?.ToString() + ":" + innerEx.Message);
			}
			else
			{
				ReportOutOfBandMessage(text);
			}
			if (ThrowOnEventWriteErrors)
			{
				throw new EventSourceException(innerEx);
			}
		}
		finally
		{
			m_EventSourceExceptionRecurenceCount--;
		}
	}

	internal static EventOpcode GetOpcodeWithDefault(EventOpcode opcode, string eventName)
	{
		if (opcode == EventOpcode.Info && eventName != null)
		{
			if (eventName.EndsWith("Start", StringComparison.Ordinal))
			{
				return EventOpcode.Start;
			}
			if (eventName.EndsWith("Stop", StringComparison.Ordinal))
			{
				return EventOpcode.Stop;
			}
		}
		return opcode;
	}

	internal void SendCommand(EventListener listener, EventProviderType eventProviderType, int perEventSourceSessionId, int etwSessionId, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> commandArguments)
	{
		if (!IsSupported)
		{
			return;
		}
		EventCommandEventArgs eventCommandEventArgs = new EventCommandEventArgs(command, commandArguments, this, listener, eventProviderType, perEventSourceSessionId, etwSessionId, enable, level, matchAnyKeyword);
		lock (EventListener.EventListenersLock)
		{
			if (m_completelyInited)
			{
				m_deferredCommands = null;
				DoCommand(eventCommandEventArgs);
				return;
			}
			if (m_deferredCommands == null)
			{
				m_deferredCommands = eventCommandEventArgs;
				return;
			}
			EventCommandEventArgs eventCommandEventArgs2 = m_deferredCommands;
			while (eventCommandEventArgs2.nextCommand != null)
			{
				eventCommandEventArgs2 = eventCommandEventArgs2.nextCommand;
			}
			eventCommandEventArgs2.nextCommand = eventCommandEventArgs;
		}
	}

	internal void DoCommand(EventCommandEventArgs commandArgs)
	{
		if (!IsSupported || m_etwProvider == null || m_eventPipeProvider == null)
		{
			return;
		}
		m_outOfBandMessageCount = 0;
		try
		{
			EnsureDescriptorsInitialized();
			commandArgs.dispatcher = GetDispatcher(commandArgs.listener);
			if (commandArgs.dispatcher == null && commandArgs.listener != null)
			{
				throw new ArgumentException(SR.EventSource_ListenerNotFound);
			}
			if (commandArgs.Arguments == null)
			{
				IDictionary<string, string> dictionary2 = (commandArgs.Arguments = new Dictionary<string, string>());
			}
			if (commandArgs.Command == EventCommand.Update)
			{
				for (int i = 0; i < m_eventData.Length; i++)
				{
					EnableEventForDispatcher(commandArgs.dispatcher, commandArgs.eventProviderType, i, IsEnabledByDefault(i, commandArgs.enable, commandArgs.level, commandArgs.matchAnyKeyword));
				}
				if (commandArgs.enable)
				{
					if (!m_eventSourceEnabled)
					{
						m_level = commandArgs.level;
						m_matchAnyKeyword = commandArgs.matchAnyKeyword;
					}
					else
					{
						if (commandArgs.level > m_level)
						{
							m_level = commandArgs.level;
						}
						if (commandArgs.matchAnyKeyword == EventKeywords.None)
						{
							m_matchAnyKeyword = EventKeywords.None;
						}
						else if (m_matchAnyKeyword != EventKeywords.None)
						{
							m_matchAnyKeyword |= commandArgs.matchAnyKeyword;
						}
					}
				}
				bool flag = commandArgs.perEventSourceSessionId >= 0;
				if (commandArgs.perEventSourceSessionId == 0 && !commandArgs.enable)
				{
					flag = false;
				}
				if (commandArgs.listener == null)
				{
					if (!flag)
					{
						commandArgs.perEventSourceSessionId = -commandArgs.perEventSourceSessionId;
					}
					commandArgs.perEventSourceSessionId--;
				}
				commandArgs.Command = (flag ? EventCommand.Enable : EventCommand.Disable);
				if (flag && commandArgs.dispatcher == null && !SelfDescribingEvents)
				{
					SendManifest(m_rawManifest);
				}
				if (commandArgs.enable)
				{
					m_eventSourceEnabled = true;
				}
				OnEventCommand(commandArgs);
				m_eventCommandExecuted?.Invoke(this, commandArgs);
				if (commandArgs.enable)
				{
					return;
				}
				for (int j = 0; j < m_eventData.Length; j++)
				{
					bool enabledForAnyListener = false;
					for (EventDispatcher eventDispatcher = m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
					{
						if (eventDispatcher.m_EventEnabled[j])
						{
							enabledForAnyListener = true;
							break;
						}
					}
					m_eventData[j].EnabledForAnyListener = enabledForAnyListener;
				}
				if (!AnyEventEnabled())
				{
					m_level = EventLevel.LogAlways;
					m_matchAnyKeyword = EventKeywords.None;
					m_eventSourceEnabled = false;
				}
			}
			else
			{
				if (commandArgs.Command == EventCommand.SendManifest && m_rawManifest != null)
				{
					SendManifest(m_rawManifest);
				}
				OnEventCommand(commandArgs);
				m_eventCommandExecuted?.Invoke(this, commandArgs);
			}
		}
		catch (Exception ex)
		{
			ReportOutOfBandMessage("ERROR: Exception in Command Processing for EventSource " + Name + ": " + ex.Message);
		}
	}

	internal bool EnableEventForDispatcher(EventDispatcher dispatcher, EventProviderType eventProviderType, int eventId, bool value)
	{
		if (!IsSupported)
		{
			return false;
		}
		if (dispatcher == null)
		{
			if (eventId >= m_eventData.Length)
			{
				return false;
			}
			if (m_etwProvider != null && eventProviderType == EventProviderType.ETW)
			{
				m_eventData[eventId].EnabledForETW = value;
			}
			if (m_eventPipeProvider != null && eventProviderType == EventProviderType.EventPipe)
			{
				m_eventData[eventId].EnabledForEventPipe = value;
			}
		}
		else
		{
			if (eventId >= dispatcher.m_EventEnabled.Length)
			{
				return false;
			}
			dispatcher.m_EventEnabled[eventId] = value;
			if (value)
			{
				m_eventData[eventId].EnabledForAnyListener = true;
			}
		}
		return true;
	}

	private bool AnyEventEnabled()
	{
		for (int i = 0; i < m_eventData.Length; i++)
		{
			if (m_eventData[i].EnabledForETW || m_eventData[i].EnabledForAnyListener || m_eventData[i].EnabledForEventPipe)
			{
				return true;
			}
		}
		return false;
	}

	private void EnsureDescriptorsInitialized()
	{
		if (m_eventData != null)
		{
			return;
		}
		m_rawManifest = CreateManifestAndDescriptors(GetType(), Name, this);
		if (!AllowDuplicateSourceNames)
		{
			foreach (WeakReference<EventSource> s_EventSource in EventListener.s_EventSources)
			{
				if (s_EventSource.TryGetTarget(out var target) && target.Guid == m_guid && !target.IsDisposed && target != this)
				{
					throw new ArgumentException(SR.Format(SR.EventSource_EventSourceGuidInUse, m_guid));
				}
			}
		}
		for (EventDispatcher eventDispatcher = m_Dispatchers; eventDispatcher != null; eventDispatcher = eventDispatcher.m_Next)
		{
			EventDispatcher eventDispatcher2 = eventDispatcher;
			if (eventDispatcher2.m_EventEnabled == null)
			{
				eventDispatcher2.m_EventEnabled = new bool[m_eventData.Length];
			}
		}
		DefineEventPipeEvents();
	}

	private unsafe void SendManifest(byte[] rawManifest)
	{
		if (rawManifest == null)
		{
			return;
		}
		fixed (byte* ptr2 = rawManifest)
		{
			EventDescriptor eventDescriptor = new EventDescriptor(65534, 1, 0, 0, 254, 65534, 72057594037927935L);
			ManifestEnvelope manifestEnvelope = default(ManifestEnvelope);
			manifestEnvelope.Format = ManifestEnvelope.ManifestFormats.SimpleXmlFormat;
			manifestEnvelope.MajorVersion = 1;
			manifestEnvelope.MinorVersion = 0;
			manifestEnvelope.Magic = 91;
			int num = rawManifest.Length;
			manifestEnvelope.ChunkNumber = 0;
			EventProvider.EventData* ptr = stackalloc EventProvider.EventData[2];
			ptr->Ptr = (ulong)(&manifestEnvelope);
			ptr->Size = (uint)sizeof(ManifestEnvelope);
			ptr->Reserved = 0u;
			ptr[1].Ptr = (ulong)ptr2;
			ptr[1].Reserved = 0u;
			int num2 = 65280;
			while (true)
			{
				IL_00c7:
				manifestEnvelope.TotalChunks = (ushort)((num + (num2 - 1)) / num2);
				while (num > 0)
				{
					ptr[1].Size = (uint)Math.Min(num, num2);
					if (m_etwProvider != null && !m_etwProvider.WriteEvent(ref eventDescriptor, IntPtr.Zero, null, null, 2, (IntPtr)ptr))
					{
						if (EventProvider.GetLastWriteEventError() == EventProvider.WriteEventErrorCode.EventTooBig && manifestEnvelope.ChunkNumber == 0 && num2 > 256)
						{
							num2 /= 2;
							goto IL_00c7;
						}
						if (ThrowOnEventWriteErrors)
						{
							ThrowEventSourceException("SendManifest");
						}
						break;
					}
					num -= num2;
					ptr[1].Ptr += (uint)num2;
					manifestEnvelope.ChunkNumber++;
					if (manifestEnvelope.ChunkNumber % 5 == 0)
					{
						Thread.Sleep(15);
					}
				}
				break;
			}
		}
	}

	internal static bool IsCustomAttributeDefinedHelper(MemberInfo member, Type attributeType, EventManifestOptions flags = EventManifestOptions.None)
	{
		if (!member.Module.Assembly.ReflectionOnly && (flags & EventManifestOptions.AllowEventSourceOverride) == 0)
		{
			return member.IsDefined(attributeType, inherit: false);
		}
		foreach (CustomAttributeData customAttribute in CustomAttributeData.GetCustomAttributes(member))
		{
			if (AttributeTypeNamesMatch(attributeType, customAttribute.Constructor.ReflectedType))
			{
				return true;
			}
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	internal static Attribute GetCustomAttributeHelper(MemberInfo member, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type attributeType, EventManifestOptions flags = EventManifestOptions.None)
	{
		if (!member.Module.Assembly.ReflectionOnly && (flags & EventManifestOptions.AllowEventSourceOverride) == 0)
		{
			return member.GetCustomAttribute(attributeType, inherit: false);
		}
		foreach (CustomAttributeData customAttribute in CustomAttributeData.GetCustomAttributes(member))
		{
			if (!AttributeTypeNamesMatch(attributeType, customAttribute.Constructor.ReflectedType))
			{
				continue;
			}
			Attribute attribute = null;
			if (customAttribute.ConstructorArguments.Count == 1)
			{
				attribute = (Attribute)Activator.CreateInstance(attributeType, customAttribute.ConstructorArguments[0].Value);
			}
			else if (customAttribute.ConstructorArguments.Count == 0)
			{
				attribute = (Attribute)Activator.CreateInstance(attributeType);
			}
			if (attribute == null)
			{
				continue;
			}
			foreach (CustomAttributeNamedArgument namedArgument in customAttribute.NamedArguments)
			{
				PropertyInfo property = attributeType.GetProperty(namedArgument.MemberInfo.Name, BindingFlags.Instance | BindingFlags.Public);
				object obj = namedArgument.TypedValue.Value;
				if (property.PropertyType.IsEnum)
				{
					string value = obj.ToString();
					obj = Enum.Parse(property.PropertyType, value);
				}
				property.SetValue(attribute, obj, null);
			}
			return attribute;
		}
		return null;
	}

	private static bool AttributeTypeNamesMatch(Type attributeType, Type reflectedAttributeType)
	{
		if (!(attributeType == reflectedAttributeType) && !string.Equals(attributeType.FullName, reflectedAttributeType.FullName, StringComparison.Ordinal))
		{
			if (string.Equals(attributeType.Name, reflectedAttributeType.Name, StringComparison.Ordinal) && attributeType.Namespace.EndsWith("Diagnostics.Tracing", StringComparison.Ordinal))
			{
				return reflectedAttributeType.Namespace.EndsWith("Diagnostics.Tracing", StringComparison.Ordinal);
			}
			return false;
		}
		return true;
	}

	private static Type GetEventSourceBaseType(Type eventSourceType, bool allowEventSourceOverride, bool reflectionOnly)
	{
		Type type = eventSourceType;
		if (type.BaseType == null)
		{
			return null;
		}
		do
		{
			type = type.BaseType;
		}
		while (type != null && type.IsAbstract);
		if (type != null)
		{
			if (!allowEventSourceOverride)
			{
				if ((reflectionOnly && type.FullName != typeof(EventSource).FullName) || (!reflectionOnly && type != typeof(EventSource)))
				{
					return null;
				}
			}
			else if (type.Name != "EventSource")
			{
				return null;
			}
		}
		return type;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which has dynamically accessed members requirements, but its use of this method satisfies these requirements because it passes in the result of GetType with the same annotations.")]
	private static byte[] CreateManifestAndDescriptors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type eventSourceType, string eventSourceDllName, EventSource source, EventManifestOptions flags = EventManifestOptions.None)
	{
		ManifestBuilder manifestBuilder = null;
		bool flag = source == null || !source.SelfDescribingEvents;
		Exception ex = null;
		byte[] result = null;
		if (eventSourceType.IsAbstract && (flags & EventManifestOptions.Strict) == 0)
		{
			return null;
		}
		try
		{
			MethodInfo[] methods = eventSourceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int num = 1;
			EventMetadata[] eventData = null;
			Dictionary<string, string> eventsByName = null;
			if (source != null || (flags & EventManifestOptions.Strict) != 0)
			{
				eventData = new EventMetadata[methods.Length + 1];
				eventData[0].Name = "";
			}
			ResourceManager resources = null;
			EventSourceAttribute eventSourceAttribute = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute), flags);
			if (eventSourceAttribute != null && eventSourceAttribute.LocalizationResources != null)
			{
				resources = new ResourceManager(eventSourceAttribute.LocalizationResources, eventSourceType.Assembly);
			}
			manifestBuilder = ((source == null) ? new ManifestBuilder(GetName(eventSourceType, flags), GetGuid(eventSourceType), eventSourceDllName, resources, flags) : new ManifestBuilder(source.Name, source.Guid, eventSourceDllName, resources, flags));
			manifestBuilder.StartEvent("EventSourceMessage", new EventAttribute(0)
			{
				Level = EventLevel.LogAlways,
				Task = (EventTask)65534
			});
			manifestBuilder.AddEventParameter(typeof(string), "message");
			manifestBuilder.EndEvent();
			if ((flags & EventManifestOptions.Strict) != 0)
			{
				if (!(GetEventSourceBaseType(eventSourceType, (flags & EventManifestOptions.AllowEventSourceOverride) != 0, eventSourceType.Assembly.ReflectionOnly) != null))
				{
					manifestBuilder.ManifestError(SR.EventSource_TypeMustDeriveFromEventSource);
				}
				if (!eventSourceType.IsAbstract && !eventSourceType.IsSealed)
				{
					manifestBuilder.ManifestError(SR.EventSource_TypeMustBeSealedOrAbstract);
				}
			}
			string[] array = new string[3] { "Keywords", "Tasks", "Opcodes" };
			foreach (string text in array)
			{
				Type nestedType = eventSourceType.GetNestedType(text);
				if (!(nestedType != null))
				{
					continue;
				}
				if (eventSourceType.IsAbstract)
				{
					manifestBuilder.ManifestError(SR.Format(SR.EventSource_AbstractMustNotDeclareKTOC, nestedType.Name));
					continue;
				}
				FieldInfo[] fields = nestedType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo staticField in fields)
				{
					AddProviderEnumKind(manifestBuilder, staticField, text);
				}
			}
			manifestBuilder.AddKeyword("Session3", 17592186044416uL);
			manifestBuilder.AddKeyword("Session2", 35184372088832uL);
			manifestBuilder.AddKeyword("Session1", 70368744177664uL);
			manifestBuilder.AddKeyword("Session0", 140737488355328uL);
			if (eventSourceType != typeof(EventSource))
			{
				foreach (MethodInfo methodInfo in methods)
				{
					ParameterInfo[] args = methodInfo.GetParameters();
					EventAttribute eventAttribute = (EventAttribute)GetCustomAttributeHelper(methodInfo, typeof(EventAttribute), flags);
					if (methodInfo.IsStatic)
					{
						continue;
					}
					if (eventSourceType.IsAbstract)
					{
						if (eventAttribute != null)
						{
							manifestBuilder.ManifestError(SR.Format(SR.EventSource_AbstractMustNotDeclareEventMethods, methodInfo.Name, eventAttribute.EventId));
						}
						continue;
					}
					if (eventAttribute == null)
					{
						if (methodInfo.ReturnType != typeof(void) || methodInfo.IsVirtual || IsCustomAttributeDefinedHelper(methodInfo, typeof(NonEventAttribute), flags))
						{
							continue;
						}
						EventAttribute eventAttribute2 = new EventAttribute(num);
						eventAttribute = eventAttribute2;
					}
					else if (eventAttribute.EventId <= 0)
					{
						manifestBuilder.ManifestError(SR.Format(SR.EventSource_NeedPositiveId, methodInfo.Name), runtimeCritical: true);
						continue;
					}
					if (methodInfo.Name.LastIndexOf('.') >= 0)
					{
						manifestBuilder.ManifestError(SR.Format(SR.EventSource_EventMustNotBeExplicitImplementation, methodInfo.Name, eventAttribute.EventId));
					}
					num++;
					string name = methodInfo.Name;
					if (eventAttribute.Opcode == EventOpcode.Info)
					{
						bool flag2 = eventAttribute.Task == EventTask.None;
						if (flag2)
						{
							eventAttribute.Task = (EventTask)(65534 - eventAttribute.EventId);
						}
						if (!eventAttribute.IsOpcodeSet)
						{
							eventAttribute.Opcode = GetOpcodeWithDefault(EventOpcode.Info, name);
						}
						if (flag2)
						{
							if (eventAttribute.Opcode == EventOpcode.Start)
							{
								string text2 = name.Substring(0, name.Length - "Start".Length);
								if (string.Compare(name, 0, text2, 0, text2.Length) == 0 && string.Compare(name, text2.Length, "Start", 0, Math.Max(name.Length - text2.Length, "Start".Length)) == 0)
								{
									manifestBuilder.AddTask(text2, (int)eventAttribute.Task);
								}
							}
							else if (eventAttribute.Opcode == EventOpcode.Stop)
							{
								int num2 = eventAttribute.EventId - 1;
								if (eventData != null && num2 < eventData.Length)
								{
									EventMetadata eventMetadata = eventData[num2];
									string text3 = name.Substring(0, name.Length - "Stop".Length);
									if (eventMetadata.Descriptor.Opcode == 1 && string.Compare(eventMetadata.Name, 0, text3, 0, text3.Length) == 0 && string.Compare(eventMetadata.Name, text3.Length, "Start", 0, Math.Max(eventMetadata.Name.Length - text3.Length, "Start".Length)) == 0)
									{
										eventAttribute.Task = (EventTask)eventMetadata.Descriptor.Task;
										flag2 = false;
									}
								}
								if (flag2 && (flags & EventManifestOptions.Strict) != 0)
								{
									throw new ArgumentException(SR.EventSource_StopsFollowStarts);
								}
							}
						}
					}
					bool hasRelatedActivityID = RemoveFirstArgIfRelatedActivityId(ref args);
					if (source == null || !source.SelfDescribingEvents)
					{
						manifestBuilder.StartEvent(name, eventAttribute);
						for (int l = 0; l < args.Length; l++)
						{
							manifestBuilder.AddEventParameter(args[l].ParameterType, args[l].Name);
						}
						manifestBuilder.EndEvent();
					}
					if (source == null && (flags & EventManifestOptions.Strict) == 0)
					{
						continue;
					}
					DebugCheckEvent(ref eventsByName, eventData, methodInfo, eventAttribute, manifestBuilder, flags);
					if (eventAttribute.Channel != 0)
					{
						eventAttribute.Keywords |= (EventKeywords)manifestBuilder.GetChannelKeyword(eventAttribute.Channel, (ulong)eventAttribute.Keywords);
					}
					if (manifestBuilder.HasResources)
					{
						string key = "event_" + name;
						string localizedMessage = manifestBuilder.GetLocalizedMessage(key, CultureInfo.CurrentUICulture, etwFormat: false);
						if (localizedMessage != null)
						{
							eventAttribute.Message = localizedMessage;
						}
					}
					AddEventDescriptor(ref eventData, name, eventAttribute, args, hasRelatedActivityID);
				}
			}
			NameInfo.ReserveEventIDsBelow(num);
			if (source != null)
			{
				TrimEventDescriptors(ref eventData);
				source.m_eventData = eventData;
				source.m_channelData = manifestBuilder.GetChannelData();
			}
			if (!eventSourceType.IsAbstract && (source == null || !source.SelfDescribingEvents))
			{
				flag = (flags & EventManifestOptions.OnlyIfNeededForRegistration) == 0 || manifestBuilder.GetChannelData().Length != 0;
				if (!flag && (flags & EventManifestOptions.Strict) == 0)
				{
					return null;
				}
				result = manifestBuilder.CreateManifest();
			}
		}
		catch (Exception ex2)
		{
			if ((flags & EventManifestOptions.Strict) == 0)
			{
				throw;
			}
			ex = ex2;
		}
		if ((flags & EventManifestOptions.Strict) != 0 && ((manifestBuilder != null && manifestBuilder.Errors.Count > 0) || ex != null))
		{
			string text4 = string.Empty;
			if (manifestBuilder != null && manifestBuilder.Errors.Count > 0)
			{
				bool flag3 = true;
				foreach (string error in manifestBuilder.Errors)
				{
					if (!flag3)
					{
						text4 += Environment.NewLine;
					}
					flag3 = false;
					text4 += error;
				}
			}
			else
			{
				text4 = "Unexpected error: " + ex.Message;
			}
			throw new ArgumentException(text4, ex);
		}
		if (!flag)
		{
			return null;
		}
		return result;
	}

	private static bool RemoveFirstArgIfRelatedActivityId(ref ParameterInfo[] args)
	{
		if (args.Length != 0 && args[0].ParameterType == typeof(Guid) && string.Equals(args[0].Name, "relatedActivityId", StringComparison.OrdinalIgnoreCase))
		{
			ParameterInfo[] array = new ParameterInfo[args.Length - 1];
			Array.Copy(args, 1, array, 0, args.Length - 1);
			args = array;
			return true;
		}
		return false;
	}

	private static void AddProviderEnumKind(ManifestBuilder manifest, FieldInfo staticField, string providerEnumKind)
	{
		bool reflectionOnly = staticField.Module.Assembly.ReflectionOnly;
		Type fieldType = staticField.FieldType;
		if ((!reflectionOnly && fieldType == typeof(EventOpcode)) || AttributeTypeNamesMatch(fieldType, typeof(EventOpcode)))
		{
			if (!(providerEnumKind != "Opcodes"))
			{
				int value = (int)staticField.GetRawConstantValue();
				manifest.AddOpcode(staticField.Name, value);
				return;
			}
		}
		else if ((!reflectionOnly && fieldType == typeof(EventTask)) || AttributeTypeNamesMatch(fieldType, typeof(EventTask)))
		{
			if (!(providerEnumKind != "Tasks"))
			{
				int value2 = (int)staticField.GetRawConstantValue();
				manifest.AddTask(staticField.Name, value2);
				return;
			}
		}
		else
		{
			if ((reflectionOnly || !(fieldType == typeof(EventKeywords))) && !AttributeTypeNamesMatch(fieldType, typeof(EventKeywords)))
			{
				return;
			}
			if (!(providerEnumKind != "Keywords"))
			{
				ulong value3 = (ulong)(long)staticField.GetRawConstantValue();
				manifest.AddKeyword(staticField.Name, value3);
				return;
			}
		}
		manifest.ManifestError(SR.Format(SR.EventSource_EnumKindMismatch, staticField.Name, staticField.FieldType.Name, providerEnumKind));
	}

	private static void AddEventDescriptor([NotNull] ref EventMetadata[] eventData, string eventName, EventAttribute eventAttribute, ParameterInfo[] eventParameters, bool hasRelatedActivityID)
	{
		if (eventData.Length <= eventAttribute.EventId)
		{
			EventMetadata[] array = new EventMetadata[Math.Max(eventData.Length + 16, eventAttribute.EventId + 1)];
			Array.Copy(eventData, array, eventData.Length);
			eventData = array;
		}
		ref EventMetadata reference = ref eventData[eventAttribute.EventId];
		reference.Descriptor = new EventDescriptor(eventAttribute.EventId, eventAttribute.Version, (byte)eventAttribute.Channel, (byte)eventAttribute.Level, (byte)eventAttribute.Opcode, (int)eventAttribute.Task, (long)eventAttribute.Keywords | (long)SessionMask.All.ToEventKeywords());
		reference.Tags = eventAttribute.Tags;
		reference.Name = eventName;
		reference.Parameters = eventParameters;
		reference.Message = eventAttribute.Message;
		reference.ActivityOptions = eventAttribute.ActivityOptions;
		reference.HasRelatedActivityID = hasRelatedActivityID;
		reference.EventHandle = IntPtr.Zero;
		int num = eventParameters.Length;
		bool allParametersAreInt = true;
		bool allParametersAreString = true;
		foreach (ParameterInfo parameterInfo in eventParameters)
		{
			Type parameterType = parameterInfo.ParameterType;
			if (parameterType == typeof(string))
			{
				allParametersAreInt = false;
				continue;
			}
			if (parameterType == typeof(int) || (parameterType.IsEnum && Type.GetTypeCode(parameterType.GetEnumUnderlyingType()) <= TypeCode.UInt32))
			{
				allParametersAreString = false;
				continue;
			}
			if (parameterType == typeof(byte[]))
			{
				num++;
			}
			allParametersAreInt = false;
			allParametersAreString = false;
		}
		reference.AllParametersAreInt32 = allParametersAreInt;
		reference.AllParametersAreString = allParametersAreString;
		reference.EventListenerParameterCount = num;
	}

	private static void TrimEventDescriptors(ref EventMetadata[] eventData)
	{
		int num = eventData.Length;
		while (0 < num)
		{
			num--;
			if (eventData[num].Descriptor.EventId != 0)
			{
				break;
			}
		}
		if (eventData.Length - num > 2)
		{
			EventMetadata[] array = new EventMetadata[num + 1];
			Array.Copy(eventData, array, array.Length);
			eventData = array;
		}
	}

	internal void AddListener(EventListener listener)
	{
		lock (EventListener.EventListenersLock)
		{
			bool[] eventEnabled = null;
			if (m_eventData != null)
			{
				eventEnabled = new bool[m_eventData.Length];
			}
			m_Dispatchers = new EventDispatcher(m_Dispatchers, eventEnabled, listener);
			listener.OnEventSourceCreated(this);
		}
	}

	private static void DebugCheckEvent(ref Dictionary<string, string> eventsByName, EventMetadata[] eventData, MethodInfo method, EventAttribute eventAttribute, ManifestBuilder manifest, EventManifestOptions options)
	{
		int eventId = eventAttribute.EventId;
		string name = method.Name;
		int helperCallFirstArg = GetHelperCallFirstArg(method);
		if (helperCallFirstArg >= 0 && eventId != helperCallFirstArg)
		{
			manifest.ManifestError(SR.Format(SR.EventSource_MismatchIdToWriteEvent, name, eventId, helperCallFirstArg), runtimeCritical: true);
		}
		if (eventId < eventData.Length && eventData[eventId].Descriptor.EventId != 0)
		{
			manifest.ManifestError(SR.Format(SR.EventSource_EventIdReused, name, eventId, eventData[eventId].Name), runtimeCritical: true);
		}
		for (int i = 0; i < eventData.Length; i++)
		{
			if (eventData[i].Name != null && eventData[i].Descriptor.Task == (int)eventAttribute.Task && (EventOpcode)eventData[i].Descriptor.Opcode == eventAttribute.Opcode)
			{
				manifest.ManifestError(SR.Format(SR.EventSource_TaskOpcodePairReused, name, eventId, eventData[i].Name, i));
				if ((options & EventManifestOptions.Strict) == 0)
				{
					break;
				}
			}
		}
		if (eventAttribute.Opcode != 0)
		{
			bool flag = false;
			if (eventAttribute.Task == EventTask.None)
			{
				flag = true;
			}
			else
			{
				EventTask eventTask = (EventTask)(65534 - eventId);
				if (eventAttribute.Opcode != EventOpcode.Start && eventAttribute.Opcode != EventOpcode.Stop && eventAttribute.Task == eventTask)
				{
					flag = true;
				}
			}
			if (flag)
			{
				manifest.ManifestError(SR.Format(SR.EventSource_EventMustHaveTaskIfNonDefaultOpcode, name, eventId));
			}
		}
		if (eventsByName == null)
		{
			eventsByName = new Dictionary<string, string>();
		}
		if (eventsByName.ContainsKey(name))
		{
			manifest.ManifestError(SR.Format(SR.EventSource_EventNameReused, name), runtimeCritical: true);
		}
		eventsByName[name] = name;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The method calls MethodBase.GetMethodBody. Trimming application can change IL of various methodswhich can lead to change of behavior. This method only uses this to validate usage of event source APIs.In the worst case it will not be able to determine the value it's looking for and will not performany validation.")]
	private static int GetHelperCallFirstArg(MethodInfo method)
	{
		byte[] iLAsByteArray = method.GetMethodBody().GetILAsByteArray();
		int num = -1;
		for (int i = 0; i < iLAsByteArray.Length; i++)
		{
			switch (iLAsByteArray[i])
			{
			case 14:
			case 16:
				i++;
				continue;
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
				if (i > 0 && iLAsByteArray[i - 1] == 2)
				{
					num = iLAsByteArray[i] - 22;
				}
				continue;
			case 31:
				if (i > 0 && iLAsByteArray[i - 1] == 2)
				{
					num = iLAsByteArray[i + 1];
				}
				i++;
				continue;
			case 32:
				i += 4;
				continue;
			case 40:
				i += 4;
				if (num >= 0)
				{
					for (int j = i + 1; j < iLAsByteArray.Length; j++)
					{
						if (iLAsByteArray[j] == 42)
						{
							return num;
						}
						if (iLAsByteArray[j] != 0)
						{
							break;
						}
					}
				}
				num = -1;
				continue;
			case 44:
			case 45:
				num = -1;
				i++;
				continue;
			case 57:
			case 58:
				num = -1;
				i += 4;
				continue;
			case 140:
			case 141:
				i += 4;
				continue;
			case 254:
				i++;
				if (i < iLAsByteArray.Length && iLAsByteArray[i] < 6)
				{
					continue;
				}
				break;
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 20:
			case 37:
			case 103:
			case 104:
			case 105:
			case 106:
			case 109:
			case 110:
			case 162:
				continue;
			}
			return -1;
		}
		return -1;
	}

	internal void ReportOutOfBandMessage(string msg)
	{
		try
		{
			if (m_outOfBandMessageCount < 15)
			{
				m_outOfBandMessageCount++;
			}
			else
			{
				if (m_outOfBandMessageCount == 16)
				{
					return;
				}
				m_outOfBandMessageCount = 16;
				msg = "Reached message limit.   End of EventSource error messages.";
			}
			Debugger.Log(0, null, "EventSource Error: " + msg + Environment.NewLine);
			WriteEventString(msg);
			WriteStringToAllListeners("EventSourceMessage", msg);
		}
		catch
		{
		}
	}

	private static EventSourceSettings ValidateSettings(EventSourceSettings settings)
	{
		if ((settings & (EventSourceSettings.EtwManifestEventFormat | EventSourceSettings.EtwSelfDescribingEventFormat)) == (EventSourceSettings.EtwManifestEventFormat | EventSourceSettings.EtwSelfDescribingEventFormat))
		{
			throw new ArgumentException(SR.EventSource_InvalidEventFormat, "settings");
		}
		if ((settings & (EventSourceSettings.EtwManifestEventFormat | EventSourceSettings.EtwSelfDescribingEventFormat)) == 0)
		{
			settings |= EventSourceSettings.EtwSelfDescribingEventFormat;
		}
		return settings;
	}

	public EventSource(string eventSourceName)
		: this(eventSourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
	{
	}

	public EventSource(string eventSourceName, EventSourceSettings config)
		: this(eventSourceName, config, (string[]?)null)
	{
	}

	public EventSource(string eventSourceName, EventSourceSettings config, params string[]? traits)
		: this((eventSourceName == null) ? default(Guid) : GenerateGuidFromName(eventSourceName.ToUpperInvariant()), eventSourceName, config, traits)
	{
		if (eventSourceName == null)
		{
			throw new ArgumentNullException("eventSourceName");
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	public unsafe void Write(string? eventName)
	{
		if (IsEnabled())
		{
			EventSourceOptions options = default(EventSourceOptions);
			WriteImpl(eventName, ref options, null, null, null, SimpleEventTypes<EmptyStruct>.Instance);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	public unsafe void Write(string? eventName, EventSourceOptions options)
	{
		if (IsEnabled())
		{
			WriteImpl(eventName, ref options, null, null, null, SimpleEventTypes<EmptyStruct>.Instance);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public unsafe void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, T data)
	{
		if (IsEnabled())
		{
			EventSourceOptions options = default(EventSourceOptions);
			WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public unsafe void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, EventSourceOptions options, T data)
	{
		if (IsEnabled())
		{
			WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public unsafe void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, ref EventSourceOptions options, ref T data)
	{
		if (IsEnabled())
		{
			WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
	[RequiresUnreferencedCode("EventSource will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public unsafe void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref T data)
	{
		if (!IsEnabled())
		{
			return;
		}
		fixed (Guid* pActivityId = &activityId)
		{
			fixed (Guid* ptr = &relatedActivityId)
			{
				WriteImpl(eventName, ref options, data, pActivityId, (relatedActivityId == Guid.Empty) ? null : ptr, SimpleEventTypes<T>.Instance);
			}
		}
	}

	private unsafe void WriteMultiMerge(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid* activityID, Guid* childActivityID, params object[] values)
	{
		if (IsEnabled())
		{
			byte level = (((options.valuesSet & 4u) != 0) ? options.level : eventTypes.level);
			EventKeywords keywords = ((((uint)options.valuesSet & (true ? 1u : 0u)) != 0) ? options.keywords : eventTypes.keywords);
			if (IsEnabled((EventLevel)level, keywords))
			{
				WriteMultiMergeInner(eventName, ref options, eventTypes, activityID, childActivityID, values);
			}
		}
	}

	private unsafe void WriteMultiMergeInner(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid* activityID, Guid* childActivityID, params object[] values)
	{
		int num = 0;
		byte level = (((options.valuesSet & 4u) != 0) ? options.level : eventTypes.level);
		byte opcode = (((options.valuesSet & 8u) != 0) ? options.opcode : eventTypes.opcode);
		EventTags tags = (((options.valuesSet & 2u) != 0) ? options.tags : eventTypes.Tags);
		EventKeywords keywords = ((((uint)options.valuesSet & (true ? 1u : 0u)) != 0) ? options.keywords : eventTypes.keywords);
		NameInfo nameInfo = eventTypes.GetNameInfo(eventName ?? eventTypes.Name, tags);
		if (nameInfo == null)
		{
			return;
		}
		num = nameInfo.identity;
		EventDescriptor eventDescriptor = new EventDescriptor(num, level, opcode, (long)keywords);
		IntPtr orCreateEventHandle = nameInfo.GetOrCreateEventHandle(m_eventPipeProvider, m_eventHandleTable, eventDescriptor, eventTypes);
		int pinCount = eventTypes.pinCount;
		byte* scratch = stackalloc byte[(int)(uint)eventTypes.scratchSize];
		EventData* ptr = stackalloc EventData[eventTypes.dataCount + 3];
		for (int i = 0; i < eventTypes.dataCount + 3; i++)
		{
			ptr[i] = default(EventData);
		}
		GCHandle* ptr2 = stackalloc GCHandle[pinCount];
		for (int j = 0; j < pinCount; j++)
		{
			ptr2[j] = default(GCHandle);
		}
		ReadOnlySpan<byte> providerMetadata = ProviderMetadata;
		fixed (byte* pointer = providerMetadata)
		{
			fixed (byte* pointer2 = nameInfo.nameMetadata)
			{
				fixed (byte* pointer3 = eventTypes.typeMetadata)
				{
					ptr->SetMetadata(pointer, providerMetadata.Length, 2);
					ptr[1].SetMetadata(pointer2, nameInfo.nameMetadata.Length, 1);
					ptr[2].SetMetadata(pointer3, eventTypes.typeMetadata.Length, 1);
					try
					{
						DataCollector.ThreadInstance.Enable(scratch, eventTypes.scratchSize, ptr + 3, eventTypes.dataCount, ptr2, pinCount);
						for (int k = 0; k < eventTypes.typeInfos.Length; k++)
						{
							TraceLoggingTypeInfo traceLoggingTypeInfo = eventTypes.typeInfos[k];
							traceLoggingTypeInfo.WriteData(traceLoggingTypeInfo.PropertyValueFactory(values[k]));
						}
						WriteEventRaw(eventName, ref eventDescriptor, orCreateEventHandle, activityID, childActivityID, (int)(DataCollector.ThreadInstance.Finish() - ptr), (IntPtr)ptr);
					}
					finally
					{
						WriteCleanup(ptr2, pinCount);
					}
				}
			}
		}
	}

	internal unsafe void WriteMultiMerge(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid* activityID, Guid* childActivityID, EventData* data)
	{
		if (!IsEnabled())
		{
			return;
		}
		fixed (EventSourceOptions* ptr2 = &options)
		{
			EventDescriptor descriptor;
			NameInfo nameInfo = UpdateDescriptor(eventName, eventTypes, ref options, out descriptor);
			if (nameInfo == null)
			{
				return;
			}
			IntPtr orCreateEventHandle = nameInfo.GetOrCreateEventHandle(m_eventPipeProvider, m_eventHandleTable, descriptor, eventTypes);
			int num = eventTypes.dataCount + eventTypes.typeInfos.Length * 2 + 3;
			EventData* ptr = stackalloc EventData[num];
			for (int i = 0; i < num; i++)
			{
				ptr[i] = default(EventData);
			}
			ReadOnlySpan<byte> providerMetadata = ProviderMetadata;
			fixed (byte* pointer = providerMetadata)
			{
				fixed (byte* pointer2 = nameInfo.nameMetadata)
				{
					fixed (byte* pointer3 = eventTypes.typeMetadata)
					{
						ptr->SetMetadata(pointer, providerMetadata.Length, 2);
						ptr[1].SetMetadata(pointer2, nameInfo.nameMetadata.Length, 1);
						ptr[2].SetMetadata(pointer3, eventTypes.typeMetadata.Length, 1);
						int num2 = 3;
						for (int j = 0; j < eventTypes.typeInfos.Length; j++)
						{
							ptr[num2].m_Ptr = data[j].m_Ptr;
							ptr[num2].m_Size = data[j].m_Size;
							if (data[j].m_Size == 4 && eventTypes.typeInfos[j].DataType == typeof(bool))
							{
								ptr[num2].m_Size = 1;
							}
							num2++;
						}
						WriteEventRaw(eventName, ref descriptor, orCreateEventHandle, activityID, childActivityID, num2, (IntPtr)ptr);
					}
				}
			}
		}
	}

	private unsafe void WriteImpl(string eventName, ref EventSourceOptions options, object data, Guid* pActivityId, Guid* pRelatedActivityId, TraceLoggingEventTypes eventTypes)
	{
		try
		{
			fixed (EventSourceOptions* ptr3 = &options)
			{
				options.Opcode = (options.IsOpcodeSet ? options.Opcode : GetOpcodeWithDefault(options.Opcode, eventName));
				EventDescriptor descriptor;
				NameInfo nameInfo = UpdateDescriptor(eventName, eventTypes, ref options, out descriptor);
				if (nameInfo == null)
				{
					return;
				}
				IntPtr orCreateEventHandle = nameInfo.GetOrCreateEventHandle(m_eventPipeProvider, m_eventHandleTable, descriptor, eventTypes);
				int pinCount = eventTypes.pinCount;
				byte* scratch = stackalloc byte[(int)(uint)eventTypes.scratchSize];
				EventData* ptr = stackalloc EventData[eventTypes.dataCount + 3];
				for (int i = 0; i < eventTypes.dataCount + 3; i++)
				{
					ptr[i] = default(EventData);
				}
				GCHandle* ptr2 = stackalloc GCHandle[pinCount];
				for (int j = 0; j < pinCount; j++)
				{
					ptr2[j] = default(GCHandle);
				}
				ReadOnlySpan<byte> providerMetadata = ProviderMetadata;
				fixed (byte* pointer = providerMetadata)
				{
					fixed (byte* pointer2 = nameInfo.nameMetadata)
					{
						fixed (byte* pointer3 = eventTypes.typeMetadata)
						{
							ptr->SetMetadata(pointer, providerMetadata.Length, 2);
							ptr[1].SetMetadata(pointer2, nameInfo.nameMetadata.Length, 1);
							ptr[2].SetMetadata(pointer3, eventTypes.typeMetadata.Length, 1);
							EventOpcode opcode = (EventOpcode)descriptor.Opcode;
							Guid activityId = Guid.Empty;
							Guid relatedActivityId = Guid.Empty;
							if (pActivityId == null && pRelatedActivityId == null && (options.ActivityOptions & EventActivityOptions.Disable) == 0)
							{
								switch (opcode)
								{
								case EventOpcode.Start:
									m_activityTracker.OnStart(m_name, eventName, 0, ref activityId, ref relatedActivityId, options.ActivityOptions);
									break;
								case EventOpcode.Stop:
									m_activityTracker.OnStop(m_name, eventName, 0, ref activityId);
									break;
								}
								if (activityId != Guid.Empty)
								{
									pActivityId = &activityId;
								}
								if (relatedActivityId != Guid.Empty)
								{
									pRelatedActivityId = &relatedActivityId;
								}
							}
							try
							{
								DataCollector.ThreadInstance.Enable(scratch, eventTypes.scratchSize, ptr + 3, eventTypes.dataCount, ptr2, pinCount);
								TraceLoggingTypeInfo traceLoggingTypeInfo = eventTypes.typeInfos[0];
								traceLoggingTypeInfo.WriteData(traceLoggingTypeInfo.PropertyValueFactory(data));
								WriteEventRaw(eventName, ref descriptor, orCreateEventHandle, pActivityId, pRelatedActivityId, (int)(DataCollector.ThreadInstance.Finish() - ptr), (IntPtr)ptr);
								if (m_Dispatchers != null)
								{
									EventPayload payload = (EventPayload)eventTypes.typeInfos[0].GetData(data);
									WriteToAllListeners(eventName, ref descriptor, nameInfo.tags, pActivityId, pRelatedActivityId, payload);
								}
							}
							catch (Exception ex)
							{
								if (ex is EventSourceException)
								{
									throw;
								}
								ThrowEventSourceException(eventName, ex);
							}
							finally
							{
								WriteCleanup(ptr2, pinCount);
							}
						}
					}
				}
			}
		}
		catch (Exception ex2)
		{
			if (ex2 is EventSourceException)
			{
				throw;
			}
			ThrowEventSourceException(eventName, ex2);
		}
	}

	private unsafe void WriteToAllListeners(string eventName, ref EventDescriptor eventDescriptor, EventTags tags, Guid* pActivityId, Guid* pChildActivityId, EventPayload payload)
	{
		EventWrittenEventArgs eventWrittenEventArgs = new EventWrittenEventArgs(this, -1, pActivityId, pChildActivityId)
		{
			EventName = eventName,
			Level = (EventLevel)eventDescriptor.Level,
			Keywords = (EventKeywords)eventDescriptor.Keywords,
			Opcode = (EventOpcode)eventDescriptor.Opcode,
			Tags = tags
		};
		if (payload != null)
		{
			eventWrittenEventArgs.Payload = new ReadOnlyCollection<object>((IList<object>)payload.Values);
			eventWrittenEventArgs.PayloadNames = new ReadOnlyCollection<string>((IList<string>)payload.Keys);
		}
		DispatchToAllListeners(eventWrittenEventArgs);
	}

	[NonEvent]
	private unsafe static void WriteCleanup(GCHandle* pPins, int cPins)
	{
		DataCollector.ThreadInstance.Disable();
		for (int i = 0; i < cPins; i++)
		{
			if (pPins[i].IsAllocated)
			{
				pPins[i].Free();
			}
		}
	}

	private void InitializeProviderMetadata()
	{
		if (ProviderMetadata.Length > 0)
		{
			return;
		}
		if (m_traits != null)
		{
			List<byte> list = new List<byte>(100);
			for (int i = 0; i < m_traits.Length - 1; i += 2)
			{
				if (!m_traits[i].StartsWith("ETW_", StringComparison.Ordinal))
				{
					continue;
				}
				string text = m_traits[i].Substring(4);
				if (!byte.TryParse(text, out var result))
				{
					if (!(text == "GROUP"))
					{
						throw new ArgumentException(SR.Format(SR.EventSource_UnknownEtwTrait, text), "traits");
					}
					result = 1;
				}
				string value = m_traits[i + 1];
				int count = list.Count;
				list.Add(0);
				list.Add(0);
				list.Add(result);
				int num = AddValueToMetaData(list, value) + 3;
				list[count] = (byte)num;
				list[count + 1] = (byte)(num >> 8);
			}
			byte[] array = Statics.MetadataForString(Name, 0, list.Count, 0);
			int num2 = array.Length - list.Count;
			foreach (byte item in list)
			{
				array[num2++] = item;
			}
			m_providerMetadata = array;
		}
		else
		{
			m_providerMetadata = Statics.MetadataForString(Name, 0, 0, 0);
		}
	}

	private static int AddValueToMetaData(List<byte> metaData, string value)
	{
		if (value.Length == 0)
		{
			return 0;
		}
		int count = metaData.Count;
		char c = value[0];
		switch (c)
		{
		case '@':
			metaData.AddRange(Encoding.UTF8.GetBytes(value.Substring(1)));
			break;
		case '{':
			metaData.AddRange(new Guid(value).ToByteArray());
			break;
		case '#':
		{
			for (int i = 1; i < value.Length; i++)
			{
				if (value[i] != ' ')
				{
					if (i + 1 >= value.Length)
					{
						throw new ArgumentException(SR.EventSource_EvenHexDigits, "traits");
					}
					metaData.Add((byte)(HexDigit(value[i]) * 16 + HexDigit(value[i + 1])));
					i++;
				}
			}
			break;
		}
		default:
			if ('A' <= c || ' ' == c)
			{
				metaData.AddRange(Encoding.UTF8.GetBytes(value));
				break;
			}
			throw new ArgumentException(SR.Format(SR.EventSource_IllegalValue, value), "traits");
		}
		return metaData.Count - count;
	}

	private static int HexDigit(char c)
	{
		if ('0' <= c && c <= '9')
		{
			return c - 48;
		}
		if ('a' <= c)
		{
			c = (char)(c - 32);
		}
		if ('A' <= c && c <= 'F')
		{
			return c - 65 + 10;
		}
		throw new ArgumentException(SR.Format(SR.EventSource_BadHexDigit, c), "traits");
	}

	private NameInfo UpdateDescriptor(string name, TraceLoggingEventTypes eventInfo, ref EventSourceOptions options, out EventDescriptor descriptor)
	{
		NameInfo nameInfo = null;
		int traceloggingId = 0;
		byte level = (((options.valuesSet & 4u) != 0) ? options.level : eventInfo.level);
		byte opcode = (((options.valuesSet & 8u) != 0) ? options.opcode : eventInfo.opcode);
		EventTags tags = (((options.valuesSet & 2u) != 0) ? options.tags : eventInfo.Tags);
		EventKeywords keywords = ((((uint)options.valuesSet & (true ? 1u : 0u)) != 0) ? options.keywords : eventInfo.keywords);
		if (IsEnabled((EventLevel)level, keywords))
		{
			nameInfo = eventInfo.GetNameInfo(name ?? eventInfo.Name, tags);
			traceloggingId = nameInfo.identity;
		}
		descriptor = new EventDescriptor(traceloggingId, level, opcode, (long)keywords);
		return nameInfo;
	}
}
