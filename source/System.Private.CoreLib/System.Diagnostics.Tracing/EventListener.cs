using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics.Tracing;

public class EventListener : IDisposable
{
	[CompilerGenerated]
	private EventHandler<EventSourceCreatedEventArgs> _EventSourceCreated;

	internal volatile EventListener m_Next;

	internal static EventListener s_Listeners;

	internal static List<WeakReference<EventSource>> s_EventSources;

	private static bool s_CreatingListener;

	internal static object EventListenersLock
	{
		get
		{
			if (s_EventSources == null)
			{
				Interlocked.CompareExchange(ref s_EventSources, new List<WeakReference<EventSource>>(2), null);
			}
			return s_EventSources;
		}
	}

	public event EventHandler<EventSourceCreatedEventArgs>? EventSourceCreated
	{
		add
		{
			CallBackForExistingEventSources(addToListenersList: false, value);
			_EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Combine(_EventSourceCreated, value);
		}
		remove
		{
			_EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Remove(_EventSourceCreated, value);
		}
	}

	public event EventHandler<EventWrittenEventArgs>? EventWritten;

	static EventListener()
	{
		GC.KeepAlive(NativeRuntimeEventSource.Log);
	}

	public EventListener()
	{
		CallBackForExistingEventSources(addToListenersList: true, delegate(object obj, EventSourceCreatedEventArgs args)
		{
			args.EventSource.AddListener((EventListener)obj);
		});
	}

	public virtual void Dispose()
	{
		lock (EventListenersLock)
		{
			if (s_Listeners == null)
			{
				return;
			}
			if (this == s_Listeners)
			{
				EventListener listenerToRemove = s_Listeners;
				s_Listeners = m_Next;
				RemoveReferencesToListenerInEventSources(listenerToRemove);
				return;
			}
			EventListener eventListener = s_Listeners;
			EventListener next;
			while (true)
			{
				next = eventListener.m_Next;
				if (next == null)
				{
					return;
				}
				if (next == this)
				{
					break;
				}
				eventListener = next;
			}
			eventListener.m_Next = next.m_Next;
			RemoveReferencesToListenerInEventSources(next);
		}
	}

	public void EnableEvents(EventSource eventSource, EventLevel level)
	{
		EnableEvents(eventSource, level, EventKeywords.None);
	}

	public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
	{
		EnableEvents(eventSource, level, matchAnyKeyword, null);
	}

	public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string?>? arguments)
	{
		if (eventSource == null)
		{
			throw new ArgumentNullException("eventSource");
		}
		eventSource.SendCommand(this, EventProviderType.None, 0, 0, EventCommand.Update, enable: true, level, matchAnyKeyword, arguments);
		if (eventSource.GetType() == typeof(NativeRuntimeEventSource))
		{
			EventPipeEventDispatcher.Instance.SendCommand(this, EventCommand.Update, enable: true, level, matchAnyKeyword);
		}
	}

	public void DisableEvents(EventSource eventSource)
	{
		if (eventSource == null)
		{
			throw new ArgumentNullException("eventSource");
		}
		eventSource.SendCommand(this, EventProviderType.None, 0, 0, EventCommand.Update, enable: false, EventLevel.LogAlways, EventKeywords.None, null);
		if (eventSource.GetType() == typeof(NativeRuntimeEventSource))
		{
			EventPipeEventDispatcher.Instance.SendCommand(this, EventCommand.Update, enable: false, EventLevel.LogAlways, EventKeywords.None);
		}
	}

	public static int EventSourceIndex(EventSource eventSource)
	{
		return eventSource.m_id;
	}

	protected internal virtual void OnEventSourceCreated(EventSource eventSource)
	{
		EventHandler<EventSourceCreatedEventArgs> eventSourceCreated = _EventSourceCreated;
		if (eventSourceCreated != null)
		{
			EventSourceCreatedEventArgs eventSourceCreatedEventArgs = new EventSourceCreatedEventArgs();
			eventSourceCreatedEventArgs.EventSource = eventSource;
			eventSourceCreated(this, eventSourceCreatedEventArgs);
		}
	}

	protected internal virtual void OnEventWritten(EventWrittenEventArgs eventData)
	{
		this.EventWritten?.Invoke(this, eventData);
	}

	internal static void AddEventSource(EventSource newEventSource)
	{
		lock (EventListenersLock)
		{
			int num = -1;
			if (s_EventSources.Count % 64 == 63)
			{
				int num2 = s_EventSources.Count;
				while (0 < num2)
				{
					num2--;
					WeakReference<EventSource> weakReference = s_EventSources[num2];
					if (!weakReference.TryGetTarget(out var _))
					{
						num = num2;
						weakReference.SetTarget(newEventSource);
						break;
					}
				}
			}
			if (num < 0)
			{
				num = s_EventSources.Count;
				s_EventSources.Add(new WeakReference<EventSource>(newEventSource));
			}
			newEventSource.m_id = num;
			for (EventListener next = s_Listeners; next != null; next = next.m_Next)
			{
				newEventSource.AddListener(next);
			}
		}
	}

	internal static void DisposeOnShutdown()
	{
		List<EventSource> list = new List<EventSource>();
		lock (EventListenersLock)
		{
			foreach (WeakReference<EventSource> s_EventSource in s_EventSources)
			{
				if (s_EventSource.TryGetTarget(out var target))
				{
					list.Add(target);
				}
			}
		}
		foreach (EventSource item in list)
		{
			item.Dispose();
		}
	}

	private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
	{
		foreach (WeakReference<EventSource> s_EventSource in s_EventSources)
		{
			if (!s_EventSource.TryGetTarget(out var target))
			{
				continue;
			}
			if (target.m_Dispatchers.m_Listener == listenerToRemove)
			{
				target.m_Dispatchers = target.m_Dispatchers.m_Next;
				continue;
			}
			EventDispatcher eventDispatcher = target.m_Dispatchers;
			while (true)
			{
				EventDispatcher next = eventDispatcher.m_Next;
				if (next == null)
				{
					break;
				}
				if (next.m_Listener == listenerToRemove)
				{
					eventDispatcher.m_Next = next.m_Next;
					break;
				}
				eventDispatcher = next;
			}
		}
		EventPipeEventDispatcher.Instance.RemoveEventListener(listenerToRemove);
	}

	private void CallBackForExistingEventSources(bool addToListenersList, EventHandler<EventSourceCreatedEventArgs> callback)
	{
		lock (EventListenersLock)
		{
			if (s_CreatingListener)
			{
				throw new InvalidOperationException(SR.EventSource_ListenerCreatedInsideCallback);
			}
			try
			{
				s_CreatingListener = true;
				if (addToListenersList)
				{
					m_Next = s_Listeners;
					s_Listeners = this;
				}
				if (callback == null)
				{
					return;
				}
				WeakReference<EventSource>[] array = s_EventSources.ToArray();
				foreach (WeakReference<EventSource> weakReference in array)
				{
					if (weakReference.TryGetTarget(out var target))
					{
						EventSourceCreatedEventArgs eventSourceCreatedEventArgs = new EventSourceCreatedEventArgs();
						eventSourceCreatedEventArgs.EventSource = target;
						callback(this, eventSourceCreatedEventArgs);
					}
				}
			}
			finally
			{
				s_CreatingListener = false;
			}
		}
	}
}
