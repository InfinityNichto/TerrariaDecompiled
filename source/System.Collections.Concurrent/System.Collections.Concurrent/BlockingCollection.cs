using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Collections.Concurrent;

[UnsupportedOSPlatform("browser")]
[DebuggerTypeProxy(typeof(BlockingCollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}, Type = {_collection}")]
public class BlockingCollection<T> : IEnumerable<T>, IEnumerable, ICollection, IDisposable, IReadOnlyCollection<T>
{
	private IProducerConsumerCollection<T> _collection;

	private int _boundedCapacity;

	private SemaphoreSlim _freeNodes;

	private SemaphoreSlim _occupiedNodes;

	private bool _isDisposed;

	private CancellationTokenSource _consumersCancellationTokenSource;

	private CancellationTokenSource _producersCancellationTokenSource;

	private volatile int _currentAdders;

	public int BoundedCapacity
	{
		get
		{
			CheckDisposed();
			return _boundedCapacity;
		}
	}

	public bool IsAddingCompleted
	{
		get
		{
			CheckDisposed();
			return _currentAdders == int.MinValue;
		}
	}

	public bool IsCompleted
	{
		get
		{
			CheckDisposed();
			if (IsAddingCompleted)
			{
				return _occupiedNodes.CurrentCount == 0;
			}
			return false;
		}
	}

	public int Count
	{
		get
		{
			CheckDisposed();
			return _occupiedNodes.CurrentCount;
		}
	}

	bool ICollection.IsSynchronized
	{
		get
		{
			CheckDisposed();
			return false;
		}
	}

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException(System.SR.ConcurrentCollection_SyncRoot_NotSupported);
		}
	}

	public BlockingCollection()
		: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>())
	{
	}

	public BlockingCollection(int boundedCapacity)
		: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), boundedCapacity)
	{
	}

	public BlockingCollection(IProducerConsumerCollection<T> collection, int boundedCapacity)
	{
		if (boundedCapacity < 1)
		{
			throw new ArgumentOutOfRangeException("boundedCapacity", boundedCapacity, System.SR.BlockingCollection_ctor_BoundedCapacityRange);
		}
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		int count = collection.Count;
		if (count > boundedCapacity)
		{
			throw new ArgumentException(System.SR.BlockingCollection_ctor_CountMoreThanCapacity);
		}
		Initialize(collection, boundedCapacity, count);
	}

	public BlockingCollection(IProducerConsumerCollection<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		Initialize(collection, -1, collection.Count);
	}

	[MemberNotNull("_collection")]
	[MemberNotNull("_consumersCancellationTokenSource")]
	[MemberNotNull("_producersCancellationTokenSource")]
	[MemberNotNull("_occupiedNodes")]
	private void Initialize(IProducerConsumerCollection<T> collection, int boundedCapacity, int collectionCount)
	{
		_collection = collection;
		_boundedCapacity = boundedCapacity;
		_isDisposed = false;
		_consumersCancellationTokenSource = new CancellationTokenSource();
		_producersCancellationTokenSource = new CancellationTokenSource();
		if (boundedCapacity == -1)
		{
			_freeNodes = null;
		}
		else
		{
			_freeNodes = new SemaphoreSlim(boundedCapacity - collectionCount);
		}
		_occupiedNodes = new SemaphoreSlim(collectionCount);
	}

	public void Add(T item)
	{
		TryAddWithNoTimeValidation(item, -1, CancellationToken.None);
	}

	public void Add(T item, CancellationToken cancellationToken)
	{
		TryAddWithNoTimeValidation(item, -1, cancellationToken);
	}

	public bool TryAdd(T item)
	{
		return TryAddWithNoTimeValidation(item, 0, CancellationToken.None);
	}

	public bool TryAdd(T item, TimeSpan timeout)
	{
		ValidateTimeout(timeout);
		return TryAddWithNoTimeValidation(item, (int)timeout.TotalMilliseconds, CancellationToken.None);
	}

	public bool TryAdd(T item, int millisecondsTimeout)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryAddWithNoTimeValidation(item, millisecondsTimeout, CancellationToken.None);
	}

	public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryAddWithNoTimeValidation(item, millisecondsTimeout, cancellationToken);
	}

	private bool TryAddWithNoTimeValidation(T item, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		CheckDisposed();
		cancellationToken.ThrowIfCancellationRequested();
		if (IsAddingCompleted)
		{
			throw new InvalidOperationException(System.SR.BlockingCollection_Completed);
		}
		bool flag = true;
		if (_freeNodes != null)
		{
			CancellationTokenSource cancellationTokenSource = null;
			try
			{
				flag = _freeNodes.Wait(0, default(CancellationToken));
				if (!flag && millisecondsTimeout != 0)
				{
					cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _producersCancellationTokenSource.Token);
					flag = _freeNodes.Wait(millisecondsTimeout, cancellationTokenSource.Token);
				}
			}
			catch (OperationCanceledException)
			{
				cancellationToken.ThrowIfCancellationRequested();
				throw new InvalidOperationException(System.SR.BlockingCollection_Add_ConcurrentCompleteAdd);
			}
			finally
			{
				cancellationTokenSource?.Dispose();
			}
		}
		if (flag)
		{
			SpinWait spinWait = default(SpinWait);
			while (true)
			{
				int currentAdders = _currentAdders;
				if (((uint)currentAdders & 0x80000000u) != 0)
				{
					spinWait.Reset();
					while (_currentAdders != int.MinValue)
					{
						spinWait.SpinOnce();
					}
					throw new InvalidOperationException(System.SR.BlockingCollection_Completed);
				}
				if (Interlocked.CompareExchange(ref _currentAdders, currentAdders + 1, currentAdders) == currentAdders)
				{
					break;
				}
				spinWait.SpinOnce(-1);
			}
			try
			{
				bool flag2 = false;
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					flag2 = _collection.TryAdd(item);
				}
				catch
				{
					if (_freeNodes != null)
					{
						_freeNodes.Release();
					}
					throw;
				}
				if (!flag2)
				{
					throw new InvalidOperationException(System.SR.BlockingCollection_Add_Failed);
				}
				_occupiedNodes.Release();
			}
			finally
			{
				Interlocked.Decrement(ref _currentAdders);
			}
		}
		return flag;
	}

	public T Take()
	{
		if (!TryTake(out var item, -1, CancellationToken.None))
		{
			throw new InvalidOperationException(System.SR.BlockingCollection_CantTakeWhenDone);
		}
		return item;
	}

	public T Take(CancellationToken cancellationToken)
	{
		if (!TryTake(out var item, -1, cancellationToken))
		{
			throw new InvalidOperationException(System.SR.BlockingCollection_CantTakeWhenDone);
		}
		return item;
	}

	public bool TryTake([MaybeNullWhen(false)] out T item)
	{
		return TryTake(out item, 0, CancellationToken.None);
	}

	public bool TryTake([MaybeNullWhen(false)] out T item, TimeSpan timeout)
	{
		ValidateTimeout(timeout);
		return TryTakeWithNoTimeValidation(out item, (int)timeout.TotalMilliseconds, CancellationToken.None, null);
	}

	public bool TryTake([MaybeNullWhen(false)] out T item, int millisecondsTimeout)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryTakeWithNoTimeValidation(out item, millisecondsTimeout, CancellationToken.None, null);
	}

	public bool TryTake([MaybeNullWhen(false)] out T item, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryTakeWithNoTimeValidation(out item, millisecondsTimeout, cancellationToken, null);
	}

	private bool TryTakeWithNoTimeValidation([MaybeNullWhen(false)] out T item, int millisecondsTimeout, CancellationToken cancellationToken, CancellationTokenSource combinedTokenSource)
	{
		CheckDisposed();
		item = default(T);
		cancellationToken.ThrowIfCancellationRequested();
		if (IsCompleted)
		{
			return false;
		}
		bool flag = false;
		CancellationTokenSource cancellationTokenSource = combinedTokenSource;
		try
		{
			flag = _occupiedNodes.Wait(0);
			if (!flag && millisecondsTimeout != 0)
			{
				if (cancellationTokenSource == null)
				{
					cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumersCancellationTokenSource.Token);
				}
				flag = _occupiedNodes.Wait(millisecondsTimeout, cancellationTokenSource.Token);
			}
		}
		catch (OperationCanceledException)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return false;
		}
		finally
		{
			if (cancellationTokenSource != null && combinedTokenSource == null)
			{
				cancellationTokenSource.Dispose();
			}
		}
		if (flag)
		{
			bool flag2 = false;
			bool flag3 = true;
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				flag2 = _collection.TryTake(out item);
				flag3 = false;
				if (!flag2)
				{
					throw new InvalidOperationException(System.SR.BlockingCollection_Take_CollectionModified);
				}
			}
			finally
			{
				if (flag2)
				{
					if (_freeNodes != null)
					{
						_freeNodes.Release();
					}
				}
				else if (flag3)
				{
					_occupiedNodes.Release();
				}
				if (IsCompleted)
				{
					CancelWaitingConsumers();
				}
			}
		}
		return flag;
	}

	public static int AddToAny(BlockingCollection<T>[] collections, T item)
	{
		return TryAddToAny(collections, item, -1, CancellationToken.None);
	}

	public static int AddToAny(BlockingCollection<T>[] collections, T item, CancellationToken cancellationToken)
	{
		return TryAddToAny(collections, item, -1, cancellationToken);
	}

	public static int TryAddToAny(BlockingCollection<T>[] collections, T item)
	{
		return TryAddToAny(collections, item, 0, CancellationToken.None);
	}

	public static int TryAddToAny(BlockingCollection<T>[] collections, T item, TimeSpan timeout)
	{
		ValidateTimeout(timeout);
		return TryAddToAnyCore(collections, item, (int)timeout.TotalMilliseconds, CancellationToken.None);
	}

	public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryAddToAnyCore(collections, item, millisecondsTimeout, CancellationToken.None);
	}

	public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryAddToAnyCore(collections, item, millisecondsTimeout, cancellationToken);
	}

	private static int TryAddToAnyCore(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken externalCancellationToken)
	{
		ValidateCollectionsArray(collections, isAddOperation: true);
		int num = millisecondsTimeout;
		uint startTime = 0u;
		if (millisecondsTimeout != -1)
		{
			startTime = (uint)Environment.TickCount;
		}
		int num2 = TryAddToAnyFast(collections, item);
		if (num2 > -1)
		{
			return num2;
		}
		CancellationToken[] cancellationTokens;
		List<WaitHandle> handles = GetHandles(collections, externalCancellationToken, isAddOperation: true, out cancellationTokens);
		while (millisecondsTimeout == -1 || num >= 0)
		{
			num2 = -1;
			using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens))
			{
				handles.Add(cancellationTokenSource.Token.WaitHandle);
				num2 = WaitHandle.WaitAny(handles.ToArray(), num);
				handles.RemoveAt(handles.Count - 1);
				if (cancellationTokenSource.IsCancellationRequested)
				{
					externalCancellationToken.ThrowIfCancellationRequested();
					throw new ArgumentException(System.SR.BlockingCollection_CantAddAnyWhenCompleted, "collections");
				}
			}
			if (num2 == 258)
			{
				return -1;
			}
			if (collections[num2].TryAdd(item))
			{
				return num2;
			}
			if (millisecondsTimeout != -1)
			{
				num = UpdateTimeOut(startTime, millisecondsTimeout);
			}
		}
		return -1;
	}

	private static int TryAddToAnyFast(BlockingCollection<T>[] collections, T item)
	{
		for (int i = 0; i < collections.Length; i++)
		{
			if (collections[i]._freeNodes == null)
			{
				collections[i].TryAdd(item);
				return i;
			}
		}
		return -1;
	}

	private static List<WaitHandle> GetHandles(BlockingCollection<T>[] collections, CancellationToken externalCancellationToken, bool isAddOperation, out CancellationToken[] cancellationTokens)
	{
		List<WaitHandle> list = new List<WaitHandle>(collections.Length + 1);
		List<CancellationToken> list2 = new List<CancellationToken>(collections.Length + 1);
		list2.Add(externalCancellationToken);
		if (isAddOperation)
		{
			foreach (BlockingCollection<T> blockingCollection in collections)
			{
				if (blockingCollection._freeNodes != null)
				{
					list.Add(blockingCollection._freeNodes.AvailableWaitHandle);
					list2.Add(blockingCollection._producersCancellationTokenSource.Token);
				}
			}
		}
		else
		{
			for (int j = 0; j < collections.Length; j++)
			{
				if (!collections[j].IsCompleted)
				{
					list.Add(collections[j]._occupiedNodes.AvailableWaitHandle);
					list2.Add(collections[j]._consumersCancellationTokenSource.Token);
				}
			}
		}
		cancellationTokens = list2.ToArray();
		return list;
	}

	private static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
	{
		if (originalWaitMillisecondsTimeout == 0)
		{
			return 0;
		}
		uint num = (uint)Environment.TickCount - startTime;
		if (num > int.MaxValue)
		{
			return 0;
		}
		int num2 = originalWaitMillisecondsTimeout - (int)num;
		if (num2 <= 0)
		{
			return 0;
		}
		return num2;
	}

	public static int TakeFromAny(BlockingCollection<T>[] collections, out T? item)
	{
		return TakeFromAny(collections, out item, CancellationToken.None);
	}

	public static int TakeFromAny(BlockingCollection<T>[] collections, out T? item, CancellationToken cancellationToken)
	{
		return TryTakeFromAnyCore(collections, out item, -1, isTakeOperation: true, cancellationToken);
	}

	public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T? item)
	{
		return TryTakeFromAny(collections, out item, 0);
	}

	public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T? item, TimeSpan timeout)
	{
		ValidateTimeout(timeout);
		return TryTakeFromAnyCore(collections, out item, (int)timeout.TotalMilliseconds, isTakeOperation: false, CancellationToken.None);
	}

	public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T? item, int millisecondsTimeout)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryTakeFromAnyCore(collections, out item, millisecondsTimeout, isTakeOperation: false, CancellationToken.None);
	}

	public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T? item, int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ValidateMillisecondsTimeout(millisecondsTimeout);
		return TryTakeFromAnyCore(collections, out item, millisecondsTimeout, isTakeOperation: false, cancellationToken);
	}

	private static int TryTakeFromAnyCore(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, bool isTakeOperation, CancellationToken externalCancellationToken)
	{
		ValidateCollectionsArray(collections, isAddOperation: false);
		for (int i = 0; i < collections.Length; i++)
		{
			if (!collections[i].IsCompleted && collections[i]._occupiedNodes.CurrentCount > 0 && collections[i].TryTake(out item))
			{
				return i;
			}
		}
		return TryTakeFromAnyCoreSlow(collections, out item, millisecondsTimeout, isTakeOperation, externalCancellationToken);
	}

	private static int TryTakeFromAnyCoreSlow(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, bool isTakeOperation, CancellationToken externalCancellationToken)
	{
		int num = millisecondsTimeout;
		uint startTime = 0u;
		if (millisecondsTimeout != -1)
		{
			startTime = (uint)Environment.TickCount;
		}
		while (millisecondsTimeout == -1 || num >= 0)
		{
			CancellationToken[] cancellationTokens;
			List<WaitHandle> handles = GetHandles(collections, externalCancellationToken, isAddOperation: false, out cancellationTokens);
			if (handles.Count == 0 && isTakeOperation)
			{
				throw new ArgumentException(System.SR.BlockingCollection_CantTakeAnyWhenAllDone, "collections");
			}
			if (handles.Count == 0)
			{
				break;
			}
			using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens))
			{
				handles.Add(cancellationTokenSource.Token.WaitHandle);
				int num2 = WaitHandle.WaitAny(handles.ToArray(), num);
				if (cancellationTokenSource.IsCancellationRequested)
				{
					externalCancellationToken.ThrowIfCancellationRequested();
				}
				if (!cancellationTokenSource.IsCancellationRequested)
				{
					if (num2 == 258)
					{
						break;
					}
					if (collections.Length != handles.Count - 1)
					{
						for (int i = 0; i < collections.Length; i++)
						{
							if (collections[i]._occupiedNodes.AvailableWaitHandle == handles[num2])
							{
								num2 = i;
								break;
							}
						}
					}
					if (collections[num2].TryTake(out item))
					{
						return num2;
					}
				}
			}
			if (millisecondsTimeout != -1)
			{
				num = UpdateTimeOut(startTime, millisecondsTimeout);
			}
		}
		item = default(T);
		return -1;
	}

	public void CompleteAdding()
	{
		CheckDisposed();
		if (IsAddingCompleted)
		{
			return;
		}
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int currentAdders = _currentAdders;
			if (((uint)currentAdders & 0x80000000u) != 0)
			{
				spinWait.Reset();
				while (_currentAdders != int.MinValue)
				{
					spinWait.SpinOnce();
				}
				return;
			}
			if (Interlocked.CompareExchange(ref _currentAdders, currentAdders | int.MinValue, currentAdders) == currentAdders)
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
		spinWait.Reset();
		while (_currentAdders != int.MinValue)
		{
			spinWait.SpinOnce();
		}
		if (Count == 0)
		{
			CancelWaitingConsumers();
		}
		CancelWaitingProducers();
	}

	private void CancelWaitingConsumers()
	{
		_consumersCancellationTokenSource.Cancel();
	}

	private void CancelWaitingProducers()
	{
		_producersCancellationTokenSource.Cancel();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (_freeNodes != null)
			{
				_freeNodes.Dispose();
			}
			_occupiedNodes.Dispose();
			_isDisposed = true;
		}
	}

	public T[] ToArray()
	{
		CheckDisposed();
		return _collection.ToArray();
	}

	public void CopyTo(T[] array, int index)
	{
		((ICollection)this).CopyTo((Array)array, index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CheckDisposed();
		T[] array2 = _collection.ToArray();
		try
		{
			Array.Copy(array2, 0, array, index, array2.Length);
		}
		catch (ArgumentNullException)
		{
			throw new ArgumentNullException("array");
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.BlockingCollection_CopyTo_NonNegative);
		}
		catch (ArgumentException)
		{
			throw new ArgumentException(System.SR.Collection_CopyTo_TooManyElems, "index");
		}
		catch (RankException)
		{
			throw new ArgumentException(System.SR.BlockingCollection_CopyTo_MultiDim, "array");
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException(System.SR.BlockingCollection_CopyTo_IncorrectType, "array");
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(System.SR.BlockingCollection_CopyTo_IncorrectType, "array");
		}
	}

	public IEnumerable<T> GetConsumingEnumerable()
	{
		return GetConsumingEnumerable(CancellationToken.None);
	}

	public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
	{
		CancellationTokenSource linkedTokenSource = null;
		try
		{
			linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumersCancellationTokenSource.Token);
			while (!IsCompleted)
			{
				if (TryTakeWithNoTimeValidation(out var item, -1, cancellationToken, linkedTokenSource))
				{
					yield return item;
				}
			}
		}
		finally
		{
			linkedTokenSource?.Dispose();
		}
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		CheckDisposed();
		return _collection.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<T>)this).GetEnumerator();
	}

	private static void ValidateCollectionsArray(BlockingCollection<T>[] collections, bool isAddOperation)
	{
		if (collections == null)
		{
			throw new ArgumentNullException("collections");
		}
		if (collections.Length < 1)
		{
			throw new ArgumentException(System.SR.BlockingCollection_ValidateCollectionsArray_ZeroSize, "collections");
		}
		if (collections.Length > 63 || (collections.Length == 63 && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA))
		{
			throw new ArgumentOutOfRangeException("collections", System.SR.BlockingCollection_ValidateCollectionsArray_LargeSize);
		}
		for (int i = 0; i < collections.Length; i++)
		{
			if (collections[i] == null)
			{
				throw new ArgumentException(System.SR.BlockingCollection_ValidateCollectionsArray_NullElems, "collections");
			}
			if (collections[i]._isDisposed)
			{
				throw new ObjectDisposedException("collections", System.SR.BlockingCollection_ValidateCollectionsArray_DispElems);
			}
			if (isAddOperation && collections[i].IsAddingCompleted)
			{
				throw new ArgumentException(System.SR.BlockingCollection_CantAddAnyWhenCompleted, "collections");
			}
		}
	}

	private static void ValidateTimeout(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		if ((num < 0 || num > int.MaxValue) && num != -1)
		{
			throw new ArgumentOutOfRangeException("timeout", timeout, System.SR.Format(CultureInfo.InvariantCulture, System.SR.BlockingCollection_TimeoutInvalid, int.MaxValue));
		}
	}

	private static void ValidateMillisecondsTimeout(int millisecondsTimeout)
	{
		if (millisecondsTimeout < 0 && millisecondsTimeout != -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, System.SR.Format(CultureInfo.InvariantCulture, System.SR.BlockingCollection_TimeoutInvalid, int.MaxValue));
		}
	}

	private void CheckDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException("BlockingCollection", System.SR.BlockingCollection_Disposed);
		}
	}
}
