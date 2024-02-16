using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks;

[DebuggerDisplay("Count = {Count}")]
internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>, IEnumerable<T>, IEnumerable
{
	bool IProducerConsumerQueue<T>.IsEmpty => base.IsEmpty;

	int IProducerConsumerQueue<T>.Count => base.Count;

	void IProducerConsumerQueue<T>.Enqueue(T item)
	{
		Enqueue(item);
	}

	bool IProducerConsumerQueue<T>.TryDequeue([MaybeNullWhen(false)] out T result)
	{
		return TryDequeue(out result);
	}
}
