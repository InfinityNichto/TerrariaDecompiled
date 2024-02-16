using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics;

internal static class NtProcessManager
{
	private enum ValueId
	{
		Unknown = -1,
		HandleCount,
		PoolPagedBytes,
		PoolNonpagedBytes,
		ElapsedTime,
		VirtualBytesPeak,
		VirtualBytes,
		PrivateBytes,
		PageFileBytes,
		PageFileBytesPeak,
		WorkingSetPeak,
		WorkingSet,
		ThreadId,
		ProcessId,
		BasePriority,
		CurrentPriority,
		UserTime,
		PrivilegedTime,
		StartAddress,
		ThreadState,
		ThreadWaitReason
	}

	private static readonly Dictionary<string, ValueId> s_valueIds = new Dictionary<string, ValueId>(19)
	{
		{
			"Pool Paged Bytes",
			ValueId.PoolPagedBytes
		},
		{
			"Pool Nonpaged Bytes",
			ValueId.PoolNonpagedBytes
		},
		{
			"Elapsed Time",
			ValueId.ElapsedTime
		},
		{
			"Virtual Bytes Peak",
			ValueId.VirtualBytesPeak
		},
		{
			"Virtual Bytes",
			ValueId.VirtualBytes
		},
		{
			"Private Bytes",
			ValueId.PrivateBytes
		},
		{
			"Page File Bytes",
			ValueId.PageFileBytes
		},
		{
			"Page File Bytes Peak",
			ValueId.PageFileBytesPeak
		},
		{
			"Working Set Peak",
			ValueId.WorkingSetPeak
		},
		{
			"Working Set",
			ValueId.WorkingSet
		},
		{
			"ID Thread",
			ValueId.ThreadId
		},
		{
			"ID Process",
			ValueId.ProcessId
		},
		{
			"Priority Base",
			ValueId.BasePriority
		},
		{
			"Priority Current",
			ValueId.CurrentPriority
		},
		{
			"% User Time",
			ValueId.UserTime
		},
		{
			"% Privileged Time",
			ValueId.PrivilegedTime
		},
		{
			"Start Address",
			ValueId.StartAddress
		},
		{
			"Thread State",
			ValueId.ThreadState
		},
		{
			"Thread Wait Reason",
			ValueId.ThreadWaitReason
		}
	};

	internal static int SystemProcessID => 4;

	public static int[] GetProcessIds(string machineName, bool isRemoteMachine)
	{
		ProcessInfo[] processInfos = GetProcessInfos(machineName, isRemoteMachine);
		int[] array = new int[processInfos.Length];
		for (int i = 0; i < processInfos.Length; i++)
		{
			array[i] = processInfos[i].ProcessId;
		}
		return array;
	}

	public static int[] GetProcessIds()
	{
		int[] array = new int[256];
		int needed;
		while (true)
		{
			int num = array.Length * 4;
			if (!global::Interop.Kernel32.EnumProcesses(array, num, out needed))
			{
				throw new Win32Exception();
			}
			if (needed != num)
			{
				break;
			}
			array = new int[array.Length * 2];
		}
		int[] array2 = new int[needed / 4];
		Array.Copy(array, array2, array2.Length);
		return array2;
	}

	public static ProcessModuleCollection GetModules(int processId)
	{
		return GetModules(processId, firstModuleOnly: false);
	}

	public static ProcessModule GetFirstModule(int processId)
	{
		ProcessModuleCollection modules = GetModules(processId, firstModuleOnly: true);
		if (modules.Count != 0)
		{
			return modules[0];
		}
		return null;
	}

	public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
	{
		return global::Interop.Kernel32.GetProcessId(processHandle);
	}

	public static ProcessInfo[] GetProcessInfos(string machineName, bool isRemoteMachine)
	{
		try
		{
			PerformanceCounterLib performanceCounterLib = PerformanceCounterLib.GetPerformanceCounterLib(machineName, new CultureInfo("en"));
			return GetProcessInfos(performanceCounterLib);
		}
		catch (Exception innerException)
		{
			if (isRemoteMachine)
			{
				throw new InvalidOperationException(System.SR.CouldntConnectToRemoteMachine, innerException);
			}
			throw;
		}
	}

