using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace System.Threading;

public sealed class ExecutionContext : IDisposable, ISerializable
{
	internal static readonly ExecutionContext Default = new ExecutionContext();

	private static volatile ExecutionContext s_defaultFlowSuppressed;

	private readonly IAsyncLocalValueMap m_localValues;

	private readonly IAsyncLocal[] m_localChangeNotifications;

	private readonly bool m_isFlowSuppressed;

	private readonly bool m_isDefault;

	internal bool HasChangeNotifications => m_localChangeNotifications != null;

	internal bool IsDefault => m_isDefault;

	private ExecutionContext()
	{
		m_isDefault = true;
	}

	private ExecutionContext(IAsyncLocalValueMap localValues, IAsyncLocal[] localChangeNotifications, bool isFlowSuppressed)
	{
		m_localValues = localValues;
		m_localChangeNotifications = localChangeNotifications;
		m_isFlowSuppressed = isFlowSuppressed;
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public static ExecutionContext? Capture()
	{
		ExecutionContext executionContext = Thread.CurrentThread._executionContext;
		if (executionContext == null)
		{
			executionContext = Default;
		}
		else if (executionContext.m_isFlowSuppressed)
		{
			executionContext = null;
		}
		return executionContext;
	}

	internal static ExecutionContext CaptureForRestore()
	{
		return Thread.CurrentThread._executionContext;
	}

	private ExecutionContext ShallowClone(bool isFlowSuppressed)
	{
		if (m_localValues == null || AsyncLocalValueMap.IsEmpty(m_localValues))
		{
			if (!isFlowSuppressed)
			{
				return null;
			}
			return s_defaultFlowSuppressed ?? (s_defaultFlowSuppressed = new ExecutionContext(AsyncLocalValueMap.Empty, new IAsyncLocal[0], isFlowSuppressed: true));
		}
		return new ExecutionContext(m_localValues, m_localChangeNotifications, isFlowSuppressed);
	}

	public static AsyncFlowControl SuppressFlow()
	{
		Thread currentThread = Thread.CurrentThread;
		ExecutionContext executionContext = currentThread._executionContext ?? Default;
		if (executionContext.m_isFlowSuppressed)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotSupressFlowMultipleTimes);
		}
		executionContext = executionContext.ShallowClone(isFlowSuppressed: true);
		AsyncFlowControl result = default(AsyncFlowControl);
		currentThread._executionContext = executionContext;
		result.Initialize(currentThread);
		return result;
	}

	public static void RestoreFlow()
	{
		Thread currentThread = Thread.CurrentThread;
		ExecutionContext executionContext = currentThread._executionContext;
		if (executionContext == null || !executionContext.m_isFlowSuppressed)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotRestoreUnsupressedFlow);
		}
		currentThread._executionContext = executionContext.ShallowClone(isFlowSuppressed: false);
	}

	public static bool IsFlowSuppressed()
	{
		return Thread.CurrentThread._executionContext?.m_isFlowSuppressed ?? false;
	}

	public static void Run(ExecutionContext executionContext, ContextCallback callback, object? state)
	{
		if (executionContext == null)
		{
			ThrowNullContext();
		}
		RunInternal(executionContext, callback, state);
	}

	internal static void RunInternal(ExecutionContext executionContext, ContextCallback callback, object state)
	{
		Thread currentThread = Thread.CurrentThread;
		ExecutionContext executionContext2 = currentThread._executionContext;
		if (executionContext2 != null && executionContext2.m_isDefault)
		{
			executionContext2 = null;
		}
		ExecutionContext executionContext3 = executionContext2;
		SynchronizationContext synchronizationContext = currentThread._synchronizationContext;
		if (executionContext != null && executionContext.m_isDefault)
		{
			executionContext = null;
		}
		if (executionContext3 != executionContext)
		{
			RestoreChangedContextToThread(currentThread, executionContext, executionContext3);
		}
		ExceptionDispatchInfo exceptionDispatchInfo = null;
		try
		{
			callback(state);
		}
		catch (Exception source)
		{
			exceptionDispatchInfo = ExceptionDispatchInfo.Capture(source);
		}
		if (currentThread._synchronizationContext != synchronizationContext)
		{
			currentThread._synchronizationContext = synchronizationContext;
		}
		ExecutionContext executionContext4 = currentThread._executionContext;
		if (executionContext4 != executionContext3)
		{
			RestoreChangedContextToThread(currentThread, executionContext3, executionContext4);
		}
		exceptionDispatchInfo?.Throw();
	}

	public static void Restore(ExecutionContext executionContext)
	{
		if (executionContext == null)
		{
			ThrowNullContext();
		}
		RestoreInternal(executionContext);
	}

	internal static void RestoreInternal(ExecutionContext executionContext)
	{
		Thread currentThread = Thread.CurrentThread;
		ExecutionContext executionContext2 = currentThread._executionContext;
		if (executionContext2 != null && executionContext2.m_isDefault)
		{
			executionContext2 = null;
		}
		if (executionContext != null && executionContext.m_isDefault)
		{
			executionContext = null;
		}
		if (executionContext2 != executionContext)
		{
			RestoreChangedContextToThread(currentThread, executionContext, executionContext2);
		}
	}

	internal static void RunFromThreadPoolDispatchLoop(Thread threadPoolThread, ExecutionContext executionContext, ContextCallback callback, object state)
	{
		if (executionContext != null && !executionContext.m_isDefault)
		{
			RestoreChangedContextToThread(threadPoolThread, executionContext, null);
		}
		ExceptionDispatchInfo exceptionDispatchInfo = null;
		try
		{
			callback(state);
		}
		catch (Exception source)
		{
			exceptionDispatchInfo = ExceptionDispatchInfo.Capture(source);
		}
		ExecutionContext executionContext2 = threadPoolThread._executionContext;
		threadPoolThread._synchronizationContext = null;
		if (executionContext2 != null)
		{
			RestoreChangedContextToThread(threadPoolThread, null, executionContext2);
		}
		exceptionDispatchInfo?.Throw();
	}

	internal static void RunForThreadPoolUnsafe<TState>(ExecutionContext executionContext, Action<TState> callback, in TState state)
	{
		Thread.CurrentThread._executionContext = executionContext;
		if (executionContext.HasChangeNotifications)
		{
			OnValuesChanged(null, executionContext);
		}
		callback(state);
	}

	internal static void RestoreChangedContextToThread(Thread currentThread, ExecutionContext contextToRestore, ExecutionContext currentContext)
	{
		currentThread._executionContext = contextToRestore;
		if ((currentContext != null && currentContext.HasChangeNotifications) || (contextToRestore != null && contextToRestore.HasChangeNotifications))
		{
			OnValuesChanged(currentContext, contextToRestore);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ResetThreadPoolThread(Thread currentThread)
	{
		ExecutionContext executionContext = currentThread._executionContext;
		currentThread._synchronizationContext = null;
		currentThread._executionContext = null;
		if (executionContext != null && executionContext.HasChangeNotifications)
		{
			OnValuesChanged(executionContext, null);
			currentThread._synchronizationContext = null;
			currentThread._executionContext = null;
		}
	}

	internal static void OnValuesChanged(ExecutionContext previousExecutionCtx, ExecutionContext nextExecutionCtx)
	{
		IAsyncLocal[] array = previousExecutionCtx?.m_localChangeNotifications;
		IAsyncLocal[] array2 = nextExecutionCtx?.m_localChangeNotifications;
		try
		{
			if (array != null && array2 != null)
			{
				IAsyncLocal[] array3 = array;
				foreach (IAsyncLocal asyncLocal in array3)
				{
					previousExecutionCtx.m_localValues.TryGetValue(asyncLocal, out var value);
					nextExecutionCtx.m_localValues.TryGetValue(asyncLocal, out var value2);
					if (value != value2)
					{
						asyncLocal.OnValueChanged(value, value2, contextChanged: true);
					}
				}
				if (array2 == array)
				{
					return;
				}
				IAsyncLocal[] array4 = array2;
				foreach (IAsyncLocal asyncLocal2 in array4)
				{
					if (!previousExecutionCtx.m_localValues.TryGetValue(asyncLocal2, out var value3))
					{
						nextExecutionCtx.m_localValues.TryGetValue(asyncLocal2, out var value4);
						if (value3 != value4)
						{
							asyncLocal2.OnValueChanged(value3, value4, contextChanged: true);
						}
					}
				}
				return;
			}
			if (array != null)
			{
				IAsyncLocal[] array5 = array;
				foreach (IAsyncLocal asyncLocal3 in array5)
				{
					previousExecutionCtx.m_localValues.TryGetValue(asyncLocal3, out var value5);
					if (value5 != null)
					{
						asyncLocal3.OnValueChanged(value5, null, contextChanged: true);
					}
				}
				return;
			}
			IAsyncLocal[] array6 = array2;
			foreach (IAsyncLocal asyncLocal4 in array6)
			{
				nextExecutionCtx.m_localValues.TryGetValue(asyncLocal4, out var value6);
				if (value6 != null)
				{
					asyncLocal4.OnValueChanged(null, value6, contextChanged: true);
				}
			}
		}
		catch (Exception exception)
		{
			Environment.FailFast(SR.ExecutionContext_ExceptionInAsyncLocalNotification, exception);
		}
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private static void ThrowNullContext()
	{
		throw new InvalidOperationException(SR.InvalidOperation_NullContext);
	}

	internal static object GetLocalValue(IAsyncLocal local)
	{
		ExecutionContext executionContext = Thread.CurrentThread._executionContext;
		if (executionContext == null)
		{
			return null;
		}
		executionContext.m_localValues.TryGetValue(local, out var value);
		return value;
	}

	internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
	{
		ExecutionContext executionContext = Thread.CurrentThread._executionContext;
		object value = null;
		bool flag = false;
		if (executionContext != null)
		{
			flag = executionContext.m_localValues.TryGetValue(local, out value);
		}
		if (value == newValue)
		{
			return;
		}
		IAsyncLocal[] array = null;
		bool flag2 = false;
		IAsyncLocalValueMap asyncLocalValueMap;
		if (executionContext != null)
		{
			flag2 = executionContext.m_isFlowSuppressed;
			asyncLocalValueMap = executionContext.m_localValues.Set(local, newValue, !needChangeNotifications);
			array = executionContext.m_localChangeNotifications;
		}
		else
		{
			asyncLocalValueMap = AsyncLocalValueMap.Create(local, newValue, !needChangeNotifications);
		}
		if (needChangeNotifications && !flag)
		{
			if (array == null)
			{
				array = new IAsyncLocal[1] { local };
			}
			else
			{
				int num = array.Length;
				Array.Resize(ref array, num + 1);
				array[num] = local;
			}
		}
		Thread.CurrentThread._executionContext = ((!flag2 && AsyncLocalValueMap.IsEmpty(asyncLocalValueMap)) ? null : new ExecutionContext(asyncLocalValueMap, array, flag2));
		if (needChangeNotifications)
		{
			local.OnValueChanged(value, newValue, contextChanged: false);
		}
	}

	public ExecutionContext CreateCopy()
	{
		return this;
	}

	public void Dispose()
	{
	}
}
