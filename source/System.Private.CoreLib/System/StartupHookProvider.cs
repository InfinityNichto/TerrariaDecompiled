using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace System;

internal static class StartupHookProvider
{
	private struct StartupHookNameOrPath
	{
		public AssemblyName AssemblyName;

		public string Path;
	}

	private static bool IsSupported
	{
		get
		{
			if (!AppContext.TryGetSwitch("System.StartupHookProvider.IsSupported", out var isEnabled))
			{
				return true;
			}
			return isEnabled;
		}
	}

	private static void ProcessStartupHooks()
	{
		if (!IsSupported)
		{
			return;
		}
		if (EventSource.IsSupported)
		{
			RuntimeEventSource.Initialize();
		}
		if (!(AppContext.GetData("STARTUP_HOOKS") is string text))
		{
			return;
		}
		Span<char> span = stackalloc char[4]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar,
			' ',
			','
		};
		ReadOnlySpan<char> readOnlySpan = span;
		string[] array = text.Split(Path.PathSeparator);
		StartupHookNameOrPath[] array2 = new StartupHookNameOrPath[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i];
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			if (Path.IsPathFullyQualified(text2))
			{
				array2[i].Path = text2;
				continue;
			}
			for (int j = 0; j < readOnlySpan.Length; j++)
			{
				if (text2.Contains(readOnlySpan[j]))
				{
					throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, text2));
				}
			}
			if (text2.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, text2));
			}
			try
			{
				array2[i].AssemblyName = new AssemblyName(text2);
			}
			catch (Exception innerException)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, text2), innerException);
			}
		}
		StartupHookNameOrPath[] array3 = array2;
		foreach (StartupHookNameOrPath startupHook in array3)
		{
			CallStartupHook(startupHook);
		}
	}

	[RequiresUnreferencedCode("The StartupHookSupport feature switch has been enabled for this app which is being trimmed. Startup hook code is not observable by the trimmer and so required assemblies, types and members may be removed")]
	private static void CallStartupHook(StartupHookNameOrPath startupHook)
	{
		Assembly assembly;
		try
		{
			if (startupHook.Path != null)
			{
				assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(startupHook.Path);
			}
			else
			{
				if (startupHook.AssemblyName == null)
				{
					return;
				}
				assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(startupHook.AssemblyName);
			}
		}
		catch (Exception innerException)
		{
			throw new ArgumentException(SR.Format(SR.Argument_StartupHookAssemblyLoadFailed, startupHook.Path ?? startupHook.AssemblyName.ToString()), innerException);
		}
		Type type = assembly.GetType("StartupHook", throwOnError: true);
		MethodInfo method = type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		bool flag = false;
		if (method == null)
		{
			try
			{
				method = type.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch (AmbiguousMatchException)
			{
			}
			if (!(method != null))
			{
				throw new MissingMethodException("StartupHook", "Initialize");
			}
			flag = true;
		}
		else if (method.ReturnType != typeof(void))
		{
			flag = true;
		}
		if (flag)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSignature, "StartupHook" + Type.Delimiter + "Initialize", startupHook.Path ?? startupHook.AssemblyName.ToString()));
		}
		method.Invoke(null, null);
	}
}
