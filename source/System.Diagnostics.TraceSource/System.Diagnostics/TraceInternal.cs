using System.Reflection;

namespace System.Diagnostics;

internal static class TraceInternal
{
	private sealed class TraceProvider : DebugProvider
	{
		public override void Fail(string message, string detailMessage)
		{
			TraceInternal.Fail(message, detailMessage);
		}

		public override void OnIndentLevelChanged(int indentLevel)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.IndentLevel = indentLevel;
				}
			}
		}

		public override void OnIndentSizeChanged(int indentSize)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.IndentSize = indentSize;
				}
			}
		}

		public override void Write(string message)
		{
			TraceInternal.Write(message);
		}

		public override void WriteLine(string message)
		{
			TraceInternal.WriteLine(message);
		}
	}

	private static volatile string s_appName;

	private static volatile TraceListenerCollection s_listeners;

	private static volatile bool s_autoFlush;

	private static volatile bool s_useGlobalLock;

	private static volatile bool s_settingsInitialized;

	internal static readonly object critSec = new object();

	public static TraceListenerCollection Listeners
	{
		get
		{
			InitializeSettings();
			if (s_listeners == null)
			{
				lock (critSec)
				{
					if (s_listeners == null)
					{
						Debug.SetProvider(new TraceProvider());
						s_listeners = new TraceListenerCollection();
						TraceListener traceListener = new DefaultTraceListener();
						traceListener.IndentLevel = Debug.IndentLevel;
						traceListener.IndentSize = Debug.IndentSize;
						s_listeners.Add(traceListener);
					}
				}
			}
			return s_listeners;
		}
	}

	internal static string AppName
	{
		get
		{
			if (s_appName == null)
			{
				s_appName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
			}
			return s_appName;
		}
	}

	public static bool AutoFlush
	{
		get
		{
			InitializeSettings();
			return s_autoFlush;
		}
		set
		{
			InitializeSettings();
			s_autoFlush = value;
		}
	}

	public static bool UseGlobalLock
	{
		get
		{
			InitializeSettings();
			return s_useGlobalLock;
		}
		set
		{
			InitializeSettings();
			s_useGlobalLock = value;
		}
	}

	public static int IndentLevel
	{
		get
		{
			return Debug.IndentLevel;
		}
		set
		{
			Debug.IndentLevel = value;
		}
	}

	public static int IndentSize
	{
		get
		{
			return Debug.IndentSize;
		}
		set
		{
			Debug.IndentSize = value;
		}
	}

	public static void Indent()
	{
		Debug.IndentLevel++;
	}

	public static void Unindent()
	{
		Debug.IndentLevel--;
	}

	public static void Flush()
	{
		if (s_listeners == null)
		{
			return;
		}
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Flush();
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
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

	public static void Close()
	{
		if (s_listeners == null)
		{
			return;
		}
		lock (critSec)
		{
			foreach (TraceListener listener in Listeners)
			{
				listener.Close();
			}
		}
	}

	public static void Assert(bool condition)
	{
		if (!condition)
		{
			Fail(string.Empty);
		}
	}

	public static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			Fail(message);
		}
	}

	public static void Assert(bool condition, string message, string detailMessage)
	{
		if (!condition)
		{
			Fail(message, detailMessage);
		}
	}

	public static void Fail(string message)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Fail(message);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Fail(message);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Fail(message);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void Fail(string message, string detailMessage)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Fail(message, detailMessage);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Fail(message, detailMessage);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Fail(message, detailMessage);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	private static void InitializeSettings()
	{
		if (s_settingsInitialized)
		{
			return;
		}
		lock (critSec)
		{
			if (!s_settingsInitialized)
			{
				s_autoFlush = DiagnosticsConfiguration.AutoFlush;
				s_useGlobalLock = DiagnosticsConfiguration.UseGlobalLock;
				s_settingsInitialized = true;
			}
		}
	}

	internal static void Refresh()
	{
		lock (critSec)
		{
			s_settingsInitialized = false;
			s_listeners = null;
			Debug.IndentSize = DiagnosticsConfiguration.IndentSize;
		}
		InitializeSettings();
	}

	public static void TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
	{
		TraceEventCache eventCache = new TraceEventCache();
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				if (args == null)
				{
					foreach (TraceListener listener in Listeners)
					{
						listener.TraceEvent(eventCache, AppName, eventType, id, format);
						if (AutoFlush)
						{
							listener.Flush();
						}
					}
					return;
				}
				foreach (TraceListener listener2 in Listeners)
				{
					listener2.TraceEvent(eventCache, AppName, eventType, id, format, args);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
				return;
			}
		}
		if (args == null)
		{
			foreach (TraceListener listener3 in Listeners)
			{
				if (!listener3.IsThreadSafe)
				{
					lock (listener3)
					{
						listener3.TraceEvent(eventCache, AppName, eventType, id, format);
						if (AutoFlush)
						{
							listener3.Flush();
						}
					}
				}
				else
				{
					listener3.TraceEvent(eventCache, AppName, eventType, id, format);
					if (AutoFlush)
					{
						listener3.Flush();
					}
				}
			}
			return;
		}
		foreach (TraceListener listener4 in Listeners)
		{
			if (!listener4.IsThreadSafe)
			{
				lock (listener4)
				{
					listener4.TraceEvent(eventCache, AppName, eventType, id, format, args);
					if (AutoFlush)
					{
						listener4.Flush();
					}
				}
			}
			else
			{
				listener4.TraceEvent(eventCache, AppName, eventType, id, format, args);
				if (AutoFlush)
				{
					listener4.Flush();
				}
			}
		}
	}

	public static void Write(string message)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Write(message);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Write(message);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Write(message);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void Write(object value)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Write(value);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Write(value);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Write(value);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void Write(string message, string category)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Write(message, category);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Write(message, category);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Write(message, category);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void Write(object value, string category)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.Write(value, category);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.Write(value, category);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.Write(value, category);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void WriteLine(string message)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.WriteLine(message);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.WriteLine(message);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.WriteLine(message);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void WriteLine(object value)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.WriteLine(value);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.WriteLine(value);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.WriteLine(value);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void WriteLine(string message, string category)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.WriteLine(message, category);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.WriteLine(message, category);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.WriteLine(message, category);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void WriteLine(object value, string category)
	{
		if (UseGlobalLock)
		{
			lock (critSec)
			{
				foreach (TraceListener listener in Listeners)
				{
					listener.WriteLine(value, category);
					if (AutoFlush)
					{
						listener.Flush();
					}
				}
				return;
			}
		}
		foreach (TraceListener listener2 in Listeners)
		{
			if (!listener2.IsThreadSafe)
			{
				lock (listener2)
				{
					listener2.WriteLine(value, category);
					if (AutoFlush)
					{
						listener2.Flush();
					}
				}
			}
			else
			{
				listener2.WriteLine(value, category);
				if (AutoFlush)
				{
					listener2.Flush();
				}
			}
		}
	}

	public static void WriteIf(bool condition, string message)
	{
		if (condition)
		{
			Write(message);
		}
	}

	public static void WriteIf(bool condition, object value)
	{
		if (condition)
		{
			Write(value);
		}
	}

	public static void WriteIf(bool condition, string message, string category)
	{
		if (condition)
		{
			Write(message, category);
		}
	}

	public static void WriteIf(bool condition, object value, string category)
	{
		if (condition)
		{
			Write(value, category);
		}
	}

	public static void WriteLineIf(bool condition, string message)
	{
		if (condition)
		{
			WriteLine(message);
		}
	}

	public static void WriteLineIf(bool condition, object value)
	{
		if (condition)
		{
			WriteLine(value);
		}
	}

	public static void WriteLineIf(bool condition, string message, string category)
	{
		if (condition)
		{
			WriteLine(message, category);
		}
	}

	public static void WriteLineIf(bool condition, object value, string category)
	{
		if (condition)
		{
			WriteLine(value, category);
		}
	}
}
