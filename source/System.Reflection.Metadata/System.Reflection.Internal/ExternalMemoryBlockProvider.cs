using System.IO;

namespace System.Reflection.Internal;

internal sealed class ExternalMemoryBlockProvider : MemoryBlockProvider
{
	private unsafe byte* _memory;

	private int _size;

	public override int Size => _size;

	public unsafe byte* Pointer => _memory;

	public unsafe ExternalMemoryBlockProvider(byte* memory, int size)
	{
		_memory = memory;
		_size = size;
	}

	protected unsafe override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
	{
		return new ExternalMemoryBlock(this, _memory + start, size);
	}

	public unsafe override Stream GetStream(out StreamConstraints constraints)
	{
		constraints = new StreamConstraints(null, 0L, _size);
		return new ReadOnlyUnmanagedMemoryStream(_memory, _size);
	}

	protected unsafe override void Dispose(bool disposing)
	{
		_memory = null;
		_size = 0;
	}
}
