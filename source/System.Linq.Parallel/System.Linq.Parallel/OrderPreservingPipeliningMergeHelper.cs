using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class OrderPreservingPipeliningMergeHelper<TOutput, TKey> : IMergeHelper<TOutput>
{
	private sealed class ProducerComparer : IComparer<Producer<TKey>>
	{
		private readonly IComparer<TKey> _keyComparer;

		internal ProducerComparer(IComparer<TKey> keyComparer)
		{
			_keyComparer = keyComparer;
		}

		public int Compare(Producer<TKey> x, Producer<TKey> y)
		{
			return _keyComparer.Compare(y.MaxKey, x.MaxKey);
		}
	}

	private sealed class OrderedPipeliningMergeEnumerator : MergeEnumerator<TOutput>
	{
		private readonly OrderPreservingPipeliningMergeHelper<TOutput, TKey> _mergeHelper;

		private readonly FixedMaxHeap<Producer<TKey>> _producerHeap;

		private readonly TOutput[] _producerNextElement;

		private readonly Queue<Pair<TKey, TOutput>>[] _privateBuffer;

		private bool _initialized;

		public override TOutput Current
		{
			get
			{
				int producerIndex = _producerHeap.MaxValue.ProducerIndex;
				return _producerNextElement[producerIndex];
			}
		}

		internal OrderedPipeliningMergeEnumerator(OrderPreservingPipeliningMergeHelper<TOutput, TKey> mergeHelper, IComparer<Producer<TKey>> producerComparer)
			: base(mergeHelper._taskGroupState)
		{
			int partitionCount = mergeHelper._partitions.PartitionCount;
			_mergeHelper = mergeHelper;
			_producerHeap = new FixedMaxHeap<Producer<TKey>>(partitionCount, producerComparer);
			_privateBuffer = new Queue<Pair<TKey, TOutput>>[partitionCount];
			_producerNextElement = new TOutput[partitionCount];
		}

		public override bool MoveNext()
		{
			if (!_initialized)
			{
				_initialized = true;
				for (int i = 0; i < _mergeHelper._partitions.PartitionCount; i++)
				{
					Pair<TKey, TOutput> element = default(Pair<TKey, TOutput>);
					if (TryWaitForElement(i, ref element))
					{
						_producerHeap.Insert(new Producer<TKey>(element.First, i));
						_producerNextElement[i] = element.Second;
					}
					else
					{
						ThrowIfInTearDown();
					}
				}
			}
			else
			{
				if (_producerHeap.Count == 0)
				{
					return false;
				}
				int producerIndex = _producerHeap.MaxValue.ProducerIndex;
				Pair<TKey, TOutput> element2 = default(Pair<TKey, TOutput>);
				if (TryGetPrivateElement(producerIndex, ref element2) || TryWaitForElement(producerIndex, ref element2))
				{
					_producerHeap.ReplaceMax(new Producer<TKey>(element2.First, producerIndex));
					_producerNextElement[producerIndex] = element2.Second;
				}
				else
				{
					ThrowIfInTearDown();
					_producerHeap.RemoveMax();
				}
			}
			return _producerHeap.Count > 0;
		}

		private void ThrowIfInTearDown()
		{
			if (!_mergeHelper._taskGroupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
			{
				return;
			}
			try
			{
				object[] bufferLocks = _mergeHelper._bufferLocks;
				for (int i = 0; i < bufferLocks.Length; i++)
				{
					lock (bufferLocks[i])
					{
						Monitor.Pulse(bufferLocks[i]);
					}
				}
				_taskGroupState.QueryEnd(userInitiatedDispose: false);
			}
			finally
			{
				_producerHeap.Clear();
			}
		}

		private bool TryWaitForElement(int producer, ref Pair<TKey, TOutput> element)
		{
			Queue<Pair<TKey, TOutput>> queue = _mergeHelper._buffers[producer];
			object obj = _mergeHelper._bufferLocks[producer];
			lock (obj)
			{
				if (queue.Count == 0)
				{
					if (_mergeHelper._producerDone[producer])
					{
						element = default(Pair<TKey, TOutput>);
						return false;
					}
					_mergeHelper._consumerWaiting[producer] = true;
					Monitor.Wait(obj);
					if (queue.Count == 0)
					{
						element = default(Pair<TKey, TOutput>);
						return false;
					}
				}
				if (_mergeHelper._producerWaiting[producer])
				{
					Monitor.Pulse(obj);
					_mergeHelper._producerWaiting[producer] = false;
				}
				if (queue.Count < 1024)
				{
					element = queue.Dequeue();
					return true;
				}
				_privateBuffer[producer] = _mergeHelper._buffers[producer];
				_mergeHelper._buffers[producer] = new Queue<Pair<TKey, TOutput>>(128);
			}
			bool flag = TryGetPrivateElement(producer, ref element);
			return true;
		}

		private bool TryGetPrivateElement(int producer, ref Pair<TKey, TOutput> element)
		{
			Queue<Pair<TKey, TOutput>> queue = _privateBuffer[producer];
			if (queue != null)
			{
				if (queue.Count > 0)
				{
					element = queue.Dequeue();
					return true;
				}
				_privateBuffer[producer] = null;
			}
			return false;
		}

		public override void Dispose()
		{
			int num = _mergeHelper._buffers.Length;
			for (int i = 0; i < num; i++)
			{
				object obj = _mergeHelper._bufferLocks[i];
				lock (obj)
				{
					if (_mergeHelper._producerWaiting[i])
					{
						Monitor.Pulse(obj);
					}
				}
			}
			base.Dispose();
		}
	}

	private readonly QueryTaskGroupState _taskGroupState;

	private readonly PartitionedStream<TOutput, TKey> _partitions;

	private readonly TaskScheduler _taskScheduler;

	private readonly bool _autoBuffered;

	private readonly Queue<Pair<TKey, TOutput>>[] _buffers;

	private readonly bool[] _producerDone;

	private readonly bool[] _producerWaiting;

	private readonly bool[] _consumerWaiting;

	private readonly object[] _bufferLocks;

	private readonly IComparer<Producer<TKey>> _producerComparer;

	internal OrderPreservingPipeliningMergeHelper(PartitionedStream<TOutput, TKey> partitions, TaskScheduler taskScheduler, CancellationState cancellationState, bool autoBuffered, int queryId, IComparer<TKey> keyComparer)
	{
		_taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
		_partitions = partitions;
		_taskScheduler = taskScheduler;
		_autoBuffered = autoBuffered;
		int partitionCount = _partitions.PartitionCount;
		_buffers = new Queue<Pair<TKey, TOutput>>[partitionCount];
		_producerDone = new bool[partitionCount];
		_consumerWaiting = new bool[partitionCount];
		_producerWaiting = new bool[partitionCount];
		_bufferLocks = new object[partitionCount];
		if (keyComparer == Util.GetDefaultComparer<int>())
		{
			_producerComparer = (IComparer<Producer<TKey>>)(object)ProducerComparerInt.Instance;
		}
		else
		{
			_producerComparer = new ProducerComparer(keyComparer);
		}
	}

	void IMergeHelper<TOutput>.Execute()
	{
		OrderPreservingPipeliningSpoolingTask<TOutput, TKey>.Spool(_taskGroupState, _partitions, _consumerWaiting, _producerWaiting, _producerDone, _buffers, _bufferLocks, _taskScheduler, _autoBuffered);
	}

	IEnumerator<TOutput> IMergeHelper<TOutput>.GetEnumerator()
	{
		return new OrderedPipeliningMergeEnumerator(this, _producerComparer);
	}

	[ExcludeFromCodeCoverage(Justification = "An ordered pipelining merge is not intended to be used this way")]
	public TOutput[] GetResultsAsArray()
	{
		throw new InvalidOperationException();
	}
}
