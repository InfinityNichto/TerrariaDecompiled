using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerTypeProxy(typeof(System.Collections.Concurrent.IProducerConsumerCollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public class ConcurrentBag<T> : IProducerConsumerCollection<T>, IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
{
	private sealed class WorkStealingQueue
	{
		private volatile int _headIndex;

		private volatile int _tailIndex;

		private volatile T[] _array = new T[32];

		private volatile int _mask = 31;

		private int _addTakeCount;

		private int _stealCount;

		internal volatile int _currentOp;

		internal bool _frozen;

		internal readonly WorkStealingQueue _nextQueue;

		internal readonly int _ownerThreadId;

		internal bool IsEmpty => _headIndex - _tailIndex >= 0;

		internal int DangerousCount
		{
			get
			{
				int stealCount = _stealCount;
				int addTakeCount = _addTakeCount;
				return addTakeCount - stealCount;
			}
		}

		internal WorkStealingQueue(WorkStealingQueue nextQueue)
		{
			_ownerThreadId = Environment.CurrentManagedThreadId;
			_nextQueue = nextQueue;
		}

		internal void LocalPush(T item, ref long emptyToNonEmptyListTransitionCount)
		{
			bool lockTaken = false;
			try
			{
				Interlocked.Exchange(ref _currentOp, 1);
				int num = _tailIndex;
				if (num == int.MaxValue)
				{
					_currentOp = 0;
					lock (this)
					{
						_headIndex &= _mask;
						num = (_tailIndex = num & _mask);
						Interlocked.Exchange(ref _currentOp, 1);
					}
				}
				int headIndex = _headIndex;
				if (!_frozen && headIndex - (num - 1) < 0 && num - (headIndex + _mask) < 0)
				{
					_array[num & _mask] = item;
					_tailIndex = num + 1;
				}
				else
				{
					_currentOp = 0;
					Monitor.Enter(this, ref lockTaken);
					headIndex = _headIndex;
					int num2 = num - headIndex;
					if (num2 >= _mask)
					{
						T[] array = new T[_array.Length << 1];
						int num3 = headIndex & _mask;
						if (num3 == 0)
						{
							Array.Copy(_array, array, _array.Length);
						}
						else
						{
							Array.Copy(_array, num3, array, 0, _array.Length - num3);
							Array.Copy(_array, 0, array, _array.Length - num3, num3);
						}
						_array = array;
						_headIndex = 0;
						num = (_tailIndex = num2);
						_mask = (_mask << 1) | 1;
					}
					_array[num & _mask] = item;
					_tailIndex = num + 1;
					if (num2 == 0)
					{
						Interlocked.Increment(ref emptyToNonEmptyListTransitionCount);
					}
					_addTakeCount -= _stealCount;
					_stealCount = 0;
				}
				checked
				{
					_addTakeCount++;
				}
			}
			finally
			{
				_currentOp = 0;
				if (lockTaken)
				{
					Monitor.Exit(this);
				}
			}
		}

		internal void LocalClear()
		{
			lock (this)
			{
				if (_headIndex - _tailIndex < 0)
				{
					_headIndex = (_tailIndex = 0);
					_addTakeCount = (_stealCount = 0);
					Array.Clear(_array);
				}
			}
		}

		internal bool TryLocalPop([MaybeNullWhen(false)] out T result)
		{
			int tailIndex = _tailIndex;
			if (_headIndex - tailIndex >= 0)
			{
				result = default(T);
				return false;
			}
			bool lockTaken = false;
			try
			{
				_currentOp = 2;
				Interlocked.Exchange(ref _tailIndex, --tailIndex);
				if (!_frozen && _headIndex - tailIndex < 0)
				{
					int num = tailIndex & _mask;
					result = _array[num];
					if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
					{
						_array[num] = default(T);
					}
					_addTakeCount--;
					return true;
				}
				_currentOp = 0;
				Monitor.Enter(this, ref lockTaken);
				if (_headIndex - tailIndex <= 0)
				{
					int num2 = tailIndex & _mask;
					result = _array[num2];
					if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
					{
						_array[num2] = default(T);
					}
					_addTakeCount--;
					return true;
				}
				_tailIndex = tailIndex + 1;
				result = default(T);
				return false;
			}
			finally
			{
				_currentOp = 0;
				if (lockTaken)
				{
					Monitor.Exit(this);
				}
			}
		}

		internal bool TryLocalPeek([MaybeNullWhen(false)] out T result)
		{
			int tailIndex = _tailIndex;
			if (_headIndex - tailIndex < 0)
			{
				lock (this)
				{
					if (_headIndex - tailIndex < 0)
					{
						result = _array[(tailIndex - 1) & _mask];
						return true;
					}
				}
			}
			result = default(T);
			return false;
		}

		internal bool TrySteal([MaybeNullWhen(false)] out T result, bool take)
		{
			lock (this)
			{
				int headIndex = _headIndex;
				if (take)
				{
					if (headIndex - (_tailIndex - 2) >= 0 && _currentOp == 1)
					{
						SpinWait spinWait = default(SpinWait);
						do
						{
							spinWait.SpinOnce();
						}
						while (_currentOp == 1);
					}
					Interlocked.Exchange(ref _headIndex, headIndex + 1);
					if (headIndex < _tailIndex)
					{
						int num = headIndex & _mask;
						result = _array[num];
						if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
						{
							_array[num] = default(T);
						}
						_stealCount++;
						return true;
					}
					_headIndex = headIndex;
				}
				else if (headIndex < _tailIndex)
				{
					result = _array[headIndex & _mask];
					return true;
				}
			}
			result = default(T);
			return false;
		}

		internal int DangerousCopyTo(T[] array, int arrayIndex)
		{
			int headIndex = _headIndex;
			int dangerousCount = DangerousCount;
			for (int num = arrayIndex + dangerousCount - 1; num >= arrayIndex; num--)
			{
				array[num] = _array[headIndex++ & _mask];
			}
			return dangerousCount;
		}
	}

	private sealed class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly T[] _array;

		private T _current;

		private int _index;

		public T Current => _current;

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _array.Length + 1)
				{
					throw new InvalidOperationException(System.SR.ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished);
				}
				return Current;
			}
		}

		public Enumerator(T[] array)
		{
			_array = array;
		}

		public bool MoveNext()
		{
			if (_index < _array.Length)
			{
				_current = _array[_index++];
				return true;
			}
			_index = _array.Length + 1;
			return false;
		}

		public void Reset()
		{
			_index = 0;
			_current = default(T);
		}

		public void Dispose()
		{
		}
	}

	private readonly ThreadLocal<WorkStealingQueue> _locals;

	private volatile WorkStealingQueue _workStealingQueues;

	private long _emptyToNonEmptyListTransitionCount;

	public int Count
	{
		get
		{
			if (_workStealingQueues == null)
			{
				return 0;
			}
			bool lockTaken = false;
			try
			{
				FreezeBag(ref lockTaken);
				return DangerousCount;
			}
			finally
			{
				UnfreezeBag(lockTaken);
			}
		}
	}

	private int DangerousCount
	{
		get
		{
			int num = 0;
			for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
			{
				num = checked(num + workStealingQueue.DangerousCount);
			}
			return num;
		}
	}

	public bool IsEmpty
	{
		get
		{
			WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
			if (currentThreadWorkStealingQueue != null)
			{
				if (!currentThreadWorkStealingQueue.IsEmpty)
				{
					return false;
				}
				if (currentThreadWorkStealingQueue._nextQueue == null && currentThreadWorkStealingQueue == _workStealingQueues)
				{
					return true;
				}
			}
			bool lockTaken = false;
			try
			{
				FreezeBag(ref lockTaken);
				for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
				{
					if (!workStealingQueue.IsEmpty)
					{
						return false;
					}
				}
			}
			finally
			{
				UnfreezeBag(lockTaken);
			}
			return true;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException(System.SR.ConcurrentCollection_SyncRoot_NotSupported);
		}
	}

	private object GlobalQueuesLock => _locals;

	public ConcurrentBag()
	{
		_locals = new ThreadLocal<WorkStealingQueue>();
	}

	public ConcurrentBag(IEnumerable<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection", System.SR.ConcurrentBag_Ctor_ArgumentNullException);
		}
		_locals = new ThreadLocal<WorkStealingQueue>();
		WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: true);
		foreach (T item in collection)
		{
			currentThreadWorkStealingQueue.LocalPush(item, ref _emptyToNonEmptyListTransitionCount);
		}
	}

	public void Add(T item)
	{
		GetCurrentThreadWorkStealingQueue(forceCreate: true).LocalPush(item, ref _emptyToNonEmptyListTransitionCount);
	}

	bool IProducerConsumerCollection<T>.TryAdd(T item)
	{
		Add(item);
		return true;
	}

	public bool TryTake([MaybeNullWhen(false)] out T result)
	{
		WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
		if (currentThreadWorkStealingQueue == null || !currentThreadWorkStealingQueue.TryLocalPop(out result))
		{
			return TrySteal(out result, take: true);
		}
		return true;
	}

	public bool TryPeek([MaybeNullWhen(false)] out T result)
	{
		WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
		if (currentThreadWorkStealingQueue == null || !currentThreadWorkStealingQueue.TryLocalPeek(out result))
		{
			return TrySteal(out result, take: false);
		}
		return true;
	}

	private WorkStealingQueue GetCurrentThreadWorkStealingQueue(bool forceCreate)
	{
		WorkStealingQueue workStealingQueue = _locals.Value;
		if (workStealingQueue == null)
		{
			if (!forceCreate)
			{
				return null;
			}
			workStealingQueue = CreateWorkStealingQueueForCurrentThread();
		}
		return workStealingQueue;
	}

	private WorkStealingQueue CreateWorkStealingQueueForCurrentThread()
	{
		lock (GlobalQueuesLock)
		{
			WorkStealingQueue workStealingQueues = _workStealingQueues;
			WorkStealingQueue workStealingQueue = ((workStealingQueues != null) ? GetUnownedWorkStealingQueue() : null);
			if (workStealingQueue == null)
			{
				workStealingQueue = (_workStealingQueues = new WorkStealingQueue(workStealingQueues));
			}
			_locals.Value = workStealingQueue;
			return workStealingQueue;
		}
	}

	private WorkStealingQueue GetUnownedWorkStealingQueue()
	{
		int currentManagedThreadId = Environment.CurrentManagedThreadId;
		for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
		{
			if (workStealingQueue._ownerThreadId == currentManagedThreadId)
			{
				return workStealingQueue;
			}
		}
		return null;
	}

	private bool TrySteal([MaybeNullWhen(false)] out T result, bool take)
	{
		if (CDSCollectionETWBCLProvider.Log.IsEnabled())
		{
			if (take)
			{
				CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryTakeSteals();
			}
			else
			{
				CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryPeekSteals();
			}
		}
		while (true)
		{
			long num = Interlocked.Read(ref _emptyToNonEmptyListTransitionCount);
			WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
			bool num2;
			if (currentThreadWorkStealingQueue != null)
			{
				if (TryStealFromTo(currentThreadWorkStealingQueue._nextQueue, null, out result, take))
				{
					goto IL_0078;
				}
				num2 = TryStealFromTo(_workStealingQueues, currentThreadWorkStealingQueue, out result, take);
			}
			else
			{
				num2 = TryStealFromTo(_workStealingQueues, null, out result, take);
			}
			if (!num2)
			{
				if (Interlocked.Read(ref _emptyToNonEmptyListTransitionCount) == num)
				{
					break;
				}
				continue;
			}
			goto IL_0078;
			IL_0078:
			return true;
		}
		return false;
	}

	private bool TryStealFromTo(WorkStealingQueue startInclusive, WorkStealingQueue endExclusive, [MaybeNullWhen(false)] out T result, bool take)
	{
		for (WorkStealingQueue workStealingQueue = startInclusive; workStealingQueue != endExclusive; workStealingQueue = workStealingQueue._nextQueue)
		{
			if (workStealingQueue.TrySteal(out result, take))
			{
				return true;
			}
		}
		result = default(T);
		return false;
	}

	public void CopyTo(T[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", System.SR.ConcurrentBag_CopyTo_ArgumentNullException);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.Collection_CopyTo_ArgumentOutOfRangeException);
		}
		if (_workStealingQueues == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			FreezeBag(ref lockTaken);
			int dangerousCount = DangerousCount;
			if (index > array.Length - dangerousCount)
			{
				throw new ArgumentException(System.SR.Collection_CopyTo_TooManyElems, "index");
			}
			try
			{
				int num = CopyFromEachQueueToArray(array, index);
			}
			catch (ArrayTypeMismatchException ex)
			{
				throw new InvalidCastException(ex.Message, ex);
			}
		}
		finally
		{
			UnfreezeBag(lockTaken);
		}
	}

	private int CopyFromEachQueueToArray(T[] array, int index)
	{
		int num = index;
		for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
		{
			num += workStealingQueue.DangerousCopyTo(array, num);
		}
		return num - index;
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array is T[] array2)
		{
			CopyTo(array2, index);
			return;
		}
		if (array == null)
		{
			throw new ArgumentNullException("array", System.SR.ConcurrentBag_CopyTo_ArgumentNullException);
		}
		ToArray().CopyTo(array, index);
	}

	public T[] ToArray()
	{
		if (_workStealingQueues != null)
		{
			bool lockTaken = false;
			try
			{
				FreezeBag(ref lockTaken);
				int dangerousCount = DangerousCount;
				if (dangerousCount > 0)
				{
					T[] array = new T[dangerousCount];
					int num = CopyFromEachQueueToArray(array, 0);
					return array;
				}
			}
			finally
			{
				UnfreezeBag(lockTaken);
			}
		}
		return Array.Empty<T>();
	}

	public void Clear()
	{
		if (_workStealingQueues == null)
		{
			return;
		}
		WorkStealingQueue currentThreadWorkStealingQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
		if (currentThreadWorkStealingQueue != null)
		{
			currentThreadWorkStealingQueue.LocalClear();
			if (currentThreadWorkStealingQueue._nextQueue == null && currentThreadWorkStealingQueue == _workStealingQueues)
			{
				return;
			}
		}
		bool lockTaken = false;
		try
		{
			FreezeBag(ref lockTaken);
			for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
			{
				T result;
				while (workStealingQueue.TrySteal(out result, take: true))
				{
				}
			}
		}
		finally
		{
			UnfreezeBag(lockTaken);
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(ToArray());
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void FreezeBag(ref bool lockTaken)
	{
		Monitor.Enter(GlobalQueuesLock, ref lockTaken);
		WorkStealingQueue workStealingQueues = _workStealingQueues;
		for (WorkStealingQueue workStealingQueue = workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
		{
			Monitor.Enter(workStealingQueue, ref workStealingQueue._frozen);
		}
		Interlocked.MemoryBarrier();
		for (WorkStealingQueue workStealingQueue2 = workStealingQueues; workStealingQueue2 != null; workStealingQueue2 = workStealingQueue2._nextQueue)
		{
			if (workStealingQueue2._currentOp != 0)
			{
				SpinWait spinWait = default(SpinWait);
				do
				{
					spinWait.SpinOnce();
				}
				while (workStealingQueue2._currentOp != 0);
			}
		}
	}

	private void UnfreezeBag(bool lockTaken)
	{
		if (!lockTaken)
		{
			return;
		}
		for (WorkStealingQueue workStealingQueue = _workStealingQueues; workStealingQueue != null; workStealingQueue = workStealingQueue._nextQueue)
		{
			if (workStealingQueue._frozen)
			{
				workStealingQueue._frozen = false;
				Monitor.Exit(workStealingQueue);
			}
		}
		Monitor.Exit(GlobalQueuesLock);
	}
}
