using System.Reflection;

namespace System.Net;

[DefaultMember("Item")]
internal readonly struct MultiMemory
{
	private readonly byte[][] _blocks;

	private readonly uint _start;

	private readonly uint _length;

	public int Length => (int)_length;

	public int BlockCount => (int)(GetBlockIndex(_start + _length + 16383) - GetBlockIndex(_start));

	internal MultiMemory(byte[][] blocks, uint start, uint length)
	{
		if (length == 0)
		{
			_blocks = null;
			_start = 0u;
			_length = 0u;
		}
		else
		{
			_blocks = blocks;
			_start = start;
			_length = length;
		}
	}

	private static uint GetBlockIndex(uint offset)
	{
		return offset / 16384;
	}

	private static uint GetOffsetInBlock(uint offset)
	{
		return offset % 16384;
	}

	public Memory<byte> GetBlock(int blockIndex)
	{
		if ((uint)blockIndex >= BlockCount)
		{
			throw new IndexOutOfRangeException();
		}
		uint num = ((blockIndex == 0) ? GetOffsetInBlock(_start) : 0u);
		uint num2 = ((blockIndex == BlockCount - 1) ? (GetOffsetInBlock(_start + _length - 1) + 1) : 16384u);
		return new Memory<byte>(_blocks[GetBlockIndex(_start) + blockIndex], (int)num, (int)(num2 - num));
	}

	public MultiMemory Slice(int start, int length)
	{
		if ((uint)start > _length || (uint)length > (uint)((int)_length - start))
		{
			throw new IndexOutOfRangeException();
		}
		return new MultiMemory(_blocks, _start + (uint)start, (uint)length);
	}

	public void CopyTo(Span<byte> destination)
	{
		if (destination.Length < _length)
		{
			throw new ArgumentOutOfRangeException("destination");
		}
		int blockCount = BlockCount;
		for (int i = 0; i < blockCount; i++)
		{
			Memory<byte> block = GetBlock(i);
			block.Span.CopyTo(destination);
			destination = destination.Slice(block.Length);
		}
	}

	public void CopyFrom(ReadOnlySpan<byte> source)
	{
		if (_length < source.Length)
		{
			throw new ArgumentOutOfRangeException("source");
		}
		int blockCount = BlockCount;
		for (int i = 0; i < blockCount; i++)
		{
			Memory<byte> block = GetBlock(i);
			if (source.Length <= block.Length)
			{
				source.CopyTo(block.Span);
				break;
			}
			source.Slice(0, block.Length).CopyTo(block.Span);
			source = source.Slice(block.Length);
		}
	}
}
