using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

public static class Debugger
{
	private sealed class CrossThreadDependencyNotification : ICustomDebuggerNotification
	{
	}

	public static readonly string? DefaultCategory;

	public static extern bool IsAttached
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Break()
	{
		BreakInternal();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void BreakInternal();

	public static bool Launch()
	{
		if (!IsAttached)
		{
			return LaunchInternal();
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void NotifyOfCrossThreadDependencySlow()
	{
		CustomNotification(new CrossThreadDependencyNotification());
	}

	public static void NotifyOfCrossThreadDependency()
	{
		if (IsAttached)
		{
			NotifyOfCrossThreadDependencySlow();
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern bool LaunchInternal();

	public static void Log(int level, string? category, string? message)
	{
		LogInternal(level, category, message);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void LogInternal(int level, string category, string message);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool IsLogging();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void CustomNotification(ICustomDebuggerNotification data);
}
