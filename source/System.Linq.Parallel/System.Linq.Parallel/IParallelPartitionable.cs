namespace System.Linq.Parallel;

internal interface IParallelPartitionable<T>
{
	QueryOperatorEnumerator<T, int>[] GetPartitions(int partitionCount);
}
