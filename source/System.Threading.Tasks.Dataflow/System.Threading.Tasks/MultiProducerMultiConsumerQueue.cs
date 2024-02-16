using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks;

[DebuggerDisplay("Count = {Count}")]
internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, System.Threading.Tasks.IProducerConsumerQueue<T>, IEnumerable<T>, IEnumerable
{
	bool System.Threading.Tasks.IProducerConsumerQueue<T>.IsEmpty => base.IsEmpty;

	int System.Threading.Tasks.IProducerConsumerQueue<T>.Count => base.Count;

	void System.Threading.Tasks.IProducerConsumerQueue<T>.Enqueue(T item)
	{
		Enqueue(item);
	}

	bool System.Threading.Tasks.IProducerConsumerQueue<T>.TryDequeue([MaybeNullWhen(false)] out T result)
	{
		return TryDequeue(out result);
	}

	int System.Threading.Tasks.IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
	{
		return base.Count;
	}
}
