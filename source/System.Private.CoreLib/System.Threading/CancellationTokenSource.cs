using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading;

public class CancellationTokenSource : IDisposable
{
	private sealed class Linked1CancellationTokenSource : CancellationTokenSource
	{
		private readonly CancellationTokenRegistration _reg1;

		internal Linked1CancellationTokenSource(CancellationToken token1)
		{
			_reg1 = token1.UnsafeRegister(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_reg1.Dispose();
				base.Dispose(disposing);
			}
		}
	}

	private sealed class Linked2CancellationTokenSource : CancellationTokenSource
	{
		private readonly CancellationTokenRegistration _reg1;

		private readonly CancellationTokenRegistration _reg2;

		internal Linked2CancellationTokenSource(CancellationToken token1, CancellationToken token2)
		{
			_reg1 = token1.UnsafeRegister(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
			_reg2 = token2.UnsafeRegister(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_reg1.Dispose();
				_reg2.Dispose();
				base.Dispose(disposing);
			}
		}
	}

	private sealed class LinkedNCancellationTokenSource : CancellationTokenSource
	{
		internal static readonly Action<object> s_linkedTokenCancelDelegate = delegate(object s)
		{
			((CancellationTokenSource)s).NotifyCancellation(throwOnFirstException: false);
		};

		private CancellationTokenRegistration[] _linkingRegistrations;

