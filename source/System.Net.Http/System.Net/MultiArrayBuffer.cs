using System.Buffers;

namespace System.Net;

internal struct MultiArrayBuffer : IDisposable
{
	private byte[][] _blocks;

	private uint _allocatedEnd;

	private uint _activeStart;

	private uint _availableStart;

	public bool IsEmpty => _activeStart == _availableStart;

	public System.Net.MultiMemory ActiveMemory => new System.Net.MultiMemory(_blocks, _activeStart, _availableStart - _activeStart);

	public System.Net.MultiMemory AvailableMemory => new System.Net.MultiMemory(_blocks, _availableStart, _allocatedEnd - _availableStart);

	public MultiArrayBuffer(int initialBufferSize)
	{
		this = default(System.Net.MultiArrayBuffer);
	}

	public void Dispose()
	{
		_activeStart = 0u;
		_availableStart = 0u;
		if (_blocks == null)
		{
			return;
		}
		for (int i = 0; i < _blocks.Length; i++)
		{
			byte[] array = _blocks[i];
			if (array != null)
			{
				_blocks[i] = null;
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		_blocks = null;
		_allocatedEnd = 0u;
	}

	public void Discard(int byteCount)
	{
		if (byteCount == ActiveMemory.Length)
		{
			DiscardAll();
			return;
		}
		uint startBlock = _activeStart / 16384;
		_activeStart += (uint)byteCount;
		uint endBlock = _activeStart / 16384;
		FreeBlocks(startBlock, endBlock);
	}

	public void DiscardAll()
	{
		uint startBlock = _activeStart / 16384;
		uint endBlock = _allocatedEnd / 16384;
		FreeBlocks(startBlock, endBlock);
		_activeStart = (_availableStart = (_allocatedEnd = 0u));
	}

	private void FreeBlocks(uint startBlock, uint endBlock)
	{
		byte[][] blocks = _blocks;
		for (uint num = startBlock; num < endBlock; num++)
		{
			byte[] array = blocks[num];
			blocks[num] = null;
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public void Commit(int byteCount)
	{
		_availableStart += (uint)byteCount;
	}

	public void EnsureAvailableSpace(int byteCount)
	{
		if (byteCount > AvailableMemory.Length)
		{
			GrowAvailableSpace(byteCount);
		}
	}

	public void GrowAvailableSpace(int byteCount)
	{
		uint num = (uint)(byteCount - AvailableMemory.Length);
		uint num2 = (num + 16384 - 1) / 16384;
		if (_blocks == null)
		{
			int num3;
			for (num3 = 4; num3 < num2; num3 *= 2)
			{
			}
			_blocks = new byte[num3][];
		}
		else
		{
			uint num4 = _allocatedEnd / 16384;
			uint num5 = (uint)_blocks.Length;
			if (num4 + num2 > num5)
			{
				uint num6 = _activeStart / 16384;
				uint num7 = num4 - num6;
				uint num8 = num7 + num2;
				Span<byte[]> span;
				if (num8 > num5)
				{
					while (num5 < num8)
					{
						num5 *= 2;
					}
					byte[][] array = new byte[num5][];
					span = _blocks.AsSpan();
					span = span.Slice((int)num6, (int)num7);
					span.CopyTo(array);
					_blocks = array;
				}
				else
				{
					span = _blocks.AsSpan();
					span = span.Slice((int)num6, (int)num7);
					span.CopyTo(_blocks);
					span = _blocks.AsSpan();
					span = span.Slice((int)num7, (int)num6);
					span.Clear();
				}
				uint num9 = num6 * 16384;
				_allocatedEnd -= num9;
				_activeStart -= num9;
				_availableStart -= num9;
			}
		}
		uint num10 = _allocatedEnd / 16384;
		for (uint num11 = 0u; num11 < num2; num11++)
		{
			_blocks[num10++] = ArrayPool<byte>.Shared.Rent(16384);
		}
		_allocatedEnd = num10 * 16384;
	}
}
