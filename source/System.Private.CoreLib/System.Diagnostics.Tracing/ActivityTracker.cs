using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing;

internal sealed class ActivityTracker
{
	private sealed class ActivityInfo
	{
		internal readonly string m_name;

		private readonly long m_uniqueId;

		internal readonly Guid m_guid;

		internal readonly int m_activityPathGuidOffset;

		internal readonly int m_level;

		internal readonly EventActivityOptions m_eventOptions;

		internal long m_lastChildID;

		internal int m_stopped;

		internal readonly ActivityInfo m_creator;

		internal readonly Guid m_activityIdToRestore;

		public Guid ActivityId => m_guid;

		public ActivityInfo(string name, long uniqueId, ActivityInfo creator, Guid activityIDToRestore, EventActivityOptions options)
		{
			m_name = name;
			m_eventOptions = options;
			m_creator = creator;
			m_uniqueId = uniqueId;
			m_level = ((creator != null) ? (creator.m_level + 1) : 0);
			m_activityIdToRestore = activityIDToRestore;
			CreateActivityPathGuid(out m_guid, out m_activityPathGuidOffset);
		}

		public static string Path(ActivityInfo activityInfo)
		{
			if (activityInfo == null)
			{
				return "";
			}
			return $"{Path(activityInfo.m_creator)}/{activityInfo.m_uniqueId}";
		}

		public override string ToString()
		{
			return m_name + "(" + Path(this) + ((m_stopped != 0) ? ",DEAD)" : ")");
		}

		public static string LiveActivities(ActivityInfo list)
		{
			if (list == null)
			{
				return "";
			}
			return list.ToString() + ";" + LiveActivities(list.m_creator);
		}

		public bool CanBeOrphan()
		{
			if ((m_eventOptions & EventActivityOptions.Detachable) != 0)
			{
				return true;
			}
			return false;
		}

		private unsafe void CreateActivityPathGuid(out Guid idRet, out int activityPathGuidOffset)
		{
			fixed (Guid* outPtr = &idRet)
			{
				int whereToAddId = 0;
				if (m_creator != null)
				{
					whereToAddId = m_creator.m_activityPathGuidOffset;
					idRet = m_creator.m_guid;
				}
				else
				{
					int num = 0;
					num = Thread.GetDomainID();
					whereToAddId = AddIdToGuid(outPtr, whereToAddId, (uint)num);
				}
				activityPathGuidOffset = AddIdToGuid(outPtr, whereToAddId, (uint)m_uniqueId);
				if (12 < activityPathGuidOffset)
				{
					CreateOverflowGuid(outPtr);
				}
			}
		}

		private unsafe void CreateOverflowGuid(Guid* outPtr)
		{
			for (ActivityInfo creator = m_creator; creator != null; creator = creator.m_creator)
			{
				if (creator.m_activityPathGuidOffset <= 10)
				{
					uint id = (uint)Interlocked.Increment(ref creator.m_lastChildID);
					*outPtr = creator.m_guid;
					int num = AddIdToGuid(outPtr, creator.m_activityPathGuidOffset, id, overflow: true);
					if (num <= 12)
					{
						break;
					}
				}
			}
		}

