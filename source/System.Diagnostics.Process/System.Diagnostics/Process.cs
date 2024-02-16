using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics;

[Designer("System.Diagnostics.Design.ProcessDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class Process : Component, IDisposable
{
	private enum StreamReadMode
	{
		Undefined,
		SyncMode,
		AsyncMode
	}

	private enum State
	{
		HaveId = 1,
		IsLocal = 2,
		HaveNonExitedId = 5,
		HaveProcessInfo = 8,
		Exited = 16,
		Associated = 32
	}

	internal class ShellExecuteHelper
	{
		private unsafe readonly global::Interop.Shell32.SHELLEXECUTEINFO* _executeInfo;

		private bool _succeeded;

		private bool _notpresent;

		public int ErrorCode { get; private set; }

		public unsafe ShellExecuteHelper(global::Interop.Shell32.SHELLEXECUTEINFO* executeInfo)
		{
			_executeInfo = executeInfo;
		}

		private unsafe void ShellExecuteFunction()
		{
			try
			{
				if (!(_succeeded = global::Interop.Shell32.ShellExecuteExW(_executeInfo)))
				{
					ErrorCode = Marshal.GetLastWin32Error();
				}
			}
			catch (EntryPointNotFoundException)
			{
				_notpresent = true;
			}
		}

		public bool ShellExecuteOnSTAThread()
		{
			if (Thread.CurrentThread.GetApartmentState() != 0)
			{
				ThreadStart start = ShellExecuteFunction;
				Thread thread = new Thread(start)
				{
					IsBackground = true,
					Name = ".NET Process STA"
				};
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				thread.Join();
			}
			else
			{
				ShellExecuteFunction();
			}
			if (_notpresent)
			{
				throw new PlatformNotSupportedException(System.SR.UseShellExecuteNotSupported);
			}
			return _succeeded;
		}
	}

	private bool _haveProcessId;

	private int _processId;

	private bool _haveProcessHandle;

	private SafeProcessHandle _processHandle;

	private bool _isRemoteMachine;

	private string _machineName;

	private ProcessInfo _processInfo;

	private ProcessThreadCollection _threads;

	private ProcessModuleCollection _modules;

	private bool _haveWorkingSetLimits;

	private IntPtr _minWorkingSet;

	private IntPtr _maxWorkingSet;

	private bool _haveProcessorAffinity;

	private IntPtr _processorAffinity;

	private bool _havePriorityClass;

	private ProcessPriorityClass _priorityClass;

	private ProcessStartInfo _startInfo;

	private bool _watchForExit;

	private bool _watchingForExit;

	private EventHandler _onExited;

	private bool _exited;

	private int _exitCode;

	private DateTime? _startTime;

	private DateTime _exitTime;

	private bool _haveExitTime;

	private bool _priorityBoostEnabled;

	private bool _havePriorityBoostEnabled;

	private bool _raisedOnExited;

	private RegisteredWaitHandle _registeredWaitHandle;

	private WaitHandle _waitHandle;

	private StreamReader _standardOutput;

	private StreamWriter _standardInput;

	private StreamReader _standardError;

	private bool _disposed;

	private bool _standardInputAccessed;

	private StreamReadMode _outputStreamReadMode;

	private StreamReadMode _errorStreamReadMode;

	internal AsyncStreamReader _output;

	internal AsyncStreamReader _error;

	internal bool _pendingOutputRead;

	internal bool _pendingErrorRead;

	private static int s_cachedSerializationSwitch;

	private static readonly object s_createProcessLock = new object();

	private bool _signaled;

	private bool _haveMainWindow;

	private IntPtr _mainWindowHandle;

	private string _mainWindowTitle;

	private bool _haveResponding;

	private bool _responding;

	public SafeProcessHandle SafeHandle
	{
		get
		{
			EnsureState(State.Associated);
			return GetOrOpenProcessHandle();
		}
	}

	public IntPtr Handle => SafeHandle.DangerousGetHandle();

	private bool Associated
	{
		get
		{
			if (!_haveProcessId)
			{
				return _haveProcessHandle;
			}
			return true;
		}
	}

	public int BasePriority
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.BasePriority;
		}
	}

	public int ExitCode
	{
		get
		{
			EnsureState(State.Exited);
			return _exitCode;
		}
	}

	public bool HasExited
	{
		get
		{
			if (!_exited)
			{
				EnsureState(State.Associated);
				UpdateHasExited();
				if (_exited)
				{
					RaiseOnExited();
				}
			}
			return _exited;
		}
	}

	public DateTime StartTime
	{
		get
		{
			if (!_startTime.HasValue)
			{
				_startTime = StartTimeCore;
			}
			return _startTime.Value;
		}
	}

	public DateTime ExitTime
	{
		get
		{
			if (!_haveExitTime)
			{
				EnsureState(State.Exited);
				_exitTime = ExitTimeCore;
				_haveExitTime = true;
			}
			return _exitTime;
		}
	}

	public int Id
	{
		get
		{
			EnsureState(State.HaveId);
			return _processId;
		}
	}

	public string MachineName
	{
		get
		{
			EnsureState(State.Associated);
			return _machineName;
		}
	}

	public IntPtr MaxWorkingSet
	{
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			EnsureWorkingSetLimits();
			return _maxWorkingSet;
		}
		[SupportedOSPlatform("windows")]
		[SupportedOSPlatform("macos")]
		[SupportedOSPlatform("freebsd")]
		set
		{
			SetWorkingSetLimits(null, value);
		}
	}

	public IntPtr MinWorkingSet
	{
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			EnsureWorkingSetLimits();
			return _minWorkingSet;
		}
		[SupportedOSPlatform("windows")]
		[SupportedOSPlatform("macos")]
		[SupportedOSPlatform("freebsd")]
		set
		{
			SetWorkingSetLimits(value, null);
		}
	}

	public ProcessModuleCollection Modules
	{
		get
		{
			if (_modules == null)
			{
				EnsureState((State)7);
				_modules = ProcessManager.GetModules(_processId);
			}
			return _modules;
		}
	}

	public long NonpagedSystemMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.PoolNonPagedBytes;
		}
	}

	[Obsolete("Process.NonpagedSystemMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.NonpagedSystemMemorySize64 instead.")]
	public int NonpagedSystemMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.PoolNonPagedBytes;
		}
	}

	public long PagedMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.PageFileBytes;
		}
	}

	[Obsolete("Process.PagedMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PagedMemorySize64 instead.")]
	public int PagedMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.PageFileBytes;
		}
	}

	public long PagedSystemMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.PoolPagedBytes;
		}
	}

	[Obsolete("Process.PagedSystemMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PagedSystemMemorySize64 instead.")]
	public int PagedSystemMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.PoolPagedBytes;
		}
	}

	public long PeakPagedMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.PageFileBytesPeak;
		}
	}

	[Obsolete("Process.PeakPagedMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakPagedMemorySize64 instead.")]
	public int PeakPagedMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.PageFileBytesPeak;
		}
	}

	public long PeakWorkingSet64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.WorkingSetPeak;
		}
	}

	[Obsolete("Process.PeakWorkingSet has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakWorkingSet64 instead.")]
	public int PeakWorkingSet
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.WorkingSetPeak;
		}
	}

	public long PeakVirtualMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.VirtualBytesPeak;
		}
	}

	[Obsolete("Process.PeakVirtualMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PeakVirtualMemorySize64 instead.")]
	public int PeakVirtualMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.VirtualBytesPeak;
		}
	}

	public bool PriorityBoostEnabled
	{
		get
		{
			if (!_havePriorityBoostEnabled)
			{
				_priorityBoostEnabled = PriorityBoostEnabledCore;
				_havePriorityBoostEnabled = true;
			}
			return _priorityBoostEnabled;
		}
		set
		{
			PriorityBoostEnabledCore = value;
			_priorityBoostEnabled = value;
			_havePriorityBoostEnabled = true;
		}
	}

	public ProcessPriorityClass PriorityClass
	{
		get
		{
			if (!_havePriorityClass)
			{
				_priorityClass = PriorityClassCore;
				_havePriorityClass = true;
			}
			return _priorityClass;
		}
		set
		{
			if (!Enum.IsDefined(typeof(ProcessPriorityClass), value))
			{
				throw new InvalidEnumArgumentException("value", (int)value, typeof(ProcessPriorityClass));
			}
			PriorityClassCore = value;
			_priorityClass = value;
			_havePriorityClass = true;
		}
	}

	public long PrivateMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.PrivateBytes;
		}
	}

	[Obsolete("Process.PrivateMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.PrivateMemorySize64 instead.")]
	public int PrivateMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.PrivateBytes;
		}
	}

	public string ProcessName
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.ProcessName;
		}
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	public IntPtr ProcessorAffinity
	{
		get
		{
			if (!_haveProcessorAffinity)
			{
				_processorAffinity = ProcessorAffinityCore;
				_haveProcessorAffinity = true;
			}
			return _processorAffinity;
		}
		set
		{
			ProcessorAffinityCore = value;
			_processorAffinity = value;
			_haveProcessorAffinity = true;
		}
	}

	public int SessionId
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.SessionId;
		}
	}

	public ProcessStartInfo StartInfo
	{
		get
		{
			if (_startInfo == null)
			{
				if (Associated)
				{
					throw new InvalidOperationException(System.SR.CantGetProcessStartInfo);
				}
				_startInfo = new ProcessStartInfo();
			}
			return _startInfo;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (Associated)
			{
				throw new InvalidOperationException(System.SR.CantSetProcessStartInfo);
			}
			_startInfo = value;
		}
	}

	public ProcessThreadCollection Threads
	{
		get
		{
			if (_threads == null)
			{
				EnsureState(State.HaveProcessInfo);
				int count = _processInfo._threadInfoList.Count;
				ProcessThread[] array = new ProcessThread[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = new ProcessThread(_isRemoteMachine, _processId, _processInfo._threadInfoList[i]);
				}
				ProcessThreadCollection threads = new ProcessThreadCollection(array);
				_threads = threads;
			}
			return _threads;
		}
	}

	public int HandleCount
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.HandleCount;
		}
	}

	public long VirtualMemorySize64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.VirtualBytes;
		}
	}

	[Obsolete("Process.VirtualMemorySize has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.VirtualMemorySize64 instead.")]
	public int VirtualMemorySize
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.VirtualBytes;
		}
	}

	public bool EnableRaisingEvents
	{
		get
		{
			return _watchForExit;
		}
		set
		{
			if (value == _watchForExit)
			{
				return;
			}
			if (Associated)
			{
				if (value)
				{
					EnsureWatchingForExit();
				}
				else
				{
					StopWatchingForExit();
				}
			}
			_watchForExit = value;
		}
	}

	public StreamWriter StandardInput
	{
		get
		{
			CheckDisposed();
			if (_standardInput == null)
			{
				throw new InvalidOperationException(System.SR.CantGetStandardIn);
			}
			_standardInputAccessed = true;
			return _standardInput;
		}
	}

	public StreamReader StandardOutput
	{
		get
		{
			CheckDisposed();
			if (_standardOutput == null)
			{
				throw new InvalidOperationException(System.SR.CantGetStandardOut);
			}
			if (_outputStreamReadMode == StreamReadMode.Undefined)
			{
				_outputStreamReadMode = StreamReadMode.SyncMode;
			}
			else if (_outputStreamReadMode != StreamReadMode.SyncMode)
			{
				throw new InvalidOperationException(System.SR.CantMixSyncAsyncOperation);
			}
			return _standardOutput;
		}
	}

	public StreamReader StandardError
	{
		get
		{
			CheckDisposed();
			if (_standardError == null)
			{
				throw new InvalidOperationException(System.SR.CantGetStandardError);
			}
			if (_errorStreamReadMode == StreamReadMode.Undefined)
			{
				_errorStreamReadMode = StreamReadMode.SyncMode;
			}
			else if (_errorStreamReadMode != StreamReadMode.SyncMode)
			{
				throw new InvalidOperationException(System.SR.CantMixSyncAsyncOperation);
			}
			return _standardError;
		}
	}

	public long WorkingSet64
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return _processInfo.WorkingSet;
		}
	}

	[Obsolete("Process.WorkingSet has been deprecated because the type of the property can't represent all valid results. Use System.Diagnostics.Process.WorkingSet64 instead.")]
	public int WorkingSet
	{
		get
		{
			EnsureState(State.HaveProcessInfo);
			return (int)_processInfo.WorkingSet;
		}
	}

	public ISynchronizeInvoke? SynchronizingObject { get; set; }

	public ProcessModule? MainModule
	{
		get
		{
			EnsureState((State)3);
			return NtProcessManager.GetFirstModule(_processId);
		}
	}

	private DateTime ExitTimeCore => GetProcessTimes().ExitTime;

	public TimeSpan PrivilegedProcessorTime => GetProcessTimes().PrivilegedProcessorTime;

	internal DateTime StartTimeCore => GetProcessTimes().StartTime;

	public TimeSpan TotalProcessorTime => GetProcessTimes().TotalProcessorTime;

	public TimeSpan UserProcessorTime => GetProcessTimes().UserProcessorTime;

	private bool PriorityBoostEnabledCore
	{
		get
		{
			using SafeProcessHandle handle = GetProcessHandle(1024);
			if (!global::Interop.Kernel32.GetProcessPriorityBoost(handle, out var disabled))
			{
				throw new Win32Exception();
			}
			return !disabled;
		}
		set
		{
			using SafeProcessHandle handle = GetProcessHandle(512);
			if (!global::Interop.Kernel32.SetProcessPriorityBoost(handle, !value))
			{
				throw new Win32Exception();
			}
		}
	}

	private ProcessPriorityClass PriorityClassCore
	{
		get
		{
			using SafeProcessHandle handle = GetProcessHandle(1024);
			int priorityClass = global::Interop.Kernel32.GetPriorityClass(handle);
			if (priorityClass == 0)
			{
				throw new Win32Exception();
			}
			return (ProcessPriorityClass)priorityClass;
		}
		set
		{
			using SafeProcessHandle handle = GetProcessHandle(512);
			if (!global::Interop.Kernel32.SetPriorityClass(handle, (int)value))
			{
				throw new Win32Exception();
			}
		}
	}

	private IntPtr ProcessorAffinityCore
	{
		get
		{
			using SafeProcessHandle handle = GetProcessHandle(1024);
			if (!global::Interop.Kernel32.GetProcessAffinityMask(handle, out var processMask, out var _))
			{
				throw new Win32Exception();
			}
			return processMask;
		}
		set
		{
			using SafeProcessHandle handle = GetProcessHandle(512);
			if (!global::Interop.Kernel32.SetProcessAffinityMask(handle, value))
			{
				throw new Win32Exception();
			}
		}
	}

	public IntPtr MainWindowHandle
	{
		get
		{
			if (!_haveMainWindow)
			{
				EnsureState((State)3);
				_mainWindowHandle = ProcessManager.GetMainWindowHandle(_processId);
				_haveMainWindow = _mainWindowHandle != IntPtr.Zero;
			}
			return _mainWindowHandle;
		}
	}

	public string MainWindowTitle
	{
		get
		{
			if (_mainWindowTitle == null)
			{
				_mainWindowTitle = GetMainWindowTitle();
			}
			return _mainWindowTitle;
		}
	}

	public bool Responding
	{
		get
		{
			if (!_haveResponding)
			{
				_responding = IsRespondingCore();
				_haveResponding = true;
			}
			return _responding;
		}
	}

	private unsafe int ParentProcessId
	{
		get
		{
			using SafeProcessHandle processHandle = GetProcessHandle(1024);
			Unsafe.SkipInit(out global::Interop.NtDll.PROCESS_BASIC_INFORMATION pROCESS_BASIC_INFORMATION);
			if (global::Interop.NtDll.NtQueryInformationProcess(processHandle, 0, &pROCESS_BASIC_INFORMATION, (uint)sizeof(global::Interop.NtDll.PROCESS_BASIC_INFORMATION), out var _) != 0)
			{
				throw new Win32Exception(System.SR.ProcessInformationUnavailable);
			}
			return (int)(uint)pROCESS_BASIC_INFORMATION.InheritedFromUniqueProcessId;
		}
	}

	public event DataReceivedEventHandler? OutputDataReceived;

	public event DataReceivedEventHandler? ErrorDataReceived;

	public event EventHandler Exited
	{
		add
		{
			_onExited = (EventHandler)Delegate.Combine(_onExited, value);
		}
		remove
		{
			_onExited = (EventHandler)Delegate.Remove(_onExited, value);
		}
	}

	public Process()
	{
		if (GetType() == typeof(Process))
		{
			GC.SuppressFinalize(this);
		}
		_machineName = ".";
		_outputStreamReadMode = StreamReadMode.Undefined;
		_errorStreamReadMode = StreamReadMode.Undefined;
	}

	private Process(string machineName, bool isRemoteMachine, int processId, ProcessInfo processInfo)
	{
		GC.SuppressFinalize(this);
		_processInfo = processInfo;
		_machineName = machineName;
		_isRemoteMachine = isRemoteMachine;
		_processId = processId;
		_haveProcessId = true;
		_outputStreamReadMode = StreamReadMode.Undefined;
		_errorStreamReadMode = StreamReadMode.Undefined;
	}

	private void CompletionCallback(object waitHandleContext, bool wasSignaled)
	{
		lock (this)
		{
			if (waitHandleContext == _waitHandle)
			{
				StopWatchingForExit();
				RaiseOnExited();
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				Close();
			}
			_disposed = true;
		}
	}

	public bool CloseMainWindow()
	{
		return CloseMainWindowCore();
	}

	public bool WaitForInputIdle()
	{
		return WaitForInputIdle(int.MaxValue);
	}

	public bool WaitForInputIdle(int milliseconds)
	{
		return WaitForInputIdleCore(milliseconds);
	}

	public void Close()
	{
		if (!Associated)
		{
			return;
		}
		lock (this)
		{
			StopWatchingForExit();
		}
		if (_haveProcessHandle)
		{
			_processHandle.Dispose();
			_processHandle = null;
			_haveProcessHandle = false;
		}
		_haveProcessId = false;
		_isRemoteMachine = false;
		_machineName = ".";
		_raisedOnExited = false;
		try
		{
			if (_standardOutput != null && (_outputStreamReadMode == StreamReadMode.AsyncMode || _outputStreamReadMode == StreamReadMode.Undefined))
			{
				if (_outputStreamReadMode == StreamReadMode.AsyncMode)
				{
					_output?.CancelOperation();
					_output?.Dispose();
				}
				_standardOutput.Close();
			}
			if (_standardError != null && (_errorStreamReadMode == StreamReadMode.AsyncMode || _errorStreamReadMode == StreamReadMode.Undefined))
			{
				if (_errorStreamReadMode == StreamReadMode.AsyncMode)
				{
					_error?.CancelOperation();
					_error?.Dispose();
				}
				_standardError.Close();
			}
			if (_standardInput != null && !_standardInputAccessed)
			{
				_standardInput.Close();
			}
		}
		finally
		{
			_standardOutput = null;
			_standardInput = null;
			_standardError = null;
			_output = null;
			_error = null;
			CloseCore();
			Refresh();
		}
	}

	private void EnsureState(State state)
	{
		if ((state & State.Associated) != 0 && !Associated)
		{
			throw new InvalidOperationException(System.SR.NoAssociatedProcess);
		}
		if ((state & State.HaveId) != 0)
		{
			if (!_haveProcessId)
			{
				if (!_haveProcessHandle)
				{
					EnsureState(State.Associated);
					throw new InvalidOperationException(System.SR.ProcessIdRequired);
				}
				SetProcessId(ProcessManager.GetProcessIdFromHandle(_processHandle));
			}
			_ = state & State.HaveNonExitedId;
			_ = 5;
		}
		if ((state & State.IsLocal) != 0 && _isRemoteMachine)
		{
			throw new NotSupportedException(System.SR.NotSupportedRemote);
		}
		if ((state & State.HaveProcessInfo) != 0 && _processInfo == null)
		{
			if ((state & State.HaveNonExitedId) != State.HaveNonExitedId)
			{
				EnsureState(State.HaveNonExitedId);
			}
			_processInfo = ProcessManager.GetProcessInfo(_processId, _machineName);
			if (_processInfo == null)
			{
				throw new InvalidOperationException(System.SR.NoProcessInfo);
			}
		}
		if ((state & State.Exited) != 0)
		{
			if (!HasExited)
			{
				throw new InvalidOperationException(System.SR.WaitTillExit);
			}
			if (!_haveProcessHandle)
			{
				throw new InvalidOperationException(System.SR.NoProcessHandle);
			}
		}
	}

	private void EnsureWorkingSetLimits()
	{
		if (!_haveWorkingSetLimits)
		{
			GetWorkingSetLimits(out _minWorkingSet, out _maxWorkingSet);
			_haveWorkingSetLimits = true;
		}
	}

	private void SetWorkingSetLimits(IntPtr? min, IntPtr? max)
	{
		SetWorkingSetLimitsCore(min, max, out _minWorkingSet, out _maxWorkingSet);
		_haveWorkingSetLimits = true;
	}

	public static Process GetProcessById(int processId, string machineName)
	{
		if (!ProcessManager.IsProcessRunning(processId, machineName))
		{
			throw new ArgumentException(System.SR.Format(System.SR.MissingProccess, processId.ToString()));
		}
		return new Process(machineName, ProcessManager.IsRemoteMachine(machineName), processId, null);
	}

	public static Process GetProcessById(int processId)
	{
		return GetProcessById(processId, ".");
	}

	public static Process[] GetProcessesByName(string? processName)
	{
		return GetProcessesByName(processName, ".");
	}

	public static Process[] GetProcesses()
	{
		return GetProcesses(".");
	}

	public static Process[] GetProcesses(string machineName)
	{
		bool isRemoteMachine = ProcessManager.IsRemoteMachine(machineName);
		ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(machineName);
		Process[] array = new Process[processInfos.Length];
		for (int i = 0; i < processInfos.Length; i++)
		{
			ProcessInfo processInfo = processInfos[i];
			array[i] = new Process(machineName, isRemoteMachine, processInfo.ProcessId, processInfo);
		}
		return array;
	}

	public static Process GetCurrentProcess()
	{
		return new Process(".", isRemoteMachine: false, Environment.ProcessId, null);
	}

	protected void OnExited()
	{
		EventHandler onExited = _onExited;
		if (onExited != null)
		{
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.BeginInvoke(onExited, new object[2]
				{
					this,
					EventArgs.Empty
				});
			}
			else
			{
				onExited(this, EventArgs.Empty);
			}
		}
	}

	private void RaiseOnExited()
	{
		if (_raisedOnExited)
		{
			return;
		}
		lock (this)
		{
			if (!_raisedOnExited)
			{
				_raisedOnExited = true;
				OnExited();
			}
		}
	}

	public void Refresh()
	{
		_processInfo = null;
		_threads?.Dispose();
		_threads = null;
		_modules?.Dispose();
		_modules = null;
		_exited = false;
		_haveWorkingSetLimits = false;
		_haveProcessorAffinity = false;
		_havePriorityClass = false;
		_haveExitTime = false;
		_havePriorityBoostEnabled = false;
		RefreshCore();
	}

	private SafeProcessHandle GetOrOpenProcessHandle()
	{
		if (!_haveProcessHandle)
		{
			CheckDisposed();
			SetProcessHandle(GetProcessHandle());
		}
		return _processHandle;
	}

	private void SetProcessHandle(SafeProcessHandle processHandle)
	{
		_processHandle = processHandle;
		_haveProcessHandle = true;
		if (_watchForExit)
		{
			EnsureWatchingForExit();
		}
	}

	private void SetProcessId(int processId)
	{
		_processId = processId;
		_haveProcessId = true;
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public bool Start()
	{
		Close();
		ProcessStartInfo startInfo = StartInfo;
		if (startInfo.FileName.Length == 0)
		{
			throw new InvalidOperationException(System.SR.FileNameMissing);
		}
		if (startInfo.StandardInputEncoding != null && !startInfo.RedirectStandardInput)
		{
			throw new InvalidOperationException(System.SR.StandardInputEncodingNotAllowed);
		}
		if (startInfo.StandardOutputEncoding != null && !startInfo.RedirectStandardOutput)
		{
			throw new InvalidOperationException(System.SR.StandardOutputEncodingNotAllowed);
		}
		if (startInfo.StandardErrorEncoding != null && !startInfo.RedirectStandardError)
		{
			throw new InvalidOperationException(System.SR.StandardErrorEncodingNotAllowed);
		}
		if (!string.IsNullOrEmpty(startInfo.Arguments) && startInfo.HasArgumentList)
		{
			throw new InvalidOperationException(System.SR.ArgumentAndArgumentListInitialized);
		}
		CheckDisposed();
		SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref s_cachedSerializationSwitch);
		return StartCore(startInfo);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Process Start(string fileName)
	{
		return Start(new ProcessStartInfo(fileName));
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Process Start(string fileName, string arguments)
	{
		return Start(new ProcessStartInfo(fileName, arguments));
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Process Start(string fileName, IEnumerable<string> arguments)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		if (arguments == null)
		{
			throw new ArgumentNullException("arguments");
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName);
		foreach (string argument in arguments)
		{
			processStartInfo.ArgumentList.Add(argument);
		}
		return Start(processStartInfo);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Process? Start(ProcessStartInfo startInfo)
	{
		Process process = new Process();
		if (startInfo == null)
		{
			throw new ArgumentNullException("startInfo");
		}
		process.StartInfo = startInfo;
		if (!process.Start())
		{
			return null;
		}
		return process;
	}

	private void StopWatchingForExit()
	{
		if (!_watchingForExit)
		{
			return;
		}
		RegisteredWaitHandle registeredWaitHandle = null;
		WaitHandle waitHandle = null;
		lock (this)
		{
			if (_watchingForExit)
			{
				_watchingForExit = false;
				waitHandle = _waitHandle;
				_waitHandle = null;
				registeredWaitHandle = _registeredWaitHandle;
				_registeredWaitHandle = null;
			}
		}
		registeredWaitHandle?.Unregister(null);
		waitHandle?.Dispose();
	}

	public override string ToString()
	{
		string text = base.ToString();
		try
		{
			if (Associated)
			{
				if (_processInfo == null)
				{
					_processInfo = ProcessManager.GetProcessInfo(_processId, _machineName);
				}
				if (_processInfo != null)
				{
					string processName = _processInfo.ProcessName;
					if (processName.Length != 0)
					{
						text = text + " (" + processName + ")";
					}
				}
			}
		}
		catch
		{
		}
		return text;
	}

	public void WaitForExit()
	{
		WaitForExit(-1);
	}

	public bool WaitForExit(int milliseconds)
	{
		bool flag = WaitForExitCore(milliseconds);
		if (flag && _watchForExit)
		{
			RaiseOnExited();
		}
		return flag;
	}

	public async Task WaitForExitAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!Associated)
		{
			throw new InvalidOperationException(System.SR.NoAssociatedProcess);
		}
		if (!HasExited)
		{
			cancellationToken.ThrowIfCancellationRequested();
		}
		try
		{
			EnableRaisingEvents = true;
		}
		catch (InvalidOperationException)
		{
			if (!HasExited)
			{
				throw;
			}
			await WaitUntilOutputEOF(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler handler = delegate
		{
			tcs.TrySetResult();
		};
		Exited += handler;
		try
		{
			if (!HasExited)
			{
				using (cancellationToken.UnsafeRegister(delegate(object s, CancellationToken cancellationToken)
				{
					((TaskCompletionSource)s).TrySetCanceled(cancellationToken);
				}, tcs))
				{
					await tcs.Task.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			await WaitUntilOutputEOF(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			Exited -= handler;
		}
		async Task WaitUntilOutputEOF(CancellationToken cancellationToken)
		{
			if (_output != null)
			{
				await _output.EOF.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_error != null)
			{
				await _error.EOF.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	public void BeginOutputReadLine()
	{
		if (_outputStreamReadMode == StreamReadMode.Undefined)
		{
			_outputStreamReadMode = StreamReadMode.AsyncMode;
		}
		else if (_outputStreamReadMode != StreamReadMode.AsyncMode)
		{
			throw new InvalidOperationException(System.SR.CantMixSyncAsyncOperation);
		}
		if (_pendingOutputRead)
		{
			throw new InvalidOperationException(System.SR.PendingAsyncOperation);
		}
		_pendingOutputRead = true;
		if (_output == null)
		{
			if (_standardOutput == null)
			{
				throw new InvalidOperationException(System.SR.CantGetStandardOut);
			}
			Stream baseStream = _standardOutput.BaseStream;
			_output = new AsyncStreamReader(baseStream, OutputReadNotifyUser, _standardOutput.CurrentEncoding);
		}
		_output.BeginReadLine();
	}

	public void BeginErrorReadLine()
	{
		if (_errorStreamReadMode == StreamReadMode.Undefined)
		{
			_errorStreamReadMode = StreamReadMode.AsyncMode;
		}
		else if (_errorStreamReadMode != StreamReadMode.AsyncMode)
		{
			throw new InvalidOperationException(System.SR.CantMixSyncAsyncOperation);
		}
		if (_pendingErrorRead)
		{
			throw new InvalidOperationException(System.SR.PendingAsyncOperation);
		}
		_pendingErrorRead = true;
		if (_error == null)
		{
			if (_standardError == null)
			{
				throw new InvalidOperationException(System.SR.CantGetStandardError);
			}
			Stream baseStream = _standardError.BaseStream;
			_error = new AsyncStreamReader(baseStream, ErrorReadNotifyUser, _standardError.CurrentEncoding);
		}
		_error.BeginReadLine();
	}

	public void CancelOutputRead()
	{
		CheckDisposed();
		if (_output != null)
		{
			_output.CancelOperation();
			_pendingOutputRead = false;
			return;
		}
		throw new InvalidOperationException(System.SR.NoAsyncOperation);
	}

	public void CancelErrorRead()
	{
		CheckDisposed();
		if (_error != null)
		{
			_error.CancelOperation();
			_pendingErrorRead = false;
			return;
		}
		throw new InvalidOperationException(System.SR.NoAsyncOperation);
	}

	internal void OutputReadNotifyUser(string data)
	{
		DataReceivedEventHandler outputDataReceived = this.OutputDataReceived;
		if (outputDataReceived != null)
		{
			DataReceivedEventArgs dataReceivedEventArgs = new DataReceivedEventArgs(data);
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.Invoke(outputDataReceived, new object[2] { this, dataReceivedEventArgs });
			}
			else
			{
				outputDataReceived(this, dataReceivedEventArgs);
			}
		}
	}

	internal void ErrorReadNotifyUser(string data)
	{
		DataReceivedEventHandler errorDataReceived = this.ErrorDataReceived;
		if (errorDataReceived != null)
		{
			DataReceivedEventArgs dataReceivedEventArgs = new DataReceivedEventArgs(data);
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.Invoke(errorDataReceived, new object[2] { this, dataReceivedEventArgs });
			}
			else
			{
				errorDataReceived(this, dataReceivedEventArgs);
			}
		}
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
	}

	private static Win32Exception CreateExceptionForErrorStartingProcess(string errorMessage, int errorCode, string fileName, string workingDirectory)
	{
		string p = (string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory);
		string message = System.SR.Format(System.SR.ErrorStartingProcess, fileName, p, errorMessage);
		return new Win32Exception(errorCode, message);
	}

	public static Process[] GetProcessesByName(string? processName, string machineName)
	{
		if (processName == null)
		{
			processName = string.Empty;
		}
		Process[] processes = GetProcesses(machineName);
		List<Process> list = new List<Process>();
		for (int i = 0; i < processes.Length; i++)
		{
			if (string.Equals(processName, processes[i].ProcessName, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(processes[i]);
			}
			else
			{
				processes[i].Dispose();
			}
		}
		return list.ToArray();
	}

	[CLSCompliant(false)]
	[SupportedOSPlatform("windows")]
	public static Process? Start(string fileName, string userName, SecureString password, string domain)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName);
		processStartInfo.UserName = userName;
		processStartInfo.Password = password;
		processStartInfo.Domain = domain;
		processStartInfo.UseShellExecute = false;
		return Start(processStartInfo);
	}

	[CLSCompliant(false)]
	[SupportedOSPlatform("windows")]
	public static Process? Start(string fileName, string arguments, string userName, SecureString password, string domain)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName, arguments);
		processStartInfo.UserName = userName;
		processStartInfo.Password = password;
		processStartInfo.Domain = domain;
		processStartInfo.UseShellExecute = false;
		return Start(processStartInfo);
	}

	public static void EnterDebugMode()
	{
		SetPrivilege("SeDebugPrivilege", 2);
	}

	public static void LeaveDebugMode()
	{
		SetPrivilege("SeDebugPrivilege", 0);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public void Kill()
	{
		using SafeProcessHandle safeProcessHandle = GetProcessHandle(4097, throwIfExited: false);
		if (!safeProcessHandle.IsInvalid && !global::Interop.Kernel32.TerminateProcess(safeProcessHandle, -1))
		{
			Win32Exception ex = new Win32Exception();
			if (ex.NativeErrorCode != 5 || !global::Interop.Kernel32.GetExitCodeProcess(safeProcessHandle, out var exitCode) || exitCode == 259)
			{
				throw ex;
			}
		}
	}

	private void RefreshCore()
	{
		_signaled = false;
		_haveMainWindow = false;
		_mainWindowTitle = null;
		_haveResponding = false;
	}

	private void CloseCore()
	{
	}

	private void EnsureWatchingForExit()
	{
		if (_watchingForExit)
		{
			return;
		}
		lock (this)
		{
			if (!_watchingForExit)
			{
				_watchingForExit = true;
				try
				{
					_waitHandle = new global::Interop.Kernel32.ProcessWaitHandle(GetOrOpenProcessHandle());
					_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_waitHandle, CompletionCallback, _waitHandle, -1, executeOnlyOnce: true);
					return;
				}
				catch
				{
					_watchingForExit = false;
					throw;
				}
			}
		}
	}

	private bool WaitForExitCore(int milliseconds)
	{
		SafeProcessHandle safeProcessHandle = null;
		try
		{
			safeProcessHandle = GetProcessHandle(1048576, throwIfExited: false);
			if (safeProcessHandle.IsInvalid)
			{
				return true;
			}
			using global::Interop.Kernel32.ProcessWaitHandle processWaitHandle = new global::Interop.Kernel32.ProcessWaitHandle(safeProcessHandle);
			return _signaled = processWaitHandle.WaitOne(milliseconds);
		}
		finally
		{
			if (milliseconds == -1)
			{
				_output?.EOF.GetAwaiter().GetResult();
				_error?.EOF.GetAwaiter().GetResult();
			}
			safeProcessHandle?.Dispose();
		}
	}

	private void UpdateHasExited()
	{
		using SafeProcessHandle safeProcessHandle = GetProcessHandle(1052672, throwIfExited: false);
		if (safeProcessHandle.IsInvalid)
		{
			_exited = true;
			return;
		}
		if (global::Interop.Kernel32.GetExitCodeProcess(safeProcessHandle, out var exitCode) && exitCode != 259)
		{
			_exitCode = exitCode;
			_exited = true;
			return;
		}
		if (!_signaled)
		{
			using global::Interop.Kernel32.ProcessWaitHandle processWaitHandle = new global::Interop.Kernel32.ProcessWaitHandle(safeProcessHandle);
			_signaled = processWaitHandle.WaitOne(0);
		}
		if (_signaled)
		{
			if (!global::Interop.Kernel32.GetExitCodeProcess(safeProcessHandle, out exitCode))
			{
				throw new Win32Exception();
			}
			_exitCode = exitCode;
			_exited = true;
		}
	}

	private SafeProcessHandle GetProcessHandle()
	{
		return GetProcessHandle(2035711);
	}

	private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
	{
		using SafeProcessHandle handle = GetProcessHandle(1024);
		if (!global::Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out minWorkingSet, out maxWorkingSet, out var _))
		{
			throw new Win32Exception();
		}
	}

	private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
	{
		using SafeProcessHandle handle = GetProcessHandle(1280);
		if (!global::Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out var min, out var max, out var flags))
		{
			throw new Win32Exception();
		}
		if (newMin.HasValue)
		{
			min = newMin.Value;
		}
		if (newMax.HasValue)
		{
			max = newMax.Value;
		}
		if ((long)min > (long)max)
		{
			if (newMin.HasValue)
			{
				throw new ArgumentException(System.SR.BadMinWorkset);
			}
			throw new ArgumentException(System.SR.BadMaxWorkset);
		}
		if (!global::Interop.Kernel32.SetProcessWorkingSetSizeEx(handle, min, max, 0))
		{
			throw new Win32Exception();
		}
		if (!global::Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out min, out max, out flags))
		{
			throw new Win32Exception();
		}
		resultingMin = min;
		resultingMax = max;
	}

	private unsafe bool StartWithCreateProcess(ProcessStartInfo startInfo)
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder commandLine = new System.Text.ValueStringBuilder(initialBuffer);
		BuildCommandLine(startInfo, ref commandLine);
		global::Interop.Kernel32.STARTUPINFO lpStartupInfo = default(global::Interop.Kernel32.STARTUPINFO);
		global::Interop.Kernel32.PROCESS_INFORMATION lpProcessInformation = default(global::Interop.Kernel32.PROCESS_INFORMATION);
		global::Interop.Kernel32.SECURITY_ATTRIBUTES procSecAttrs = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		SafeProcessHandle safeProcessHandle = new SafeProcessHandle();
		SafeFileHandle parentHandle = null;
		SafeFileHandle childHandle = null;
		SafeFileHandle parentHandle2 = null;
		SafeFileHandle childHandle2 = null;
		SafeFileHandle parentHandle3 = null;
		SafeFileHandle childHandle3 = null;
		lock (s_createProcessLock)
		{
			try
			{
				lpStartupInfo.cb = sizeof(global::Interop.Kernel32.STARTUPINFO);
				if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
				{
					if (startInfo.RedirectStandardInput)
					{
						CreatePipe(out parentHandle, out childHandle, parentInputs: true);
					}
					else
					{
						childHandle = new SafeFileHandle(global::Interop.Kernel32.GetStdHandle(-10), ownsHandle: false);
					}
					if (startInfo.RedirectStandardOutput)
					{
						CreatePipe(out parentHandle2, out childHandle2, parentInputs: false);
					}
					else
					{
						childHandle2 = new SafeFileHandle(global::Interop.Kernel32.GetStdHandle(-11), ownsHandle: false);
					}
					if (startInfo.RedirectStandardError)
					{
						CreatePipe(out parentHandle3, out childHandle3, parentInputs: false);
					}
					else
					{
						childHandle3 = new SafeFileHandle(global::Interop.Kernel32.GetStdHandle(-12), ownsHandle: false);
					}
					lpStartupInfo.hStdInput = childHandle.DangerousGetHandle();
					lpStartupInfo.hStdOutput = childHandle2.DangerousGetHandle();
					lpStartupInfo.hStdError = childHandle3.DangerousGetHandle();
					lpStartupInfo.dwFlags = 256;
				}
				int num = 0;
				if (startInfo.CreateNoWindow)
				{
					num |= 0x8000000;
				}
				string text = null;
				if (startInfo._environmentVariables != null)
				{
					num |= 0x400;
					text = GetEnvironmentVariablesBlock(startInfo._environmentVariables);
				}
				string text2 = startInfo.WorkingDirectory;
				if (text2.Length == 0)
				{
					text2 = null;
				}
				int num2 = 0;
				bool flag;
				if (startInfo.UserName.Length != 0)
				{
					if (startInfo.Password != null && startInfo.PasswordInClearText != null)
					{
						throw new ArgumentException(System.SR.CantSetDuplicatePassword);
					}
					global::Interop.Advapi32.LogonFlags logonFlags = (global::Interop.Advapi32.LogonFlags)0;
					if (startInfo.LoadUserProfile)
					{
						logonFlags = global::Interop.Advapi32.LogonFlags.LOGON_WITH_PROFILE;
					}
					fixed (char* ptr = startInfo.PasswordInClearText ?? string.Empty)
					{
						fixed (char* ptr2 = text)
						{
							fixed (char* cmdLine = &commandLine.GetPinnableReference(terminate: true))
							{
								IntPtr intPtr = ((startInfo.Password != null) ? Marshal.SecureStringToGlobalAllocUnicode(startInfo.Password) : IntPtr.Zero);
								try
								{
									flag = global::Interop.Advapi32.CreateProcessWithLogonW(startInfo.UserName, startInfo.Domain, (intPtr != IntPtr.Zero) ? intPtr : ((IntPtr)ptr), logonFlags, null, cmdLine, num, (IntPtr)ptr2, text2, ref lpStartupInfo, ref lpProcessInformation);
									if (!flag)
									{
										num2 = Marshal.GetLastWin32Error();
									}
								}
								finally
								{
									if (intPtr != IntPtr.Zero)
									{
										Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
									}
								}
							}
						}
					}
				}
				else
				{
					fixed (char* ptr3 = text)
					{
						fixed (char* lpCommandLine = &commandLine.GetPinnableReference(terminate: true))
						{
							flag = global::Interop.Kernel32.CreateProcess(null, lpCommandLine, ref procSecAttrs, ref procSecAttrs, bInheritHandles: true, num, (IntPtr)ptr3, text2, ref lpStartupInfo, ref lpProcessInformation);
							if (!flag)
							{
								num2 = Marshal.GetLastWin32Error();
							}
						}
					}
				}
				if (lpProcessInformation.hProcess != IntPtr.Zero && lpProcessInformation.hProcess != new IntPtr(-1))
				{
					Marshal.InitHandle(safeProcessHandle, lpProcessInformation.hProcess);
				}
				if (lpProcessInformation.hThread != IntPtr.Zero && lpProcessInformation.hThread != new IntPtr(-1))
				{
					global::Interop.Kernel32.CloseHandle(lpProcessInformation.hThread);
				}
				if (!flag)
				{
					string errorMessage = ((num2 == 193 || num2 == 216) ? System.SR.InvalidApplication : GetErrorMessage(num2));
					throw CreateExceptionForErrorStartingProcess(errorMessage, num2, startInfo.FileName, text2);
				}
			}
			finally
			{
				childHandle?.Dispose();
				childHandle2?.Dispose();
				childHandle3?.Dispose();
			}
		}
		if (startInfo.RedirectStandardInput)
		{
			Encoding encoding = startInfo.StandardInputEncoding ?? GetEncoding((int)global::Interop.Kernel32.GetConsoleCP());
			_standardInput = new StreamWriter(new FileStream(parentHandle, FileAccess.Write, 4096, isAsync: false), encoding, 4096);
			_standardInput.AutoFlush = true;
		}
		if (startInfo.RedirectStandardOutput)
		{
			Encoding encoding2 = startInfo.StandardOutputEncoding ?? GetEncoding((int)global::Interop.Kernel32.GetConsoleOutputCP());
			_standardOutput = new StreamReader(new FileStream(parentHandle2, FileAccess.Read, 4096, isAsync: false), encoding2, detectEncodingFromByteOrderMarks: true, 4096);
		}
		if (startInfo.RedirectStandardError)
		{
			Encoding encoding3 = startInfo.StandardErrorEncoding ?? GetEncoding((int)global::Interop.Kernel32.GetConsoleOutputCP());
			_standardError = new StreamReader(new FileStream(parentHandle3, FileAccess.Read, 4096, isAsync: false), encoding3, detectEncodingFromByteOrderMarks: true, 4096);
		}
		commandLine.Dispose();
		if (safeProcessHandle.IsInvalid)
		{
			return false;
		}
		SetProcessHandle(safeProcessHandle);
		SetProcessId(lpProcessInformation.dwProcessId);
		return true;
	}

	private static Encoding GetEncoding(int codePage)
	{
		Encoding supportedConsoleEncoding = EncodingHelper.GetSupportedConsoleEncoding(codePage);
		return new ConsoleEncoding(supportedConsoleEncoding);
	}

	private static void BuildCommandLine(ProcessStartInfo startInfo, ref System.Text.ValueStringBuilder commandLine)
	{
		ReadOnlySpan<char> value = startInfo.FileName.AsSpan().Trim();
		bool flag = value.Length > 0 && value[0] == '"' && value[value.Length - 1] == '"';
		if (!flag)
		{
			commandLine.Append('"');
		}
		commandLine.Append(value);
		if (!flag)
		{
			commandLine.Append('"');
		}
		startInfo.AppendArgumentsTo(ref commandLine);
	}

	private ProcessThreadTimes GetProcessTimes()
	{
		using SafeProcessHandle safeProcessHandle = GetProcessHandle(4096, throwIfExited: false);
		if (safeProcessHandle.IsInvalid)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ProcessHasExited, _processId.ToString()));
		}
		ProcessThreadTimes processThreadTimes = new ProcessThreadTimes();
		if (!global::Interop.Kernel32.GetProcessTimes(safeProcessHandle, out processThreadTimes._create, out processThreadTimes._exit, out processThreadTimes._kernel, out processThreadTimes._user))
		{
			throw new Win32Exception();
		}
		return processThreadTimes;
	}

	private unsafe static void SetPrivilege(string privilegeName, int attrib)
	{
		SafeTokenHandle TokenHandle = null;
		try
		{
			if (!global::Interop.Advapi32.OpenProcessToken(global::Interop.Kernel32.GetCurrentProcess(), 32, out TokenHandle))
			{
				throw new Win32Exception();
			}
			if (!global::Interop.Advapi32.LookupPrivilegeValue(null, privilegeName, out var lpLuid))
			{
				throw new Win32Exception();
			}
			Unsafe.SkipInit(out global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE);
			tOKEN_PRIVILEGE.PrivilegeCount = 1u;
			tOKEN_PRIVILEGE.Privileges.Luid = lpLuid;
			tOKEN_PRIVILEGE.Privileges.Attributes = (uint)attrib;
			global::Interop.Advapi32.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges: false, &tOKEN_PRIVILEGE, 0u, null, null);
			if (Marshal.GetLastWin32Error() != 0)
			{
				throw new Win32Exception();
			}
		}
		finally
		{
			TokenHandle?.Dispose();
		}
	}

	private SafeProcessHandle GetProcessHandle(int access, bool throwIfExited = true)
	{
		if (_haveProcessHandle)
		{
			if (throwIfExited)
			{
				using global::Interop.Kernel32.ProcessWaitHandle processWaitHandle = new global::Interop.Kernel32.ProcessWaitHandle(_processHandle);
				if (processWaitHandle.WaitOne(0))
				{
					throw new InvalidOperationException(_haveProcessId ? System.SR.Format(System.SR.ProcessHasExited, _processId.ToString()) : System.SR.ProcessHasExitedNoId);
				}
			}
			return new SafeProcessHandle(_processHandle.DangerousGetHandle(), ownsHandle: false);
		}
		EnsureState((State)3);
		SafeProcessHandle safeProcessHandle = ProcessManager.OpenProcess(_processId, access, throwIfExited);
		if (throwIfExited && ((uint)access & 0x400u) != 0 && global::Interop.Kernel32.GetExitCodeProcess(safeProcessHandle, out _exitCode) && _exitCode != 259)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ProcessHasExited, _processId.ToString()));
		}
		return safeProcessHandle;
	}

	private static void CreatePipeWithSecurityAttributes(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref global::Interop.Kernel32.SECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
	{
		if (!global::Interop.Kernel32.CreatePipe(out hReadPipe, out hWritePipe, ref lpPipeAttributes, nSize) || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
		{
			throw new Win32Exception();
		}
	}

	private void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES lpPipeAttributes = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		lpPipeAttributes.bInheritHandle = global::Interop.BOOL.TRUE;
		SafeFileHandle hReadPipe = null;
		try
		{
			if (parentInputs)
			{
				CreatePipeWithSecurityAttributes(out childHandle, out hReadPipe, ref lpPipeAttributes, 0);
			}
			else
			{
				CreatePipeWithSecurityAttributes(out hReadPipe, out childHandle, ref lpPipeAttributes, 0);
			}
			IntPtr currentProcess = global::Interop.Kernel32.GetCurrentProcess();
			if (!global::Interop.Kernel32.DuplicateHandle(currentProcess, (SafeHandle)hReadPipe, currentProcess, out parentHandle, 0, bInheritHandle: false, 2))
			{
				throw new Win32Exception();
			}
		}
		finally
		{
			if (hReadPipe != null && !hReadPipe.IsInvalid)
			{
				hReadPipe.Dispose();
			}
		}
	}

	private static string GetEnvironmentVariablesBlock(IDictionary<string, string> sd)
	{
		string[] array = new string[sd.Count];
		sd.Keys.CopyTo(array, 0);
		Array.Sort(array, (IComparer<string>?)StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder(8 * array.Length);
		string[] array2 = array;
		foreach (string text in array2)
		{
			stringBuilder.Append(text).Append('=').Append(sd[text])
				.Append('\0');
		}
		return stringBuilder.ToString();
	}

	private static string GetErrorMessage(int error)
	{
		return global::Interop.Kernel32.GetMessage(error);
	}

	private bool StartCore(ProcessStartInfo startInfo)
	{
		if (!startInfo.UseShellExecute)
		{
			return StartWithCreateProcess(startInfo);
		}
		return StartWithShellExecuteEx(startInfo);
	}

	private unsafe bool StartWithShellExecuteEx(ProcessStartInfo startInfo)
	{
		if (!string.IsNullOrEmpty(startInfo.UserName) || startInfo.Password != null)
		{
			throw new InvalidOperationException(System.SR.CantStartAsUser);
		}
		if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
		{
			throw new InvalidOperationException(System.SR.CantRedirectStreams);
		}
		if (startInfo.StandardInputEncoding != null)
		{
			throw new InvalidOperationException(System.SR.StandardInputEncodingNotAllowed);
		}
		if (startInfo.StandardErrorEncoding != null)
		{
			throw new InvalidOperationException(System.SR.StandardErrorEncodingNotAllowed);
		}
		if (startInfo.StandardOutputEncoding != null)
		{
			throw new InvalidOperationException(System.SR.StandardOutputEncodingNotAllowed);
		}
		if (startInfo._environmentVariables != null)
		{
			throw new InvalidOperationException(System.SR.CantUseEnvVars);
		}
		string text = startInfo.BuildArguments();
		fixed (char* lpFile = ((startInfo.FileName.Length > 0) ? startInfo.FileName : null))
		{
			fixed (char* lpVerb = ((startInfo.Verb.Length > 0) ? startInfo.Verb : null))
			{
				fixed (char* lpParameters = ((text.Length > 0) ? text : null))
				{
					fixed (char* lpDirectory = ((startInfo.WorkingDirectory.Length > 0) ? startInfo.WorkingDirectory : null))
					{
						global::Interop.Shell32.SHELLEXECUTEINFO sHELLEXECUTEINFO = default(global::Interop.Shell32.SHELLEXECUTEINFO);
						sHELLEXECUTEINFO.cbSize = (uint)sizeof(global::Interop.Shell32.SHELLEXECUTEINFO);
						sHELLEXECUTEINFO.lpFile = lpFile;
						sHELLEXECUTEINFO.lpVerb = lpVerb;
						sHELLEXECUTEINFO.lpParameters = lpParameters;
						sHELLEXECUTEINFO.lpDirectory = lpDirectory;
						sHELLEXECUTEINFO.fMask = 320u;
						global::Interop.Shell32.SHELLEXECUTEINFO sHELLEXECUTEINFO2 = sHELLEXECUTEINFO;
						if (startInfo.ErrorDialog)
						{
							sHELLEXECUTEINFO2.hwnd = startInfo.ErrorDialogParentHandle;
						}
						else
						{
							sHELLEXECUTEINFO2.fMask |= 1024u;
						}
						sHELLEXECUTEINFO2.nShow = startInfo.WindowStyle switch
						{
							ProcessWindowStyle.Hidden => 0, 
							ProcessWindowStyle.Minimized => 2, 
							ProcessWindowStyle.Maximized => 3, 
							_ => 1, 
						};
						ShellExecuteHelper shellExecuteHelper = new ShellExecuteHelper(&sHELLEXECUTEINFO2);
						if (!shellExecuteHelper.ShellExecuteOnSTAThread())
						{
							int num = shellExecuteHelper.ErrorCode;
							if (num == 0)
							{
								num = GetShellError(sHELLEXECUTEINFO2.hInstApp);
							}
							string text2;
							switch (num)
							{
							case 120:
								throw new PlatformNotSupportedException(System.SR.UseShellExecuteNotSupported);
							default:
								text2 = GetErrorMessage(num);
								break;
							case 193:
							case 216:
								text2 = System.SR.InvalidApplication;
								break;
							}
							string errorMessage = text2;
							throw CreateExceptionForErrorStartingProcess(errorMessage, num, startInfo.FileName, startInfo.WorkingDirectory);
						}
						if (sHELLEXECUTEINFO2.hProcess != IntPtr.Zero)
						{
							SetProcessHandle(new SafeProcessHandle(sHELLEXECUTEINFO2.hProcess));
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private int GetShellError(IntPtr error)
	{
		long num = (long)error;
		long num2 = num - 2;
		if ((ulong)num2 <= 6uL)
		{
			switch (num2)
			{
			case 0L:
				return 2;
			case 1L:
				return 3;
			case 3L:
				return 5;
			case 6L:
				return 8;
			case 2L:
			case 4L:
			case 5L:
				goto IL_0083;
			}
		}
		long num3 = num - 26;
		if ((ulong)num3 <= 6uL)
		{
			switch (num3)
			{
			case 2L:
			case 3L:
			case 4L:
				return 1156;
			case 0L:
				return 32;
			case 5L:
				return 1155;
			case 6L:
				return 1157;
			}
		}
		goto IL_0083;
		IL_0083:
		return (int)(long)error;
	}

	private unsafe string GetMainWindowTitle()
	{
		IntPtr mainWindowHandle = MainWindowHandle;
		if (mainWindowHandle == IntPtr.Zero)
		{
			return string.Empty;
		}
		int windowTextLengthW = global::Interop.User32.GetWindowTextLengthW(mainWindowHandle);
		if (windowTextLengthW == 0)
		{
			return string.Empty;
		}
		windowTextLengthW++;
		Span<char> span = ((windowTextLengthW > 256) ? ((Span<char>)new char[windowTextLengthW]) : stackalloc char[256]);
		Span<char> span2 = span;
		fixed (char* lpString = span2)
		{
			windowTextLengthW = global::Interop.User32.GetWindowTextW(mainWindowHandle, lpString, span2.Length);
		}
		return span2.Slice(0, windowTextLengthW).ToString();
	}

	private bool CloseMainWindowCore()
	{
		IntPtr mainWindowHandle = MainWindowHandle;
		if (mainWindowHandle == (IntPtr)0)
		{
			return false;
		}
		int windowLong = global::Interop.User32.GetWindowLong(mainWindowHandle, -16);
		if (((uint)windowLong & 0x8000000u) != 0)
		{
			return false;
		}
		global::Interop.User32.PostMessageW(mainWindowHandle, 16, IntPtr.Zero, IntPtr.Zero);
		return true;
	}

	private bool IsRespondingCore()
	{
		IntPtr mainWindowHandle = MainWindowHandle;
		if (mainWindowHandle == (IntPtr)0)
		{
			return true;
		}
		IntPtr pdwResult;
		return global::Interop.User32.SendMessageTimeout(mainWindowHandle, 0, IntPtr.Zero, IntPtr.Zero, 2, 5000, out pdwResult) != (IntPtr)0;
	}

	private bool WaitForInputIdleCore(int milliseconds)
	{
		using SafeProcessHandle handle = GetProcessHandle(1049600);
		return global::Interop.User32.WaitForInputIdle(handle, milliseconds) switch
		{
			0 => true, 
			258 => false, 
			_ => throw new InvalidOperationException(System.SR.InputIdleUnkownError), 
		};
	}

	private bool IsParentOf(Process possibleChild)
	{
		try
		{
			return StartTime < possibleChild.StartTime && Id == possibleChild.ParentProcessId;
		}
		catch (Exception e) when (IsProcessInvalidException(e))
		{
			return false;
		}
	}

	private bool Equals(Process process)
	{
		try
		{
			return Id == process.Id && StartTime == process.StartTime;
		}
		catch (Exception e) when (IsProcessInvalidException(e))
		{
			return false;
		}
	}

	private List<Exception> KillTree()
	{
		using SafeProcessHandle safeProcessHandle = GetProcessHandle(4096, throwIfExited: false);
		if (safeProcessHandle.IsInvalid)
		{
			return null;
		}
		return KillTree(safeProcessHandle);
	}

	private List<Exception> KillTree(SafeProcessHandle handle)
	{
		List<Exception> list = null;
		try
		{
			Kill();
		}
		catch (Win32Exception item)
		{
			(list ?? (list = new List<Exception>())).Add(item);
		}
		List<(Process, SafeProcessHandle)> processHandlePairs = GetProcessHandlePairs((Process thisProcess, Process otherProcess) => thisProcess.IsParentOf(otherProcess));
		try
		{
			foreach (var item2 in processHandlePairs)
			{
				List<Exception> list2 = item2.Item1.KillTree(item2.Item2);
				if (list2 != null)
				{
					(list ?? (list = new List<Exception>())).AddRange(list2);
				}
			}
			return list;
		}
		finally
		{
			foreach (var item3 in processHandlePairs)
			{
				item3.Item1.Dispose();
				item3.Item2.Dispose();
			}
		}
	}

	private List<(Process Process, SafeProcessHandle Handle)> GetProcessHandlePairs(Func<Process, Process, bool> predicate)
	{
		List<(Process, SafeProcessHandle)> list = new List<(Process, SafeProcessHandle)>();
		Process[] processes = GetProcesses();
		foreach (Process process2 in processes)
		{
			SafeProcessHandle safeProcessHandle = SafeGetHandle(process2);
			if (!safeProcessHandle.IsInvalid)
			{
				if (predicate(this, process2))
				{
					list.Add((process2, safeProcessHandle));
					continue;
				}
				process2.Dispose();
				safeProcessHandle.Dispose();
			}
		}
		return list;
		static SafeProcessHandle SafeGetHandle(Process process)
		{
			try
			{
				return process.GetProcessHandle(4096, throwIfExited: false);
			}
			catch (Win32Exception)
			{
				return SafeProcessHandle.InvalidHandle;
			}
		}
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public void Kill(bool entireProcessTree)
	{
		if (!entireProcessTree)
		{
			Kill();
			return;
		}
		EnsureState((State)34);
		if (IsSelfOrDescendantOf(GetCurrentProcess()))
		{
			throw new InvalidOperationException(System.SR.KillEntireProcessTree_DisallowedBecauseTreeContainsCallingProcess);
		}
		List<Exception> list = KillTree();
		if (list == null || list.Count == 0)
		{
			return;
		}
		throw new AggregateException(System.SR.KillEntireProcessTree_TerminationIncomplete, list);
	}

	private bool IsSelfOrDescendantOf(Process processOfInterest)
	{
		if (Equals(processOfInterest))
		{
			return true;
		}
		Process[] processes = GetProcesses();
		try
		{
			Queue<Process> queue = new Queue<Process>();
			Process result = this;
			do
			{
				foreach (Process childProcess in result.GetChildProcesses(processes))
				{
					if (processOfInterest.Equals(childProcess))
					{
						return true;
					}
					queue.Enqueue(childProcess);
				}
			}
			while (queue.TryDequeue(out result));
		}
		finally
		{
			Process[] array = processes;
			foreach (Process process in array)
			{
				process.Dispose();
			}
		}
		return false;
	}

	private IReadOnlyList<Process> GetChildProcesses(Process[] processes = null)
	{
		bool flag = processes == null;
		processes = processes ?? GetProcesses();
		List<Process> list = new List<Process>();
		Process[] array = processes;
		foreach (Process process in array)
		{
			bool flag2 = flag;
			try
			{
				if (IsParentOf(process))
				{
					list.Add(process);
					flag2 = false;
				}
			}
			finally
			{
				if (flag2)
				{
					process.Dispose();
				}
			}
		}
		return list;
	}

	private static bool IsProcessInvalidException(Exception e)
	{
		if (!(e is InvalidOperationException))
		{
			return e is Win32Exception;
		}
		return true;
	}
}
