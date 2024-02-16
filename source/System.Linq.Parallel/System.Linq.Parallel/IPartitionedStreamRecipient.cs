namespace System.Linq.Parallel;

internal interface IPartitionedStreamRecipient<TElement>
{
	void Receive<TKey>(PartitionedStream<TElement, TKey> partitionedStream);
}
