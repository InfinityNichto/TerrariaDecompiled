using System.Collections.Immutable;

namespace System.Reflection.Internal;

internal sealed class ByteArrayMemoryBlock : AbstractMemoryBlock
{
	private ByteArrayMemoryProvider _provider;

	private readonly int _start;

	private readonly int _size;

	public unsafe override byte* Pointer => _provider.Pointer + _start;

	public override int Size => _size;

	internal ByteArrayMemoryBlock(ByteArrayMemoryProvider provider, int start, int size)
	{
		_provider = provider;
		_size = size;
		_start = start;
	}

	public override void Dispose()
	{
		_provider = null;
	}

	public override ImmutableArray<byte> GetContentUnchecked(int start, int length)
	{
		return ImmutableArray.Create(_provider.Array, _start + start, length);
	}
}
