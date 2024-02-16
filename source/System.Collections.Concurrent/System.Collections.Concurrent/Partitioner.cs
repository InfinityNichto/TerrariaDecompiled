using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent;

public abstract class Partitioner<TSource>
{
	public virtual bool SupportsDynamicPartitions => false;

	public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);

	public virtual IEnumerable<TSource> GetDynamicPartitions()
	{
		throw new NotSupportedException(System.SR.Partitioner_DynamicPartitionsNotSupported);
	}
}
public static class Partitioner
{
	private abstract class DynamicPartitionEnumerator_Abstract<TSource, TSourceReader> : IEnumerator<KeyValuePair<long, TSource>>, IDisposable, IEnumerator
	{
		protected readonly TSourceReader _sharedReader;

		protected static int s_defaultMaxChunkSize = GetDefaultChunkSize<TSource>();

		protected StrongBox<int> _currentChunkSize;

		protected StrongBox<int> _localOffset;

		private int _doublingCountdown;

		protected readonly int _maxChunkSize;

		protected readonly SharedLong _sharedIndex;

		protected abstract bool HasNoElementsLeft { get; }

		public abstract KeyValuePair<long, TSource> Current { get; }

		object IEnumerator.Current => Current;

		protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
			: this(sharedReader, sharedIndex, useSingleChunking: false)
		{
		}

		protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex, bool useSingleChunking)
		{
			_sharedReader = sharedReader;
			_sharedIndex = sharedIndex;
			_maxChunkSize = (useSingleChunking ? 1 : s_defaultMaxChunkSize);
		}

		protected abstract bool GrabNextChunk(int requestedChunkSize);

