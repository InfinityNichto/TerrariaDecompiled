using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class OrderPreservingPipeliningSpoolingTask<TOutput, TKey> : SpoolingTaskBase
{
	private readonly QueryTaskGroupState _taskGroupState;

	private readonly QueryOperatorEnumerator<TOutput, TKey> _partition;

	private readonly bool[] _consumerWaiting;

	private readonly bool[] _producerWaiting;

	private readonly bool[] _producerDone;

	private readonly int _partitionIndex;

	private readonly Queue<Pair<TKey, TOutput>>[] _buffers;

	private readonly object _bufferLock;

	private readonly bool _autoBuffered;

	internal OrderPreservingPipeliningSpoolingTask(QueryOperatorEnumerator<TOutput, TKey> partition, QueryTaskGroupState taskGroupState, bool[] consumerWaiting, bool[] producerWaiting, bool[] producerDone, int partitionIndex, Queue<Pair<TKey, TOutput>>[] buffers, object bufferLock, bool autoBuffered)
		: base(partitionIndex, taskGroupState)
	{
		_partition = partition;
		_taskGroupState = taskGroupState;
		_producerDone = producerDone;
		_consumerWaiting = consumerWaiting;
		_producerWaiting = producerWaiting;
		_partitionIndex = partitionIndex;
		_buffers = buffers;
		_bufferLock = bufferLock;
		_autoBuffered = autoBuffered;
	}

	protected override void SpoolingWork()
	{
		TOutput currentElement = default(TOutput);
		TKey currentKey = default(TKey);
		int num = ((!_autoBuffered) ? 1 : 16);
		Pair<TKey, TOutput>[] array = new Pair<TKey, TOutput>[num];
		QueryOperatorEnumerator<TOutput, TKey> partition = _partition;
		CancellationToken mergedCancellationToken = _taskGroupState.CancellationState.MergedCancellationToken;
		int i;
		do
		{
			for (i = 0; i < num; i++)
			{
				if (!partition.MoveNext(ref currentElement, ref currentKey))
				{
					break;
				}
				array[i] = new Pair<TKey, TOutput>(currentKey, currentElement);
			}
			if (i == 0)
			{
				break;
			}
			lock (_bufferLock)
			{
				if (mergedCancellationToken.IsCancellationRequested)
				{
					break;
				}
				for (int j = 0; j < i; j++)
				{
					_buffers[_partitionIndex].Enqueue(array[j]);
				}
				if (_consumerWaiting[_partitionIndex])
				{
					Monitor.Pulse(_bufferLock);
					_consumerWaiting[_partitionIndex] = false;
				}
				if (_buffers[_partitionIndex].Count >= 8192)
				{
					_producerWaiting[_partitionIndex] = true;
					Monitor.Wait(_bufferLock);
				}
			}
		}
		while (i == num);
	}

	public static void Spool(QueryTaskGroupState groupState, PartitionedStream<TOutput, TKey> partitions, bool[] consumerWaiting, bool[] producerWaiting, bool[] producerDone, Queue<Pair<TKey, TOutput>>[] buffers, object[] bufferLocks, TaskScheduler taskScheduler, bool autoBuffered)
	{
		int degreeOfParallelism = partitions.PartitionCount;
		for (int i = 0; i < degreeOfParallelism; i++)
		{
			buffers[i] = new Queue<Pair<TKey, TOutput>>(128);
			bufferLocks[i] = new object();
		}
		Task task = new Task(delegate
		{
			for (int j = 0; j < degreeOfParallelism; j++)
			{
				QueryTask queryTask = new OrderPreservingPipeliningSpoolingTask<TOutput, TKey>(partitions[j], groupState, consumerWaiting, producerWaiting, producerDone, j, buffers, bufferLocks[j], autoBuffered);
				queryTask.RunAsynchronously(taskScheduler);
			}
		});
		groupState.QueryBegin(task);
		task.Start(taskScheduler);
	}

	protected override void SpoolingFinally()
	{
		lock (_bufferLock)
		{
			_producerDone[_partitionIndex] = true;
			if (_consumerWaiting[_partitionIndex])
			{
				Monitor.Pulse(_bufferLock);
				_consumerWaiting[_partitionIndex] = false;
			}
		}
		base.SpoolingFinally();
		_partition.Dispose();
	}
}
