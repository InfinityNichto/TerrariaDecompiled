using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics;

[Designer("System.Diagnostics.Design.ProcessThreadDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class ProcessThread : Component
{
	private enum State
	{
		IsLocal = 2
	}

	private readonly bool _isRemoteMachine;

	private readonly int _processId;

	private readonly ThreadInfo _threadInfo;

	private bool? _priorityBoostEnabled;

	private ThreadPriorityLevel? _priorityLevel;

	public int BasePriority => _threadInfo._basePriority;

	public int CurrentPriority => _threadInfo._currentPriority;

	public int Id => (int)_threadInfo._threadId;

	public bool PriorityBoostEnabled
	{
		get
		{
			if (!_priorityBoostEnabled.HasValue)
			{
				_priorityBoostEnabled = PriorityBoostEnabledCore;
			}
			return _priorityBoostEnabled.Value;
		}
		set
		{
			PriorityBoostEnabledCore = value;
			_priorityBoostEnabled = value;
		}
	}

	public ThreadPriorityLevel PriorityLevel
	{
		[SupportedOSPlatform("windows")]
		[SupportedOSPlatform("linux")]
		[SupportedOSPlatform("freebsd")]
		get
		{
			if (!_priorityLevel.HasValue)
			{
				_priorityLevel = PriorityLevelCore;
			}
			return _priorityLevel.Value;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			PriorityLevelCore = value;
			_priorityLevel = value;
		}
	}

	public IntPtr StartAddress => _threadInfo._startAddress;

	public ThreadState ThreadState => _threadInfo._threadState;

	public ThreadWaitReason WaitReason
	{
		get
		{
			if (_threadInfo._threadState != ThreadState.Wait)
			{
				throw new InvalidOperationException(System.SR.WaitReasonUnavailable);
			}
			return _threadInfo._threadWaitReason;
		}
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	public DateTime StartTime => GetStartTime();

	public int IdealProcessor
	{
		set
		{
			using SafeThreadHandle handle = OpenThreadHandle(32);
			if (global::Interop.Kernel32.SetThreadIdealProcessor(handle, value) < 0)
			{
				throw new Win32Exception();
			}
		}
	}

	private bool PriorityBoostEnabledCore
	{
		get
		{
			using SafeThreadHandle handle = OpenThreadHandle(64);
			if (!global::Interop.Kernel32.GetThreadPriorityBoost(handle, out var disabled))
			{
				throw new Win32Exception();
			}
			return !disabled;
		}
		set
		{
			using SafeThreadHandle handle = OpenThreadHandle(32);
			if (!global::Interop.Kernel32.SetThreadPriorityBoost(handle, !value))
			{
				throw new Win32Exception();
			}
		}
	}

	private ThreadPriorityLevel PriorityLevelCore
	{
		get
		{
			using SafeThreadHandle handle = OpenThreadHandle(64);
			int threadPriority = global::Interop.Kernel32.GetThreadPriority(handle);
			if (threadPriority == int.MaxValue)
			{
				throw new Win32Exception();
			}
			return (ThreadPriorityLevel)threadPriority;
		}
		set
		{
			using SafeThreadHandle handle = OpenThreadHandle(32);
			if (!global::Interop.Kernel32.SetThreadPriority(handle, (int)value))
			{
				throw new Win32Exception();
			}
		}
	}

	[SupportedOSPlatform("windows")]
	public IntPtr ProcessorAffinity
	{
		set
		{
			using SafeThreadHandle handle = OpenThreadHandle(96);
			if (global::Interop.Kernel32.SetThreadAffinityMask(handle, value) == IntPtr.Zero)
			{
				throw new Win32Exception();
			}
		}
	}

	public TimeSpan PrivilegedProcessorTime => GetThreadTimes().PrivilegedProcessorTime;

	public TimeSpan TotalProcessorTime => GetThreadTimes().TotalProcessorTime;

	public TimeSpan UserProcessorTime => GetThreadTimes().UserProcessorTime;

	internal ProcessThread(bool isRemoteMachine, int processId, ThreadInfo threadInfo)
	{
		_isRemoteMachine = isRemoteMachine;
		_processId = processId;
		_threadInfo = threadInfo;
	}

	private void EnsureState(State state)
	{
		if ((state & State.IsLocal) != 0 && _isRemoteMachine)
		{
			throw new NotSupportedException(System.SR.NotSupportedRemoteThread);
		}
	}

	public void ResetIdealProcessor()
	{
		int idealProcessor = ((IntPtr.Size == 4) ? 32 : 64);
		IdealProcessor = idealProcessor;
	}

	private DateTime GetStartTime()
	{
		return GetThreadTimes().StartTime;
	}

	private ProcessThreadTimes GetThreadTimes()
	{
		using SafeThreadHandle handle = OpenThreadHandle(64);
		ProcessThreadTimes processThreadTimes = new ProcessThreadTimes();
		if (!global::Interop.Kernel32.GetThreadTimes(handle, out processThreadTimes._create, out processThreadTimes._exit, out processThreadTimes._kernel, out processThreadTimes._user))
		{
			throw new Win32Exception();
		}
		return processThreadTimes;
	}

	private SafeThreadHandle OpenThreadHandle(int access)
	{
		EnsureState(State.IsLocal);
		return ProcessManager.OpenThread((int)_threadInfo._threadId, access);
	}
}
