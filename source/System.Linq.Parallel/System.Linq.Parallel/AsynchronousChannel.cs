using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class AsynchronousChannel<T> : IDisposable
{
	private readonly T[][] _buffer;

	private readonly int _index;

	private volatile int _producerBufferIndex;

	private volatile int _consumerBufferIndex;

	private volatile bool _done;

	private T[] _producerChunk;

	private int _producerChunkIndex;

	private T[] _consumerChunk;

	private int _consumerChunkIndex;

	private readonly int _chunkSize;

	private ManualResetEventSlim _producerEvent;

	private IntValueEvent _consumerEvent;

	private volatile int _producerIsWaiting;

	private volatile int _consumerIsWaiting;

	private readonly CancellationToken _cancellationToken;

	internal bool IsFull
	{
		get
		{
			int producerBufferIndex = _producerBufferIndex;
			int consumerBufferIndex = _consumerBufferIndex;
			if (producerBufferIndex != consumerBufferIndex - 1)
			{
				if (consumerBufferIndex == 0)
				{
					return producerBufferIndex == _buffer.Length - 1;
				}
				return false;
			}
			return true;
		}
	}

	internal bool IsChunkBufferEmpty => _producerBufferIndex == _consumerBufferIndex;

	internal bool IsDone => _done;

	internal AsynchronousChannel(int index, int chunkSize, CancellationToken cancellationToken, IntValueEvent consumerEvent)
		: this(index, 512, chunkSize, cancellationToken, consumerEvent)
	{
	}

	internal AsynchronousChannel(int index, int capacity, int chunkSize, CancellationToken cancellationToken, IntValueEvent consumerEvent)
	{
		if (chunkSize == 0)
		{
			chunkSize = Scheduling.GetDefaultChunkSize<T>();
		}
		_index = index;
		_buffer = new T[capacity + 1][];
		_producerBufferIndex = 0;
		_consumerBufferIndex = 0;
		_producerEvent = new ManualResetEventSlim();
		_consumerEvent = consumerEvent;
		_chunkSize = chunkSize;
		_producerChunk = new T[chunkSize];
		_producerChunkIndex = 0;
		_cancellationToken = cancellationToken;
	}

	internal void FlushBuffers()
	{
		FlushCachedChunk();
	}

	internal void SetDone()
	{
		_done = true;
		lock (this)
		{
			if (_consumerEvent != null)
			{
				_consumerEvent.Set(_index);
			}
		}
	}

	internal void Enqueue(T item)
	{
		int producerChunkIndex = _producerChunkIndex;
		_producerChunk[producerChunkIndex] = item;
		if (producerChunkIndex == _chunkSize - 1)
		{
			EnqueueChunk(_producerChunk);
			_producerChunk = new T[_chunkSize];
		}
		_producerChunkIndex = (producerChunkIndex + 1) % _chunkSize;
	}

	private void EnqueueChunk(T[] chunk)
	{
		if (IsFull)
		{
			WaitUntilNonFull();
		}
		int producerBufferIndex = _producerBufferIndex;
		_buffer[producerBufferIndex] = chunk;
		Interlocked.Exchange(ref _producerBufferIndex, (producerBufferIndex + 1) % _buffer.Length);
		if (_consumerIsWaiting == 1 && !IsChunkBufferEmpty)
		{
			_consumerIsWaiting = 0;
			_consumerEvent.Set(_index);
		}
	}

	private void WaitUntilNonFull()
	{
		do
		{
			_producerEvent.Reset();
			Interlocked.Exchange(ref _producerIsWaiting, 1);
			if (IsFull)
			{
				_producerEvent.Wait(_cancellationToken);
			}
			else
			{
				_producerIsWaiting = 0;
			}
		}
		while (IsFull);
	}

	private void FlushCachedChunk()
	{
		if (_producerChunk != null && _producerChunkIndex != 0)
		{
			T[] array = new T[_producerChunkIndex];
			Array.Copy(_producerChunk, array, _producerChunkIndex);
			EnqueueChunk(array);
			_producerChunk = null;
		}
	}

	internal bool TryDequeue([MaybeNullWhen(false)][AllowNull] ref T item)
	{
		if (_consumerChunk == null)
		{
			if (!TryDequeueChunk(ref _consumerChunk))
			{
				return false;
			}
			_consumerChunkIndex = 0;
		}
		item = _consumerChunk[_consumerChunkIndex];
		_consumerChunkIndex++;
		if (_consumerChunkIndex == _consumerChunk.Length)
		{
			_consumerChunk = null;
		}
		return true;
	}

	private bool TryDequeueChunk([NotNullWhen(true)] ref T[] chunk)
	{
		if (IsChunkBufferEmpty)
		{
			return false;
		}
		chunk = InternalDequeueChunk();
		return true;
	}

	internal bool TryDequeue([MaybeNullWhen(false)][AllowNull] ref T item, ref bool isDone)
	{
		isDone = false;
		if (_consumerChunk == null)
		{
			if (!TryDequeueChunk(ref _consumerChunk, ref isDone))
			{
				return false;
			}
			_consumerChunkIndex = 0;
		}
		item = _consumerChunk[_consumerChunkIndex];
		_consumerChunkIndex++;
		if (_consumerChunkIndex == _consumerChunk.Length)
		{
			_consumerChunk = null;
		}
		return true;
	}

	private bool TryDequeueChunk([NotNullWhen(true)] ref T[] chunk, ref bool isDone)
	{
		isDone = false;
		while (IsChunkBufferEmpty)
		{
			if (IsDone && IsChunkBufferEmpty)
			{
				isDone = true;
				return false;
			}
			Interlocked.Exchange(ref _consumerIsWaiting, 1);
			if (IsChunkBufferEmpty && !IsDone)
			{
				return false;
			}
			_consumerIsWaiting = 0;
		}
		chunk = InternalDequeueChunk();
		return true;
	}

	private T[] InternalDequeueChunk()
	{
		int consumerBufferIndex = _consumerBufferIndex;
		T[] result = _buffer[consumerBufferIndex];
		_buffer[consumerBufferIndex] = null;
		Interlocked.Exchange(ref _consumerBufferIndex, (consumerBufferIndex + 1) % _buffer.Length);
		if (_producerIsWaiting == 1 && !IsFull)
		{
			_producerIsWaiting = 0;
			_producerEvent.Set();
		}
		return result;
	}

	internal void DoneWithDequeueWait()
	{
		_consumerIsWaiting = 0;
	}

	public void Dispose()
	{
		lock (this)
		{
			_producerEvent.Dispose();
			_producerEvent = null;
			_consumerEvent = null;
		}
	}
}