		private unsafe static int AddIdToGuid(Guid* outPtr, int whereToAddId, uint id, bool overflow = false)
		{
			byte* ptr = (byte*)outPtr;
			byte* ptr2 = ptr + 12;
			ptr += whereToAddId;
			if (ptr2 <= ptr)
			{
				return 13;
			}
			if (0 < id && id <= 10 && !overflow)
			{
				WriteNibble(ref ptr, ptr2, id);
			}
			else
			{
				uint num = 4u;
				if (id <= 255)
				{
					num = 1u;
				}
				else if (id <= 65535)
				{
					num = 2u;
				}
				else if (id <= 16777215)
				{
					num = 3u;
				}
				if (overflow)
				{
					if (ptr2 <= ptr + 2)
					{
						return 13;
					}
					WriteNibble(ref ptr, ptr2, 11u);
				}
				WriteNibble(ref ptr, ptr2, 12 + (num - 1));
				if (ptr < ptr2 && *ptr != 0)
				{
					if (id < 4096)
					{
						*ptr = (byte)(192 + (id >> 8));
						id &= 0xFFu;
					}
					ptr++;
				}
				while (0 < num)
				{
					if (ptr2 <= ptr)
					{
						ptr++;
						break;
					}
					*(ptr++) = (byte)id;
					id >>= 8;
					num--;
				}
			}
			*(int*)((byte*)outPtr + (nint)3 * (nint)4) = (int)(*(uint*)outPtr + *(uint*)((byte*)outPtr + 4) + *(uint*)((byte*)outPtr + (nint)2 * (nint)4) + 1503500717) ^ Environment.ProcessId;
			return (int)(ptr - (byte*)outPtr);
		}

		private unsafe static void WriteNibble(ref byte* ptr, byte* endPtr, uint value)
		{
			if (*ptr != 0)
			{
				byte* intPtr = ptr++;
				*intPtr |= (byte)value;
			}
			else
			{
				*ptr = (byte)(value << 4);
			}
		}
	}

	private AsyncLocal<ActivityInfo> m_current;

	private bool m_checkedForEnable;

	private static readonly ActivityTracker s_activityTrackerInstance = new ActivityTracker();

	private static long m_nextId;

	public static ActivityTracker Instance => s_activityTrackerInstance;

