using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class ListQueryResults<T> : QueryResults<T>
{
	private readonly IList<T> _source;

	private readonly int _partitionCount;

	private readonly bool _useStriping;

	internal override bool IsIndexible => true;

	internal override int ElementsCount => _source.Count;

	internal ListQueryResults(IList<T> source, int partitionCount, bool useStriping)
	{
		_source = source;
		_partitionCount = partitionCount;
		_useStriping = useStriping;
	}

	internal override void GivePartitionedStream(IPartitionedStreamRecipient<T> recipient)
	{
		PartitionedStream<T, int> partitionedStream = GetPartitionedStream();
		recipient.Receive(partitionedStream);
	}

	internal override T GetElement(int index)
	{
		return _source[index];
	}

	internal PartitionedStream<T, int> GetPartitionedStream()
	{
		return ExchangeUtilities.PartitionDataSource(_source, _partitionCount, _useStriping);
	}
}
