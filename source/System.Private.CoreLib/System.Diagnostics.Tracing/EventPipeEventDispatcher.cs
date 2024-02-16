using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.Tracing;

internal sealed class EventPipeEventDispatcher
{
	internal sealed class EventListenerSubscription
	{
		internal EventKeywords MatchAnyKeywords { get; private set; }

		internal EventLevel Level { get; private set; }

		internal EventListenerSubscription(EventKeywords matchAnyKeywords, EventLevel level)
		{
			MatchAnyKeywords = matchAnyKeywords;
			Level = level;
		}
	}

	internal static readonly EventPipeEventDispatcher Instance = new EventPipeEventDispatcher();

	private readonly IntPtr m_RuntimeProviderID;

	private ulong m_sessionID;

	private DateTime m_syncTimeUtc;

	private long m_syncTimeQPC;

	private long m_timeQPCFrequency;

	private bool m_stopDispatchTask;

	private readonly EventPipeWaitHandle m_dispatchTaskWaitHandle = new EventPipeWaitHandle();

	private Task m_dispatchTask;

	private readonly object m_dispatchControlLock = new object();

	private readonly Dictionary<EventListener, EventListenerSubscription> m_subscriptions = new Dictionary<EventListener, EventListenerSubscription>();

	private EventPipeEventDispatcher()
	{
		m_RuntimeProviderID = EventPipeInternal.GetProvider("Microsoft-Windows-DotNETRuntime");
		m_dispatchTaskWaitHandle.SafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, ownsHandle: false);
	}

	internal void SendCommand(EventListener eventListener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeywords)
	{
		if (command == EventCommand.Update && enable)
		{
			lock (m_dispatchControlLock)
			{
				m_subscriptions[eventListener] = new EventListenerSubscription(matchAnyKeywords, level);
				CommitDispatchConfiguration();
				return;
			}
		}
		if (command == EventCommand.Update && !enable)
		{
			RemoveEventListener(eventListener);
		}
	}

	internal void RemoveEventListener(EventListener listener)
	{
		lock (m_dispatchControlLock)
		{
			if (m_subscriptions.ContainsKey(listener))
			{
				m_subscriptions.Remove(listener);
			}
			CommitDispatchConfiguration();
		}
	}

	private unsafe void CommitDispatchConfiguration()
	{
		StopDispatchTask();
		EventPipeInternal.Disable(m_sessionID);
		if (m_subscriptions.Count <= 0)
		{
			return;
		}
		EventKeywords eventKeywords = EventKeywords.None;
		EventLevel eventLevel = EventLevel.LogAlways;
		foreach (EventListenerSubscription value in m_subscriptions.Values)
		{
			eventKeywords |= value.MatchAnyKeywords;
			eventLevel = ((value.Level > eventLevel) ? value.Level : eventLevel);
		}
		EventPipeProviderConfiguration[] providers = new EventPipeProviderConfiguration[1]
		{
			new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntime", (ulong)eventKeywords, (uint)eventLevel, null)
		};
		m_sessionID = EventPipeInternal.Enable(null, EventPipeSerializationFormat.NetTrace, 10u, providers);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out EventPipeSessionInfo eventPipeSessionInfo);
		EventPipeInternal.GetSessionInfo(m_sessionID, &eventPipeSessionInfo);
		m_syncTimeUtc = DateTime.FromFileTimeUtc(eventPipeSessionInfo.StartTimeAsUTCFileTime);
		m_syncTimeQPC = eventPipeSessionInfo.StartTimeStamp;
		m_timeQPCFrequency = eventPipeSessionInfo.TimeStampFrequency;
		StartDispatchTask();
	}

	private void StartDispatchTask()
	{
		if (m_dispatchTask == null)
		{
			m_stopDispatchTask = false;
			m_dispatchTaskWaitHandle.SafeWaitHandle = new SafeWaitHandle(EventPipeInternal.GetWaitHandle(m_sessionID), ownsHandle: false);
			m_dispatchTask = Task.Factory.StartNew(DispatchEventsToEventListeners, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
	}

	private void StopDispatchTask()
	{
		if (m_dispatchTask != null)
		{
			m_stopDispatchTask = true;
			EventWaitHandle.Set(m_dispatchTaskWaitHandle.SafeWaitHandle);
			m_dispatchTask.Wait();
			m_dispatchTask = null;
		}
	}

	private unsafe void DispatchEventsToEventListeners()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out EventPipeEventInstanceData eventPipeEventInstanceData);
		while (!m_stopDispatchTask)
		{
			bool flag = false;
			while (!m_stopDispatchTask && EventPipeInternal.GetNextEvent(m_sessionID, &eventPipeEventInstanceData))
			{
				flag = true;
				if (eventPipeEventInstanceData.ProviderID == m_RuntimeProviderID)
				{
					ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>((void*)eventPipeEventInstanceData.Payload, (int)eventPipeEventInstanceData.PayloadLength);
					DateTime timeStamp = TimeStampToDateTime(eventPipeEventInstanceData.TimeStamp);
					NativeRuntimeEventSource.Log.ProcessEvent(eventPipeEventInstanceData.EventID, eventPipeEventInstanceData.ThreadID, timeStamp, eventPipeEventInstanceData.ActivityId, eventPipeEventInstanceData.ChildActivityId, payload);
				}
			}
			if (!m_stopDispatchTask)
			{
				if (!flag)
				{
					m_dispatchTaskWaitHandle.WaitOne();
				}
				Thread.Sleep(10);
			}
		}
	}

	private DateTime TimeStampToDateTime(long timeStamp)
	{
		if (timeStamp == long.MaxValue)
		{
			return DateTime.MaxValue;
		}
		long num = (long)((double)(timeStamp - m_syncTimeQPC) * 10000000.0 / (double)m_timeQPCFrequency) + m_syncTimeUtc.Ticks;
		if (num < 0 || 3155378975999999999L < num)
		{
			num = 3155378975999999999L;
		}
		return new DateTime(num, DateTimeKind.Utc);
	}
}
