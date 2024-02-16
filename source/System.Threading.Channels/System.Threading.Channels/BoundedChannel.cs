using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels;

[DebuggerDisplay("Items={ItemsCountForDebugger}, Capacity={_bufferedCapacity}, Mode={_mode}, Closed={ChannelIsClosedForDebugger}")]
[DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
internal sealed class BoundedChannel<T> : Channel<T>, IDebugEnumerable<T>
{
	[DebuggerDisplay("Items={ItemsCountForDebugger}")]
	[DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
	private sealed class BoundedChannelReader : ChannelReader<T>, IDebugEnumerable<T>
	{
		internal readonly BoundedChannel<T> _parent;

		private readonly AsyncOperation<T> _readerSingleton;

		private readonly AsyncOperation<bool> _waiterSingleton;

		public override Task Completion => _parent._completion.Task;

		public override bool CanCount => true;

		public override bool CanPeek => true;

		public override int Count
		{
			get
			{
				BoundedChannel<T> parent = _parent;
				lock (parent.SyncObj)
				{
					return parent._items.Count;
				}
			}
		}

		private int ItemsCountForDebugger => _parent._items.Count;

		internal BoundedChannelReader(BoundedChannel<T> parent)
		{
			_parent = parent;
			_readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, default(CancellationToken), pooled: true);
			_waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, default(CancellationToken), pooled: true);
		}

		public override bool TryRead([MaybeNullWhen(false)] out T item)
		{
			BoundedChannel<T> parent = _parent;
			lock (parent.SyncObj)
			{
				if (!parent._items.IsEmpty)
				{
					item = DequeueItemAndPostProcess();
					return true;
				}
			}
			item = default(T);
			return false;
		}

		public override bool TryPeek([MaybeNullWhen(false)] out T item)
		{
			BoundedChannel<T> parent = _parent;
			lock (parent.SyncObj)
			{
				if (!parent._items.IsEmpty)
				{
					item = parent._items.PeekHead();
					return true;
				}
			}
			item = default(T);
			return false;
		}

		public override ValueTask<T> ReadAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
			}
			BoundedChannel<T> parent = _parent;
			lock (parent.SyncObj)
			{
				if (!parent._items.IsEmpty)
				{
					return new ValueTask<T>(DequeueItemAndPostProcess());
				}
				if (parent._doneWriting != null)
				{
					return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
				}
				if (!cancellationToken.CanBeCanceled)
				{
					AsyncOperation<T> readerSingleton = _readerSingleton;
					if (readerSingleton.TryOwnAndReset())
					{
						parent._blockedReaders.EnqueueTail(readerSingleton);
						return readerSingleton.ValueTaskOfT;
					}
				}
				AsyncOperation<T> asyncOperation = new AsyncOperation<T>(parent._runContinuationsAsynchronously | cancellationToken.CanBeCanceled, cancellationToken);
				parent._blockedReaders.EnqueueTail(asyncOperation);
				return asyncOperation.ValueTaskOfT;
			}
		}

		public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
			}
			BoundedChannel<T> parent = _parent;
			lock (parent.SyncObj)
			{
				if (!parent._items.IsEmpty)
				{
					return new ValueTask<bool>(result: true);
				}
				if (parent._doneWriting != null)
				{
					return (parent._doneWriting != ChannelUtilities.s_doneWritingSentinel) ? new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) : default(ValueTask<bool>);
				}
				if (!cancellationToken.CanBeCanceled)
				{
					AsyncOperation<bool> waiterSingleton = _waiterSingleton;
					if (waiterSingleton.TryOwnAndReset())
					{
						ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, waiterSingleton);
						return waiterSingleton.ValueTaskOfT;
					}
				}
				AsyncOperation<bool> asyncOperation = new AsyncOperation<bool>(parent._runContinuationsAsynchronously | cancellationToken.CanBeCanceled, cancellationToken);
				ChannelUtilities.QueueWaiter(ref _parent._waitingReadersTail, asyncOperation);
				return asyncOperation.ValueTaskOfT;
			}
		}

		private T DequeueItemAndPostProcess()
		{
			BoundedChannel<T> parent = _parent;
			T result = parent._items.DequeueHead();
			if (parent._doneWriting != null)
			{
				if (parent._items.IsEmpty)
				{
					ChannelUtilities.Complete(parent._completion, parent._doneWriting);
				}
			}
			else
			{
				while (!parent._blockedWriters.IsEmpty)
				{
					VoidAsyncOperationWithData<T> voidAsyncOperationWithData = parent._blockedWriters.DequeueHead();
					if (voidAsyncOperationWithData.TrySetResult(default(VoidResult)))
					{
						parent._items.EnqueueTail(voidAsyncOperationWithData.Item);
						return result;
					}
				}
				ChannelUtilities.WakeUpWaiters(ref parent._waitingWritersTail, result: true);
			}
			return result;
		}

		IEnumerator<T> IDebugEnumerable<T>.GetEnumerator()
		{
			return _parent._items.GetEnumerator();
		}
	}

	[DebuggerDisplay("Items={ItemsCountForDebugger}, Capacity={CapacityForDebugger}")]
	[DebuggerTypeProxy(typeof(DebugEnumeratorDebugView<>))]
	private sealed class BoundedChannelWriter : ChannelWriter<T>, IDebugEnumerable<T>
	{
		internal readonly BoundedChannel<T> _parent;

		private readonly VoidAsyncOperationWithData<T> _writerSingleton;

		private readonly AsyncOperation<bool> _waiterSingleton;

		private int ItemsCountForDebugger => _parent._items.Count;

		private int CapacityForDebugger => _parent._bufferedCapacity;

		internal BoundedChannelWriter(BoundedChannel<T> parent)
		{
			_parent = parent;
			_writerSingleton = new VoidAsyncOperationWithData<T>(runContinuationsAsynchronously: true, default(CancellationToken), pooled: true);
			_waiterSingleton = new AsyncOperation<bool>(runContinuationsAsynchronously: true, default(CancellationToken), pooled: true);
		}

		public override bool TryComplete(Exception error)
		{
			BoundedChannel<T> parent = _parent;
			bool isEmpty;
			lock (parent.SyncObj)
			{
				if (parent._doneWriting != null)
				{
					return false;
				}
				parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;
				isEmpty = parent._items.IsEmpty;
			}
			if (isEmpty)
			{
				ChannelUtilities.Complete(parent._completion, error);
			}
			ChannelUtilities.FailOperations<AsyncOperation<T>, T>(parent._blockedReaders, ChannelUtilities.CreateInvalidCompletionException(error));
			ChannelUtilities.FailOperations<VoidAsyncOperationWithData<T>, VoidResult>(parent._blockedWriters, ChannelUtilities.CreateInvalidCompletionException(error));
			ChannelUtilities.WakeUpWaiters(ref parent._waitingReadersTail, result: false, error);
			ChannelUtilities.WakeUpWaiters(ref parent._waitingWritersTail, result: false, error);
			return true;
		}

		public override bool TryWrite(T item)
		{
			AsyncOperation<T> asyncOperation = null;
			AsyncOperation<bool> listTail = null;
			BoundedChannel<T> parent = _parent;
			bool lockTaken = false;
			try
			{
				Monitor.Enter(parent.SyncObj, ref lockTaken);
				if (parent._doneWriting != null)
				{
					return false;
				}
				int count = parent._items.Count;
				if (count != 0)
				{
					if (count < parent._bufferedCapacity)
					{
						parent._items.EnqueueTail(item);
						return true;
					}
					if (parent._mode == BoundedChannelFullMode.Wait)
					{
						return false;
					}
					if (parent._mode == BoundedChannelFullMode.DropWrite)
					{
						Monitor.Exit(parent.SyncObj);
						lockTaken = false;
						parent._itemDropped?.Invoke(item);
						return true;
					}
					T obj = ((parent._mode == BoundedChannelFullMode.DropNewest) ? parent._items.DequeueTail() : parent._items.DequeueHead());
					parent._items.EnqueueTail(item);
					Monitor.Exit(parent.SyncObj);
					lockTaken = false;
					parent._itemDropped?.Invoke(obj);
					return true;
				}
				while (!parent._blockedReaders.IsEmpty)
				{
					AsyncOperation<T> asyncOperation2 = parent._blockedReaders.DequeueHead();
					if (asyncOperation2.UnregisterCancellation())
					{
						asyncOperation = asyncOperation2;
						break;
					}
				}
				if (asyncOperation == null)
				{
					parent._items.EnqueueTail(item);
					listTail = parent._waitingReadersTail;
					if (listTail == null)
					{
						return true;
					}
					parent._waitingReadersTail = null;
				}
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(parent.SyncObj);
				}
			}
			if (asyncOperation != null)
			{
				bool flag = asyncOperation.TrySetResult(item);
			}
			else
			{
				ChannelUtilities.WakeUpWaiters(ref listTail, result: true);
			}
			return true;
		}

		public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
			}
			BoundedChannel<T> parent = _parent;
			lock (parent.SyncObj)
			{
				if (parent._doneWriting != null)
				{
					return (parent._doneWriting != ChannelUtilities.s_doneWritingSentinel) ? new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) : default(ValueTask<bool>);
				}
				if (parent._items.Count < parent._bufferedCapacity || parent._mode != 0)
				{
					return new ValueTask<bool>(result: true);
				}
				if (!cancellationToken.CanBeCanceled)
				{
					AsyncOperation<bool> waiterSingleton = _waiterSingleton;
					if (waiterSingleton.TryOwnAndReset())
					{
						ChannelUtilities.QueueWaiter(ref parent._waitingWritersTail, waiterSingleton);
						return waiterSingleton.ValueTaskOfT;
					}
				}
				AsyncOperation<bool> asyncOperation = new AsyncOperation<bool>(runContinuationsAsynchronously: true, cancellationToken);
				ChannelUtilities.QueueWaiter(ref parent._waitingWritersTail, asyncOperation);
				return asyncOperation.ValueTaskOfT;
			}
		}

		public override ValueTask WriteAsync(T item, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return new ValueTask(Task.FromCanceled(cancellationToken));
			}
			AsyncOperation<T> asyncOperation = null;
			AsyncOperation<bool> listTail = null;
			BoundedChannel<T> parent = _parent;
			bool lockTaken = false;
			try
			{
				Monitor.Enter(parent.SyncObj, ref lockTaken);
				if (parent._doneWriting != null)
				{
					return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(parent._doneWriting)));
				}
				int count = parent._items.Count;
				if (count != 0)
				{
					if (count < parent._bufferedCapacity)
					{
						parent._items.EnqueueTail(item);
						return default(ValueTask);
					}
					if (parent._mode == BoundedChannelFullMode.Wait)
					{
						if (!cancellationToken.CanBeCanceled)
						{
							VoidAsyncOperationWithData<T> writerSingleton = _writerSingleton;
							if (writerSingleton.TryOwnAndReset())
							{
								writerSingleton.Item = item;
								parent._blockedWriters.EnqueueTail(writerSingleton);
								return writerSingleton.ValueTask;
							}
						}
						VoidAsyncOperationWithData<T> voidAsyncOperationWithData = new VoidAsyncOperationWithData<T>(runContinuationsAsynchronously: true, cancellationToken);
						voidAsyncOperationWithData.Item = item;
						parent._blockedWriters.EnqueueTail(voidAsyncOperationWithData);
						return voidAsyncOperationWithData.ValueTask;
					}
					if (parent._mode == BoundedChannelFullMode.DropWrite)
					{
						Monitor.Exit(parent.SyncObj);
						lockTaken = false;
						parent._itemDropped?.Invoke(item);
						return default(ValueTask);
					}
					T obj = ((parent._mode == BoundedChannelFullMode.DropNewest) ? parent._items.DequeueTail() : parent._items.DequeueHead());
					parent._items.EnqueueTail(item);
					Monitor.Exit(parent.SyncObj);
					lockTaken = false;
					parent._itemDropped?.Invoke(obj);
					return default(ValueTask);
				}
				while (!parent._blockedReaders.IsEmpty)
				{
					AsyncOperation<T> asyncOperation2 = parent._blockedReaders.DequeueHead();
					if (asyncOperation2.UnregisterCancellation())
					{
						asyncOperation = asyncOperation2;
						break;
					}
				}
				if (asyncOperation == null)
				{
					parent._items.EnqueueTail(item);
					listTail = parent._waitingReadersTail;
					if (listTail == null)
					{
						return default(ValueTask);
					}
					parent._waitingReadersTail = null;
				}
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(parent.SyncObj);
				}
			}
			if (asyncOperation != null)
			{
				bool flag = asyncOperation.TrySetResult(item);
			}
			else
			{
				ChannelUtilities.WakeUpWaiters(ref listTail, result: true);
			}
			return default(ValueTask);
		}

		IEnumerator<T> IDebugEnumerable<T>.GetEnumerator()
		{
			return _parent._items.GetEnumerator();
		}
	}

	private readonly BoundedChannelFullMode _mode;

	private readonly Action<T> _itemDropped;

	private readonly TaskCompletionSource _completion;

	private readonly int _bufferedCapacity;

	private readonly Deque<T> _items = new Deque<T>();

	private readonly Deque<AsyncOperation<T>> _blockedReaders = new Deque<AsyncOperation<T>>();

	private readonly Deque<VoidAsyncOperationWithData<T>> _blockedWriters = new Deque<VoidAsyncOperationWithData<T>>();

	private AsyncOperation<bool> _waitingReadersTail;

	private AsyncOperation<bool> _waitingWritersTail;

	private readonly bool _runContinuationsAsynchronously;

	private Exception _doneWriting;

	private object SyncObj => _items;

	private int ItemsCountForDebugger => _items.Count;

	private bool ChannelIsClosedForDebugger => _doneWriting != null;

	internal BoundedChannel(int bufferedCapacity, BoundedChannelFullMode mode, bool runContinuationsAsynchronously, Action<T> itemDropped)
	{
		_bufferedCapacity = bufferedCapacity;
		_mode = mode;
		_runContinuationsAsynchronously = runContinuationsAsynchronously;
		_itemDropped = itemDropped;
		_completion = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
		base.Reader = new BoundedChannelReader(this);
		base.Writer = new BoundedChannelWriter(this);
	}

	IEnumerator<T> IDebugEnumerable<T>.GetEnumerator()
	{
		return _items.GetEnumerator();
	}
}
