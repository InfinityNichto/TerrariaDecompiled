namespace System.Diagnostics.Tracing;

internal static class RuntimeEventSourceHelper
{
	private static long s_prevProcUserTime;

	private static long s_prevProcKernelTime;

	private static long s_prevSystemUserTime;

	private static long s_prevSystemKernelTime;

	internal static int GetCpuUsage()
	{
		int result = 0;
		if (Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out var _, out var exit, out var kernel, out var user) && Interop.Kernel32.GetSystemTimes(out exit, out var kernel2, out var user2))
		{
			long num = user - s_prevProcUserTime + (kernel - s_prevProcKernelTime);
			long num2 = kernel2 - s_prevSystemUserTime + (user2 - s_prevSystemKernelTime);
			if (s_prevSystemUserTime != 0L && s_prevSystemKernelTime != 0L && num2 != 0L)
			{
				result = (int)(num * 100 / num2);
			}
			s_prevProcUserTime = user;
			s_prevProcKernelTime = kernel;
			s_prevSystemUserTime = kernel2;
			s_prevSystemKernelTime = user2;
		}
		return result;
	}
}
