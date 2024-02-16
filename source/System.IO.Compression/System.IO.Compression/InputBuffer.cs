namespace System.IO.Compression;

internal sealed class InputBuffer
{
	private Memory<byte> _buffer;

	private uint _bitBuffer;

	private int _bitsInBuffer;

	public int AvailableBits => _bitsInBuffer;

	public int AvailableBytes => _buffer.Length + _bitsInBuffer / 8;

	public bool EnsureBitsAvailable(int count)
	{
		if (_bitsInBuffer < count)
		{
			if (NeedsInput())
			{
				return false;
			}
			_bitBuffer |= (uint)(_buffer.Span[0] << _bitsInBuffer);
			_buffer = _buffer.Slice(1);
			_bitsInBuffer += 8;
			if (_bitsInBuffer < count)
			{
				if (NeedsInput())
				{
					return false;
				}
				_bitBuffer |= (uint)(_buffer.Span[0] << _bitsInBuffer);
				_buffer = _buffer.Slice(1);
				_bitsInBuffer += 8;
			}
		}
		return true;
	}

	public uint TryLoad16Bits()
	{
		if (_bitsInBuffer < 8)
		{
			if (_buffer.Length > 1)
			{
				Span<byte> span = _buffer.Span;
				_bitBuffer |= (uint)(span[0] << _bitsInBuffer);
				_bitBuffer |= (uint)(span[1] << _bitsInBuffer + 8);
				_buffer = _buffer.Slice(2);
				_bitsInBuffer += 16;
			}
			else if (_buffer.Length != 0)
			{
				_bitBuffer |= (uint)(_buffer.Span[0] << _bitsInBuffer);
				_buffer = _buffer.Slice(1);
				_bitsInBuffer += 8;
			}
		}
		else if (_bitsInBuffer < 16 && !_buffer.IsEmpty)
		{
			_bitBuffer |= (uint)(_buffer.Span[0] << _bitsInBuffer);
			_buffer = _buffer.Slice(1);
			_bitsInBuffer += 8;
		}
		return _bitBuffer;
	}

	private uint GetBitMask(int count)
	{
		return (uint)((1 << count) - 1);
	}

	public int GetBits(int count)
	{
		if (!EnsureBitsAvailable(count))
		{
			return -1;
		}
		int result = (int)(_bitBuffer & GetBitMask(count));
		_bitBuffer >>= count;
		_bitsInBuffer -= count;
		return result;
	}

	public int CopyTo(Memory<byte> output)
	{
		int num = 0;
		while (_bitsInBuffer > 0 && !output.IsEmpty)
		{
			output.Span[0] = (byte)_bitBuffer;
			output = output.Slice(1);
			_bitBuffer >>= 8;
			_bitsInBuffer -= 8;
			num++;
		}
		if (output.IsEmpty)
		{
			return num;
		}
		int num2 = Math.Min(output.Length, _buffer.Length);
		_buffer.Slice(0, num2).CopyTo(output);
		_buffer = _buffer.Slice(num2);
		return num + num2;
	}

	public int CopyTo(byte[] output, int offset, int length)
	{
		return CopyTo(output.AsMemory(offset, length));
	}

	public bool NeedsInput()
	{
		return _buffer.IsEmpty;
	}

	public void SetInput(Memory<byte> buffer)
	{
		if (_buffer.IsEmpty)
		{
			_buffer = buffer;
		}
	}

	public void SetInput(byte[] buffer, int offset, int length)
	{
		SetInput(buffer.AsMemory(offset, length));
	}

	public void SkipBits(int n)
	{
		_bitBuffer >>= n;
		_bitsInBuffer -= n;
	}

	public void SkipToByteBoundary()
	{
		_bitBuffer >>= _bitsInBuffer % 8;
		_bitsInBuffer -= _bitsInBuffer % 8;
	}
}
