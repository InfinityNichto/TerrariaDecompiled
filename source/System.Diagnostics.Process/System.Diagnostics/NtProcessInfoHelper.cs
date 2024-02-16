using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics;

internal static class NtProcessInfoHelper
{
	private static long[] CachedBuffer;

	internal unsafe static ProcessInfo[] GetProcessInfos(int? processIdFilter = null)
	{
		int num = 131072;
		long[] array = Interlocked.Exchange(ref CachedBuffer, null);
		try
		{
			while (true)
			{
				if (array == null)
				{
					array = new long[(num + 7) / 8];
				}
				uint requiredSize = 0u;
				fixed (long* systemInformation = array)
				{
					uint num2 = global::Interop.NtDll.NtQuerySystemInformation(5, systemInformation, (uint)(array.Length * 8), &requiredSize);
					if (num2 != 3221225476u)
					{
						if ((int)num2 < 0)
						{
							throw new InvalidOperationException(System.SR.CouldntGetProcessInfos, new Win32Exception((int)num2));
						}
						return GetProcessInfos(MemoryMarshal.AsBytes<long>(array), processIdFilter);
					}
				}
				array = null;
				num = GetNewBufferSize(num, (int)requiredSize);
			}
		}
		finally
		{
			Interlocked.Exchange(ref CachedBuffer, array);
		}
	}

	private static int GetNewBufferSize(int existingBufferSize, int requiredSize)
	{
		int num = ((requiredSize != 0) ? (requiredSize + 10240) : (existingBufferSize * 2));
		if (num < 0)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	private unsafe static ProcessInfo[] GetProcessInfos(ReadOnlySpan<byte> data, int? processIdFilter)
	{
		Dictionary<int, ProcessInfo> dictionary = new Dictionary<int, ProcessInfo>(60);
		int num = 0;
		while (true)
		{
			ref readonly global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION reference = ref MemoryMarshal.AsRef<global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION>(data.Slice(num));
			int num2 = reference.UniqueProcessId.ToInt32();
			if (!processIdFilter.HasValue || processIdFilter.GetValueOrDefault() == num2)
			{
				ProcessInfo processInfo = new ProcessInfo((int)reference.NumberOfThreads)
				{
					ProcessId = num2,
					SessionId = (int)reference.SessionId,
					PoolPagedBytes = (long)(ulong)reference.QuotaPagedPoolUsage,
					PoolNonPagedBytes = (long)(ulong)reference.QuotaNonPagedPoolUsage,
					VirtualBytes = (long)(ulong)reference.VirtualSize,
					VirtualBytesPeak = (long)(ulong)reference.PeakVirtualSize,
					WorkingSetPeak = (long)(ulong)reference.PeakWorkingSetSize,
					WorkingSet = (long)(ulong)reference.WorkingSetSize,
					PageFileBytesPeak = (long)(ulong)reference.PeakPagefileUsage,
					PageFileBytes = (long)(ulong)reference.PagefileUsage,
					PrivateBytes = (long)(ulong)reference.PrivatePageCount,
					BasePriority = reference.BasePriority,
					HandleCount = (int)reference.HandleCount
				};
				if (reference.ImageName.Buffer == IntPtr.Zero)
				{
					if (processInfo.ProcessId == NtProcessManager.SystemProcessID)
					{
						processInfo.ProcessName = "System";
					}
					else if (processInfo.ProcessId == 0)
					{
						processInfo.ProcessName = "Idle";
					}
					else
					{
						processInfo.ProcessName = processInfo.ProcessId.ToString(CultureInfo.InvariantCulture);
					}
				}
				else
				{
					string processShortName = GetProcessShortName(new ReadOnlySpan<char>(reference.ImageName.Buffer.ToPointer(), reference.ImageName.Length / 2));
					processInfo.ProcessName = processShortName;
				}
				dictionary[processInfo.ProcessId] = processInfo;
				int num3 = num + sizeof(global::Interop.NtDll.SYSTEM_PROCESS_INFORMATION);
				for (int i = 0; i < reference.NumberOfThreads; i++)
				{
					ref readonly global::Interop.NtDll.SYSTEM_THREAD_INFORMATION reference2 = ref MemoryMarshal.AsRef<global::Interop.NtDll.SYSTEM_THREAD_INFORMATION>(data.Slice(num3));
					ThreadInfo item = new ThreadInfo
					{
						_processId = (int)reference2.ClientId.UniqueProcess,
						_threadId = (ulong)(long)reference2.ClientId.UniqueThread,
						_basePriority = reference2.BasePriority,
						_currentPriority = reference2.Priority,
						_startAddress = reference2.StartAddress,
						_threadState = (ThreadState)reference2.ThreadState,
						_threadWaitReason = NtProcessManager.GetThreadWaitReason((int)reference2.WaitReason)
					};
					processInfo._threadInfoList.Add(item);
					num3 += sizeof(global::Interop.NtDll.SYSTEM_THREAD_INFORMATION);
				}
			}
			if (reference.NextEntryOffset == 0)
			{
				break;
			}
			num += (int)reference.NextEntryOffset;
		}
		ProcessInfo[] array = new ProcessInfo[dictionary.Values.Count];
		dictionary.Values.CopyTo(array, 0);
		return array;
	}

	internal static string GetProcessShortName(ReadOnlySpan<char> name)
	{
		if (name.IsEmpty)
		{
			return string.Empty;
		}
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] == '\\')
			{
				num = i;
			}
			else if (name[i] == '.')
			{
				num2 = i;
			}
		}
		if (num2 == -1)
		{
			num2 = name.Length - 1;
		}
		else
		{
			ReadOnlySpan<char> span = name.Slice(num2);
			num2 = ((!MemoryExtensions.Equals(span, ".exe", StringComparison.OrdinalIgnoreCase)) ? (name.Length - 1) : (num2 - 1));
		}
		num = ((num != -1) ? (num + 1) : 0);
		return name.Slice(num, num2 - num + 1).ToString();
	}
}
