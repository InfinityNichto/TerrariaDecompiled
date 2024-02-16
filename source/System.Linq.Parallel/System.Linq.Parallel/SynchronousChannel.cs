using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class SynchronousChannel<T>
{
	private Queue<T> _queue;

	internal int Count => _queue.Count;

	internal SynchronousChannel()
	{
	}

	internal void Init()
	{
		_queue = new Queue<T>();
	}

	internal void Enqueue(T item)
	{
		_queue.Enqueue(item);
	}

	internal T Dequeue()
	{
		return _queue.Dequeue();
	}

	internal void SetDone()
	{
	}

	internal void CopyTo(T[] array, int arrayIndex)
	{
		_queue.CopyTo(array, arrayIndex);
	}
}
