#define TRACE
using System.Collections.Generic;
using System.Collections.Specialized;

namespace System.Diagnostics;

public class TraceSource
{
	private static readonly List<WeakReference<TraceSource>> s_tracesources = new List<WeakReference<TraceSource>>();

	private static int s_LastCollectionCount;

	private volatile SourceSwitch _internalSwitch;

	private volatile TraceListenerCollection _listeners;

	private readonly SourceLevels _switchLevel;

	private readonly string _sourceName;

	internal volatile bool _initCalled;

	private StringDictionary _attributes;

	public StringDictionary Attributes
	{
		get
		{
			Initialize();
			if (_attributes == null)
			{
				_attributes = new StringDictionary();
			}
			return _attributes;
		}
	}

	public string Name => _sourceName;

	public TraceListenerCollection Listeners
	{
		get
		{
			Initialize();
			return _listeners;
		}
	}

	public SourceSwitch Switch
	{
		get
		{
			Initialize();
			return _internalSwitch;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("Switch");
			}
			Initialize();
			_internalSwitch = value;
		}
	}

	public TraceSource(string name)
		: this(name, SourceLevels.Off)
	{
	}

	public TraceSource(string name, SourceLevels defaultLevel)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullEmptyArgument, "name"), "name");
		}
		_sourceName = name;
		_switchLevel = defaultLevel;
		lock (s_tracesources)
		{
			_pruneCachedTraceSources();
			s_tracesources.Add(new WeakReference<TraceSource>(this));
		}
	}

	private static void _pruneCachedTraceSources()
	{
		lock (s_tracesources)
		{
			if (s_LastCollectionCount == GC.CollectionCount(2))
			{
				return;
			}
			List<WeakReference<TraceSource>> list = new List<WeakReference<TraceSource>>(s_tracesources.Count);
			for (int i = 0; i < s_tracesources.Count; i++)
			{
				if (s_tracesources[i].TryGetTarget(out var _))
				{
					list.Add(s_tracesources[i]);
				}
			}
			if (list.Count < s_tracesources.Count)
			{
				s_tracesources.Clear();
				s_tracesources.AddRange(list);
				s_tracesources.TrimExcess();
			}
			s_LastCollectionCount = GC.CollectionCount(2);
		}
	}

	private void Initialize()
	{
		if (_initCalled)
		{
			return;
		}
		lock (this)
		{
			if (!_initCalled)
			{
				NoConfigInit();
				_initCalled = true;
			}
		}
	}

	private void NoConfigInit()
	{
		_internalSwitch = new SourceSwitch(_sourceName, _switchLevel.ToString());
		_listeners = new TraceListenerCollection();
		_listeners.Add(new DefaultTraceListener());
	}

	public void Close()
	{
		if (_listeners == null)
		{
			return;
		}
		lock (TraceInternal.critSec)
		{
			foreach (TraceListener listener in _listeners)
			{
				listener.Close();
			}
		}
	}

	public void Flush()
	{
		if (_listeners == null)
		{
			return;
		}
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				foreach (TraceListener listener in _listeners)
				{
					listener.Flush();
				}
				return;
			}
		}
		foreach (TraceListener listener2 in _listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Flush();
				}
			}
			else
			{
				listener2.Flush();
			}
		}
	}

	protected internal virtual string[]? GetSupportedAttributes()
	{
		return null;
	}

	internal static void RefreshAll()
	{
		lock (s_tracesources)
		{
			_pruneCachedTraceSources();
			for (int i = 0; i < s_tracesources.Count; i++)
			{
				if (s_tracesources[i].TryGetTarget(out var target))
				{
					target.Refresh();
				}
			}
		}
	}

	internal void Refresh()
	{
		if (!_initCalled)
		{
			Initialize();
		}
	}

	[Conditional("TRACE")]
	public void TraceEvent(TraceEventType eventType, int id)
	{
		Initialize();
		if (!_internalSwitch.ShouldTrace(eventType) || _listeners == null)
		{
			return;
		}
		TraceEventCache eventCache = new TraceEventCache();
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceEvent(eventCache, Name, eventType, id);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceEvent(eventCache, Name, eventType, id);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceEvent(eventCache, Name, eventType, id);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}

	[Conditional("TRACE")]
	public void TraceEvent(TraceEventType eventType, int id, string? message)
	{
		Initialize();
		if (!_internalSwitch.ShouldTrace(eventType) || _listeners == null)
		{
			return;
		}
		TraceEventCache eventCache = new TraceEventCache();
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceEvent(eventCache, Name, eventType, id, message);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceEvent(eventCache, Name, eventType, id, message);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceEvent(eventCache, Name, eventType, id, message);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}

	[Conditional("TRACE")]
	public void TraceEvent(TraceEventType eventType, int id, string? format, params object?[]? args)
	{
		Initialize();
		if (!_internalSwitch.ShouldTrace(eventType) || _listeners == null)
		{
			return;
		}
		TraceEventCache eventCache = new TraceEventCache();
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceEvent(eventCache, Name, eventType, id, format, args);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceEvent(eventCache, Name, eventType, id, format, args);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceEvent(eventCache, Name, eventType, id, format, args);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}

	[Conditional("TRACE")]
	public void TraceData(TraceEventType eventType, int id, object? data)
	{
		Initialize();
		if (!_internalSwitch.ShouldTrace(eventType) || _listeners == null)
		{
			return;
		}
		TraceEventCache eventCache = new TraceEventCache();
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceData(eventCache, Name, eventType, id, data);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceData(eventCache, Name, eventType, id, data);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceData(eventCache, Name, eventType, id, data);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}

	[Conditional("TRACE")]
	public void TraceData(TraceEventType eventType, int id, params object?[]? data)
	{
		Initialize();
		if (!_internalSwitch.ShouldTrace(eventType) || _listeners == null)
		{
			return;
		}
		TraceEventCache eventCache = new TraceEventCache();
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceData(eventCache, Name, eventType, id, data);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceData(eventCache, Name, eventType, id, data);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceData(eventCache, Name, eventType, id, data);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}

	[Conditional("TRACE")]
	public void TraceInformation(string? message)
	{
		TraceEvent(TraceEventType.Information, 0, message, null);
	}

	[Conditional("TRACE")]
	public void TraceInformation(string? format, params object?[]? args)
	{
		TraceEvent(TraceEventType.Information, 0, format, args);
	}

	[Conditional("TRACE")]
	public void TraceTransfer(int id, string? message, Guid relatedActivityId)
	{
		Initialize();
		TraceEventCache eventCache = new TraceEventCache();
		if (!_internalSwitch.ShouldTrace(TraceEventType.Transfer) || _listeners == null)
		{
			return;
		}
		if (TraceInternal.UseGlobalLock)
		{
			lock (TraceInternal.critSec)
			{
				for (int i = 0; i < _listeners.Count; i++)
				{
					TraceListener traceListener = _listeners[i];
					traceListener.TraceTransfer(eventCache, Name, id, message, relatedActivityId);
					if (Trace.AutoFlush)
					{
						traceListener.Flush();
					}
				}
				return;
			}
		}
		for (int j = 0; j < _listeners.Count; j++)
		{
			TraceListener traceListener2 = _listeners[j];
			if (!traceListener2.IsThreadSafe)
			{
				lock (traceListener2)
				{
					traceListener2.TraceTransfer(eventCache, Name, id, message, relatedActivityId);
					if (Trace.AutoFlush)
					{
						traceListener2.Flush();
					}
				}
			}
			else
			{
				traceListener2.TraceTransfer(eventCache, Name, id, message, relatedActivityId);
				if (Trace.AutoFlush)
				{
					traceListener2.Flush();
				}
			}
		}
	}
}
