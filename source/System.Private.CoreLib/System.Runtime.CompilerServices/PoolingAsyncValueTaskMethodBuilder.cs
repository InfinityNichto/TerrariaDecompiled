using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Internal;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Auto)]
public struct PoolingAsyncValueTaskMethodBuilder
{
	private static readonly PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.StateMachineBox s_syncSuccessSentinel = PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.s_syncSuccessSentinel;

	private PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.StateMachineBox m_task;

	public ValueTask Task
	{
		get
		{
			if (m_task == s_syncSuccessSentinel)
			{
				return default(ValueTask);
			}
			PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.StateMachineBox stateMachineBox = m_task ?? (m_task = PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox());
			return new ValueTask(stateMachineBox, stateMachineBox.Version);
		}
	}

	public static PoolingAsyncValueTaskMethodBuilder Create()
	{
		return default(PoolingAsyncValueTaskMethodBuilder);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, null);
	}

	public void SetResult()
	{
		if (m_task == null)
		{
			m_task = s_syncSuccessSentinel;
		}
		else
		{
			m_task.SetResult(default(VoidTaskResult));
		}
	}

	public void SetException(Exception exception)
	{
		PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref m_task);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		PoolingAsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}
}
[StructLayout(LayoutKind.Auto)]
public struct PoolingAsyncValueTaskMethodBuilder<TResult>
{
	internal abstract class StateMachineBox : IValueTaskSource<TResult>, IValueTaskSource
	{
		protected Action _moveNextAction;

		public ExecutionContext Context;

		protected ManualResetValueTaskSourceCore<TResult> _valueTaskSource;

		public short Version => _valueTaskSource.Version;

		public void SetResult(TResult result)
		{
			_valueTaskSource.SetResult(result);
		}

		public void SetException(Exception error)
		{
			_valueTaskSource.SetException(error);
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _valueTaskSource.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_valueTaskSource.OnCompleted(continuation, state, token, flags);
		}

		TResult IValueTaskSource<TResult>.GetResult(short token)
		{
			throw NotImplemented.ByDesign;
		}

		void IValueTaskSource.GetResult(short token)
		{
			throw NotImplemented.ByDesign;
		}
	}

	private sealed class SyncSuccessSentinelStateMachineBox : StateMachineBox
	{
		public SyncSuccessSentinelStateMachineBox()
		{
			SetResult(default(TResult));
		}
	}

	private sealed class StateMachineBox<TStateMachine> : StateMachineBox, IValueTaskSource<TResult>, IValueTaskSource, IAsyncStateMachineBox, IThreadPoolWorkItem where TStateMachine : IAsyncStateMachine
	{
		private static readonly ContextCallback s_callback = ExecutionContextCallback;

		private static readonly PaddedReference[] s_perCoreCache = new PaddedReference[Environment.ProcessorCount];

		[ThreadStatic]
		private static StateMachineBox<TStateMachine> t_tlsCache;

		public TStateMachine StateMachine;

