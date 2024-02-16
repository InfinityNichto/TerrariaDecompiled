using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Threading;

public static class ThreadPool
{
	internal static readonly bool UsePortableThreadPool = InitializeConfigAndDetermineUsePortableThreadPool();

	private static readonly bool IsWorkerTrackingEnabledInConfig = GetEnableWorkerTracking();

	internal static readonly ThreadPoolWorkQueue s_workQueue = new ThreadPoolWorkQueue();

	internal static readonly Action<object> s_invokeAsyncStateMachineBox = delegate(object state)
	{
		if (!(state is IAsyncStateMachineBox asyncStateMachineBox))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
		}
		else
		{
			asyncStateMachineBox.MoveNext();
		}
	};

	internal static bool SupportsTimeSensitiveWorkItems => UsePortableThreadPool;

	public static int ThreadCount => (UsePortableThreadPool ? PortableThreadPool.ThreadPoolInstance.ThreadCount : 0) + GetThreadCount();

	public static long CompletedWorkItemCount
	{
		get
		{
			long num = GetCompletedWorkItemCount();
			if (UsePortableThreadPool)
			{
				num += PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
			}
			return num;
		}
	}

	private static long PendingUnmanagedWorkItemCount
	{
		get
		{
			if (!UsePortableThreadPool)
			{
				return GetPendingUnmanagedWorkItemCount();
			}
			return 0L;
		}
	}

	internal static bool EnableWorkerTracking
	{
		get
		{
			if (IsWorkerTrackingEnabledInConfig)
			{
				return EventSource.IsSupported;
			}
			return false;
		}
	}

	public static long PendingWorkItemCount
	{
		get
		{
			ThreadPoolWorkQueue threadPoolWorkQueue = s_workQueue;
			return ThreadPoolWorkQueue.LocalCount + threadPoolWorkQueue.GlobalCount + PendingUnmanagedWorkItemCount;
		}
	}

	private unsafe static bool InitializeConfigAndDetermineUsePortableThreadPool()
	{
		bool result = false;
		int configVariableIndex = 0;
		while (true)
		{
			uint configValue;
			bool isBoolean;
			char* appContextConfigName;
			int nextConfigUInt32Value = GetNextConfigUInt32Value(configVariableIndex, out configValue, out isBoolean, out appContextConfigName);
			if (nextConfigUInt32Value < 0)
			{
				break;
			}
			configVariableIndex = nextConfigUInt32Value;
			if (appContextConfigName == null)
			{
				result = true;
				continue;
			}
			string text = new string(appContextConfigName);
			if (isBoolean)
			{
				AppContext.SetSwitch(text, configValue != 0);
			}
			else
			{
				AppContext.SetData(text, configValue);
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern int GetNextConfigUInt32Value(int configVariableIndex, out uint configValue, out bool isBoolean, out char* appContextConfigName);

	private static bool GetEnableWorkerTracking()
	{
		if (!UsePortableThreadPool)
		{
			return GetEnableWorkerTrackingNative();
		}
		return AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", defaultValue: false);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CanSetMinIOCompletionThreads(int ioCompletionThreads);

	internal static void SetMinIOCompletionThreads(int ioCompletionThreads)
	{
		bool flag = SetMinThreadsNative(1, ioCompletionThreads);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CanSetMaxIOCompletionThreads(int ioCompletionThreads);

	internal static void SetMaxIOCompletionThreads(int ioCompletionThreads)
	{
		bool flag = SetMaxThreadsNative(1, ioCompletionThreads);
	}

	public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
	{
		if (UsePortableThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
		}
		if (workerThreads >= 0 && completionPortThreads >= 0)
		{
			return SetMaxThreadsNative(workerThreads, completionPortThreads);
		}
		return false;
	}

	public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
	{
		GetMaxThreadsNative(out workerThreads, out completionPortThreads);
		if (UsePortableThreadPool)
		{
			workerThreads = PortableThreadPool.ThreadPoolInstance.GetMaxThreads();
		}
	}

	public static bool SetMinThreads(int workerThreads, int completionPortThreads)
	{
		if (UsePortableThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
		}
		if (workerThreads >= 0 && completionPortThreads >= 0)
		{
			return SetMinThreadsNative(workerThreads, completionPortThreads);
		}
		return false;
	}

	public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
	{
		GetMinThreadsNative(out workerThreads, out completionPortThreads);
		if (UsePortableThreadPool)
		{
			workerThreads = PortableThreadPool.ThreadPoolInstance.GetMinThreads();
		}
	}

	public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
	{
		GetAvailableThreadsNative(out workerThreads, out completionPortThreads);
		if (UsePortableThreadPool)
		{
			workerThreads = PortableThreadPool.ThreadPoolInstance.GetAvailableThreads();
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetThreadCount();

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern long GetCompletedWorkItemCount();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern long GetPendingUnmanagedWorkItemCount();

	private static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, bool flowExecutionContext)
	{
		if (waitObject == null)
		{
			throw new ArgumentNullException("waitObject");
		}
		if (callBack == null)
		{
			throw new ArgumentNullException("callBack");
		}
		RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(waitObject, new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext), (int)millisecondsTimeOutInterval, !executeOnlyOnce);
		registeredWaitHandle.OnBeforeRegister();
		if (UsePortableThreadPool)
		{
			PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);
		}
		else
		{
			IntPtr nativeRegisteredWaitHandle = RegisterWaitForSingleObjectNative(waitObject, registeredWaitHandle.Callback, (uint)registeredWaitHandle.TimeoutDurationMs, !registeredWaitHandle.Repeating, registeredWaitHandle);
			registeredWaitHandle.SetNativeRegisteredWaitHandle(nativeRegisteredWaitHandle);
		}
		return registeredWaitHandle;
	}

	internal static void UnsafeQueueWaitCompletion(CompleteWaitThreadPoolWorkItem completeWaitWorkItem)
	{
		QueueWaitCompletionNative(completeWaitWorkItem);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void QueueWaitCompletionNative(CompleteWaitThreadPoolWorkItem completeWaitWorkItem);

	internal static void RequestWorkerThread()
	{
		if (UsePortableThreadPool)
		{
			PortableThreadPool.ThreadPoolInstance.RequestWorker();
		}
		else
		{
			RequestWorkerThreadNative();
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern Interop.BOOL RequestWorkerThreadNative();

	private static void EnsureGateThreadRunning()
	{
		PortableThreadPool.EnsureGateThreadRunning();
	}

	internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization)
	{
		return PerformRuntimeSpecificGateActivitiesNative(cpuUtilization) != Interop.BOOL.FALSE;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern Interop.BOOL PerformRuntimeSpecificGateActivitiesNative(int cpuUtilization);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

	[CLSCompliant(false)]
	[SupportedOSPlatform("windows")]
	public unsafe static bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
	{
		return PostQueuedCompletionStatus(overlapped);
	}

	private static void UnsafeQueueUnmanagedWorkItem(IntPtr callback, IntPtr state)
	{
		UnsafeQueueTimeSensitiveWorkItemInternal(new UnmanagedThreadPoolWorkItem(callback, state));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs)
	{
		if (UsePortableThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);
		}
		return NotifyWorkItemCompleteNative();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool NotifyWorkItemCompleteNative();

	internal static void ReportThreadStatus(bool isWorking)
	{
		if (UsePortableThreadPool)
		{
			PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
		}
		else
		{
			ReportThreadStatusNative(isWorking);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ReportThreadStatusNative(bool isWorking);

	internal static void NotifyWorkItemProgress()
	{
		if (UsePortableThreadPool)
		{
			PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
		}
		else
		{
			NotifyWorkItemProgressNative();
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void NotifyWorkItemProgressNative();

	internal static bool NotifyThreadBlocked()
	{
		if (UsePortableThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();
		}
		return false;
	}

	internal static void NotifyThreadUnblocked()
	{
		PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
	}

	internal static object GetOrCreateThreadLocalCompletionCountObject()
	{
		if (!UsePortableThreadPool)
		{
			return null;
		}
		return PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool GetEnableWorkerTrackingNative();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr RegisterWaitForSingleObjectNative(WaitHandle waitHandle, object state, uint timeOutInterval, bool executeOnlyOnce, RegisteredWaitHandle registeredWaitHandle);

	[Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
	[SupportedOSPlatform("windows")]
	public static bool BindHandle(IntPtr osHandle)
	{
		return BindIOCompletionCallbackNative(osHandle);
	}

	[SupportedOSPlatform("windows")]
	public static bool BindHandle(SafeHandle osHandle)
	{
		if (osHandle == null)
		{
			throw new ArgumentNullException("osHandle");
		}
		bool flag = false;
		bool success = false;
		try
		{
			osHandle.DangerousAddRef(ref success);
			return BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
		}
		finally
		{
			if (success)
			{
				osHandle.DangerousRelease();
			}
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);

	[CLSCompliant(false)]
	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval > int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[CLSCompliant(false)]
	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval > int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (millisecondsTimeOutInterval > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (millisecondsTimeOutInterval > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, TimeSpan timeout, bool executeOnlyOnce)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, TimeSpan timeout, bool executeOnlyOnce)
	{
		long num = (long)timeout.TotalMilliseconds;
		if (num < -1)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, flowExecutionContext: false);
	}

	public static bool QueueUserWorkItem(WaitCallback callBack)
	{
		return QueueUserWorkItem(callBack, null);
	}

	public static bool QueueUserWorkItem(WaitCallback callBack, object? state)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		ExecutionContext executionContext = ExecutionContext.Capture();
		object callback = ((executionContext == null || executionContext.IsDefault) ? ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallbackDefaultContext(callBack, state)) : ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallback(callBack, state, executionContext)));
		s_workQueue.Enqueue(callback, forceGlobal: true);
		return true;
	}

	public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		ExecutionContext executionContext = ExecutionContext.Capture();
		object callback = ((executionContext == null || executionContext.IsDefault) ? ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state)) : ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallback<TState>(callBack, state, executionContext)));
		s_workQueue.Enqueue(callback, !preferLocal);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		if ((object)callBack == s_invokeAsyncStateMachineBox)
		{
			if (!(state is IAsyncStateMachineBox))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
			}
			UnsafeQueueUserWorkItemInternal(state, preferLocal);
			return true;
		}
		s_workQueue.Enqueue(new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state), !preferLocal);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object? state)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		object callback = new QueueUserWorkItemCallbackDefaultContext(callBack, state);
		s_workQueue.Enqueue(callback, forceGlobal: true);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem(IThreadPoolWorkItem callBack, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		if (callBack is Task)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.callBack);
		}
		UnsafeQueueUserWorkItemInternal(callBack, preferLocal);
		return true;
	}

	internal static void UnsafeQueueUserWorkItemInternal(object callBack, bool preferLocal)
	{
		s_workQueue.Enqueue(callBack, !preferLocal);
	}

	internal static void UnsafeQueueTimeSensitiveWorkItemInternal(IThreadPoolWorkItem timeSensitiveWorkItem)
	{
		s_workQueue.EnqueueTimeSensitiveWorkItem(timeSensitiveWorkItem);
	}

	internal static bool TryPopCustomWorkItem(object workItem)
	{
		return ThreadPoolWorkQueue.LocalFindAndPop(workItem);
	}

	internal static IEnumerable<object> GetQueuedWorkItems()
	{
		if (SupportsTimeSensitiveWorkItems)
		{
			foreach (IThreadPoolWorkItem item in s_workQueue.timeSensitiveWorkQueue)
			{
				yield return item;
			}
		}
		foreach (object workItem in s_workQueue.workItems)
		{
			yield return workItem;
		}
		ThreadPoolWorkQueue.WorkStealingQueue[] queues = ThreadPoolWorkQueue.WorkStealingQueueList.Queues;
		foreach (ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue in queues)
		{
			if (workStealingQueue == null || workStealingQueue.m_array == null)
			{
				continue;
			}
			object[] items = workStealingQueue.m_array;
			foreach (object obj in items)
			{
				if (obj != null)
				{
					yield return obj;
				}
			}
		}
	}

	internal static IEnumerable<object> GetLocallyQueuedWorkItems()
	{
		ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue = ThreadPoolWorkQueueThreadLocals.threadLocals?.workStealingQueue;
		if (workStealingQueue == null || workStealingQueue.m_array == null)
		{
			yield break;
		}
		object[] items = workStealingQueue.m_array;
		foreach (object obj in items)
		{
			if (obj != null)
			{
				yield return obj;
			}
		}
	}

	internal static IEnumerable<object> GetGloballyQueuedWorkItems()
	{
		if (SupportsTimeSensitiveWorkItems)
		{
			foreach (IThreadPoolWorkItem item in s_workQueue.timeSensitiveWorkQueue)
			{
				yield return item;
			}
		}
		foreach (object workItem in s_workQueue.workItems)
		{
			yield return workItem;
		}
	}

	private static object[] ToObjectArray(IEnumerable<object> workitems)
	{
		int num = 0;
		foreach (object workitem in workitems)
		{
			num++;
		}
		object[] array = new object[num];
		num = 0;
		foreach (object workitem2 in workitems)
		{
			if (num < array.Length)
			{
				array[num] = workitem2;
			}
			num++;
		}
		return array;
	}

	internal static object[] GetQueuedWorkItemsForDebugger()
	{
		return ToObjectArray(GetQueuedWorkItems());
	}

	internal static object[] GetGloballyQueuedWorkItemsForDebugger()
	{
		return ToObjectArray(GetGloballyQueuedWorkItems());
	}

	internal static object[] GetLocallyQueuedWorkItemsForDebugger()
	{
		return ToObjectArray(GetLocallyQueuedWorkItems());
	}
}
