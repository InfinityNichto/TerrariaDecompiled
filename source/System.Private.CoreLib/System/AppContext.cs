using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;

namespace System;

public static class AppContext
{
	private static Dictionary<string, object> s_dataStore;

	private static Dictionary<string, bool> s_switches;

	private static string s_defaultBaseDirectory;

	public static string BaseDirectory => (GetData("APP_CONTEXT_BASE_DIRECTORY") as string) ?? s_defaultBaseDirectory ?? (s_defaultBaseDirectory = GetBaseDirectoryCore());

	public static string? TargetFrameworkName => Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

	public static event UnhandledExceptionEventHandler? UnhandledException;

	public static event EventHandler<FirstChanceExceptionEventArgs>? FirstChanceException;

	public static event EventHandler? ProcessExit;

	public static object? GetData(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (s_dataStore == null)
		{
			return null;
		}
		lock (s_dataStore)
		{
			s_dataStore.TryGetValue(name, out var value);
			return value;
		}
	}

	public static void SetData(string name, object? data)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (s_dataStore == null)
		{
			Interlocked.CompareExchange(ref s_dataStore, new Dictionary<string, object>(), null);
		}
		lock (s_dataStore)
		{
			s_dataStore[name] = data;
		}
	}

	internal static void OnProcessExit()
	{
		AssemblyLoadContext.OnProcessExit();
		if (EventSource.IsSupported)
		{
			EventListener.DisposeOnShutdown();
		}
		AppContext.ProcessExit?.Invoke(AppDomain.CurrentDomain, EventArgs.Empty);
	}

	public static bool TryGetSwitch(string switchName, out bool isEnabled)
	{
		if (switchName == null)
		{
			throw new ArgumentNullException("switchName");
		}
		if (switchName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "switchName");
		}
		if (s_switches != null)
		{
			lock (s_switches)
			{
				if (s_switches.TryGetValue(switchName, out isEnabled))
				{
					return true;
				}
			}
		}
		if (GetData(switchName) is string value && bool.TryParse(value, out isEnabled))
		{
			return true;
		}
		isEnabled = false;
		return false;
	}

	public static void SetSwitch(string switchName, bool isEnabled)
	{
		if (switchName == null)
		{
			throw new ArgumentNullException("switchName");
		}
		if (switchName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "switchName");
		}
		if (s_switches == null)
		{
			Interlocked.CompareExchange(ref s_switches, new Dictionary<string, bool>(), null);
		}
		lock (s_switches)
		{
			s_switches[switchName] = isEnabled;
		}
	}

	internal unsafe static void Setup(char** pNames, char** pValues, int count)
	{
		s_dataStore = new Dictionary<string, object>(count);
		for (int i = 0; i < count; i++)
		{
			s_dataStore.Add(new string(pNames[i]), new string(pValues[i]));
		}
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "Single File apps should always set APP_CONTEXT_BASE_DIRECTORY therefore code handles Assembly.Location equals null")]
	private static string GetBaseDirectoryCore()
	{
		string path = Assembly.GetEntryAssembly()?.Location;
		string text = Path.GetDirectoryName(path);
		if (text == null)
		{
			return string.Empty;
		}
		if (!Path.EndsInDirectorySeparator(text))
		{
			text += "\\";
		}
		return text;
	}

	internal static void LogSwitchValues(RuntimeEventSource ev)
	{
		if (s_switches != null)
		{
			lock (s_switches)
			{
				foreach (KeyValuePair<string, bool> s_switch in s_switches)
				{
					ev.LogAppContextSwitch(s_switch.Key, s_switch.Value ? 1 : 0);
				}
			}
		}
		if (s_dataStore == null)
		{
			return;
		}
		lock (s_dataStore)
		{
			if (s_switches != null)
			{
				lock (s_switches)
				{
					LogDataStore(ev, s_switches);
					return;
				}
			}
			LogDataStore(ev, null);
		}
		static void LogDataStore(RuntimeEventSource ev, Dictionary<string, bool> switches)
		{
			foreach (KeyValuePair<string, object> item in s_dataStore)
			{
				if (item.Value is string value && bool.TryParse(value, out var result) && (switches == null || !switches.ContainsKey(item.Key)))
				{
					ev.LogAppContextSwitch(item.Key, result ? 1 : 0);
				}
			}
		}
	}
}
