using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

public readonly struct PEMemoryBlock
{
	private readonly AbstractMemoryBlock _block;

	private readonly int _offset;

	public unsafe byte* Pointer
	{
		get
		{
			if (_block == null)
			{
				return null;
			}
			return _block.Pointer + _offset;
		}
	}

	public int Length => (_block?.Size - _offset).GetValueOrDefault();

	internal PEMemoryBlock(AbstractMemoryBlock block, int offset = 0)
	{
		_block = block;
		_offset = offset;
	}

	public unsafe BlobReader GetReader()
	{
		return new BlobReader(Pointer, Length);
	}

	public unsafe BlobReader GetReader(int start, int length)
	{
		BlobUtilities.ValidateRange(Length, start, length, "length");
		return new BlobReader(Pointer + start, length);
	}

	public ImmutableArray<byte> GetContent()
	{
		return _block?.GetContentUnchecked(_offset, Length) ?? ImmutableArray<byte>.Empty;
	}

	public ImmutableArray<byte> GetContent(int start, int length)
	{
		BlobUtilities.ValidateRange(Length, start, length, "length");
		return _block?.GetContentUnchecked(_offset + start, length) ?? ImmutableArray<byte>.Empty;
	}
}
