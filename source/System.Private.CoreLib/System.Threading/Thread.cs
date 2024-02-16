using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace System.Threading;

public sealed class Thread : CriticalFinalizerObject
{
	private sealed class StartHelper
	{
		internal int _maxStackSize;

		internal Delegate _start;

		internal object _startArg;

		internal CultureInfo _culture;

		internal CultureInfo _uiCulture;

		internal ExecutionContext _executionContext;

		internal static readonly ContextCallback s_threadStartContextCallback = Callback;

		internal StartHelper(Delegate start)
		{
			_start = start;
		}

		private static void Callback(object state)
		{
			((StartHelper)state).RunWorker();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Run()
		{
			if (_executionContext != null && !_executionContext.IsDefault)
			{
				System.Threading.ExecutionContext.RunInternal(_executionContext, s_threadStartContextCallback, this);
			}
			else
			{
				RunWorker();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RunWorker()
		{
			InitializeCulture();
			Delegate start = _start;
			_start = null;
			if (start is ThreadStart threadStart)
			{
				threadStart();
				return;
			}
			ParameterizedThreadStart parameterizedThreadStart = (ParameterizedThreadStart)start;
			object startArg = _startArg;
			_startArg = null;
			parameterizedThreadStart(startArg);
		}

		private void InitializeCulture()
		{
			if (_culture != null)
			{
				CultureInfo.CurrentCulture = _culture;
				_culture = null;
			}
			if (_uiCulture != null)
			{
				CultureInfo.CurrentUICulture = _uiCulture;
				_uiCulture = null;
			}
		}
	}

	private static class LocalDataStore
	{
		private static Dictionary<string, LocalDataStoreSlot> s_nameToSlotMap;

		public static LocalDataStoreSlot AllocateSlot()
		{
			return new LocalDataStoreSlot(new ThreadLocal<object>());
		}

		private static Dictionary<string, LocalDataStoreSlot> EnsureNameToSlotMap()
		{
			Dictionary<string, LocalDataStoreSlot> dictionary = s_nameToSlotMap;
			if (dictionary != null)
			{
				return dictionary;
			}
			dictionary = new Dictionary<string, LocalDataStoreSlot>();
			return Interlocked.CompareExchange(ref s_nameToSlotMap, dictionary, null) ?? dictionary;
		}

		public static LocalDataStoreSlot AllocateNamedSlot(string name)
		{
			LocalDataStoreSlot localDataStoreSlot = AllocateSlot();
			Dictionary<string, LocalDataStoreSlot> dictionary = EnsureNameToSlotMap();
			lock (dictionary)
			{
				dictionary.Add(name, localDataStoreSlot);
				return localDataStoreSlot;
			}
		}

		public static LocalDataStoreSlot GetNamedSlot(string name)
		{
			Dictionary<string, LocalDataStoreSlot> dictionary = EnsureNameToSlotMap();
			lock (dictionary)
			{
				if (!dictionary.TryGetValue(name, out var value))
				{
					value = (dictionary[name] = AllocateSlot());
				}
				return value;
			}
		}

		public static void FreeNamedSlot(string name)
		{
			Dictionary<string, LocalDataStoreSlot> dictionary = EnsureNameToSlotMap();
			lock (dictionary)
			{
				dictionary.Remove(name);
			}
		}

		private static ThreadLocal<object> GetThreadLocal(LocalDataStoreSlot slot)
		{
			if (slot == null)
			{
				throw new ArgumentNullException("slot");
			}
			return slot.Data;
		}

		public static object GetData(LocalDataStoreSlot slot)
		{
			return GetThreadLocal(slot).Value;
		}

		public static void SetData(LocalDataStoreSlot slot, object value)
		{
			GetThreadLocal(slot).Value = value;
		}
	}

	internal ExecutionContext _executionContext;

	internal SynchronizationContext _synchronizationContext;

	private string _name;

	private StartHelper _startHelper;

	private IntPtr _DONT_USE_InternalThread;

	private int _priority;

	private int _managedThreadId;

	private bool _mayNeedResetForThreadPool;

	private static readonly bool s_isProcessorNumberReallyFast = ProcessorIdCache.ProcessorNumberSpeedCheck();

	private static AsyncLocal<IPrincipal> s_asyncLocalPrincipal;

	[ThreadStatic]
	private static Thread t_currentThread;

	public extern int ManagedThreadId
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		[Intrinsic]
		get;
	}

	public extern bool IsAlive
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
	}

