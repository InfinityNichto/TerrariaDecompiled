namespace System.Buffers;

internal class PointerMemoryManager<T> : MemoryManager<T> where T : struct
{
	private unsafe readonly void* _pointer;

	private readonly int _length;

	internal unsafe PointerMemoryManager(void* pointer, int length)
	{
		_pointer = pointer;
		_length = length;
	}

	protected override void Dispose(bool disposing)
	{
	}

	public unsafe override Span<T> GetSpan()
	{
		return new Span<T>(_pointer, _length);
	}

	public override MemoryHandle Pin(int elementIndex = 0)
	{
		throw new NotSupportedException();
	}

	public override void Unpin()
	{
	}
}