	public void OnStart(string providerName, string activityName, int task, ref Guid activityId, ref Guid relatedActivityId, EventActivityOptions options, bool useTplSource = true)
	{
		if (m_current == null)
		{
			if (m_checkedForEnable)
			{
				return;
			}
			m_checkedForEnable = true;
			if (useTplSource && TplEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)128L))
			{
				Enable();
			}
			if (m_current == null)
			{
				return;
			}
		}
		ActivityInfo value = m_current.Value;
		string text = NormalizeActivityName(providerName, activityName, task);
		TplEventSource tplEventSource = (useTplSource ? TplEventSource.Log : null);
		bool flag = tplEventSource?.Debug ?? false;
		if (flag)
		{
			tplEventSource.DebugFacilityMessage("OnStartEnter", text);
			tplEventSource.DebugFacilityMessage("OnStartEnterActivityState", ActivityInfo.LiveActivities(value));
		}
		if (value != null)
		{
			if (value.m_level >= 100)
			{
				activityId = Guid.Empty;
				relatedActivityId = Guid.Empty;
				if (flag)
				{
					tplEventSource.DebugFacilityMessage("OnStartRET", "Fail");
				}
				return;
			}
			if ((options & EventActivityOptions.Recursive) == 0)
			{
				ActivityInfo activityInfo = FindActiveActivity(text, value);
				if (activityInfo != null)
				{
					OnStop(providerName, activityName, task, ref activityId);
					value = m_current.Value;
				}
			}
		}
		long uniqueId = ((value != null) ? Interlocked.Increment(ref value.m_lastChildID) : Interlocked.Increment(ref m_nextId));
		relatedActivityId = EventSource.CurrentThreadActivityId;
		ActivityInfo activityInfo2 = new ActivityInfo(text, uniqueId, value, relatedActivityId, options);
		m_current.Value = activityInfo2;
		activityId = activityInfo2.ActivityId;
		if (flag)
		{
			tplEventSource.DebugFacilityMessage("OnStartRetActivityState", ActivityInfo.LiveActivities(activityInfo2));
			tplEventSource.DebugFacilityMessage1("OnStartRet", activityId.ToString(), relatedActivityId.ToString());
		}
	}

	public void OnStop(string providerName, string activityName, int task, ref Guid activityId, bool useTplSource = true)
	{
		if (m_current == null)
		{
			return;
		}
		string text = NormalizeActivityName(providerName, activityName, task);
		TplEventSource tplEventSource = (useTplSource ? TplEventSource.Log : null);
		bool flag = tplEventSource?.Debug ?? false;
		if (flag)
		{
			tplEventSource.DebugFacilityMessage("OnStopEnter", text);
			tplEventSource.DebugFacilityMessage("OnStopEnterActivityState", ActivityInfo.LiveActivities(m_current.Value));
		}
		ActivityInfo activityInfo;
		ActivityInfo activityInfo2;
		do
		{
			ActivityInfo value = m_current.Value;
			activityInfo = null;
			activityInfo2 = FindActiveActivity(text, value);
			if (activityInfo2 == null)
			{
				activityId = Guid.Empty;
				if (flag)
				{
					tplEventSource.DebugFacilityMessage("OnStopRET", "Fail");
				}
				return;
			}
			activityId = activityInfo2.ActivityId;
			ActivityInfo activityInfo3 = value;
			while (activityInfo3 != activityInfo2 && activityInfo3 != null)
			{
				if (activityInfo3.m_stopped != 0)
				{
					activityInfo3 = activityInfo3.m_creator;
					continue;
				}
				if (activityInfo3.CanBeOrphan())
				{
					if (activityInfo == null)
					{
						activityInfo = activityInfo3;
					}
				}
				else
				{
					activityInfo3.m_stopped = 1;
				}
				activityInfo3 = activityInfo3.m_creator;
			}
		}
		while (Interlocked.CompareExchange(ref activityInfo2.m_stopped, 1, 0) != 0);
		if (activityInfo == null)
		{
			activityInfo = activityInfo2.m_creator;
		}
		m_current.Value = activityInfo;
		if (flag)
		{
			tplEventSource.DebugFacilityMessage("OnStopRetActivityState", ActivityInfo.LiveActivities(activityInfo));
			tplEventSource.DebugFacilityMessage("OnStopRet", activityId.ToString());
		}
	}

	public void Enable()
	{
		if (m_current == null)
		{
			try
			{
				m_current = new AsyncLocal<ActivityInfo>(ActivityChanging);
			}
			catch (NotImplementedException)
			{
				Debugger.Log(0, null, "Activity Enabled() called but AsyncLocals Not Supported (pre V4.6).  Ignoring Enable");
			}
		}
	}

	private static ActivityInfo FindActiveActivity(string name, ActivityInfo startLocation)
	{
		for (ActivityInfo activityInfo = startLocation; activityInfo != null; activityInfo = activityInfo.m_creator)
		{
			if (name == activityInfo.m_name && activityInfo.m_stopped == 0)
			{
				return activityInfo;
			}
		}
		return null;
	}

	private static string NormalizeActivityName(string providerName, string activityName, int task)
	{
		if (activityName.EndsWith("Start", StringComparison.Ordinal))
		{
			return providerName + activityName.AsSpan(0, activityName.Length - "Start".Length);
		}
		if (activityName.EndsWith("Stop", StringComparison.Ordinal))
		{
			return providerName + activityName.AsSpan(0, activityName.Length - "Stop".Length);
		}
		if (task != 0)
		{
			return providerName + "task" + task;
		}
		return providerName + activityName;
	}

	private void ActivityChanging(AsyncLocalValueChangedArgs<ActivityInfo> args)
	{
		ActivityInfo activityInfo = args.CurrentValue;
		ActivityInfo previousValue = args.PreviousValue;
		if (previousValue != null && previousValue.m_creator == activityInfo && (activityInfo == null || previousValue.m_activityIdToRestore != activityInfo.ActivityId))
		{
			EventSource.SetCurrentThreadActivityId(previousValue.m_activityIdToRestore);
			return;
		}
		while (activityInfo != null)
		{
			if (activityInfo.m_stopped == 0)
			{
				EventSource.SetCurrentThreadActivityId(activityInfo.ActivityId);
				return;
			}
			activityInfo = activityInfo.m_creator;
		}
		EventSource.SetCurrentThreadActivityId(Guid.Empty);
	}
}