	public bool IsBackground
	{
		get
		{
			return IsBackgroundNative();
		}
		set
		{
			SetBackgroundNative(value);
			if (!value)
			{
				_mayNeedResetForThreadPool = true;
			}
		}
	}

	public extern bool IsThreadPoolThread
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal set;
	}

	public ThreadPriority Priority
	{
		get
		{
			return (ThreadPriority)GetPriorityNative();
		}
		set
		{
			SetPriorityNative((int)value);
			if (value != ThreadPriority.Normal)
			{
				_mayNeedResetForThreadPool = true;
			}
		}
	}

	public ThreadState ThreadState => (ThreadState)GetThreadStateNative();

	internal static extern int OptimalMaxSpinWaitsPerSpinIteration
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		[CompilerGenerated]
		get;
	}

	[UnsupportedOSPlatformGuard("browser")]
	internal static bool IsThreadStartSupported => true;

	public CultureInfo CurrentCulture
	{
		get
		{
			RequireCurrentThread();
			return CultureInfo.CurrentCulture;
		}
		set
		{
			if (this != CurrentThread)
			{
				SetCultureOnUnstartedThread(value, uiCulture: false);
			}
			else
			{
				CultureInfo.CurrentCulture = value;
			}
		}
	}

	public CultureInfo CurrentUICulture
	{
		get
		{
			RequireCurrentThread();
			return CultureInfo.CurrentUICulture;
		}
		set
		{
			if (this != CurrentThread)
			{
				SetCultureOnUnstartedThread(value, uiCulture: true);
			}
			else
			{
				CultureInfo.CurrentUICulture = value;
			}
		}
	}

	public static IPrincipal? CurrentPrincipal
	{
		get
		{
			IPrincipal principal = s_asyncLocalPrincipal?.Value;
			if (principal == null)
			{
				principal = (CurrentPrincipal = AppDomain.CurrentDomain.GetThreadPrincipal());
			}
			return principal;
		}
		set
		{
			if (s_asyncLocalPrincipal == null)
			{
				if (value == null)
				{
					return;
				}
				Interlocked.CompareExchange(ref s_asyncLocalPrincipal, new AsyncLocal<IPrincipal>(), null);
			}
			s_asyncLocalPrincipal.Value = value;
		}
	}

	public static Thread CurrentThread
	{
		[Intrinsic]
		get
		{
			return t_currentThread ?? InitializeCurrentThread();
		}
	}

	internal static ulong CurrentOSThreadId => GetCurrentOSThreadId();

	public ExecutionContext? ExecutionContext => System.Threading.ExecutionContext.Capture();

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			lock (this)
			{
				if (_name != value)
				{
					_name = value;
					ThreadNameChanged(value);
					_mayNeedResetForThreadPool = true;
				}
			}
		}
	}

	[Obsolete("The ApartmentState property has been deprecated. Use GetApartmentState, SetApartmentState or TrySetApartmentState.")]
	public ApartmentState ApartmentState
	{
		get
		{
			return GetApartmentState();
		}
		set
		{
			TrySetApartmentState(value);
		}
	}

	private Thread()
	{
	}

	internal ThreadHandle GetNativeHandle()
	{
		IntPtr dONT_USE_InternalThread = _DONT_USE_InternalThread;
		if (dONT_USE_InternalThread == IntPtr.Zero)
		{
			throw new ArgumentException(null, SR.Argument_InvalidHandle);
		}
		return new ThreadHandle(dONT_USE_InternalThread);
	}

	private unsafe void StartCore()
	{
		lock (this)
		{
			fixed (char* pThreadName = _name)
			{
				StartInternal(GetNativeHandle(), _startHelper?._maxStackSize ?? 0, _priority, pThreadName);
			}
		}
	}

	[DllImport("QCall")]
	private unsafe static extern void StartInternal(ThreadHandle t, int stackSize, int priority, char* pThreadName);

	private void StartCallback()
	{
		StartHelper startHelper = _startHelper;
		_startHelper = null;
		startHelper.Run();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr InternalGetCurrentThread();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void SleepInternal(int millisecondsTimeout);

	[DllImport("QCall")]
	internal static extern void UninterruptibleSleep0();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void SpinWaitInternal(int iterations);

	public static void SpinWait(int iterations)
	{
		SpinWaitInternal(iterations);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern Interop.BOOL YieldInternal();

	public static bool Yield()
	{
		return YieldInternal() != Interop.BOOL.FALSE;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Thread InitializeCurrentThread()
	{
		return t_currentThread = GetCurrentThreadNative();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern Thread GetCurrentThreadNative();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void Initialize();

	~Thread()
	{
		InternalFinalize();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void InternalFinalize();

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void InformThreadNameChange(ThreadHandle t, string name, int len);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool IsBackgroundNative();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void SetBackgroundNative(bool isBackground);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern int GetPriorityNative();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void SetPriorityNative(int priority);

	[DllImport("QCall")]
	private static extern ulong GetCurrentOSThreadId();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern int GetThreadStateNative();

	public ApartmentState GetApartmentState()
	{
		return (ApartmentState)GetApartmentStateNative();
	}

	private bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
	{
		ApartmentState apartmentState = (ApartmentState)SetApartmentStateNative((int)state);
		if (state == ApartmentState.Unknown && apartmentState == ApartmentState.MTA)
		{
			return true;
		}
		if (apartmentState != state)
		{
			if (throwOnError)
			{
				string message = SR.Format(SR.Thread_ApartmentState_ChangeFailed, apartmentState);
				throw new InvalidOperationException(message);
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern int GetApartmentStateNative();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern int SetApartmentStateNative(int state);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern void DisableComObjectEagerCleanup();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern void Interrupt();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern bool Join(int millisecondsTimeout);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetCurrentProcessorNumber();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetCurrentProcessorId()
	{
		if (s_isProcessorNumberReallyFast)
		{
			return GetCurrentProcessorNumber();
		}
		return ProcessorIdCache.GetCurrentProcessorId();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ResetThreadPoolThread()
	{
		if (ThreadPool.UsePortableThreadPool && _mayNeedResetForThreadPool)
		{
			ResetThreadPoolThreadSlow();
		}
	}

	public Thread(ThreadStart start)
	{
		if (start == null)
		{
			throw new ArgumentNullException("start");
		}
		_startHelper = new StartHelper(start);
		Initialize();
	}

	public Thread(ThreadStart start, int maxStackSize)
	{
		if (start == null)
		{
			throw new ArgumentNullException("start");
		}
		if (maxStackSize < 0)
		{
			throw new ArgumentOutOfRangeException("maxStackSize", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_startHelper = new StartHelper(start)
		{
			_maxStackSize = maxStackSize
		};
		Initialize();
	}

	public Thread(ParameterizedThreadStart start)
	{
		if (start == null)
		{
			throw new ArgumentNullException("start");
		}
		_startHelper = new StartHelper(start);
		Initialize();
	}

	public Thread(ParameterizedThreadStart start, int maxStackSize)
	{
		if (start == null)
		{
			throw new ArgumentNullException("start");
		}
		if (maxStackSize < 0)
		{
			throw new ArgumentOutOfRangeException("maxStackSize", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_startHelper = new StartHelper(start)
		{
			_maxStackSize = maxStackSize
		};
		Initialize();
	}

	[UnsupportedOSPlatform("browser")]
	public void Start(object? parameter)
	{
		Start(parameter, captureContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public void UnsafeStart(object? parameter)
	{
		Start(parameter, captureContext: false);
	}

	private void Start(object parameter, bool captureContext)
	{
		StartHelper startHelper = _startHelper;
		if (startHelper != null)
		{
			if (startHelper._start is ThreadStart)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ThreadWrongThreadStart);
			}
			startHelper._startArg = parameter;
			startHelper._executionContext = (captureContext ? System.Threading.ExecutionContext.Capture() : null);
		}
		StartCore();
	}

	[UnsupportedOSPlatform("browser")]
	public void Start()
	{
		Start(captureContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public void UnsafeStart()
	{
		Start(captureContext: false);
	}

	private void Start(bool captureContext)
	{
		StartHelper startHelper = _startHelper;
		if (startHelper != null)
		{
			startHelper._startArg = null;
			startHelper._executionContext = (captureContext ? System.Threading.ExecutionContext.Capture() : null);
		}
		StartCore();
	}

	private void RequireCurrentThread()
	{
		if (this != CurrentThread)
		{
			throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
		}
	}

	private void SetCultureOnUnstartedThread(CultureInfo value, bool uiCulture)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		StartHelper startHelper = _startHelper;
		if ((ThreadState & ThreadState.Unstarted) == 0)
		{
			throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
		}
		if (uiCulture)
		{
			startHelper._uiCulture = value;
		}
		else
		{
			startHelper._culture = value;
		}
	}

	private void ThreadNameChanged(string value)
	{
		InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Sleep(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		SleepInternal(millisecondsTimeout);
	}

	internal void SetThreadPoolWorkerThreadName()
	{
		lock (this)
		{
			_name = ".NET ThreadPool Worker";
			ThreadNameChanged(".NET ThreadPool Worker");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ResetThreadPoolThreadSlow()
	{
		_mayNeedResetForThreadPool = false;
		if (_name != ".NET ThreadPool Worker")
		{
			SetThreadPoolWorkerThreadName();
		}
		if (!IsBackground)
		{
			IsBackground = true;
		}
		if (Priority != ThreadPriority.Normal)
		{
			Priority = ThreadPriority.Normal;
		}
	}

	[Obsolete("Thread.Abort is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0006", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public void Abort()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
	}

	[Obsolete("Thread.Abort is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0006", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public void Abort(object? stateInfo)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
	}

	[Obsolete("Thread.Abort is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0006", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void ResetAbort()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
	}

	[Obsolete("Thread.Suspend has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
	public void Suspend()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
	}

	[Obsolete("Thread.Resume has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
	public void Resume()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
	}

	public static void BeginCriticalRegion()
	{
	}

	public static void EndCriticalRegion()
	{
	}

	public static void BeginThreadAffinity()
	{
	}

	public static void EndThreadAffinity()
	{
	}

	public static LocalDataStoreSlot AllocateDataSlot()
	{
		return LocalDataStore.AllocateSlot();
	}

	public static LocalDataStoreSlot AllocateNamedDataSlot(string name)
	{
		return LocalDataStore.AllocateNamedSlot(name);
	}

	public static LocalDataStoreSlot GetNamedDataSlot(string name)
	{
		return LocalDataStore.GetNamedSlot(name);
	}

	public static void FreeNamedDataSlot(string name)
	{
		LocalDataStore.FreeNamedSlot(name);
	}

	public static object? GetData(LocalDataStoreSlot slot)
	{
		return LocalDataStore.GetData(slot);
	}

	public static void SetData(LocalDataStoreSlot slot, object? data)
	{
		LocalDataStore.SetData(slot, data);
	}

	[SupportedOSPlatform("windows")]
	public void SetApartmentState(ApartmentState state)
	{
		SetApartmentState(state, throwOnError: true);
	}

	public bool TrySetApartmentState(ApartmentState state)
	{
		return SetApartmentState(state, throwOnError: false);
	}

	private bool SetApartmentState(ApartmentState state, bool throwOnError)
	{
		if ((uint)state > 2u)
		{
			throw new ArgumentOutOfRangeException("state", SR.ArgumentOutOfRange_Enum);
		}
		return SetApartmentStateUnchecked(state, throwOnError);
	}

	[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public CompressedStack GetCompressedStack()
	{
		throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
	}

	[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public void SetCompressedStack(CompressedStack stack)
	{
		throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
	}

	public static AppDomain GetDomain()
	{
		return AppDomain.CurrentDomain;
	}

	public static int GetDomainID()
	{
		return 1;
	}

	public override int GetHashCode()
	{
		return ManagedThreadId;
	}

	public void Join()
	{
		Join(-1);
	}

	public bool Join(TimeSpan timeout)
	{
		return Join(WaitHandle.ToTimeoutMilliseconds(timeout));
	}

	public static void MemoryBarrier()
	{
		Interlocked.MemoryBarrier();
	}

	public static void Sleep(TimeSpan timeout)
	{
		Sleep(WaitHandle.ToTimeoutMilliseconds(timeout));
	}

	public static byte VolatileRead(ref byte address)
	{
		return Volatile.Read(ref address);
	}

	public static double VolatileRead(ref double address)
	{
		return Volatile.Read(ref address);
	}

	public static short VolatileRead(ref short address)
	{
		return Volatile.Read(ref address);
	}

	public static int VolatileRead(ref int address)
	{
		return Volatile.Read(ref address);
	}

	public static long VolatileRead(ref long address)
	{
		return Volatile.Read(ref address);
	}

	public static IntPtr VolatileRead(ref IntPtr address)
	{
		return Volatile.Read(ref address);
	}

	[return: NotNullIfNotNull("address")]
	public static object? VolatileRead([NotNullIfNotNull("address")] ref object? address)
	{
		return Volatile.Read(ref address);
	}

	[CLSCompliant(false)]
	public static sbyte VolatileRead(ref sbyte address)
	{
		return Volatile.Read(ref address);
	}

	public static float VolatileRead(ref float address)
	{
		return Volatile.Read(ref address);
	}

	[CLSCompliant(false)]
	public static ushort VolatileRead(ref ushort address)
	{
		return Volatile.Read(ref address);
	}

	[CLSCompliant(false)]
	public static uint VolatileRead(ref uint address)
	{
		return Volatile.Read(ref address);
	}

	[CLSCompliant(false)]
	public static ulong VolatileRead(ref ulong address)
	{
		return Volatile.Read(ref address);
	}

	[CLSCompliant(false)]
	public static UIntPtr VolatileRead(ref UIntPtr address)
	{
		return Volatile.Read(ref address);
	}

	public static void VolatileWrite(ref byte address, byte value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref double address, double value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref short address, short value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref int address, int value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref long address, long value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref IntPtr address, IntPtr value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite([NotNullIfNotNull("value")] ref object? address, object? value)
	{
		Volatile.Write(ref address, value);
	}

	[CLSCompliant(false)]
	public static void VolatileWrite(ref sbyte address, sbyte value)
	{
		Volatile.Write(ref address, value);
	}

	public static void VolatileWrite(ref float address, float value)
	{
		Volatile.Write(ref address, value);
	}

	[CLSCompliant(false)]
	public static void VolatileWrite(ref ushort address, ushort value)
	{
		Volatile.Write(ref address, value);
	}

	[CLSCompliant(false)]
	public static void VolatileWrite(ref uint address, uint value)
	{
		Volatile.Write(ref address, value);
	}

	[CLSCompliant(false)]
	public static void VolatileWrite(ref ulong address, ulong value)
	{
		Volatile.Write(ref address, value);
	}

	[CLSCompliant(false)]
	public static void VolatileWrite(ref UIntPtr address, UIntPtr value)
	{
		Volatile.Write(ref address, value);
	}
}
