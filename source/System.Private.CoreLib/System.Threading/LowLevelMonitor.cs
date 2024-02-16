using System.Runtime.InteropServices;

namespace System.Threading;

[StructLayout(LayoutKind.Auto)]
internal struct LowLevelMonitor
{
	internal struct Monitor
	{
		public Interop.Kernel32.CRITICAL_SECTION _criticalSection;

		public Interop.Kernel32.CONDITION_VARIABLE _conditionVariable;
	}

	private unsafe Monitor* _pMonitor;

	public void Dispose()
	{
		DisposeCore();
	}

	public void Acquire()
	{
		AcquireCore();
	}

	public void Release()
	{
		ReleaseCore();
	}

	public void Wait()
	{
		WaitCore();
	}

	public void Signal_Release()
	{
		Signal_ReleaseCore();
	}

	public unsafe void Initialize()
	{
		_pMonitor = (Monitor*)(void*)Marshal.AllocHGlobal(sizeof(Monitor));
		Interop.Kernel32.InitializeCriticalSection(&_pMonitor->_criticalSection);
		Interop.Kernel32.InitializeConditionVariable(&_pMonitor->_conditionVariable);
	}

	private unsafe void DisposeCore()
	{
		if (_pMonitor != null)
		{
			Interop.Kernel32.DeleteCriticalSection(&_pMonitor->_criticalSection);
			Marshal.FreeHGlobal((IntPtr)_pMonitor);
			_pMonitor = null;
		}
	}

	private unsafe void AcquireCore()
	{
		Interop.Kernel32.EnterCriticalSection(&_pMonitor->_criticalSection);
	}

	private unsafe void ReleaseCore()
	{
		Interop.Kernel32.LeaveCriticalSection(&_pMonitor->_criticalSection);
	}

	private void WaitCore()
	{
		WaitCore(-1);
	}

	private unsafe bool WaitCore(int timeoutMilliseconds)
	{
		return Interop.Kernel32.SleepConditionVariableCS(&_pMonitor->_conditionVariable, &_pMonitor->_criticalSection, timeoutMilliseconds);
	}

	private unsafe void Signal_ReleaseCore()
	{
		Interop.Kernel32.WakeConditionVariable(&_pMonitor->_conditionVariable);
		Interop.Kernel32.LeaveCriticalSection(&_pMonitor->_criticalSection);
	}
}
