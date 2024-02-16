namespace System.Buffers;

public sealed class ArrayBufferWriter<T> : IBufferWriter<T>
{
	private T[] _buffer;

	private int _index;

	public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

	public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

	public int WrittenCount => _index;

	public int Capacity => _buffer.Length;

	public int FreeCapacity => _buffer.Length - _index;

	public ArrayBufferWriter()
	{
		_buffer = Array.Empty<T>();
		_index = 0;
	}

	public ArrayBufferWriter(int initialCapacity)
	{
		if (initialCapacity <= 0)
		{
			throw new ArgumentException(null, "initialCapacity");
		}
		_buffer = new T[initialCapacity];
		_index = 0;
	}

	public void Clear()
	{
		_buffer.AsSpan(0, _index).Clear();
		_index = 0;
	}

	public void Advance(int count)
	{
		if (count < 0)
		{
			throw new ArgumentException(null, "count");
		}
		if (_index > _buffer.Length - count)
		{
			ThrowInvalidOperationException_AdvancedTooFar(_buffer.Length);
		}
		_index += count;
	}

	public Memory<T> GetMemory(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsMemory(_index);
	}

	public Span<T> GetSpan(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsSpan(_index);
	}

	private void CheckAndResizeBuffer(int sizeHint)
	{
		if (sizeHint < 0)
		{
			throw new ArgumentException("sizeHint");
		}
		if (sizeHint == 0)
		{
			sizeHint = 1;
		}
		if (sizeHint <= FreeCapacity)
		{
			return;
		}
		int num = _buffer.Length;
		int num2 = Math.Max(sizeHint, num);
		if (num == 0)
		{
			num2 = Math.Max(num2, 256);
		}
		int num3 = num + num2;
		if ((uint)num3 > 2147483647u)
		{
			uint num4 = (uint)(num - FreeCapacity + sizeHint);
			if (num4 > 2147483591)
			{
				ThrowOutOfMemoryException(num4);
			}
			num3 = 2147483591;
		}
		Array.Resize(ref _buffer, num3);
	}

	private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.BufferWriterAdvancedTooFar, capacity));
	}

	private static void ThrowOutOfMemoryException(uint capacity)
	{
		throw new OutOfMemoryException(System.SR.Format(System.SR.BufferMaximumSizeExceeded, capacity));
	}
}
