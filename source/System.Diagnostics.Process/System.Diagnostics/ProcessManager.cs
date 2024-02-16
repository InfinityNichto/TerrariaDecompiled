using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics;

internal static class ProcessManager
{
	public static bool IsRemoteMachine(string machineName)
	{
		if (machineName == null)
		{
			throw new ArgumentNullException("machineName");
		}
		if (machineName.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidParameter, "machineName", machineName));
		}
		return IsRemoteMachineCore(machineName);
	}

	public static bool IsProcessRunning(int processId)
	{
		return IsProcessRunning(processId, ".");
	}

	public static bool IsProcessRunning(int processId, string machineName)
	{
		if (!IsRemoteMachine(machineName))
		{
			using SafeProcessHandle safeProcessHandle = global::Interop.Kernel32.OpenProcess(1024, inherit: false, processId);
			if (!safeProcessHandle.IsInvalid)
			{
				return true;
			}
		}
		return Array.IndexOf(GetProcessIds(machineName), processId) >= 0;
	}

	public static ProcessInfo[] GetProcessInfos(string machineName)
	{
		if (!IsRemoteMachine(machineName))
		{
			return NtProcessInfoHelper.GetProcessInfos();
		}
		return NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
	}

	public static ProcessInfo GetProcessInfo(int processId, string machineName)
	{
		if (IsRemoteMachine(machineName))
		{
			ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
			ProcessInfo[] array = processInfos;
			foreach (ProcessInfo processInfo in array)
			{
				if (processInfo.ProcessId == processId)
				{
					return processInfo;
				}
			}
		}
		else
		{
			ProcessInfo[] processInfos2 = NtProcessInfoHelper.GetProcessInfos(processId);
			if (processInfos2.Length == 1)
			{
				return processInfos2[0];
			}
		}
		return null;
	}

	public static int[] GetProcessIds(string machineName)
	{
		if (!IsRemoteMachine(machineName))
		{
			return GetProcessIds();
		}
		return NtProcessManager.GetProcessIds(machineName, isRemoteMachine: true);
	}

	public static int[] GetProcessIds()
	{
		return NtProcessManager.GetProcessIds();
	}

	public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
	{
		return NtProcessManager.GetProcessIdFromHandle(processHandle);
	}

	public static ProcessModuleCollection GetModules(int processId)
	{
		return NtProcessManager.GetModules(processId);
	}

	private static bool IsRemoteMachineCore(string machineName)
	{
		ReadOnlySpan<char> span = machineName.AsSpan(machineName.StartsWith('\\') ? 2 : 0);
		if (!MemoryExtensions.Equals(span, ".", StringComparison.Ordinal))
		{
			return !MemoryExtensions.Equals(span, global::Interop.Kernel32.GetComputerName(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	unsafe static ProcessManager()
	{
		if (!global::Interop.Advapi32.LookupPrivilegeValue(null, "SeDebugPrivilege", out var lpLuid))
		{
			return;
		}
		SafeTokenHandle TokenHandle = null;
		try
		{
			if (global::Interop.Advapi32.OpenProcessToken(global::Interop.Kernel32.GetCurrentProcess(), 32, out TokenHandle))
			{
				Unsafe.SkipInit(out global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE);
				tOKEN_PRIVILEGE.PrivilegeCount = 1u;
				tOKEN_PRIVILEGE.Privileges.Luid = lpLuid;
				tOKEN_PRIVILEGE.Privileges.Attributes = 2u;
				global::Interop.Advapi32.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges: false, &tOKEN_PRIVILEGE, 0u, null, null);
			}
		}
		finally
		{
			TokenHandle?.Dispose();
		}
	}

	public static SafeProcessHandle OpenProcess(int processId, int access, bool throwIfExited)
	{
		SafeProcessHandle safeProcessHandle = global::Interop.Kernel32.OpenProcess(access, inherit: false, processId);
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (!safeProcessHandle.IsInvalid)
		{
			return safeProcessHandle;
		}
		if (processId == 0)
		{
			throw new Win32Exception(5);
		}
		if (lastWin32Error != 5 && !IsProcessRunning(processId))
		{
			if (throwIfExited)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ProcessHasExited, processId.ToString()));
			}
			return SafeProcessHandle.InvalidHandle;
		}
		throw new Win32Exception(lastWin32Error);
	}

	public static SafeThreadHandle OpenThread(int threadId, int access)
	{
		SafeThreadHandle safeThreadHandle = global::Interop.Kernel32.OpenThread(access, inherit: false, threadId);
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (safeThreadHandle.IsInvalid)
		{
			if (lastWin32Error == 87)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ThreadExited, threadId.ToString()));
			}
			throw new Win32Exception(lastWin32Error);
		}
		return safeThreadHandle;
	}

	public static IntPtr GetMainWindowHandle(int processId)
	{
		return MainWindowFinder.FindMainWindow(processId);
	}
}