	private static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library)
	{
		int num = 5;
		ProcessInfo[] processInfos;
		do
		{
			try
			{
				byte[] performanceData = library.GetPerformanceData("230 232");
				processInfos = GetProcessInfos(library, 230, 232, performanceData);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException(System.SR.CouldntGetProcessInfos, innerException);
			}
			num--;
		}
		while (processInfos.Length == 0 && num != 0);
		if (processInfos.Length == 0)
		{
			throw new InvalidOperationException(System.SR.ProcessDisabled);
		}
		return processInfos;
	}

	private static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library, int processIndex, int threadIndex, ReadOnlySpan<byte> data)
	{
		Dictionary<int, ProcessInfo> dictionary = new Dictionary<int, ProcessInfo>();
		List<ThreadInfo> list = new List<ThreadInfo>();
		ref readonly global::Interop.Advapi32.PERF_DATA_BLOCK reference = ref MemoryMarshal.AsRef<global::Interop.Advapi32.PERF_DATA_BLOCK>(data);
		int num = reference.HeaderLength;
		for (int i = 0; i < reference.NumObjectTypes; i++)
		{
			ref readonly global::Interop.Advapi32.PERF_OBJECT_TYPE reference2 = ref MemoryMarshal.AsRef<global::Interop.Advapi32.PERF_OBJECT_TYPE>(data.Slice(num));
			global::Interop.Advapi32.PERF_COUNTER_DEFINITION[] array = new global::Interop.Advapi32.PERF_COUNTER_DEFINITION[reference2.NumCounters];
			int num2 = num + reference2.HeaderLength;
			for (int j = 0; j < reference2.NumCounters; j++)
			{
				ref readonly global::Interop.Advapi32.PERF_COUNTER_DEFINITION reference3 = ref MemoryMarshal.AsRef<global::Interop.Advapi32.PERF_COUNTER_DEFINITION>(data.Slice(num2));
				string counterName = library.GetCounterName(reference3.CounterNameTitleIndex);
				array[j] = reference3;
				if (reference2.ObjectNameTitleIndex == processIndex)
				{
					array[j].CounterNameTitlePtr = (int)GetValueId(counterName);
				}
				else if (reference2.ObjectNameTitleIndex == threadIndex)
				{
					array[j].CounterNameTitlePtr = (int)GetValueId(counterName);
				}
				num2 += reference3.ByteLength;
			}
			int num3 = num + reference2.DefinitionLength;
			for (int k = 0; k < reference2.NumInstances; k++)
			{
				ref readonly global::Interop.Advapi32.PERF_INSTANCE_DEFINITION reference4 = ref MemoryMarshal.AsRef<global::Interop.Advapi32.PERF_INSTANCE_DEFINITION>(data.Slice(num3));
				ReadOnlySpan<char> span = global::Interop.Advapi32.PERF_INSTANCE_DEFINITION.GetName(in reference4, data.Slice(num3));
				if (!MemoryExtensions.Equals(span, "_Total", StringComparison.Ordinal))
				{
					if (reference2.ObjectNameTitleIndex == processIndex)
					{
						ProcessInfo processInfo = GetProcessInfo(data.Slice(num3 + reference4.ByteLength), array);
						if ((processInfo.ProcessId != 0 || MemoryExtensions.Equals(span, "Idle", StringComparison.OrdinalIgnoreCase)) && !dictionary.ContainsKey(processInfo.ProcessId))
						{
							if (span.Length == 15)
							{
								if (span.EndsWith(".", StringComparison.Ordinal))
								{
									span = span.Slice(0, 14);
								}
								else if (span.EndsWith(".e", StringComparison.Ordinal))
								{
									span = span.Slice(0, 13);
								}
								else if (span.EndsWith(".ex", StringComparison.Ordinal))
								{
									span = span.Slice(0, 12);
								}
							}
							processInfo.ProcessName = span.ToString();
							dictionary.Add(processInfo.ProcessId, processInfo);
						}
					}
					else if (reference2.ObjectNameTitleIndex == threadIndex)
					{
						ThreadInfo threadInfo = GetThreadInfo(data.Slice(num3 + reference4.ByteLength), array);
						if (threadInfo._threadId != 0L)
						{
							list.Add(threadInfo);
						}
					}
				}
				num3 += reference4.ByteLength;
				num3 += MemoryMarshal.AsRef<global::Interop.Advapi32.PERF_COUNTER_BLOCK>(data.Slice(num3)).ByteLength;
			}
			num += reference2.TotalByteLength;
		}
		for (int l = 0; l < list.Count; l++)
		{
			ThreadInfo threadInfo2 = list[l];
			if (dictionary.TryGetValue(threadInfo2._processId, out var value))
			{
				value._threadInfoList.Add(threadInfo2);
			}
		}
		ProcessInfo[] array2 = new ProcessInfo[dictionary.Values.Count];
		dictionary.Values.CopyTo(array2, 0);
		return array2;
	}

	private static ThreadInfo GetThreadInfo(ReadOnlySpan<byte> instanceData, global::Interop.Advapi32.PERF_COUNTER_DEFINITION[] counters)
	{
		ThreadInfo threadInfo = new ThreadInfo();
		for (int i = 0; i < counters.Length; i++)
		{
			global::Interop.Advapi32.PERF_COUNTER_DEFINITION pERF_COUNTER_DEFINITION = counters[i];
			long num = ReadCounterValue(pERF_COUNTER_DEFINITION.CounterType, instanceData.Slice(pERF_COUNTER_DEFINITION.CounterOffset));
			switch ((ValueId)pERF_COUNTER_DEFINITION.CounterNameTitlePtr)
			{
			case ValueId.ProcessId:
				threadInfo._processId = (int)num;
				break;
			case ValueId.ThreadId:
				threadInfo._threadId = (ulong)num;
				break;
			case ValueId.BasePriority:
				threadInfo._basePriority = (int)num;
				break;
			case ValueId.CurrentPriority:
				threadInfo._currentPriority = (int)num;
				break;
			case ValueId.StartAddress:
				threadInfo._startAddress = (IntPtr)num;
				break;
			case ValueId.ThreadState:
				threadInfo._threadState = (ThreadState)num;
				break;
			case ValueId.ThreadWaitReason:
				threadInfo._threadWaitReason = GetThreadWaitReason((int)num);
				break;
			}
		}
		return threadInfo;
	}

	internal static ThreadWaitReason GetThreadWaitReason(int value)
	{
		switch (value)
		{
		case 0:
		case 7:
			return ThreadWaitReason.Executive;
		case 1:
		case 8:
			return ThreadWaitReason.FreePage;
		case 2:
		case 9:
			return ThreadWaitReason.PageIn;
		case 3:
		case 10:
			return ThreadWaitReason.SystemAllocation;
		case 4:
		case 11:
			return ThreadWaitReason.ExecutionDelay;
		case 5:
		case 12:
			return ThreadWaitReason.Suspended;
		case 6:
		case 13:
			return ThreadWaitReason.UserRequest;
		case 14:
			return ThreadWaitReason.EventPairHigh;
		case 15:
			return ThreadWaitReason.EventPairLow;
		case 16:
			return ThreadWaitReason.LpcReceive;
		case 17:
			return ThreadWaitReason.LpcReply;
		case 18:
			return ThreadWaitReason.VirtualMemory;
		case 19:
			return ThreadWaitReason.PageOut;
		default:
			return ThreadWaitReason.Unknown;
		}
	}

	private static ProcessInfo GetProcessInfo(ReadOnlySpan<byte> instanceData, global::Interop.Advapi32.PERF_COUNTER_DEFINITION[] counters)
	{
		ProcessInfo processInfo = new ProcessInfo();
		for (int i = 0; i < counters.Length; i++)
		{
			global::Interop.Advapi32.PERF_COUNTER_DEFINITION pERF_COUNTER_DEFINITION = counters[i];
			long num = ReadCounterValue(pERF_COUNTER_DEFINITION.CounterType, instanceData.Slice(pERF_COUNTER_DEFINITION.CounterOffset));
			switch ((ValueId)pERF_COUNTER_DEFINITION.CounterNameTitlePtr)
			{
			case ValueId.ProcessId:
				processInfo.ProcessId = (int)num;
				break;
			case ValueId.PoolPagedBytes:
				processInfo.PoolPagedBytes = num;
				break;
			case ValueId.PoolNonpagedBytes:
				processInfo.PoolNonPagedBytes = num;
				break;
			case ValueId.VirtualBytes:
				processInfo.VirtualBytes = num;
				break;
			case ValueId.VirtualBytesPeak:
				processInfo.VirtualBytesPeak = num;
				break;
			case ValueId.WorkingSetPeak:
				processInfo.WorkingSetPeak = num;
				break;
			case ValueId.WorkingSet:
				processInfo.WorkingSet = num;
				break;
			case ValueId.PageFileBytesPeak:
				processInfo.PageFileBytesPeak = num;
				break;
			case ValueId.PageFileBytes:
				processInfo.PageFileBytes = num;
				break;
			case ValueId.PrivateBytes:
				processInfo.PrivateBytes = num;
				break;
			case ValueId.BasePriority:
				processInfo.BasePriority = (int)num;
				break;
			case ValueId.HandleCount:
				processInfo.HandleCount = (int)num;
				break;
			}
		}
		return processInfo;
	}

	private static ValueId GetValueId(string counterName)
	{
		if (counterName != null && s_valueIds.TryGetValue(counterName, out var value))
		{
			return value;
		}
		return ValueId.Unknown;
	}

	private static long ReadCounterValue(int counterType, ReadOnlySpan<byte> data)
	{
		if (((uint)counterType & 0x100u) != 0)
		{
			return MemoryMarshal.Read<long>(data);
		}
		return MemoryMarshal.Read<int>(data);
	}

	private static ProcessModuleCollection GetModules(int processId, bool firstModuleOnly)
	{
		if (processId == SystemProcessID || processId == 0)
		{
			throw new Win32Exception(-2147467259, System.SR.EnumProcessModuleFailed);
		}
		SafeProcessHandle safeProcessHandle = SafeProcessHandle.InvalidHandle;
		try
		{
			safeProcessHandle = ProcessManager.OpenProcess(processId, 1040, throwIfExited: true);
			if (!global::Interop.Kernel32.EnumProcessModules(safeProcessHandle, null, 0, out var needed))
			{
				if (!global::Interop.Kernel32.IsWow64Process(global::Interop.Kernel32.GetCurrentProcess(), out var Wow64Process))
				{
					throw new Win32Exception();
				}
				if (!global::Interop.Kernel32.IsWow64Process(safeProcessHandle, out var Wow64Process2))
				{
					throw new Win32Exception();
				}
				if (Wow64Process && !Wow64Process2)
				{
					throw new Win32Exception(299, System.SR.EnumProcessModuleFailedDueToWow);
				}
				EnumProcessModulesUntilSuccess(safeProcessHandle, null, 0, out needed);
			}
			int num = needed / IntPtr.Size;
			IntPtr[] array = new IntPtr[num];
			while (true)
			{
				int num2 = needed;
				EnumProcessModulesUntilSuccess(safeProcessHandle, array, num2, out needed);
				if (num2 == needed)
				{
					break;
				}
				if (needed > num2 && needed / IntPtr.Size > num)
				{
					num = needed / IntPtr.Size;
					array = new IntPtr[num];
				}
			}
			ProcessModuleCollection processModuleCollection = new ProcessModuleCollection(firstModuleOnly ? 1 : num);
			char[] array2 = ArrayPool<char>.Shared.Rent(260);
			try
			{
				for (int i = 0; i < num && (i <= 0 || !firstModuleOnly); i++)
				{
					IntPtr moduleHandle = array[i];
					if (!global::Interop.Kernel32.GetModuleInformation(safeProcessHandle, moduleHandle, out var ntModuleInfo))
					{
						HandleLastWin32Error();
						continue;
					}
					ProcessModule processModule = new ProcessModule
					{
						ModuleMemorySize = ntModuleInfo.SizeOfImage,
						EntryPointAddress = ntModuleInfo.EntryPoint,
						BaseAddress = ntModuleInfo.BaseOfDll
					};
					int num3 = 0;
					while ((num3 = global::Interop.Kernel32.GetModuleBaseName(safeProcessHandle, moduleHandle, array2, array2.Length)) == array2.Length)
					{
						char[] array3 = array2;
						array2 = ArrayPool<char>.Shared.Rent(num3 * 2);
						ArrayPool<char>.Shared.Return(array3);
					}
					if (num3 == 0)
					{
						processModule.Dispose();
						HandleLastWin32Error();
						continue;
					}
					processModule.ModuleName = new string(array2, 0, num3);
					while ((num3 = global::Interop.Kernel32.GetModuleFileNameEx(safeProcessHandle, moduleHandle, array2, array2.Length)) == array2.Length)
					{
						char[] array4 = array2;
						array2 = ArrayPool<char>.Shared.Rent(num3 * 2);
						ArrayPool<char>.Shared.Return(array4);
					}
					if (num3 == 0)
					{
						processModule.Dispose();
						HandleLastWin32Error();
						continue;
					}
					ReadOnlySpan<char> span = array2.AsSpan(0, num3);
					if (span.StartsWith("\\\\?\\"))
					{
						span = span.Slice("\\\\?\\".Length);
					}
					processModule.FileName = span.ToString();
					processModuleCollection.Add(processModule);
				}
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array2);
			}
			return processModuleCollection;
		}
		finally
		{
			if (!safeProcessHandle.IsInvalid)
			{
				safeProcessHandle.Dispose();
			}
		}
	}

	private static void EnumProcessModulesUntilSuccess(SafeProcessHandle processHandle, IntPtr[] modules, int size, out int needed)
	{
		int num = 0;
		while (true)
		{
			if (global::Interop.Kernel32.EnumProcessModules(processHandle, modules, size, out needed))
			{
				return;
			}
			if (num++ > 50)
			{
				break;
			}
			Thread.Sleep(1);
		}
		throw new Win32Exception();
	}

	private static void HandleLastWin32Error()
	{
		int lastWin32Error = Marshal.GetLastWin32Error();
		if (lastWin32Error != 6 && lastWin32Error != 299)
		{
			throw new Win32Exception(lastWin32Error);
		}
	}
}
