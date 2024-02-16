using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class PartitionedStream<TElement, TKey>
{
	protected QueryOperatorEnumerator<TElement, TKey>[] _partitions;

	private readonly IComparer<TKey> _keyComparer;

	private readonly OrdinalIndexState _indexState;

	internal QueryOperatorEnumerator<TElement, TKey> this[int index]
	{
		get
		{
			return _partitions[index];
		}
		set
		{
			_partitions[index] = value;
		}
	}

	public int PartitionCount => _partitions.Length;

	internal IComparer<TKey> KeyComparer => _keyComparer;

	internal OrdinalIndexState OrdinalIndexState => _indexState;

	internal PartitionedStream(int partitionCount, IComparer<TKey> keyComparer, OrdinalIndexState indexState)
	{
		_partitions = new QueryOperatorEnumerator<TElement, TKey>[partitionCount];
		_keyComparer = keyComparer;
		_indexState = indexState;
	}
}
