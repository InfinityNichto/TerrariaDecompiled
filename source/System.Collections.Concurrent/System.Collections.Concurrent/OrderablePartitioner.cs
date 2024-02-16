using System.Collections.Generic;

namespace System.Collections.Concurrent;

public abstract class OrderablePartitioner<TSource> : Partitioner<TSource>
{
	private sealed class EnumerableDropIndices : IEnumerable<TSource>, IEnumerable, IDisposable
	{
		private readonly IEnumerable<KeyValuePair<long, TSource>> _source;

		public EnumerableDropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
		{
			_source = source;
		}

		public IEnumerator<TSource> GetEnumerator()
		{
			return new EnumeratorDropIndices(_source.GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Dispose()
		{
			if (_source is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}

	private sealed class EnumeratorDropIndices : IEnumerator<TSource>, IDisposable, IEnumerator
	{
		private readonly IEnumerator<KeyValuePair<long, TSource>> _source;

		public TSource Current => _source.Current.Value;

		object IEnumerator.Current => Current;

		public EnumeratorDropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
		{
			_source = source;
		}

		public bool MoveNext()
		{
			return _source.MoveNext();
		}

		public void Dispose()
		{
			_source.Dispose();
		}

		public void Reset()
		{
			_source.Reset();
		}
	}

	public bool KeysOrderedInEachPartition { get; private set; }

	public bool KeysOrderedAcrossPartitions { get; private set; }

	public bool KeysNormalized { get; private set; }

	protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized)
	{
		KeysOrderedInEachPartition = keysOrderedInEachPartition;
		KeysOrderedAcrossPartitions = keysOrderedAcrossPartitions;
		KeysNormalized = keysNormalized;
	}

	public abstract IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);

	public virtual IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
	{
		throw new NotSupportedException(System.SR.Partitioner_DynamicPartitionsNotSupported);
	}

	public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
	{
		IList<IEnumerator<KeyValuePair<long, TSource>>> orderablePartitions = GetOrderablePartitions(partitionCount);
		if (orderablePartitions.Count != partitionCount)
		{
			throw new InvalidOperationException("OrderablePartitioner_GetPartitions_WrongNumberOfPartitions");
		}
		IEnumerator<TSource>[] array = new IEnumerator<TSource>[partitionCount];
		for (int i = 0; i < partitionCount; i++)
		{
			array[i] = new EnumeratorDropIndices(orderablePartitions[i]);
		}
		return array;
	}

	public override IEnumerable<TSource> GetDynamicPartitions()
	{
		IEnumerable<KeyValuePair<long, TSource>> orderableDynamicPartitions = GetOrderableDynamicPartitions();
		return new EnumerableDropIndices(orderableDynamicPartitions);
	}
}
