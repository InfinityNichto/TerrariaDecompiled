using System.Runtime.CompilerServices;

namespace System.Runtime;

public static class JitInfo
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetCompiledILBytes(bool currentThread = false);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetCompiledMethodCount(bool currentThread = false);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern long GetCompilationTimeInTicks(bool currentThread = false);

	public static TimeSpan GetCompilationTime(bool currentThread = false)
	{
		return TimeSpan.FromTicks(GetCompilationTimeInTicks(currentThread));
	}
}