		public abstract void Dispose();

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public bool MoveNext()
		{
			if (_localOffset == null)
			{
				_localOffset = new StrongBox<int>(-1);
				_currentChunkSize = new StrongBox<int>(0);
				_doublingCountdown = 3;
			}
			if (_localOffset.Value < _currentChunkSize.Value - 1)
			{
				_localOffset.Value++;
				return true;
			}
			int requestedChunkSize;
			if (_currentChunkSize.Value == 0)
			{
				requestedChunkSize = 1;
			}
			else if (_doublingCountdown > 0)
			{
				requestedChunkSize = _currentChunkSize.Value;
			}
			else
			{
				requestedChunkSize = Math.Min(_currentChunkSize.Value * 2, _maxChunkSize);
				_doublingCountdown = 3;
			}
			_doublingCountdown--;
			if (GrabNextChunk(requestedChunkSize))
			{
				_localOffset.Value = 0;
				return true;
			}
			return false;
		}
	}

	private sealed class DynamicPartitionerForIEnumerable<TSource> : OrderablePartitioner<TSource>
	{
		private sealed class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IEnumerable, IDisposable
		{
			private readonly IEnumerator<TSource> _sharedReader;

			private readonly SharedLong _sharedIndex;

			private volatile KeyValuePair<long, TSource>[] _fillBuffer;

			private volatile int _fillBufferSize;

			private volatile int _fillBufferCurrentPosition;

			private volatile int _activeCopiers;

			private readonly SharedBool _hasNoElementsLeft;

			private readonly SharedBool _sourceDepleted;

			private readonly object _sharedLock;

			private bool _disposed;

			private readonly SharedInt _activePartitionCount;

			private readonly bool _useSingleChunking;

			internal InternalPartitionEnumerable(IEnumerator<TSource> sharedReader, bool useSingleChunking, bool isStaticPartitioning)
			{
				_sharedReader = sharedReader;
				_sharedIndex = new SharedLong(-1L);
				_hasNoElementsLeft = new SharedBool(value: false);
				_sourceDepleted = new SharedBool(value: false);
				_sharedLock = new object();
				_useSingleChunking = useSingleChunking;
				if (!_useSingleChunking)
				{
					_fillBuffer = new KeyValuePair<long, TSource>[((Environment.ProcessorCount <= 4) ? 1 : 4) * GetDefaultChunkSize<TSource>()];
				}
				if (isStaticPartitioning)
				{
					_activePartitionCount = new SharedInt(0);
				}
				else
				{
					_activePartitionCount = null;
				}
			}

			public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(System.SR.PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed);
				}
				return new InternalPartitionEnumerator(_sharedReader, _sharedIndex, _hasNoElementsLeft, _activePartitionCount, this, _useSingleChunking);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			private void TryCopyFromFillBuffer(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
			{
				actualNumElementsGrabbed = 0;
				KeyValuePair<long, TSource>[] fillBuffer = _fillBuffer;
				if (fillBuffer != null && _fillBufferCurrentPosition < _fillBufferSize)
				{
					Interlocked.Increment(ref _activeCopiers);
					int num = Interlocked.Add(ref _fillBufferCurrentPosition, requestedChunkSize);
					int num2 = num - requestedChunkSize;
					if (num2 < _fillBufferSize)
					{
						actualNumElementsGrabbed = ((num < _fillBufferSize) ? num : (_fillBufferSize - num2));
						Array.Copy(fillBuffer, num2, destArray, 0, actualNumElementsGrabbed);
					}
					Interlocked.Decrement(ref _activeCopiers);
				}
			}

			internal bool GrabChunk(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
			{
				actualNumElementsGrabbed = 0;
				if (_hasNoElementsLeft.Value)
				{
					return false;
				}
				if (_useSingleChunking)
				{
					return GrabChunk_Single(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
				}
				return GrabChunk_Buffered(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
			}

			internal bool GrabChunk_Single(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
			{
				lock (_sharedLock)
				{
					if (_hasNoElementsLeft.Value)
					{
						return false;
					}
					try
					{
						if (_sharedReader.MoveNext())
						{
							_sharedIndex.Value = checked(_sharedIndex.Value + 1);
							destArray[0] = new KeyValuePair<long, TSource>(_sharedIndex.Value, _sharedReader.Current);
							actualNumElementsGrabbed = 1;
							return true;
						}
						_sourceDepleted.Value = true;
						_hasNoElementsLeft.Value = true;
						return false;
					}
					catch
					{
						_sourceDepleted.Value = true;
						_hasNoElementsLeft.Value = true;
						throw;
					}
				}
			}

			internal bool GrabChunk_Buffered(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
			{
				TryCopyFromFillBuffer(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
				if (actualNumElementsGrabbed == requestedChunkSize)
				{
					return true;
				}
				if (_sourceDepleted.Value)
				{
					_hasNoElementsLeft.Value = true;
					_fillBuffer = null;
					return actualNumElementsGrabbed > 0;
				}
				lock (_sharedLock)
				{
					if (_sourceDepleted.Value)
					{
						return actualNumElementsGrabbed > 0;
					}
					try
					{
						if (_activeCopiers > 0)
						{
							SpinWait spinWait = default(SpinWait);
							while (_activeCopiers > 0)
							{
								spinWait.SpinOnce();
							}
						}
						while (actualNumElementsGrabbed < requestedChunkSize)
						{
							if (_sharedReader.MoveNext())
							{
								_sharedIndex.Value = checked(_sharedIndex.Value + 1);
								destArray[actualNumElementsGrabbed] = new KeyValuePair<long, TSource>(_sharedIndex.Value, _sharedReader.Current);
								actualNumElementsGrabbed++;
								continue;
							}
							_sourceDepleted.Value = true;
							break;
						}
						KeyValuePair<long, TSource>[] fillBuffer = _fillBuffer;
						if (!_sourceDepleted.Value && fillBuffer != null && _fillBufferCurrentPosition >= fillBuffer.Length)
						{
							for (int i = 0; i < fillBuffer.Length; i++)
							{
								if (_sharedReader.MoveNext())
								{
									_sharedIndex.Value = checked(_sharedIndex.Value + 1);
									fillBuffer[i] = new KeyValuePair<long, TSource>(_sharedIndex.Value, _sharedReader.Current);
									continue;
								}
								_sourceDepleted.Value = true;
								_fillBufferSize = i;
								break;
							}
							_fillBufferCurrentPosition = 0;
						}
					}
					catch
					{
						_sourceDepleted.Value = true;
						_hasNoElementsLeft.Value = true;
						throw;
					}
				}
				return actualNumElementsGrabbed > 0;
			}

			public void Dispose()
			{
				if (!_disposed)
				{
					_disposed = true;
					_sharedReader.Dispose();
				}
			}
		}

		private sealed class InternalPartitionEnumerator : DynamicPartitionEnumerator_Abstract<TSource, IEnumerator<TSource>>
		{
			private KeyValuePair<long, TSource>[] _localList;

			private readonly SharedBool _hasNoElementsLeft;

			private readonly SharedInt _activePartitionCount;

			private readonly InternalPartitionEnumerable _enumerable;

			protected override bool HasNoElementsLeft => _hasNoElementsLeft.Value;

			public override KeyValuePair<long, TSource> Current
			{
				get
				{
					if (_currentChunkSize == null)
					{
						throw new InvalidOperationException(System.SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
					}
					return _localList[_localOffset.Value];
				}
			}

			internal InternalPartitionEnumerator(IEnumerator<TSource> sharedReader, SharedLong sharedIndex, SharedBool hasNoElementsLeft, SharedInt activePartitionCount, InternalPartitionEnumerable enumerable, bool useSingleChunking)
				: base(sharedReader, sharedIndex, useSingleChunking)
			{
				_hasNoElementsLeft = hasNoElementsLeft;
				_enumerable = enumerable;
				_activePartitionCount = activePartitionCount;
				if (_activePartitionCount != null)
				{
					Interlocked.Increment(ref _activePartitionCount.Value);
				}
			}

			protected override bool GrabNextChunk(int requestedChunkSize)
			{
				if (HasNoElementsLeft)
				{
					return false;
				}
				if (_localList == null)
				{
					_localList = new KeyValuePair<long, TSource>[_maxChunkSize];
				}
				return _enumerable.GrabChunk(_localList, requestedChunkSize, ref _currentChunkSize.Value);
			}

			public override void Dispose()
			{
				if (_activePartitionCount != null && Interlocked.Decrement(ref _activePartitionCount.Value) == 0)
				{
					_enumerable.Dispose();
				}
			}
		}

		private readonly IEnumerable<TSource> _source;

		private readonly bool _useSingleChunking;

		public override bool SupportsDynamicPartitions => true;

		internal DynamicPartitionerForIEnumerable(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
			: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: false, keysNormalized: true)
		{
			_source = source;
			_useSingleChunking = (partitionerOptions & EnumerablePartitionerOptions.NoBuffering) != 0;
		}

		public override IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
		{
			if (partitionCount <= 0)
			{
				throw new ArgumentOutOfRangeException("partitionCount");
			}
			IEnumerator<KeyValuePair<long, TSource>>[] array = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
			IEnumerable<KeyValuePair<long, TSource>> enumerable = new InternalPartitionEnumerable(_source.GetEnumerator(), _useSingleChunking, isStaticPartitioning: true);
			for (int i = 0; i < partitionCount; i++)
			{
				array[i] = enumerable.GetEnumerator();
			}
			return array;
		}

		public override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
		{
			return new InternalPartitionEnumerable(_source.GetEnumerator(), _useSingleChunking, isStaticPartitioning: false);
		}
	}

	private abstract class DynamicPartitionerForIndexRange_Abstract<TSource, TCollection> : OrderablePartitioner<TSource>
	{
		private readonly TCollection _data;

		public override bool SupportsDynamicPartitions => true;

		protected DynamicPartitionerForIndexRange_Abstract(TCollection data)
			: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: false, keysNormalized: true)
		{
			_data = data;
		}

		protected abstract IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TCollection data);

		public override IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
		{
			if (partitionCount <= 0)
			{
				throw new ArgumentOutOfRangeException("partitionCount");
			}
			IEnumerator<KeyValuePair<long, TSource>>[] array = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
			IEnumerable<KeyValuePair<long, TSource>> orderableDynamicPartitions_Factory = GetOrderableDynamicPartitions_Factory(_data);
			for (int i = 0; i < partitionCount; i++)
			{
				array[i] = orderableDynamicPartitions_Factory.GetEnumerator();
			}
			return array;
		}

		public override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
		{
			return GetOrderableDynamicPartitions_Factory(_data);
		}
	}

	private abstract class DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSourceReader> : DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>
	{
		protected int _startIndex;

		protected abstract int SourceCount { get; }

		protected override bool HasNoElementsLeft => Volatile.Read(ref _sharedIndex.Value) >= SourceCount - 1;

		protected DynamicPartitionEnumeratorForIndexRange_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
			: base(sharedReader, sharedIndex)
		{
		}

		protected override bool GrabNextChunk(int requestedChunkSize)
		{
			while (!HasNoElementsLeft)
			{
				long num = Volatile.Read(ref _sharedIndex.Value);
				if (HasNoElementsLeft)
				{
					return false;
				}
				long num2 = Math.Min(SourceCount - 1, num + requestedChunkSize);
				if (Interlocked.CompareExchange(ref _sharedIndex.Value, num2, num) == num)
				{
					_currentChunkSize.Value = (int)(num2 - num);
					_localOffset.Value = -1;
					_startIndex = (int)(num + 1);
					return true;
				}
			}
			return false;
		}

		public override void Dispose()
		{
		}
	}

	private sealed class DynamicPartitionerForIList<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, IList<TSource>>
	{
		private sealed class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IEnumerable
		{
			private readonly IList<TSource> _sharedReader;

			private readonly SharedLong _sharedIndex;

			internal InternalPartitionEnumerable(IList<TSource> sharedReader)
			{
				_sharedReader = sharedReader;
				_sharedIndex = new SharedLong(-1L);
			}

			public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
			{
				return new InternalPartitionEnumerator(_sharedReader, _sharedIndex);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private sealed class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, IList<TSource>>
		{
			protected override int SourceCount => _sharedReader.Count;

			public override KeyValuePair<long, TSource> Current
			{
				get
				{
					if (_currentChunkSize == null)
					{
						throw new InvalidOperationException(System.SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
					}
					return new KeyValuePair<long, TSource>(_startIndex + _localOffset.Value, _sharedReader[_startIndex + _localOffset.Value]);
				}
			}

			internal InternalPartitionEnumerator(IList<TSource> sharedReader, SharedLong sharedIndex)
				: base(sharedReader, sharedIndex)
			{
			}
		}

		internal DynamicPartitionerForIList(IList<TSource> source)
			: base(source)
		{
		}

		protected override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(IList<TSource> _data)
		{
			return new InternalPartitionEnumerable(_data);
		}
	}

	private sealed class DynamicPartitionerForArray<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, TSource[]>
	{
		private sealed class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IEnumerable
		{
			private readonly TSource[] _sharedReader;

			private readonly SharedLong _sharedIndex;

			internal InternalPartitionEnumerable(TSource[] sharedReader)
			{
				_sharedReader = sharedReader;
				_sharedIndex = new SharedLong(-1L);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
			{
				return new InternalPartitionEnumerator(_sharedReader, _sharedIndex);
			}
		}

		private sealed class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSource[]>
		{
			protected override int SourceCount => _sharedReader.Length;

			public override KeyValuePair<long, TSource> Current
			{
				get
				{
					if (_currentChunkSize == null)
					{
						throw new InvalidOperationException(System.SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
					}
					return new KeyValuePair<long, TSource>(_startIndex + _localOffset.Value, _sharedReader[_startIndex + _localOffset.Value]);
				}
			}

			internal InternalPartitionEnumerator(TSource[] sharedReader, SharedLong sharedIndex)
				: base(sharedReader, sharedIndex)
			{
			}
		}

		internal DynamicPartitionerForArray(TSource[] source)
			: base(source)
		{
		}

		protected override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TSource[] _data)
		{
			return new InternalPartitionEnumerable(_data);
		}
	}

	private abstract class StaticIndexRangePartitioner<TSource, TCollection> : OrderablePartitioner<TSource>
	{
		protected abstract int SourceCount { get; }

		protected StaticIndexRangePartitioner()
			: base(keysOrderedInEachPartition: true, keysOrderedAcrossPartitions: true, keysNormalized: true)
		{
		}

		protected abstract IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex);

		public override IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
		{
			if (partitionCount <= 0)
			{
				throw new ArgumentOutOfRangeException("partitionCount");
			}
			int num = SourceCount / partitionCount;
			int num2 = SourceCount % partitionCount;
			IEnumerator<KeyValuePair<long, TSource>>[] array = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
			int num3 = -1;
			for (int i = 0; i < partitionCount; i++)
			{
				int num4 = num3 + 1;
				num3 = ((i >= num2) ? (num4 + num - 1) : (num4 + num));
				array[i] = CreatePartition(num4, num3);
			}
			return array;
		}
	}

	private abstract class StaticIndexRangePartition<TSource> : IEnumerator<KeyValuePair<long, TSource>>, IDisposable, IEnumerator
	{
		protected readonly int _startIndex;

		protected readonly int _endIndex;

		protected volatile int _offset;

		public abstract KeyValuePair<long, TSource> Current { get; }

		object IEnumerator.Current => Current;

		protected StaticIndexRangePartition(int startIndex, int endIndex)
		{
			_startIndex = startIndex;
			_endIndex = endIndex;
			_offset = startIndex - 1;
		}

		public void Dispose()
		{
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}

		public bool MoveNext()
		{
			if (_offset < _endIndex)
			{
				_offset++;
				return true;
			}
			_offset = _endIndex + 1;
			return false;
		}
	}

	private sealed class StaticIndexRangePartitionerForIList<TSource> : StaticIndexRangePartitioner<TSource, IList<TSource>>
	{
		private readonly IList<TSource> _list;

		protected override int SourceCount => _list.Count;

		internal StaticIndexRangePartitionerForIList(IList<TSource> list)
		{
			_list = list;
		}

		protected override IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
		{
			return new StaticIndexRangePartitionForIList<TSource>(_list, startIndex, endIndex);
		}
	}

	private sealed class StaticIndexRangePartitionForIList<TSource> : StaticIndexRangePartition<TSource>
	{
		private readonly IList<TSource> _list;

		public override KeyValuePair<long, TSource> Current
		{
			get
			{
				if (_offset < _startIndex)
				{
					throw new InvalidOperationException(System.SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
				}
				return new KeyValuePair<long, TSource>(_offset, _list[_offset]);
			}
		}

		internal StaticIndexRangePartitionForIList(IList<TSource> list, int startIndex, int endIndex)
			: base(startIndex, endIndex)
		{
			_list = list;
		}
	}

	private sealed class StaticIndexRangePartitionerForArray<TSource> : StaticIndexRangePartitioner<TSource, TSource[]>
	{
		private readonly TSource[] _array;

		protected override int SourceCount => _array.Length;

		internal StaticIndexRangePartitionerForArray(TSource[] array)
		{
			_array = array;
		}

		protected override IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
		{
			return new StaticIndexRangePartitionForArray<TSource>(_array, startIndex, endIndex);
		}
	}

	private sealed class StaticIndexRangePartitionForArray<TSource> : StaticIndexRangePartition<TSource>
	{
		private readonly TSource[] _array;

		public override KeyValuePair<long, TSource> Current
		{
			get
			{
				if (_offset < _startIndex)
				{
					throw new InvalidOperationException(System.SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
				}
				return new KeyValuePair<long, TSource>(_offset, _array[_offset]);
			}
		}

		internal StaticIndexRangePartitionForArray(TSource[] array, int startIndex, int endIndex)
			: base(startIndex, endIndex)
		{
			_array = array;
		}
	}

	private sealed class SharedInt
	{
		internal volatile int Value;

		internal SharedInt(int value)
		{
			Value = value;
		}
	}

	private sealed class SharedBool
	{
		internal volatile bool Value;

		internal SharedBool(bool value)
		{
			Value = value;
		}
	}

	private sealed class SharedLong
	{
		internal long Value;

		internal SharedLong(long value)
		{
			Value = value;
		}
	}

	public static OrderablePartitioner<TSource> Create<TSource>(IList<TSource> list, bool loadBalance)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		if (loadBalance)
		{
			return new DynamicPartitionerForIList<TSource>(list);
		}
		return new StaticIndexRangePartitionerForIList<TSource>(list);
	}

	public static OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (loadBalance)
		{
			return new DynamicPartitionerForArray<TSource>(array);
		}
		return new StaticIndexRangePartitionerForArray<TSource>(array);
	}

	public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source)
	{
		return Create(source, EnumerablePartitionerOptions.None);
	}

	public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (((uint)partitionerOptions & 0xFFFFFFFEu) != 0)
		{
			throw new ArgumentOutOfRangeException("partitionerOptions");
		}
		return new DynamicPartitionerForIEnumerable<TSource>(source, partitionerOptions);
	}

	public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive)
	{
		if (toExclusive <= fromInclusive)
		{
			throw new ArgumentOutOfRangeException("toExclusive");
		}
		decimal num = (decimal)toExclusive - (decimal)fromInclusive;
		long num2 = (long)(num / (decimal)(Environment.ProcessorCount * 3));
		if (num2 == 0L)
		{
			num2 = 1L;
		}
		return Create(CreateRanges(fromInclusive, toExclusive, num2), EnumerablePartitionerOptions.NoBuffering);
	}

	public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize)
	{
		if (toExclusive <= fromInclusive)
		{
			throw new ArgumentOutOfRangeException("toExclusive");
		}
		if (rangeSize <= 0)
		{
			throw new ArgumentOutOfRangeException("rangeSize");
		}
		return Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
	}

	private static IEnumerable<Tuple<long, long>> CreateRanges(long fromInclusive, long toExclusive, long rangeSize)
	{
		bool shouldQuit = false;
		for (long i = fromInclusive; i < toExclusive; i += rangeSize)
		{
			if (shouldQuit)
			{
				break;
			}
			long item = i;
			long num;
			try
			{
				num = checked(i + rangeSize);
			}
			catch (OverflowException)
			{
				num = toExclusive;
				shouldQuit = true;
			}
			if (num > toExclusive)
			{
				num = toExclusive;
			}
			yield return new Tuple<long, long>(item, num);
		}
	}

	public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive)
	{
		if (toExclusive <= fromInclusive)
		{
			throw new ArgumentOutOfRangeException("toExclusive");
		}
		long num = (long)toExclusive - (long)fromInclusive;
		int num2 = (int)(num / (Environment.ProcessorCount * 3));
		if (num2 == 0)
		{
			num2 = 1;
		}
		return Create(CreateRanges(fromInclusive, toExclusive, num2), EnumerablePartitionerOptions.NoBuffering);
	}

	public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize)
	{
		if (toExclusive <= fromInclusive)
		{
			throw new ArgumentOutOfRangeException("toExclusive");
		}
		if (rangeSize <= 0)
		{
			throw new ArgumentOutOfRangeException("rangeSize");
		}
		return Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
	}

	private static IEnumerable<Tuple<int, int>> CreateRanges(int fromInclusive, int toExclusive, int rangeSize)
	{
		bool shouldQuit = false;
		for (int i = fromInclusive; i < toExclusive; i += rangeSize)
		{
			if (shouldQuit)
			{
				break;
			}
			int item = i;
			int num;
			try
			{
				num = checked(i + rangeSize);
			}
			catch (OverflowException)
			{
				num = toExclusive;
				shouldQuit = true;
			}
			if (num > toExclusive)
			{
				num = toExclusive;
			}
			yield return new Tuple<int, int>(item, num);
		}
	}

	private static int GetDefaultChunkSize<TSource>()
	{
		if (typeof(TSource).IsValueType)
		{
			return 128;
		}
		return 512 / IntPtr.Size;
	}
}
