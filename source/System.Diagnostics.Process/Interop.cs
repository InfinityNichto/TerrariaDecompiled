using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class Kernel32
	{
		internal struct PROCESS_INFORMATION
		{
			internal IntPtr hProcess;

			internal IntPtr hThread;

			internal int dwProcessId;

			internal int dwThreadId;
		}

		internal struct STARTUPINFO
		{
			internal int cb;

			internal IntPtr lpReserved;

			internal IntPtr lpDesktop;

			internal IntPtr lpTitle;

			internal int dwX;

			internal int dwY;

			internal int dwXSize;

			internal int dwYSize;

			internal int dwXCountChars;

			internal int dwYCountChars;

			internal int dwFillAttribute;

			internal int dwFlags;

			internal short wShowWindow;

			internal short cbReserved2;

			internal IntPtr lpReserved2;

			internal IntPtr hStdInput;

			internal IntPtr hStdOutput;

			internal IntPtr hStdError;
		}

		internal struct NtModuleInfo
		{
			internal IntPtr BaseOfDll;

			internal int SizeOfImage;

			internal IntPtr EntryPoint;
		}

		internal sealed class ProcessWaitHandle : WaitHandle
		{
			internal ProcessWaitHandle(SafeProcessHandle processHandle)
			{
				IntPtr currentProcess = GetCurrentProcess();
				if (!DuplicateHandle(currentProcess, (SafeHandle)processHandle, currentProcess, out SafeWaitHandle targetHandle, 0, bInheritHandle: false, 2))
				{
					int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
					targetHandle.Dispose();
					Marshal.ThrowExceptionForHR(hRForLastWin32Error);
				}
				this.SetSafeWaitHandle(targetHandle);
			}
		}

		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct CPINFOEXW
		{
			internal uint MaxCharSize;

			internal unsafe fixed byte DefaultChar[2];

			internal unsafe fixed byte LeadByte[12];

			internal char UnicodeDefaultChar;

			internal uint CodePage;

			internal unsafe fixed char CodePageName[260];
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "K32EnumProcessModules", SetLastError = true)]
		internal static extern bool EnumProcessModules(SafeProcessHandle handle, IntPtr[] modules, int size, out int needed);

		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, void* lpBuffer, int nSize, IntPtr arguments);

		internal static string GetMessage(int errorCode)
		{
			return GetMessage(errorCode, IntPtr.Zero);
		}

		internal unsafe static string GetMessage(int errorCode, IntPtr moduleHandle)
		{
			int num = 12800;
			if (moduleHandle != IntPtr.Zero)
			{
				num |= 0x800;
			}
			Span<char> span = stackalloc char[256];
			fixed (char* lpBuffer = span)
			{
				int num2 = FormatMessage(num, moduleHandle, (uint)errorCode, 0, lpBuffer, span.Length, IntPtr.Zero);
				if (num2 > 0)
				{
					return GetAndTrimString(span.Slice(0, num2));
				}
			}
			if (Marshal.GetLastWin32Error() == 122)
			{
				IntPtr intPtr = default(IntPtr);
				try
				{
					int num3 = FormatMessage(num | 0x100, moduleHandle, (uint)errorCode, 0, &intPtr, 0, IntPtr.Zero);
					if (num3 > 0)
					{
						return GetAndTrimString(new Span<char>((void*)intPtr, num3));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			return $"Unknown error (0x{errorCode:x})";
		}

		private static string GetAndTrimString(Span<char> buffer)
		{
			int num = buffer.Length;
			while (num > 0 && buffer[num - 1] <= ' ')
			{
				num--;
			}
			return buffer.Slice(0, num).ToString();
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool IsWow64Process(SafeProcessHandle hProcess, out bool Wow64Process);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out int exitCode);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetProcessTimes(SafeProcessHandle handle, out long creation, out long exit, out long kernel, out long user);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetThreadTimes(SafeThreadHandle handle, out long creation, out long exit, out long kernel, out long user);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateProcessW", SetLastError = true)]
		internal unsafe static extern bool CreateProcess(string lpApplicationName, char* lpCommandLine, ref SECURITY_ATTRIBUTES procSecAttrs, ref SECURITY_ATTRIBUTES threadSecAttrs, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool TerminateProcess(SafeProcessHandle processHandle, int exitCode);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern SafeProcessHandle OpenProcess(int access, bool inherit, int processId);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "K32EnumProcesses", SetLastError = true)]
		internal static extern bool EnumProcesses(int[] processIds, int size, out int needed);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "K32GetModuleInformation", SetLastError = true)]
		private static extern bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo, int size);

		internal unsafe static bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo)
		{
			return GetModuleInformation(processHandle, moduleHandle, out ntModuleInfo, sizeof(NtModuleInfo));
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "K32GetModuleBaseNameW", SetLastError = true)]
		internal static extern int GetModuleBaseName(SafeProcessHandle processHandle, IntPtr moduleHandle, [Out] char[] baseName, int size);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "K32GetModuleFileNameExW", SetLastError = true)]
		internal static extern int GetModuleFileNameEx(SafeProcessHandle processHandle, IntPtr moduleHandle, [Out] char[] baseName, int size);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetProcessWorkingSetSizeEx(SafeProcessHandle handle, IntPtr min, IntPtr max, int flags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetProcessWorkingSetSizeEx(SafeProcessHandle handle, out IntPtr min, out IntPtr max, out int flags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetProcessAffinityMask(SafeProcessHandle handle, IntPtr mask);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetProcessAffinityMask(SafeProcessHandle handle, out IntPtr processMask, out IntPtr systemMask);

		[DllImport("kernel32.dll")]
		public static extern int GetProcessId(SafeProcessHandle nativeHandle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetThreadPriorityBoost(SafeThreadHandle handle, out bool disabled);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetThreadPriorityBoost(SafeThreadHandle handle, bool disabled);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetProcessPriorityBoost(SafeProcessHandle handle, out bool disabled);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetProcessPriorityBoost(SafeProcessHandle handle, bool disabled);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern SafeThreadHandle OpenThread(int access, bool inherit, int threadId);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetThreadPriority(SafeThreadHandle handle, int priority);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetThreadPriority(SafeThreadHandle handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, IntPtr mask);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int SetThreadIdealProcessor(SafeThreadHandle handle, int processor);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetPriorityClass(SafeProcessHandle handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool SetPriorityClass(SafeProcessHandle handle, int priorityClass);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, SafeHandle hSourceHandle, IntPtr hTargetProcess, out SafeFileHandle targetHandle, int dwDesiredAccess, bool bInheritHandle, int dwOptions);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, SafeHandle hSourceHandle, IntPtr hTargetProcess, out SafeWaitHandle targetHandle, int dwDesiredAccess, bool bInheritHandle, int dwOptions);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW", ExactSpelling = true)]
		private static extern int GetComputerName(ref char lpBuffer, ref uint nSize);

		internal static string GetComputerName()
		{
			Span<char> span = stackalloc char[16];
			uint nSize = (uint)span.Length;
			if (GetComputerName(ref MemoryMarshal.GetReference(span), ref nSize) == 0)
			{
				return null;
			}
			return span.Slice(0, (int)nSize).ToString();
		}

		[DllImport("kernel32.dll")]
		internal static extern uint GetConsoleCP();

		[DllImport("kernel32.dll")]
		internal static extern uint GetConsoleOutputCP();

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int MultiByteToWideChar(uint CodePage, uint dwFlags, byte* lpMultiByteStr, int cbMultiByte, char* lpWideCharStr, int cchWideChar);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int WideCharToMultiByte(uint CodePage, uint dwFlags, char* lpWideCharStr, int cchWideChar, byte* lpMultiByteStr, int cbMultiByte, IntPtr lpDefaultChar, IntPtr lpUsedDefaultChar);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private unsafe static extern BOOL GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

		internal unsafe static int GetLeadByteRanges(int codePage, byte[] leadByteRanges)
		{
			int num = 0;
			Unsafe.SkipInit(out CPINFOEXW cPINFOEXW);
			if (GetCPInfoExW((uint)codePage, 0u, &cPINFOEXW) != 0)
			{
				for (int i = 0; i < 10 && leadByteRanges[i] != 0; i += 2)
				{
					leadByteRanges[i] = cPINFOEXW.LeadByte[i];
					leadByteRanges[i + 1] = cPINFOEXW.LeadByte[i + 1];
					num++;
				}
			}
			return num;
		}
	}

	internal static class NtDll
	{
		internal struct PROCESS_BASIC_INFORMATION
		{
			public uint ExitStatus;

			public IntPtr PebBaseAddress;

			public UIntPtr AffinityMask;

			public int BasePriority;

			public UIntPtr UniqueProcessId;

			public UIntPtr InheritedFromUniqueProcessId;
		}

		internal struct SYSTEM_PROCESS_INFORMATION
		{
			internal uint NextEntryOffset;

			internal uint NumberOfThreads;

			private unsafe fixed byte Reserved1[48];

			internal UNICODE_STRING ImageName;

			internal int BasePriority;

			internal IntPtr UniqueProcessId;

			private readonly UIntPtr Reserved2;

			internal uint HandleCount;

			internal uint SessionId;

			private readonly UIntPtr Reserved3;

			internal UIntPtr PeakVirtualSize;

			internal UIntPtr VirtualSize;

			private readonly uint Reserved4;

			internal UIntPtr PeakWorkingSetSize;

			internal UIntPtr WorkingSetSize;

			private readonly UIntPtr Reserved5;

			internal UIntPtr QuotaPagedPoolUsage;

			private readonly UIntPtr Reserved6;

			internal UIntPtr QuotaNonPagedPoolUsage;

			internal UIntPtr PagefileUsage;

			internal UIntPtr PeakPagefileUsage;

			internal UIntPtr PrivatePageCount;

			private unsafe fixed long Reserved7[6];
		}

		internal struct SYSTEM_THREAD_INFORMATION
		{
			private unsafe fixed long Reserved1[3];

			private readonly uint Reserved2;

			internal IntPtr StartAddress;

			internal CLIENT_ID ClientId;

			internal int Priority;

			internal int BasePriority;

			private readonly uint Reserved3;

			internal uint ThreadState;

			internal uint WaitReason;
		}

		internal struct CLIENT_ID
		{
			internal IntPtr UniqueProcess;

			internal IntPtr UniqueThread;
		}

		[DllImport("ntdll.dll", ExactSpelling = true)]
		internal unsafe static extern uint NtQueryInformationProcess(SafeProcessHandle ProcessHandle, int ProcessInformationClass, void* ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);

		[DllImport("ntdll.dll", ExactSpelling = true)]
		internal unsafe static extern uint NtQuerySystemInformation(int SystemInformationClass, void* SystemInformation, uint SystemInformationLength, uint* ReturnLength);
	}

	internal static class User32
	{
		[DllImport("user32.dll")]
		public unsafe static extern bool EnumWindows(delegate* unmanaged<IntPtr, IntPtr, BOOL> callback, IntPtr extraData);

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
		public static extern int GetWindowLong(IntPtr hWnd, int uCmd);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int GetWindowTextLengthW(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public unsafe static extern int GetWindowTextW(IntPtr hWnd, char* lpString, int nMaxCount);

		[DllImport("user32.dll", ExactSpelling = true)]
		public unsafe static extern int GetWindowThreadProcessId(IntPtr handle, int* processId);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int PostMessageW(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

		[DllImport("user32.dll")]
		public static extern BOOL IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, int flags, int timeout, out IntPtr pdwResult);

		[DllImport("user32.dll")]
		public static extern int WaitForInputIdle(SafeProcessHandle handle, int milliseconds);
	}

	internal static class Advapi32
	{
		internal struct PERF_COUNTER_BLOCK
		{
			internal int ByteLength;
		}

		internal struct PERF_COUNTER_DEFINITION
		{
			internal int ByteLength;

			internal int CounterNameTitleIndex;

			internal int CounterNameTitlePtr;

			internal int CounterHelpTitleIndex;

			internal int CounterHelpTitlePtr;

			internal int DefaultScale;

			internal int DetailLevel;

			internal int CounterType;

			internal int CounterSize;

			internal int CounterOffset;
		}

		internal struct PERF_DATA_BLOCK
		{
			internal int Signature1;

			internal int Signature2;

			internal int LittleEndian;

			internal int Version;

			internal int Revision;

			internal int TotalByteLength;

			internal int HeaderLength;

			internal int NumObjectTypes;

			internal int DefaultObject;

			internal SYSTEMTIME SystemTime;

			internal int pad1;

			internal long PerfTime;

			internal long PerfFreq;

			internal long PerfTime100nSec;

			internal int SystemNameLength;

			internal int SystemNameOffset;
		}

		internal struct PERF_INSTANCE_DEFINITION
		{
			internal int ByteLength;

			internal int ParentObjectTitleIndex;

			internal int ParentObjectInstance;

			internal int UniqueID;

			internal int NameOffset;

			internal int NameLength;

			internal static ReadOnlySpan<char> GetName(in PERF_INSTANCE_DEFINITION instance, ReadOnlySpan<byte> data)
			{
				if (instance.NameLength != 0)
				{
					return MemoryMarshal.Cast<byte, char>(data.Slice(instance.NameOffset, instance.NameLength - 2));
				}
				return default(ReadOnlySpan<char>);
			}
		}

		internal struct PERF_OBJECT_TYPE
		{
			internal int TotalByteLength;

			internal int DefinitionLength;

			internal int HeaderLength;

			internal int ObjectNameTitleIndex;

			internal int ObjectNameTitlePtr;

			internal int ObjectHelpTitleIndex;

			internal int ObjectHelpTitlePtr;

			internal int DetailLevel;

			internal int NumCounters;

			internal int DefaultCounter;

			internal int NumInstances;

			internal int CodePage;

			internal long PerfTime;

			internal long PerfFreq;
		}

		internal struct SYSTEMTIME
		{
			internal short wYear;

			internal short wMonth;

			internal short wDayOfWeek;

			internal short wDay;

			internal short wHour;

			internal short wMinute;

			internal short wSecond;

			internal short wMilliseconds;

			public override string ToString()
			{
				return "[SYSTEMTIME: " + wDay.ToString(CultureInfo.CurrentCulture) + "/" + wMonth.ToString(CultureInfo.CurrentCulture) + "/" + wYear.ToString(CultureInfo.CurrentCulture) + " " + wHour.ToString(CultureInfo.CurrentCulture) + ":" + wMinute.ToString(CultureInfo.CurrentCulture) + ":" + wSecond.ToString(CultureInfo.CurrentCulture) + "]";
			}
		}

		[Flags]
		internal enum LogonFlags
		{
			LOGON_WITH_PROFILE = 1,
			LOGON_NETCREDENTIALS_ONLY = 2
		}

		internal struct LUID
		{
			internal int LowPart;

			internal int HighPart;
		}

		internal struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;

			public uint Attributes;
		}

		internal struct TOKEN_PRIVILEGE
		{
			public uint PrivilegeCount;

			public LUID_AND_ATTRIBUTES Privileges;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out SafeTokenHandle TokenHandle);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW", SetLastError = true)]
		internal static extern bool LookupPrivilegeValue([MarshalAs(UnmanagedType.LPTStr)] string lpSystemName, [MarshalAs(UnmanagedType.LPTStr)] string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal unsafe static extern bool AdjustTokenPrivileges(SafeTokenHandle TokenHandle, bool DisableAllPrivileges, TOKEN_PRIVILEGE* NewState, uint BufferLength, TOKEN_PRIVILEGE* PreviousState, uint* ReturnLength);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern bool CreateProcessWithLogonW(string userName, string domain, IntPtr password, LogonFlags logonFlags, string appName, char* cmdLine, int creationFlags, IntPtr environmentBlock, string lpCurrentDirectory, ref Kernel32.STARTUPINFO lpStartupInfo, ref Kernel32.PROCESS_INFORMATION lpProcessInformation);
	}

	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal struct UNICODE_STRING
	{
		internal ushort Length;

		internal ushort MaximumLength;

		internal IntPtr Buffer;
	}

	internal static class Shell32
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct SHELLEXECUTEINFO
		{
			public uint cbSize;

			public uint fMask;

			public IntPtr hwnd;

			public unsafe char* lpVerb;

			public unsafe char* lpFile;

			public unsafe char* lpParameters;

			public unsafe char* lpDirectory;

			public int nShow;

			public IntPtr hInstApp;

			public IntPtr lpIDList;

			public IntPtr lpClass;

			public IntPtr hkeyClass;

			public uint dwHotKey;

			public IntPtr hIconMonitor;

			public IntPtr hProcess;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern bool ShellExecuteExW(SHELLEXECUTEINFO* pExecInfo);
	}
}