		private static ref StateMachineBox<TStateMachine> PerCoreCacheSlot
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				int num = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);
				return ref Unsafe.As<object, StateMachineBox<TStateMachine>>(ref s_perCoreCache[num].Object);
			}
		}

		public Action MoveNextAction => _moveNextAction ?? (_moveNextAction = MoveNext);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static StateMachineBox<TStateMachine> RentFromCache()
		{
			StateMachineBox<TStateMachine> stateMachineBox = t_tlsCache;
			if (stateMachineBox != null)
			{
				t_tlsCache = null;
			}
			else
			{
				ref StateMachineBox<TStateMachine> perCoreCacheSlot = ref PerCoreCacheSlot;
				if (perCoreCacheSlot == null || (stateMachineBox = Interlocked.Exchange(ref perCoreCacheSlot, null)) == null)
				{
					stateMachineBox = new StateMachineBox<TStateMachine>();
				}
			}
			return stateMachineBox;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ReturnToCache()
		{
			ClearStateUponCompletion();
			_valueTaskSource.Reset();
			if (t_tlsCache == null)
			{
				t_tlsCache = this;
				return;
			}
			ref StateMachineBox<TStateMachine> perCoreCacheSlot = ref PerCoreCacheSlot;
			if (perCoreCacheSlot == null)
			{
				Volatile.Write(ref perCoreCacheSlot, this);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearStateUponCompletion()
		{
			StateMachine = default(TStateMachine);
			Context = null;
		}

		private static void ExecutionContextCallback(object s)
		{
			Unsafe.As<StateMachineBox<TStateMachine>>(s).StateMachine.MoveNext();
		}

		void IThreadPoolWorkItem.Execute()
		{
			MoveNext();
		}

		public void MoveNext()
		{
			ExecutionContext context = Context;
			if (context == null)
			{
				StateMachine.MoveNext();
			}
			else
			{
				ExecutionContext.RunInternal(context, s_callback, this);
			}
		}

		TResult IValueTaskSource<TResult>.GetResult(short token)
		{
			try
			{
				return _valueTaskSource.GetResult(token);
			}
			finally
			{
				ReturnToCache();
			}
		}

		void IValueTaskSource.GetResult(short token)
		{
			try
			{
				_valueTaskSource.GetResult(token);
			}
			finally
			{
				ReturnToCache();
			}
		}

		IAsyncStateMachine IAsyncStateMachineBox.GetStateMachineObject()
		{
			return StateMachine;
		}
	}

	internal static readonly StateMachineBox s_syncSuccessSentinel = new SyncSuccessSentinelStateMachineBox();

	private StateMachineBox m_task;

	private TResult _result;

	public ValueTask<TResult> Task
	{
		get
		{
			if (m_task == s_syncSuccessSentinel)
			{
				return new ValueTask<TResult>(_result);
			}
			StateMachineBox stateMachineBox = m_task ?? (m_task = CreateWeaklyTypedStateMachineBox());
			return new ValueTask<TResult>(stateMachineBox, stateMachineBox.Version);
		}
	}

	public static PoolingAsyncValueTaskMethodBuilder<TResult> Create()
	{
		return default(PoolingAsyncValueTaskMethodBuilder<TResult>);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		AsyncMethodBuilderCore.Start(ref stateMachine);
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		AsyncMethodBuilderCore.SetStateMachine(stateMachine, null);
	}

	public void SetResult(TResult result)
	{
		if (m_task == null)
		{
			_result = result;
			m_task = s_syncSuccessSentinel;
		}
		else
		{
			m_task.SetResult(result);
		}
	}

	public void SetException(Exception exception)
	{
		SetException(exception, ref m_task);
	}

	internal static void SetException(Exception exception, [NotNull] ref StateMachineBox boxFieldRef)
	{
		if (exception == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
		}
		(boxFieldRef ?? (boxFieldRef = CreateWeaklyTypedStateMachineBox())).SetException(exception);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref StateMachineBox box) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		try
		{
			awaiter.OnCompleted(GetStateMachineBox(ref stateMachine, ref box).MoveNextAction);
		}
		catch (Exception exception)
		{
			System.Threading.Tasks.Task.ThrowAsync(exception, null);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, [NotNull] ref StateMachineBox boxRef) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		IAsyncStateMachineBox stateMachineBox = GetStateMachineBox(ref stateMachine, ref boxRef);
		AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, stateMachineBox);
	}

	private static IAsyncStateMachineBox GetStateMachineBox<TStateMachine>(ref TStateMachine stateMachine, [NotNull] ref StateMachineBox boxFieldRef) where TStateMachine : IAsyncStateMachine
	{
		ExecutionContext executionContext = ExecutionContext.Capture();
		if (boxFieldRef is StateMachineBox<TStateMachine> stateMachineBox)
		{
			if (stateMachineBox.Context != executionContext)
			{
				stateMachineBox.Context = executionContext;
			}
			return stateMachineBox;
		}
		if (boxFieldRef is StateMachineBox<IAsyncStateMachine> stateMachineBox2)
		{
			if (stateMachineBox2.StateMachine == null)
			{
				Debugger.NotifyOfCrossThreadDependency();
				stateMachineBox2.StateMachine = stateMachine;
			}
			stateMachineBox2.Context = executionContext;
			return stateMachineBox2;
		}
		Debugger.NotifyOfCrossThreadDependency();
		StateMachineBox<TStateMachine> stateMachineBox3 = (StateMachineBox<TStateMachine>)(boxFieldRef = StateMachineBox<TStateMachine>.RentFromCache());
		stateMachineBox3.StateMachine = stateMachine;
		stateMachineBox3.Context = executionContext;
		return stateMachineBox3;
	}

	internal static StateMachineBox CreateWeaklyTypedStateMachineBox()
	{
		return new StateMachineBox<IAsyncStateMachine>();
	}
}