		internal LinkedNCancellationTokenSource(CancellationToken[] tokens)
		{
			_linkingRegistrations = new CancellationTokenRegistration[tokens.Length];
			for (int i = 0; i < tokens.Length; i++)
			{
				if (tokens[i].CanBeCanceled)
				{
					_linkingRegistrations[i] = tokens[i].UnsafeRegister(s_linkedTokenCancelDelegate, this);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
			{
				return;
			}
			CancellationTokenRegistration[] linkingRegistrations = _linkingRegistrations;
			if (linkingRegistrations != null)
			{
				_linkingRegistrations = null;
				for (int i = 0; i < linkingRegistrations.Length; i++)
				{
					linkingRegistrations[i].Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}

	internal sealed class Registrations
	{
		public readonly CancellationTokenSource Source;

		public CallbackNode Callbacks;

		public CallbackNode FreeNodeList;

		public long NextAvailableId = 1L;

		public long ExecutingCallbackId;

		public volatile int ThreadIDExecutingCallbacks = -1;

		private int _lock;

		public Registrations(CancellationTokenSource source)
		{
			Source = source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Recycle(CallbackNode node)
		{
			node.Id = 0L;
			node.Callback = null;
			node.CallbackState = null;
			node.ExecutionContext = null;
			node.SynchronizationContext = null;
			node.Prev = null;
			node.Next = FreeNodeList;
			FreeNodeList = node;
		}

		public bool Unregister(long id, CallbackNode node)
		{
			if (id == 0L)
			{
				return false;
			}
			EnterLock();
			try
			{
				if (node.Id != id)
				{
					return false;
				}
				if (Callbacks == node)
				{
					Callbacks = node.Next;
				}
				else
				{
					node.Prev.Next = node.Next;
				}
				if (node.Next != null)
				{
					node.Next.Prev = node.Prev;
				}
				Recycle(node);
				return true;
			}
			finally
			{
				ExitLock();
			}
		}

		public void UnregisterAll()
		{
			EnterLock();
			try
			{
				CallbackNode callbackNode = Callbacks;
				Callbacks = null;
				while (callbackNode != null)
				{
					CallbackNode next = callbackNode.Next;
					Recycle(callbackNode);
					callbackNode = next;
				}
			}
			finally
			{
				ExitLock();
			}
		}

		public void WaitForCallbackToComplete(long id)
		{
			SpinWait spinWait = default(SpinWait);
			while (Volatile.Read(ref ExecutingCallbackId) == id)
			{
				spinWait.SpinOnce();
			}
		}

		public ValueTask WaitForCallbackToCompleteAsync(long id)
		{
			if (Volatile.Read(ref ExecutingCallbackId) != id)
			{
				return default(ValueTask);
			}
			return new ValueTask(Task.Factory.StartNew((Func<object?, Task>)async delegate(object s)
			{
				TupleSlim<Registrations, long> state = (TupleSlim<Registrations, long>)s;
				while (Volatile.Read(ref state.Item1.ExecutingCallbackId) == state.Item2)
				{
					await Task.Yield();
				}
			}, (object?)new TupleSlim<Registrations, long>(this, id), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap());
		}

		public void EnterLock()
		{
			ref int @lock = ref _lock;
			if (Interlocked.Exchange(ref @lock, 1) != 0)
			{
				Contention(ref @lock);
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			static void Contention(ref int value)
			{
				SpinWait spinWait = default(SpinWait);
				do
				{
					spinWait.SpinOnce();
				}
				while (Interlocked.Exchange(ref value, 1) == 1);
			}
		}

		public void ExitLock()
		{
			Volatile.Write(ref _lock, 0);
		}
	}

	internal sealed class CallbackNode
	{
		public readonly Registrations Registrations;

		public CallbackNode Prev;

		public CallbackNode Next;

		public long Id;

		public Delegate Callback;

		public object CallbackState;

		public ExecutionContext ExecutionContext;

		public SynchronizationContext SynchronizationContext;

		public CallbackNode(Registrations registrations)
		{
			Registrations = registrations;
		}

		public void ExecuteCallback()
		{
			ExecutionContext executionContext = ExecutionContext;
			if (executionContext == null)
			{
				Invoke(Callback, CallbackState, Registrations.Source);
				return;
			}
			ExecutionContext.RunInternal(executionContext, delegate(object s)
			{
				CallbackNode callbackNode = (CallbackNode)s;
				Invoke(callbackNode.Callback, callbackNode.CallbackState, callbackNode.Registrations.Source);
			}, this);
		}
	}

	internal static readonly CancellationTokenSource s_canceledSource = new CancellationTokenSource
	{
		_state = 2
	};

	internal static readonly CancellationTokenSource s_neverCanceledSource = new CancellationTokenSource();

	private static readonly TimerCallback s_timerCallback = TimerCallback;

	private volatile int _state;

	private bool _disposed;

	private volatile TimerQueueTimer _timer;

	private volatile ManualResetEvent _kernelEvent;

	private Registrations _registrations;

	public bool IsCancellationRequested => _state != 0;

	internal bool IsCancellationCompleted => _state == 2;

	public CancellationToken Token
	{
		get
		{
			ThrowIfDisposed();
			return new CancellationToken(this);
		}
	}

	internal WaitHandle WaitHandle
	{
		get
		{
			ThrowIfDisposed();
			if (_kernelEvent != null)
			{
				return _kernelEvent;
			}
			ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
			if (Interlocked.CompareExchange(ref _kernelEvent, manualResetEvent, null) != null)
			{
				manualResetEvent.Dispose();
			}
			if (IsCancellationRequested)
			{
				_kernelEvent.Set();
			}
			return _kernelEvent;
		}
	}

	private static void TimerCallback(object state)
	{
		((CancellationTokenSource)state).NotifyCancellation(throwOnFirstException: false);
	}

	public CancellationTokenSource()
	{
	}

	public CancellationTokenSource(TimeSpan delay)
	{
		long num = (long)delay.TotalMilliseconds;
		if (num < -1 || num > 4294967294u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.delay);
		}
		InitializeWithTimer((uint)num);
	}

	public CancellationTokenSource(int millisecondsDelay)
	{
		if (millisecondsDelay < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.millisecondsDelay);
		}
		InitializeWithTimer((uint)millisecondsDelay);
	}

	private void InitializeWithTimer(uint millisecondsDelay)
	{
		if (millisecondsDelay == 0)
		{
			_state = 2;
		}
		else
		{
			_timer = new TimerQueueTimer(s_timerCallback, this, millisecondsDelay, uint.MaxValue, flowExecutionContext: false);
		}
	}

	public void Cancel()
	{
		Cancel(throwOnFirstException: false);
	}

	public void Cancel(bool throwOnFirstException)
	{
		ThrowIfDisposed();
		NotifyCancellation(throwOnFirstException);
	}

	public void CancelAfter(TimeSpan delay)
	{
		long num = (long)delay.TotalMilliseconds;
		if (num < -1 || num > 4294967294u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.delay);
		}
		CancelAfter((uint)num);
	}

	public void CancelAfter(int millisecondsDelay)
	{
		if (millisecondsDelay < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.millisecondsDelay);
		}
		CancelAfter((uint)millisecondsDelay);
	}

	private void CancelAfter(uint millisecondsDelay)
	{
		ThrowIfDisposed();
		if (IsCancellationRequested)
		{
			return;
		}
		TimerQueueTimer timerQueueTimer = _timer;
		if (timerQueueTimer == null)
		{
			timerQueueTimer = new TimerQueueTimer(s_timerCallback, this, uint.MaxValue, uint.MaxValue, flowExecutionContext: false);
			TimerQueueTimer timerQueueTimer2 = Interlocked.CompareExchange(ref _timer, timerQueueTimer, null);
			if (timerQueueTimer2 != null)
			{
				timerQueueTimer.Close();
				timerQueueTimer = timerQueueTimer2;
			}
		}
		try
		{
			timerQueueTimer.Change(millisecondsDelay, uint.MaxValue);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public bool TryReset()
	{
		ThrowIfDisposed();
		if (_state == 0)
		{
			bool flag = false;
			try
			{
				TimerQueueTimer timer = _timer;
				flag = timer == null || (timer.Change(uint.MaxValue, uint.MaxValue) && !timer._everQueued);
			}
			catch (ObjectDisposedException)
			{
			}
			if (flag)
			{
				Volatile.Read(ref _registrations)?.UnregisterAll();
				return true;
			}
		}
		return false;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing || _disposed)
		{
			return;
		}
		TimerQueueTimer timer = _timer;
		if (timer != null)
		{
			_timer = null;
			timer.Close();
		}
		_registrations = null;
		if (_kernelEvent != null)
		{
			ManualResetEvent manualResetEvent = Interlocked.Exchange(ref _kernelEvent, null);
			if (manualResetEvent != null && _state != 1)
			{
				manualResetEvent.Dispose();
			}
		}
		_disposed = true;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowHelper.ThrowObjectDisposedException(ExceptionResource.CancellationTokenSource_Disposed);
		}
	}

	internal CancellationTokenRegistration Register(Delegate callback, object stateForCallback, SynchronizationContext syncContext, ExecutionContext executionContext)
	{
		if (!IsCancellationRequested)
		{
			if (_disposed)
			{
				return default(CancellationTokenRegistration);
			}
			Registrations registrations = Volatile.Read(ref _registrations);
			if (registrations == null)
			{
				registrations = new Registrations(this);
				registrations = Interlocked.CompareExchange(ref _registrations, registrations, null) ?? registrations;
			}
			CallbackNode callbackNode = null;
			long id = 0L;
			if (registrations.FreeNodeList != null)
			{
				registrations.EnterLock();
				try
				{
					callbackNode = registrations.FreeNodeList;
					if (callbackNode != null)
					{
						registrations.FreeNodeList = callbackNode.Next;
						id = (callbackNode.Id = registrations.NextAvailableId++);
						callbackNode.Callback = callback;
						callbackNode.CallbackState = stateForCallback;
						callbackNode.ExecutionContext = executionContext;
						callbackNode.SynchronizationContext = syncContext;
						callbackNode.Next = registrations.Callbacks;
						registrations.Callbacks = callbackNode;
						if (callbackNode.Next != null)
						{
							callbackNode.Next.Prev = callbackNode;
						}
					}
				}
				finally
				{
					registrations.ExitLock();
				}
			}
			if (callbackNode == null)
			{
				callbackNode = new CallbackNode(registrations);
				callbackNode.Callback = callback;
				callbackNode.CallbackState = stateForCallback;
				callbackNode.ExecutionContext = executionContext;
				callbackNode.SynchronizationContext = syncContext;
				registrations.EnterLock();
				try
				{
					id = (callbackNode.Id = registrations.NextAvailableId++);
					callbackNode.Next = registrations.Callbacks;
					if (callbackNode.Next != null)
					{
						callbackNode.Next.Prev = callbackNode;
					}
					registrations.Callbacks = callbackNode;
				}
				finally
				{
					registrations.ExitLock();
				}
			}
			if (!IsCancellationRequested || !registrations.Unregister(id, callbackNode))
			{
				return new CancellationTokenRegistration(id, callbackNode);
			}
		}
		Invoke(callback, stateForCallback, this);
		return default(CancellationTokenRegistration);
	}

	private void NotifyCancellation(bool throwOnFirstException)
	{
		if (!IsCancellationRequested && Interlocked.CompareExchange(ref _state, 1, 0) == 0)
		{
			TimerQueueTimer timer = _timer;
			if (timer != null)
			{
				_timer = null;
				timer.Close();
			}
			_kernelEvent?.Set();
			ExecuteCallbackHandlers(throwOnFirstException);
		}
	}

	private void ExecuteCallbackHandlers(bool throwOnFirstException)
	{
		Registrations registrations = Interlocked.Exchange(ref _registrations, null);
		if (registrations == null)
		{
			Interlocked.Exchange(ref _state, 2);
			return;
		}
		registrations.ThreadIDExecutingCallbacks = Environment.CurrentManagedThreadId;
		List<Exception> list = null;
		try
		{
			while (true)
			{
				registrations.EnterLock();
				CallbackNode callbacks;
				try
				{
					callbacks = registrations.Callbacks;
					if (callbacks == null)
					{
						break;
					}
					if (callbacks.Next != null)
					{
						callbacks.Next.Prev = null;
					}
					registrations.Callbacks = callbacks.Next;
					registrations.ExecutingCallbackId = callbacks.Id;
					callbacks.Id = 0L;
					goto IL_0080;
				}
				finally
				{
					registrations.ExitLock();
				}
				IL_0080:
				try
				{
					if (callbacks.SynchronizationContext != null)
					{
						callbacks.SynchronizationContext.Send(delegate(object s)
						{
							CallbackNode callbackNode = (CallbackNode)s;
							callbackNode.Registrations.ThreadIDExecutingCallbacks = Environment.CurrentManagedThreadId;
							callbackNode.ExecuteCallback();
						}, callbacks);
						registrations.ThreadIDExecutingCallbacks = Environment.CurrentManagedThreadId;
					}
					else
					{
						callbacks.ExecuteCallback();
					}
				}
				catch (Exception item) when (!throwOnFirstException)
				{
					(list ?? (list = new List<Exception>())).Add(item);
				}
			}
		}
		finally
		{
			_state = 2;
			Interlocked.Exchange(ref registrations.ExecutingCallbackId, 0L);
		}
		if (list == null)
		{
			return;
		}
		throw new AggregateException(list);
	}

	public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
	{
		if (token1.CanBeCanceled)
		{
			if (!token2.CanBeCanceled)
			{
				return new Linked1CancellationTokenSource(token1);
			}
			return new Linked2CancellationTokenSource(token1, token2);
		}
		return CreateLinkedTokenSource(token2);
	}

	public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token)
	{
		if (!token.CanBeCanceled)
		{
			return new CancellationTokenSource();
		}
		return new Linked1CancellationTokenSource(token);
	}

	public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
	{
		if (tokens == null)
		{
			throw new ArgumentNullException("tokens");
		}
		return tokens.Length switch
		{
			0 => throw new ArgumentException(SR.CancellationToken_CreateLinkedToken_TokensIsEmpty), 
			1 => CreateLinkedTokenSource(tokens[0]), 
			2 => CreateLinkedTokenSource(tokens[0], tokens[1]), 
			_ => new LinkedNCancellationTokenSource(tokens), 
		};
	}

	private static void Invoke(Delegate d, object state, CancellationTokenSource source)
	{
		if (d is Action<object> action)
		{
			action(state);
		}
		else
		{
			((Action<object, CancellationToken>)d)(state, new CancellationToken(source));
		}
	}
}
