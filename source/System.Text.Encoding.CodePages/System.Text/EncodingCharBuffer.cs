namespace System.Text;

internal sealed class EncodingCharBuffer
{
	private unsafe char* _chars;

	private unsafe readonly char* _charStart;

	private unsafe readonly char* _charEnd;

	private int _charCountResult;

	private readonly EncodingNLS _enc;

	private readonly System.Text.DecoderNLS _decoder;

	private unsafe readonly byte* _byteStart;

	private unsafe readonly byte* _byteEnd;

	private unsafe byte* _bytes;

	private readonly DecoderFallbackBuffer _fallbackBuffer;

	private DecoderFallbackBufferHelper _fallbackBufferHelper;

	internal unsafe bool MoreData => _bytes < _byteEnd;

	internal unsafe int BytesUsed => (int)(_bytes - _byteStart);

	internal int Count => _charCountResult;

	internal unsafe EncodingCharBuffer(EncodingNLS enc, System.Text.DecoderNLS decoder, char* charStart, int charCount, byte* byteStart, int byteCount)
	{
		_enc = enc;
		_decoder = decoder;
		_chars = charStart;
		_charStart = charStart;
		_charEnd = charStart + charCount;
		_byteStart = byteStart;
		_bytes = byteStart;
		_byteEnd = byteStart + byteCount;
		if (_decoder == null)
		{
			_fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
		}
		else
		{
			_fallbackBuffer = _decoder.FallbackBuffer;
		}
		_fallbackBufferHelper = new DecoderFallbackBufferHelper(_fallbackBuffer);
		_fallbackBufferHelper.InternalInitialize(_bytes, _charEnd);
	}

	internal unsafe bool AddChar(char ch, int numBytes)
	{
		if (_chars != null)
		{
			if (_chars >= _charEnd)
			{
				_bytes -= numBytes;
				_enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);
				return false;
			}
			*(_chars++) = ch;
		}
		_charCountResult++;
		return true;
	}

	internal bool AddChar(char ch)
	{
		return AddChar(ch, 1);
	}

	internal unsafe bool AddChar(char ch1, char ch2, int numBytes)
	{
		if (_chars >= _charEnd - 1)
		{
			_bytes -= numBytes;
			_enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);
			return false;
		}
		if (AddChar(ch1, numBytes))
		{
			return AddChar(ch2, numBytes);
		}
		return false;
	}

	internal unsafe void AdjustBytes(int count)
	{
		_bytes += count;
	}

	internal unsafe bool EvenMoreData(int count)
	{
		return _bytes <= _byteEnd - count;
	}

	internal unsafe byte GetNextByte()
	{
		if (_bytes >= _byteEnd)
		{
			return 0;
		}
		return *(_bytes++);
	}

	internal bool Fallback(byte fallbackByte)
	{
		byte[] byteBuffer = new byte[1] { fallbackByte };
		return Fallback(byteBuffer);
	}

	internal bool Fallback(byte byte1, byte byte2)
	{
		byte[] byteBuffer = new byte[2] { byte1, byte2 };
		return Fallback(byteBuffer);
	}

	internal bool Fallback(byte byte1, byte byte2, byte byte3, byte byte4)
	{
		byte[] byteBuffer = new byte[4] { byte1, byte2, byte3, byte4 };
		return Fallback(byteBuffer);
	}

	internal unsafe bool Fallback(byte[] byteBuffer)
	{
		if (_chars != null)
		{
			char* chars = _chars;
			if (!_fallbackBufferHelper.InternalFallback(byteBuffer, _bytes, ref _chars))
			{
				_bytes -= byteBuffer.Length;
				_fallbackBufferHelper.InternalReset();
				_enc.ThrowCharsOverflow(_decoder, _chars == _charStart);
				return false;
			}
			_charCountResult += (int)(_chars - chars);
		}
		else
		{
			_charCountResult += _fallbackBufferHelper.InternalFallback(byteBuffer, _bytes);
		}
		return true;
	}
}
